using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace GodotCompletionProviders
{
    [ExportCompletionProvider(nameof(SignalNameCompletionProvider), LanguageNames.CSharp)]
    public class SignalNameCompletionProvider : SpecificInvocationCompletionProvider
    {
        private static readonly IEnumerable<ExpectedInvocation> ExpectedInvocations = new[]
        {
            new ExpectedInvocation {MethodContainingType = GodotObjectType, MethodName = "Connect", ArgumentIndex = 0, ArgumentTypes = StringTypes},
            new ExpectedInvocation {MethodContainingType = GodotObjectType, MethodName = "Disconnect", ArgumentIndex = 0, ArgumentTypes = StringTypes},
            new ExpectedInvocation {MethodContainingType = GodotObjectType, MethodName = "IsConnected", ArgumentIndex = 0, ArgumentTypes = StringTypes},
            new ExpectedInvocation {MethodContainingType = GodotObjectType, MethodName = "EmitSignal", ArgumentIndex = 0, ArgumentTypes = StringTypes},
            new ExpectedInvocation {MethodContainingType = GodotObjectType, MethodName = "ToSignal", ArgumentIndex = 1, ArgumentTypes = StringTypes},
        };

        public SignalNameCompletionProvider() : base(ExpectedInvocations, CompletionKind.Signals, "Signal")
        {
        }
    }
}
