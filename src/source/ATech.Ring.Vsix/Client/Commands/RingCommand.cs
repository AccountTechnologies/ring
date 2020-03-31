using ATech.Ring.Protocol;

namespace ATech.Ring.Vsix.Client.Commands
{
    public abstract class RingCommand : IRingCommand
    {
        public abstract M Type { get; }
        public virtual Message AsMessage() => Message.From(Type);
    }
}