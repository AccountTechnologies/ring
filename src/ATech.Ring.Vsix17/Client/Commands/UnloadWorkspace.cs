using ATech.Ring.Protocol;

namespace ATech.Ring.Vsix.Client.Commands
{
    public class UnloadWorkspace : RingCommand
    {
        public override M Type => M.UNLOAD;
    }
}