using ATech.Ring.Protocol;

namespace ATech.Ring.Vsix.Client.Commands
{
    public class RequestWorkspaceInfo : RingCommand
    {
        public override M Type => M.WORKSPACE_INFO_RQ;
        public override Message AsMessage() => Message.From(Type);
    }
}