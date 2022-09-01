using System;
using ATech.Ring.DotNet.Cli.Abstractions.Context;
using ATech.Ring.DotNet.Cli.CsProj;

namespace ATech.Ring.DotNet.Cli.Runnables.Windows.IISExpress;

public class IISExpressContext : ITrackProcessId, ITrackProcessOutput, ICsProjContext, ITrackRetries, ITrackUri
{
    public int ProcessId { get; set; }
    public string Output { get; set; }
    public string CsProjPath { get; set; }
    public string WorkingDir { get; set; }
    public string TargetFramework { get; set; }
    public string TargetRuntime { get; set; }
    public string EntryAssemblyPath { get; set; }
    public string TempAppHostConfigPath { get; set; }
    public int ConsecutiveFailures { get; set; }
    public int TotalFailures { get; set; }
    public Uri Uri { get; set; }
}