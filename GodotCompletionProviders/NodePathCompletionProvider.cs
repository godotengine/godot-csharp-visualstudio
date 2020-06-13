using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GodotCompletionProviders
{
    [ExportCompletionProvider(nameof(NodePathCompletionProvider), LanguageNames.CSharp)]
    public class NodePathCompletionProvider : BaseCompletionProvider
    {
        // TODO: If generic GetNode, filter by type

        public NodePathCompletionProvider() : base(CompletionKind.NodePaths, "NodePath")
        {
        }

        public override async Task<CheckResult> ShouldProvideCompletion(Document document, int position)
        {
            if (!document.SupportsSyntaxTree || !document.SupportsSemanticModel)
                return CheckResult.False();

            var syntaxRoot = await document.GetSyntaxRootAsync();

            if (syntaxRoot == null)
                return CheckResult.False();

            var semanticModel = await document.GetSemanticModelAsync();

            if (semanticModel == null)
                return CheckResult.False();

            // Walk up and save literal expression if present (we can autocomplete literals).
            var currentToken = syntaxRoot.FindToken(position - 1);
            var currentNode = currentToken.Parent;
            var literalExpression = RoslynUtils.WalkUpStringSyntaxOnce(ref currentNode, ref position);

            // Walk up parenthesis because the inference service doesn't handle that.
            currentToken = syntaxRoot.FindToken(position - 1);
            currentNode = currentToken.Parent;
            if (currentToken.Kind() != SyntaxKind.CloseParenToken)
                RoslynUtils.WalkUpParenthesisExpressions(ref currentNode, ref position);

            var inferredTypes = RoslynUtils.InferTypes(semanticModel, position, null, CancellationToken.None);

            if (inferredTypes.Any(RoslynUtils.TypeIsNodePath))
                return CheckResult.True(literalExpression);

            // Our own custom inference for NodePath

            if (IsPathConstructorArgumentOfNodePath(syntaxRoot, semanticModel, currentNode, position))
                return CheckResult.True(literalExpression);

            if (IsParenthesizedExprActuallyCastToNodePath(semanticModel, currentNode))
                return CheckResult.True(literalExpression);

            return CheckResult.False();
        }

        private static bool IsPathConstructorArgumentOfNodePath(SyntaxNode syntaxRoot, SemanticModel semanticModel, SyntaxNode currentNode, int position)
        {
            // new NodePath($$) for NodePath(string) ctor

            if (!(currentNode is ArgumentListSyntax argumentList && currentNode.Parent is ObjectCreationExpressionSyntax objectCreation))
                return false;

            var previousToken = syntaxRoot.FindToken(position - 1);

            if (previousToken != argumentList.OpenParenToken)
                return false;

            if (argumentList.Arguments.Count > 1)
                return false; // The NodePath constructor we are looking for has only one parameter

            int index = RoslynUtils.GetArgumentListIndex(argumentList, previousToken);

            var info = semanticModel.GetSymbolInfo(objectCreation.Type);

            if (!(info.Symbol is INamedTypeSymbol type))
                return false;

            if (type.TypeKind == TypeKind.Delegate)
                return false;

            if (!RoslynUtils.TypeIsNodePath(type))
                return false;

            var constructors = type.InstanceConstructors.Where(m => m.Parameters.Length == 1);
            var types = RoslynUtils.InferTypeInArgument(index, constructors.Select(m => m.Parameters), argumentOpt: null);

            return types.Any(RoslynUtils.TypeIsString);
        }

        private static bool IsParenthesizedExprActuallyCastToNodePath(SemanticModel semanticModel, SyntaxNode currentNode)
        {
            // (NodePath)$$ which is detected as a parenthesized expression rather than a cast

            if (!(currentNode is ParenthesizedExpressionSyntax parenthesizedExpression))
                return false;

            if (!(parenthesizedExpression.Expression is IdentifierNameSyntax identifierNameSyntax))
                return false;

            var typeInfo = semanticModel.GetTypeInfo(identifierNameSyntax).Type;

            return typeInfo != null && RoslynUtils.TypeIsNodePath(typeInfo);
        }
    }
}
