using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;

namespace ATech.Ring.Vsix.Components
{
    public class SolutionsEventsHandler : SolutionEventsBase
    {
        public event EventHandler OnAfterOpen;
        public event EventHandler OnAfterClose;

        public SolutionsEventsHandler(IVsSolution sln) : base(sln)
        {
        }

        public override int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                OnAfterOpen?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return base.OnAfterOpenSolution(pUnkReserved, fNewSolution);
        }

        public override int OnAfterCloseSolution(object pUnkReserved)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                OnAfterClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return base.OnAfterCloseSolution(pUnkReserved);
        }
    }
}