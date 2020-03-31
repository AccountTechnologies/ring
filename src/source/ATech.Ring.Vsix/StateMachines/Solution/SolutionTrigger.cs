namespace ATech.Ring.Vsix.StateMachines.Solution
{
    public enum SolutionTrigger
    {
        Load, Unload, DebugStart, DebugAdd, DebugRemove, DebugStop, WorkspaceLoaded, WorkspaceUnloaded, WorkspaceFaulted,
        BuildStart, BuildStop
    }
}