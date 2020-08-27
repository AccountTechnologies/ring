using ATech.Ring.DotNet.Cli.Abstractions.Context;

namespace ATech.Ring.DotNet.Cli.Windows.Runnables.Kustomize
{
    public class KustomizeContext : ITrackRetries
    {
        public string KustomizationDir { get; set; }
        public Namespace[] Namespaces {get;set;} = new Namespace[]{};
        public int ConsecutiveFailures { get; set; }
        public int TotalFailures { get; set; }
    }

    public class Namespace 
    {
        public string Name {get; set;}
        public string[] Pods { get; set; } = new string[]{};
    }
}
