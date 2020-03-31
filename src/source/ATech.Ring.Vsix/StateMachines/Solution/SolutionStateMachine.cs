using Stateless;

namespace ATech.Ring.Vsix.StateMachines.Solution
{
    public class SolutionStateMachine : StateMachine<SolutionState, SolutionTrigger>
    {
        public SolutionStateMachine() : base(SolutionState.NotLoaded) { }
    }
}
