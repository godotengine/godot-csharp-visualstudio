using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace GodotCompletionProviders
{
    [ExportCompletionProvider(nameof(ScenePathCompletionProvider), LanguageNames.CSharp)]
    public class ScenePathCompletionProvider : SpecificInvocationCompletionProvider
    {
        // TODO: Support offline (not connected to a Godot editor) completion of scene paths (from the file system).

        private static readonly IEnumerable<ExpectedInvocation> ExpectedInvocations = new[]
        {
            new ExpectedInvocation {MethodContainingType = SceneTreeType, MethodName = "ChangeScene", ArgumentIndex = 0, ArgumentTypes = StringTypes}
        };

        public ScenePathCompletionProvider() : base(ExpectedInvocations, CompletionKind.ScenePaths, "Scene")
        {
        }
    }
}
