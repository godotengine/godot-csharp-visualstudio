using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using EnvDTE;
using GodotTools.IdeMessaging;
using GodotTools.IdeMessaging.Requests;
using Microsoft.VisualStudio.Shell;

namespace GodotAddinVS.GodotMessaging
{
    public class MessageHandler : ClientMessageHandler
    {
        private static ILogger Logger => GodotPackage.Instance.Logger;

        protected override async Task<Response> HandleOpenFile(OpenFileRequest request)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = GodotPackage.Instance.GetService<DTE>();

            try
            {
                dte.ItemOperations.OpenFile(request.File);
            }
            catch (ArgumentException e)
            {
                Logger?.LogError("ItemOperations.OpenFile: Invalid path or file not found", e);
                return new OpenFileResponse {Status = MessageStatus.InvalidRequestBody};
            }

            if (request.Line != null)
            {
                var textSelection = (TextSelection)dte.ActiveDocument.Selection;

                if (request.Column != null)
                {
                    textSelection.MoveToLineAndOffset(request.Line.Value, request.Column.Value);
                }
                else
                {
                    textSelection.GotoLine(request.Line.Value, Select: true);
                }
            }

            var mainWindow = dte.MainWindow;
            mainWindow.Activate();
            SetForegroundWindow(new IntPtr(mainWindow.HWnd));

            return new OpenFileResponse {Status = MessageStatus.Ok};
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
