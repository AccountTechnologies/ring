namespace ATech.Ring.DotNet.Cli.Infrastructure.Cli;

using CommandLine;

[Verb("run", HelpText = "Runs a stand-alone ring! server instance")]
public class ConsoleOptions : BaseOptions
{
    [Option('w', "workspace", Required = false, HelpText = "Specify workspace path")]
    public string? WorkspacePath { get; set; }

    [Option('l', "startup-delay-seconds", Required = false, Default = 0,
        HelpText = "Loading and starting of the specified workspace is delayed by the specified number of seconds")]
    public int StartupDelaySeconds { get; set; } = 0;
}
