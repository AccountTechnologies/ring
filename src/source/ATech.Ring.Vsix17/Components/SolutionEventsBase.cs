using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace ATech.Ring.Vsix.Components
{
    public abstract class SolutionEventsBase : IVsSolutionEvents, IDisposable
    {
        protected readonly IVsSolution SolutionService;
        private readonly uint _slnCookie;

        protected SolutionEventsBase(IVsSolution sln)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SolutionService = sln;
            SolutionService.AdviseSolutionEvents(this, out _slnCookie);
        }
        public virtual int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) => VSConstants.S_OK;
        public virtual int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;
        public virtual int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => VSConstants.S_OK;
        public virtual int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;
        public virtual int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;
        public virtual int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;
        public virtual int OnAfterOpenSolution(object pUnkReserved, int fNewSolution) => VSConstants.S_OK;
        public virtual int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.S_OK;
        public virtual int OnBeforeCloseSolution(object pUnkReserved) => VSConstants.S_OK;
        public virtual int OnAfterCloseSolution(object pUnkReserved) => VSConstants.S_OK;
        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (SolutionService != null && _slnCookie != 0) SolutionService.UnadviseSolutionEvents(_slnCookie);
        }
    }
}