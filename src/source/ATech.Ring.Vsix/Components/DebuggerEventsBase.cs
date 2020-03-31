using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Debugger.Interop;

namespace ATech.Ring.Vsix.Components
{
    public abstract class DebuggerEventsBase : IDisposable, IDebugEventCallback2
    {
        protected readonly IVsDebugger Debugger;

        protected DebuggerEventsBase(IVsDebugger debugger)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Debugger = debugger;
            Debugger.AdviseDebugEventCallback(this);
        }
        
        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Debugger?.UnadviseDebugEventCallback(this);
        }

        public virtual int Event(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
        => VSConstants.S_OK;
    }
}