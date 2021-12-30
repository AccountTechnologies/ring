namespace ATech.Ring.Vsix.StateMachines.Workspace
{
    public static class WorkspaceTriggerExtensions
    {
        public static WorkspaceStateMachine.TriggerWithParameters<TData> With<TData>(this WorkspaceTrigger t)
        {
            return new WorkspaceStateMachine.TriggerWithParameters<TData>(t);
        }
    }
}