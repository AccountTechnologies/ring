using ATech.Ring.Protocol;

namespace ATech.Ring.Vsix.Client.Commands
{
    public class RunnableExclude : RingCommand
    {
        public string UniqueId { get; set; }
        public override M Type => M.RUNNABLE_EXCLUDE;
        public override Message AsMessage() => Message.FromString(Type, UniqueId);
    }
}