using ATech.Ring.Protocol;

namespace ATech.Ring.Vsix.Client.Commands
{
    public class AllRunnablesUp : RingCommand
    {
        public override M Type => M.INCLUDE_ALL;
    }
}