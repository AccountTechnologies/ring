using CommandLine;

namespace ATech.Ring.DotNet.Cli.Infrastructure.Cli
{
    [Verb("run", HelpText = "Runs a stand-alone ring! server instance")]
    public class ConsoleOptions : BaseOptions
    {
        [Option('w', "workspace", Required = true, HelpText = "Specify workspace path")]
        public string WorkspacePath { get; set; }
    }
}