using System.Threading.Tasks;

namespace GodotCompletionProviders
{
    public interface IProviderContext
    {
        ILogger GetLogger();
        bool AreCompletionsEnabledFor(CompletionKind completionKind);
        bool CanRequestCompletionsFromServer();
        Task<string[]> RequestCompletion(CompletionKind completionKind, string absoluteFilePath);
    }
}
