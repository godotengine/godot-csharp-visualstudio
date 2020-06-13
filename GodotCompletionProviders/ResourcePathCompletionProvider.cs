using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace GodotCompletionProviders
{
    [ExportCompletionProvider(nameof(ResourcePathCompletionProvider), LanguageNames.CSharp)]
    public class ResourcePathCompletionProvider : SpecificInvocationCompletionProvider
    {
        // TODO: If generic Load, filter by type
        // TODO: Support offline (not connected to a Godot editor) completion of resource paths (from the file system).

        private static readonly IEnumerable<ExpectedInvocation> ExpectedInvocations = new[]
        {
            new ExpectedInvocation {MethodContainingType = GdType, MethodName = "Load", ArgumentIndex = 0, ArgumentTypes = StringTypes},
            new ExpectedInvocation {MethodContainingType = ResourceLoaderType, MethodName = "Load", ArgumentIndex = 0, ArgumentTypes = StringTypes}
        };

        public ResourcePathCompletionProvider() : base(ExpectedInvocations, CompletionKind.ResourcePaths, "Resource")
        {
        }
    }
}
