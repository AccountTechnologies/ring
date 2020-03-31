using ATech.Ring.Protocol;

namespace ATech.Ring.Vsix.Client.Commands
{
    public class Ping : RingCommand
    {
        public override M Type => M.PING;
    }
}