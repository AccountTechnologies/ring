namespace ATech.Ring.DotNet.Cli.Abstractions.Context
{
    public interface ITrackRetries
    {
        int ConsecutiveFailures{ get; set; }
        int TotalFailures { get; set; }
    }
}
