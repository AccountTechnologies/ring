using ATech.Ring.Vsix.StateMachines.Solution;
using ATech.Ring.Vsix.StateMachines.Workspace;

namespace ATech.Ring.Vsix.ViewModel
{
    public partial class RingWindowViewModel
    {
        public WorkspaceViewModel Workspace { get; }
        public SolutionViewModel Solution { get; }

        private bool IsSolutionLoaded => _solutionMachine.State != SolutionState.NotLoaded;
        private bool IsWorkspaceLoaded => _workspaceMachine.State != WorkspaceState.NotLoaded;
        private bool IsReady => IsSolutionLoaded && IsWorkspaceLoaded;

        private readonly WorkspaceStateMachine _workspaceMachine;
        private readonly SolutionStateMachine _solutionMachine;

        public RingWindowViewModel(SolutionStateMachine slnFsm,
                                   WorkspaceStateMachine wsFsm,
                                   SolutionViewModel slnVm,
                                   WorkspaceViewModel wsViewModel
            )
        {

            _workspaceMachine = wsFsm;
            _solutionMachine = slnFsm;
            Workspace = wsViewModel;
            Solution = slnVm;
            _workspaceMachine.Activate();
            _solutionMachine.Activate();
        }
    }
}
