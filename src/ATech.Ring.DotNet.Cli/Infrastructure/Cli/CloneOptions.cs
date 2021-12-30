using CommandLine;

namespace ATech.Ring.DotNet.Cli.Infrastructure.Cli
{
    [Verb("clone", HelpText = "Clones repos for all runnables in the current workspace")]
    public class CloneOptions : BaseOptions
    {
        [Option('w', "workspace", Required = false, HelpText = "Specify workspace path")]
        public string WorkspacePath { get; set; }

        [Option('o', "output-dir", Required = false, HelpText = "Specify the output directory")]
        public string OutputDir { get; set; }
    }
}