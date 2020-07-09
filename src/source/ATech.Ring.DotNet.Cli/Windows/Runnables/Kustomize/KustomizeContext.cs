using ATech.Ring.DotNet.Cli.Abstractions.Context;

namespace ATech.Ring.DotNet.Cli.Windows.Runnables.Kustomize
{
    public class KustomizeContext : ITrackRetries
    {
        public string KustomizationDir { get; set; }
        public PodInfo[] Pods {get;set;} = new PodInfo[]{};
        public int ConsecutiveFailures { get; set; }
        public int TotalFailures { get; set; }
    }

    public class PodInfo 
    {
          public PodInfo(string ns, string podName) => (Ns, PodName) = (ns, podName); 
          public string Ns {get;}
          public string PodName {get;}
    }
}
