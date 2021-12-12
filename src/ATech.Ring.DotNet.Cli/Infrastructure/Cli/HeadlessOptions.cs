using CommandLine;

namespace ATech.Ring.DotNet.Cli.Infrastructure.Cli
{
    [Verb("headless", HelpText = "Runs ring! server in headless mode")]
    public class HeadlessOptions : BaseOptions
    {
    }
}