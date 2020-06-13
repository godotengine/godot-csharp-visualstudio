using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GodotCompletionProviders
{
    public abstract class SpecificInvocationCompletionProvider : BaseCompletionProvider
    {
        internal static readonly TypeName GodotObjectType = new TypeName {Namespace = "Godot", Name = "Object"};
        internal static readonly TypeName SceneTreeType = new TypeName {Namespace = "Godot", Name = "SceneTree"};
        internal static readonly TypeName GdType = new TypeName {Namespace = "Godot", Name = "GD"};
        internal static readonly TypeName ResourceLoaderType = new TypeName {Namespace = "Godot", Name = "ResourceLoader"};
        internal static readonly TypeName InputType = new TypeName {Namespace = "Godot", Name = "Input"};
        internal static readonly TypeName StringNameType = new TypeName {Namespace = "Godot", Name = "StringName"};
        internal static readonly TypeName StringType = new TypeName {Namespace = "System", Name = "String"};

        internal static readonly IEnumerable<TypeName> StringTypes = new[] {StringNameType, StringType};

        public struct TypeName
        {
            public string Namespace;
            public string Name;
        }

        public struct ExpectedInvocation
        {
            public TypeName MethodContainingType;
            public string MethodName;
            public int ArgumentIndex;
            public IEnumerable<TypeName> ArgumentTypes;
        }

        private readonly IEnumerable<ExpectedInvocation> _expectedInvocations;

        protected SpecificInvocationCompletionProvider(IEnumerable<ExpectedInvocation> expectedInvocations, CompletionKind kind, string inlineDescription) : base(kind, inlineDescription)
        {
            _expectedInvocations = expectedInvocations;
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

            if (!(currentNode is ArgumentListSyntax argumentList && currentNode.Parent is InvocationExpressionSyntax invocation))
                return CheckResult.False();

            var previousToken = syntaxRoot.FindToken(position - 1);

            if (previousToken != argumentList.OpenParenToken && previousToken.Kind() != SyntaxKind.CommaToken)
                return CheckResult.False();

            if (RoslynUtils.IsExpectedInvocationArgument(semanticModel, previousToken, invocation, argumentList, _expectedInvocations))
                return CheckResult.True(literalExpression);

            return CheckResult.False();
        }
    }
}
