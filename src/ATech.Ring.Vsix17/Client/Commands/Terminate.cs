using ATech.Ring.Protocol;

namespace ATech.Ring.Vsix.Client.Commands
{
    public class Terminate : RingCommand
    {
        public override M Type => M.TERMINATE;
    }
}