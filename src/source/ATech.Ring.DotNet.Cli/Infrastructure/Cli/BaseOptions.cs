using CommandLine;

namespace ATech.Ring.DotNet.Cli.Infrastructure.Cli
{
    public class BaseOptions
    {
        [Option('p', "port", Required = false, HelpText = "Specify sever port (defaults to 7999)")]
        public int Port { get; set; } = 7999;

        [Option('d', "debug", Required = false, HelpText = "Runs debug mode")]
        public bool IsDebug { get; set; }
    }
}