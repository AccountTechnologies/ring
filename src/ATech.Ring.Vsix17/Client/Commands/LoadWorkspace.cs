using ATech.Ring.Protocol;

namespace ATech.Ring.Vsix.Client.Commands
{
    public class LoadWorkspace : RingCommand
    {
        public string FullPath { get; set; }
        public override M Type => M.LOAD;
        public override Message AsMessage() => Message.FromString(Type, FullPath);
    }
}