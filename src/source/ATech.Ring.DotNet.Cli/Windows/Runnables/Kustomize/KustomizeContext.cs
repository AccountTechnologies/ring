using ATech.Ring.DotNet.Cli.Abstractions.Context;

namespace ATech.Ring.DotNet.Cli.Windows.Runnables.Kustomize
{
    public class KustomizeContext : ITrackRetries
    {
        public string KustomizationDir { get; set; }
        public int ConsecutiveFailures { get; set; }
        public int TotalFailures { get; set; }
    }
}