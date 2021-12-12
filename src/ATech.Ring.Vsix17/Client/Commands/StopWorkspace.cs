using ATech.Ring.Protocol;

namespace ATech.Ring.Vsix.Client.Commands
{
    public class StopWorkspace : RingCommand
    {
        public override M Type => M.STOP;
    }
}