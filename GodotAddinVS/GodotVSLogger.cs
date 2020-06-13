using System;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading.Tasks;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;

namespace GodotAddinVS
{
    // ReSharper disable once InconsistentNaming
    public class GodotVSLogger : GodotTools.IdeMessaging.ILogger, GodotCompletionProviders.ILogger
    {
        private async Task LogMessageAsync(__ACTIVITYLOG_ENTRYTYPE actType, string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // ReSharper disable once SuspiciousTypeConversion.Global
            var log = (IVsActivityLog)GodotPackage.Instance.GetService<SVsActivityLog>();

            if (log == null)
                return;

            _ = log.LogEntry((uint)actType, this.ToString(), message);
        }

        public void LogDebug(string message)
        {
            _ = LogMessageAsync(__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, message);
        }

        public void LogInfo(string message)
        {
            _ = LogMessageAsync(__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, message);
        }

        public void LogWarning(string message)
        {
            _ = LogMessageAsync(__ACTIVITYLOG_ENTRYTYPE.ALE_WARNING, message);
        }

        public void LogError(string message)
        {
            _ = LogMessageAsync(__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, message);
        }

        public void LogError(string message, Exception e)
        {
            _ = LogMessageAsync(__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, message + "\n" + e);
        }
    }
}
