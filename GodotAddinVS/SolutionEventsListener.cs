using System;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace GodotAddinVS
{
    internal abstract class SolutionEventsListener : IVsSolutionEvents, IDisposable
    {
        private static volatile object _disposalLock = new object();
        private uint _eventsCookie;
        private bool _disposed;

        protected SolutionEventsListener(IServiceProvider serviceProvider)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            Solution = ServiceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            Assumes.Present(Solution);
        }

        protected IVsSolution Solution { get; }

        protected IServiceProvider ServiceProvider { get; }

        public virtual int OnAfterOpenSolution(object pUnkReserved, int fNewSolution) => VSConstants.E_NOTIMPL;

        public virtual int OnBeforeCloseSolution(object pUnkReserved) => VSConstants.E_NOTIMPL;

        public virtual int OnAfterCloseSolution(object reserved) => VSConstants.E_NOTIMPL;

        public virtual int OnQueryCloseSolution(object pUnkReserved, ref int cancel) => VSConstants.E_NOTIMPL;

        public virtual int OnAfterOpenProject(IVsHierarchy hierarchy, int added) => VSConstants.E_NOTIMPL;

        public virtual int OnAfterLoadProject(IVsHierarchy stubHierarchy, IVsHierarchy realHierarchy) => VSConstants.E_NOTIMPL;

        public virtual int OnBeforeUnloadProject(IVsHierarchy realHierarchy, IVsHierarchy rtubHierarchy) => VSConstants.E_NOTIMPL;

        public virtual int OnBeforeCloseProject(IVsHierarchy hierarchy, int removed) => VSConstants.E_NOTIMPL;

        public virtual int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int cancel) => VSConstants.E_NOTIMPL;

        public virtual int OnQueryCloseProject(IVsHierarchy hierarchy, int removing, ref int cancel) => VSConstants.E_NOTIMPL;

        public void Dispose()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Init()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            ErrorHandler.ThrowOnFailure(Solution.AdviseSolutionEvents(this, out _eventsCookie));
        }

        protected virtual void Dispose(bool disposing)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            if (_disposed)
                return;

            lock (_disposalLock)
            {
                if (disposing && _eventsCookie != 0U && Solution != null)
                {
                    Solution.UnadviseSolutionEvents(_eventsCookie);
                    _eventsCookie = 0U;
                }

                _disposed = true;
            }
        }
    }
}
