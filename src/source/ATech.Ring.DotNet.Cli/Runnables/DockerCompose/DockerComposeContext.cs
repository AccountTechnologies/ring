using ATech.Ring.DotNet.Cli.Abstractions.Context;

namespace ATech.Ring.DotNet.Cli.Windows.Runnables.DockerCompose
{
    public class DockerComposeContext : ITrackProcessId, ITrackRetries
    {
        public string ComposeFilePath { get; set; }
        public int ProcessId { get; set; }
        public int ConsecutiveFailures { get; set; }
        public int TotalFailures { get; set; }
    }
}