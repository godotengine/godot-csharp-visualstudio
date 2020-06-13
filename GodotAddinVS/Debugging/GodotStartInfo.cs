using EnvDTE;
using Mono.Debugging.Soft;
using Mono.Debugging.VisualStudio;

namespace GodotAddinVS.Debugging
{
    internal class GodotStartInfo : StartInfo
    {
        public GodotStartInfo(SoftDebuggerStartArgs args, DebuggingOptions options, Project startupProject) :
            base(args, options, startupProject)
        {
        }
    }
}
