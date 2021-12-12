using ATech.Ring.Protocol;

namespace ATech.Ring.Vsix.Client.Commands
{
    public class AllRunnablesDown : RingCommand
    {
        public override M Type => M.EXCLUDE_ALL;
    }
}