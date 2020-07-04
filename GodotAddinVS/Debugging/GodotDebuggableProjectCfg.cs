using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Mono.Debugging.Soft;
using Mono.Debugging.VisualStudio;
using System;
using System.Net;
using System.Runtime.InteropServices;

namespace GodotAddinVS.Debugging
{
    internal class GodotDebuggableProjectCfg : IVsDebuggableProjectCfg, IVsProjectFlavorCfg
    {
        private IVsProjectFlavorCfg _baseProjectCfg;
        private readonly EnvDTE.Project _baseProject;

        public GodotDebuggableProjectCfg(IVsProjectFlavorCfg baseProjectCfg, EnvDTE.Project project)
        {
            _baseProject = project;
            _baseProjectCfg = baseProjectCfg;
        }

        public int get_DisplayName(out string pbstrDisplayName)
        {
            throw new NotImplementedException();
        }

        public int get_IsDebugOnly(out int pfIsDebugOnly)
        {
            throw new NotImplementedException();
        }

        public int get_IsReleaseOnly(out int pfIsReleaseOnly)
        {
            throw new NotImplementedException();
        }

        public int EnumOutputs(out IVsEnumOutputs ppIVsEnumOutputs)
        {
            throw new NotImplementedException();
        }

        public int OpenOutput(string szOutputCanonicalName, out IVsOutput ppIVsOutput)
        {
            throw new NotImplementedException();
        }

        public int get_ProjectCfgProvider(out IVsProjectCfgProvider ppIVsProjectCfgProvider)
        {
            throw new NotImplementedException();
        }

        public int get_BuildableProjectCfg(out IVsBuildableProjectCfg ppIVsBuildableProjectCfg)
        {
            throw new NotImplementedException();
        }

        public int get_CanonicalName(out string pbstrCanonicalName)
        {
            throw new NotImplementedException();
        }

        public int get_Platform(out Guid pguidPlatform)
        {
            throw new NotImplementedException();
        }

        public int get_IsPackaged(out int pfIsPackaged)
        {
            throw new NotImplementedException();
        }

        public int get_IsSpecifyingOutputSupported(out int pfIsSpecifyingOutputSupported)
        {
            throw new NotImplementedException();
        }

        public int get_TargetCodePage(out uint puiTargetCodePage)
        {
            throw new NotImplementedException();
        }

        public int get_UpdateSequenceNumber(ULARGE_INTEGER[] puliUSN)
        {
            throw new NotImplementedException();
        }

        public int get_RootURL(out string pbstrRootURL)
        {
            throw new NotImplementedException();
        }

        public int DebugLaunch(uint grfLaunch)
        {
            bool noDebug = ((__VSDBGLAUNCHFLAGS)grfLaunch & __VSDBGLAUNCHFLAGS.DBGLAUNCH_NoDebug) != 0;
            _ = noDebug; // TODO: Run without Debugging

            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            var random = new Random(DateTime.Now.Millisecond);
            var port = 8800 + random.Next(0, 100);

            var startArgs = new SoftDebuggerListenArgs(_baseProject.Name, IPAddress.Loopback, port) {MaxConnectionAttempts = 3};

            var startInfo = new GodotStartInfo(startArgs, null, _baseProject) {WorkingDirectory = GodotPackage.Instance.GodotSolutionEventsListener?.SolutionDir};
            var session = new GodotDebuggerSession();

            var launcher = new MonoDebuggerLauncher(new Progress<string>());

            launcher.StartSession(startInfo, session);

            return VSConstants.S_OK;
        }

        public int QueryDebugLaunch(uint grfLaunch, out int pfCanLaunch)
        {
            pfCanLaunch = 1;
            return VSConstants.S_OK;
        }

        public int get_CfgType(ref Guid iidCfg, out IntPtr ppCfg)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            ppCfg = IntPtr.Zero;

            try
            {
                if (iidCfg == typeof(IVsDebuggableProjectCfg).GUID)
                {
                    ppCfg = Marshal.GetComInterfaceForObject(this, typeof(IVsDebuggableProjectCfg));
                    return VSConstants.S_OK;
                }

                if (iidCfg == typeof(IVsProjectCfgDebugTargetSelection).GUID)
                {
                    ppCfg = Marshal.GetComInterfaceForObject(GodotDebugTargetSelection.Instance,
                        typeof(IVsProjectCfgDebugTargetSelection));
                    return VSConstants.S_OK;
                }

                if ((ppCfg == IntPtr.Zero) && (_baseProjectCfg != null))
                {
                    return _baseProjectCfg.get_CfgType(ref iidCfg, out ppCfg);
                }
            }
            catch (InvalidCastException)
            {
            }

            return VSConstants.E_NOINTERFACE;
        }

        public int Close()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            if (_baseProjectCfg != null)
            {
                _baseProjectCfg.Close();
                _baseProjectCfg = null;
            }

            return VSConstants.S_OK;
        }
    }
}
