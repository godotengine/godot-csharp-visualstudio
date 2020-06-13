using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GodotCompletionProviders
{
    internal static class RoslynUtils
    {
        public static bool TypeIsNodePath(ITypeSymbol type) =>
            type.Name == "NodePath" && type.ContainingNamespace.Name == "Godot";

        public static bool TypeIsString(ITypeSymbol type) =>
            type.Name == "String" && type.ContainingNamespace.Name == "System";

        public static void WalkUpParenthesisExpressions(ref SyntaxNode currentNode, ref int position)
        {
            while (currentNode.Kind() == SyntaxKind.ParenthesizedExpression)
            {
                position = currentNode.SpanStart;
                currentNode = currentNode.Parent;
            }
        }

        private static object StringSyntaxValueFromInterpolated(InterpolatedStringExpressionSyntax interpolatedStringExpression)
        {
            if (interpolatedStringExpression.Contents.Count > 1)
                return null;

            if (interpolatedStringExpression.Contents.Count == 1)
            {
                if (interpolatedStringExpression.Contents[0] is InterpolatedStringTextSyntax interpolatedStringTextSyntax)
                    return interpolatedStringTextSyntax.TextToken.Value;
            }

            return "";
        }

        private static object StringSyntaxValueFromInterpolated(InterpolatedStringTextSyntax interpolatedStringText) =>
            interpolatedStringText.Parent is InterpolatedStringExpressionSyntax interpolatedStringExpression ?
                StringSyntaxValueFromInterpolated(interpolatedStringExpression) :
                null;

        public static object GetStringSyntaxValue(SyntaxNode stringSyntax)
        {
            return stringSyntax switch
            {
                LiteralExpressionSyntax literalExpression => literalExpression.Token.Kind() == SyntaxKind.StringLiteralToken ? literalExpression.Token.Value : null,
                InterpolatedStringTextSyntax interpolatedStringText => StringSyntaxValueFromInterpolated(interpolatedStringText),
                InterpolatedStringExpressionSyntax interpolatedStringExpression => StringSyntaxValueFromInterpolated(interpolatedStringExpression),
                _ => null
            };
        }

        private static SyntaxNode StringSyntaxFromInterpolated(InterpolatedStringExpressionSyntax interpolatedStringExpression)
        {
            if (interpolatedStringExpression.Contents.Count > 1)
                return null;

            if (interpolatedStringExpression.Contents.Count == 1)
            {
                if (!(interpolatedStringExpression.Contents[0] is InterpolatedStringTextSyntax))
                    return null;
            }

            return interpolatedStringExpression;
        }

        private static SyntaxNode StringSyntaxFromInterpolated(InterpolatedStringTextSyntax interpolatedStringText) =>
            interpolatedStringText.Parent is InterpolatedStringExpressionSyntax interpolatedStringExpression ?
                StringSyntaxFromInterpolated(interpolatedStringExpression) :
                null;

        public static SyntaxNode WalkUpStringSyntaxOnce(ref SyntaxNode currentNode, ref int position)
        {
            var result = currentNode switch
            {
                LiteralExpressionSyntax literalExpression => literalExpression.Token.Kind() == SyntaxKind.StringLiteralToken ? literalExpression : null,
                InterpolatedStringTextSyntax interpolatedStringText => StringSyntaxFromInterpolated(interpolatedStringText),
                InterpolatedStringExpressionSyntax interpolatedStringExpression => StringSyntaxFromInterpolated(interpolatedStringExpression),
                _ => null
            };

            if (result != null)
                position = result.SpanStart;

            return result;
        }

        // Borrowed from Roslyn
        private static SyntaxToken GetOpenToken(BaseArgumentListSyntax node)
        {
            if (node == null)
                return default;

            return node.Kind() switch
            {
                SyntaxKind.ArgumentList => ((ArgumentListSyntax)node).OpenParenToken,
                SyntaxKind.BracketedArgumentList => ((BracketedArgumentListSyntax)node).OpenBracketToken,
                _ => default
            };
        }

        // Borrowed from Roslyn
        public static int GetArgumentListIndex(BaseArgumentListSyntax argumentList, SyntaxToken previousToken)
        {
            if (previousToken == GetOpenToken(argumentList))
                return 0;

            int tokenIndex = argumentList.Arguments.GetWithSeparators().IndexOf(previousToken);
            return (tokenIndex + 1) / 2;
        }

        // Borrowed from Roslyn
        private static RefKind GetRefKind(this ArgumentSyntax argument)
        {
            switch (argument?.RefKindKeyword.Kind())
            {
                case SyntaxKind.RefKeyword:
                    return RefKind.Ref;
                case SyntaxKind.OutKeyword:
                    return RefKind.Out;
                case SyntaxKind.InKeyword:
                    return RefKind.In;
                default:
                    return RefKind.None;
            }
        }

        // Borrowed from Roslyn
        internal static IEnumerable<ITypeSymbol> InferTypeInArgument(
            int index,
            IEnumerable<ImmutableArray<IParameterSymbol>> parameterizedSymbols,
            ArgumentSyntax argumentOpt)
        {
            var name = argumentOpt != null && argumentOpt.NameColon != null ? argumentOpt.NameColon.Name.Identifier.ValueText : null;
            var refKind = argumentOpt.GetRefKind();
            return InferTypeInArgument(index, parameterizedSymbols, name, refKind);
        }

        // Borrowed from Roslyn
        private static IEnumerable<ITypeSymbol> InferTypeInArgument(
            int index,
            IEnumerable<ImmutableArray<IParameterSymbol>> parameterizedSymbols,
            string name,
            RefKind refKind)
        {
            // If the callsite has a named argument, then try to find a method overload that has a
            // parameter with that name.  If we can find one, then return the type of that one.
            if (name != null)
            {
                var matchingNameParameters = parameterizedSymbols.SelectMany(m => m)
                    .Where(p => p.Name == name)
                    .Select(p => p.Type);

                return matchingNameParameters;
            }

            var allParameters = new List<ITypeSymbol>();
            var matchingRefParameters = new List<ITypeSymbol>();

            foreach (var parameterSet in parameterizedSymbols)
            {
                if (index < parameterSet.Length)
                {
                    var parameter = parameterSet[index];
                    allParameters.Add(parameter.Type);

                    if (parameter.RefKind == refKind)
                    {
                        matchingRefParameters.Add(parameter.Type);
                    }
                }
            }

            return matchingRefParameters.Count > 0 ? matchingRefParameters.ToImmutableArray() : allParameters.ToImmutableArray();
        }

        // Borrowed from Roslyn
        private static ImmutableArray<ISymbol> GetBestOrAllSymbols(this SymbolInfo info)
        {
            if (info.Symbol != null)
                return ImmutableArray.Create(info.Symbol);

            if (info.CandidateSymbols.Length > 0)
                return info.CandidateSymbols;

            return ImmutableArray<ISymbol>.Empty;
        }

        public static bool IsExpectedInvocationArgument(SemanticModel semanticModel, SyntaxToken previousToken,
            InvocationExpressionSyntax invocation, ArgumentListSyntax argumentList,
            IEnumerable<SpecificInvocationCompletionProvider.ExpectedInvocation> expectedInvocations)
        {
            if (previousToken != argumentList.OpenParenToken && previousToken.Kind() != SyntaxKind.CommaToken)
                return false;

            // ReSharper disable PossibleMultipleEnumeration

            expectedInvocations = expectedInvocations.Where(ei => argumentList.Arguments.Count <= ei.ArgumentIndex + 1);

            if (!expectedInvocations.Any())
                return false;

            int index = GetArgumentListIndex(argumentList, previousToken);

            expectedInvocations = expectedInvocations.Where(ei => index == ei.ArgumentIndex);

            if (!expectedInvocations.Any())
                return false;

            var info = semanticModel.GetSymbolInfo(invocation);
            var methods = info.GetBestOrAllSymbols().OfType<IMethodSymbol>();

            if (info.Symbol == null)
            {
                var memberGroupMethods = semanticModel.GetMemberGroup(invocation.Expression).OfType<IMethodSymbol>();
                methods = methods.Concat(memberGroupMethods).Distinct();
            }

            foreach (var expected in expectedInvocations)
            {
                var filteredMethods = methods.Where(m =>
                    m.ContainingType.ContainingNamespace.Name == expected.MethodContainingType.Namespace &&
                    m.ContainingType.Name == expected.MethodContainingType.Name &&
                    m.Name == expected.MethodName);

                var types = InferTypeInArgument(index, filteredMethods.Select(m => m.Parameters), argumentOpt: null);

                if (types.Any(t => expected.ArgumentTypes
                    .Any(at => t.Name == at.Name && t.ContainingNamespace.Name == at.Namespace)))
                {
                    return true;
                }
            }

            return false;

            // ReSharper restore PossibleMultipleEnumeration
        }

        private static Type _inferenceServiceType;
        private static object _inferenceService;
        private static MethodInfo _inferTypesMethod;

        internal static ImmutableArray<ITypeSymbol> InferTypes(
            SemanticModel semanticModel, int position,
            string nameOpt, CancellationToken cancellationToken)
        {
            // I know, I know... Don't look at me like that >_>
            const string inferenceServiceTypeQualifiedName =
                "Microsoft.CodeAnalysis.CSharp.CSharpTypeInferenceService, Microsoft.CodeAnalysis.CSharp.Workspaces";
            _inferenceServiceType ??= Type.GetType(inferenceServiceTypeQualifiedName, throwOnError: true);
            _inferenceService ??= Activator.CreateInstance(_inferenceServiceType);
            _inferTypesMethod ??= _inferenceServiceType.GetMethod("InferTypes",
                new[] {typeof(SemanticModel), typeof(int), typeof(string), typeof(CancellationToken)});

            if (_inferTypesMethod == null)
                throw new MissingMethodException("Couldn't find InferTypes");

            return (ImmutableArray<ITypeSymbol>)_inferTypesMethod.Invoke(_inferenceService,
                new object[] {semanticModel, position, null, CancellationToken.None});
        }
    }
}
