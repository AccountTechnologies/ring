using Stateless;

namespace ATech.Ring.Vsix.StateMachines.Workspace
{
    public class WorkspaceStateMachine : StateMachine<WorkspaceState, WorkspaceTrigger>
    {
        public WorkspaceStateMachine() : base(WorkspaceState.NotLoaded) { }
    }
}
