using Microsoft.VisualStudio.Shell.Flavor;
using System;
using System.Runtime.InteropServices;

namespace GodotAddinVS
{
    [Guid(GodotPackage.GodotProjectGuid)]
    public class GodotFlavoredProjectFactory : FlavoredProjectFactoryBase
    {
        private readonly GodotPackage _package;

        public GodotFlavoredProjectFactory(GodotPackage package)
        {
            _package = package;
        }

        protected override object PreCreateForOuter(IntPtr outerProjectIUnknown)
        {
            return new GodotFlavoredProject(_package);
        }
    }
}
