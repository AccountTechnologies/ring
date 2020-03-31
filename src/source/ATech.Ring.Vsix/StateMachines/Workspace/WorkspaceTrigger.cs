namespace ATech.Ring.Vsix.StateMachines.Workspace
{
    public enum WorkspaceTrigger
    {
        Load,
        Sync,
        Detach,
        Unload,
        Start,
        Stop,
        Error,
        ProjectDebugStart,
        ProjectDebugStop,
        ProjectBuildStart,
        ProjectBuildStop,
        SolutionBuildStart,
        SolutionBuildStop,
        SolutionDebugStop,
        SolutionLoaded,
        SolutionUnloaded
    }
}