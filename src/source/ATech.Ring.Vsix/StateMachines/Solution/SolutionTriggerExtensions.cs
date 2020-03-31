namespace ATech.Ring.Vsix.StateMachines.Solution
{
    public static class SolutionTriggerExtensions
    {
        public static SolutionStateMachine.TriggerWithParameters<TData> With<TData>(this SolutionTrigger t)
        {
            return new SolutionStateMachine.TriggerWithParameters<TData>(t);
        }
    }
}