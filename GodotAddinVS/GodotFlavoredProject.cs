using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using GodotAddinVS.Debugging;

namespace GodotAddinVS
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(GodotPackage.GodotProjectGuid)]
    internal class GodotFlavoredProject : FlavoredProjectBase, IVsProjectFlavorCfgProvider
    {
        private IVsProjectFlavorCfgProvider _innerFlavorConfig;
        private GodotPackage _package;

        public GodotFlavoredProject(GodotPackage package)
        {
            _package = package;
        }

        public int CreateProjectFlavorCfg(IVsCfg pBaseProjectCfg, out IVsProjectFlavorCfg ppFlavorCfg)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            ppFlavorCfg = null;

            if (_innerFlavorConfig != null)
            {
                GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out var project);

                _innerFlavorConfig.CreateProjectFlavorCfg(pBaseProjectCfg, out IVsProjectFlavorCfg cfg);
                ppFlavorCfg = new GodotDebuggableProjectCfg(cfg, project as EnvDTE.Project);
            }

            return ppFlavorCfg != null ? VSConstants.S_OK : VSConstants.E_FAIL;
        }

        protected override void SetInnerProject(IntPtr innerIUnknown)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            object inner = Marshal.GetObjectForIUnknown(innerIUnknown);
            _innerFlavorConfig = inner as IVsProjectFlavorCfgProvider;

            if (serviceProvider == null)
                serviceProvider = _package;

            base.SetInnerProject(innerIUnknown);
        }
    }
}
