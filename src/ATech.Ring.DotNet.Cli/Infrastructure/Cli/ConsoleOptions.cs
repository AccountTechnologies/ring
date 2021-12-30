namespace ATech.Ring.DotNet.Cli.Infrastructure.Cli;

using CommandLine;

[Verb("run", HelpText = "Runs a stand-alone ring! server instance")]
public class ConsoleOptions : BaseOptions
{
    [Option('w', "workspace", Required = false, HelpText = "Specify workspace path")]
    public string? WorkspacePath { get; set; }
}
