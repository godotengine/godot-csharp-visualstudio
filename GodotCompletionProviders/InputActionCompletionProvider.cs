using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace GodotCompletionProviders
{
    [ExportCompletionProvider(nameof(InputActionCompletionProvider), LanguageNames.CSharp)]
    public class InputActionCompletionProvider : SpecificInvocationCompletionProvider
    {
        // TODO: Support offline (not connected to a Godot editor) completion of input actions (parse godot.project).

        private static readonly IEnumerable<ExpectedInvocation> ExpectedInvocations = new[]
        {
            new ExpectedInvocation {MethodContainingType = InputType, MethodName = "IsActionPressed", ArgumentIndex = 0, ArgumentTypes = StringTypes},
            new ExpectedInvocation {MethodContainingType = InputType, MethodName = "IsActionJustPressed", ArgumentIndex = 0, ArgumentTypes = StringTypes},
            new ExpectedInvocation {MethodContainingType = InputType, MethodName = "IsActionJustReleased", ArgumentIndex = 0, ArgumentTypes = StringTypes},
            new ExpectedInvocation {MethodContainingType = InputType, MethodName = "GetActionStrength", ArgumentIndex = 0, ArgumentTypes = StringTypes},
            new ExpectedInvocation {MethodContainingType = InputType, MethodName = "ActionPress", ArgumentIndex = 0, ArgumentTypes = StringTypes},
            new ExpectedInvocation {MethodContainingType = InputType, MethodName = "ActionRelease", ArgumentIndex = 0, ArgumentTypes = StringTypes}
        };

        public InputActionCompletionProvider() : base(ExpectedInvocations, CompletionKind.InputActions, "InputAction")
        {
        }
    }
}
