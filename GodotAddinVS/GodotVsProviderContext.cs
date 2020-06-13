using System;
using System.Threading.Tasks;
using GodotCompletionProviders;
using GodotTools.IdeMessaging;
using GodotTools.IdeMessaging.Requests;
using ILogger = GodotCompletionProviders.ILogger;

namespace GodotAddinVS
{
    internal class GodotVsProviderContext : IProviderContext
    {
        private readonly GodotPackage _package;

        public GodotVsProviderContext(GodotPackage package)
        {
            _package = package;
        }

        public ILogger GetLogger() => _package.Logger;

        public bool AreCompletionsEnabledFor(CompletionKind completionKind)
        {
            var options = (GeneralOptionsPage)GodotPackage.Instance.GetDialogPage(typeof(GeneralOptionsPage));

            if (options == null)
                return false;

            return completionKind switch
            {
                CompletionKind.NodePaths => options.ProvideNodePathCompletions,
                CompletionKind.InputActions => options.ProvideInputActionCompletions,
                CompletionKind.ResourcePaths => options.ProvideResourcePathCompletions,
                CompletionKind.ScenePaths => options.ProvideScenePathCompletions,
                CompletionKind.Signals => options.ProvideSignalNameCompletions,
                _ => false
            };
        }

        public bool CanRequestCompletionsFromServer()
        {
            var godotMessagingClient = _package.GodotSolutionEventsListener?.GodotMessagingClient;
            return godotMessagingClient != null && godotMessagingClient.IsConnected;
        }

        public async Task<string[]> RequestCompletion(CompletionKind completionKind, string absoluteFilePath)
        {
            var godotMessagingClient = _package.GodotSolutionEventsListener?.GodotMessagingClient;

            if (godotMessagingClient == null)
                throw new InvalidOperationException();

            var request = new CodeCompletionRequest {Kind = (CodeCompletionRequest.CompletionKind)completionKind, ScriptFile = absoluteFilePath};
            var response = await godotMessagingClient.SendRequest<CodeCompletionResponse>(request);

            if (response.Status == MessageStatus.Ok)
                return response.Suggestions;

            GetLogger().LogError($"Received code completion response with status '{response.Status}'.");
            return new string[] { };
        }
    }
}
