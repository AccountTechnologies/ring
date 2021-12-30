using ATech.Ring.DotNet.Cli.Abstractions.Context;
using ATech.Ring.DotNet.Cli.CsProj;

namespace ATech.Ring.DotNet.Cli.Windows.Runnables.NetExe
{
    public class NetExeContext : ITrackProcessId, 
                                 ITrackProcessOutput, 
                                 ICsProjContext,
                                 ITrackRetries
    {
        public int ProcessId { get; set; }
        public string Output { get; set; }
        public string CsProjPath { get; set; }
        public string WorkingDir { get; set; }
        public string TargetFramework { get; set; }
        public string TargetRuntime { get; set; }
        public string EntryAssemblyPath { get; set; }
        public int ConsecutiveFailures { get; set; }
        public int TotalFailures { get; set; }
    }
}