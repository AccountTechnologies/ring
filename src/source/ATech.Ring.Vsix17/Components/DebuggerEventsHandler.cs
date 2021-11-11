using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace ATech.Ring.Vsix.Components
{
    public class DebuggerEventsHandler : DebuggerEventsBase
    {
        public event EventHandler<Guid> OnProcessRemoved;
        public event EventHandler<Guid> OnProcessCreated;

        public DebuggerEventsHandler(IVsDebugger debugger) : base(debugger)
        {
        }

        public override int Event(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
        {
            switch (pEvent)
            {
                case IDebugProcessCreateEvent2 _:
                    {
                        pProcess.GetProcessId(out var logicalId);
                        OnProcessCreated?.Invoke(this, logicalId);
                    }
                    break;
                case IDebugProcessDestroyEvent2 _:
                    {
                        pProcess.GetProcessId(out var logicalId);
                        OnProcessRemoved?.Invoke(this, logicalId);
                    }
                    break;
            }

            return VSConstants.S_OK;
        }
    }
}