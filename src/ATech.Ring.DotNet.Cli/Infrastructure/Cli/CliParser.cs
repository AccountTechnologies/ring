using System;
using System.IO;
using CommandLine;

namespace ATech.Ring.DotNet.Cli.Infrastructure.Cli
{
    public static class CliParser
    {
        public static BaseOptions GetOptions(string[] args, string originalWorkingDir)
        {
            BaseOptions options = new ConsoleOptions { IsDebug = false };
            Parser.Default.ParseArguments<ConsoleOptions, HeadlessOptions, CloneOptions, ShowConfigOptions>(args)
                .WithParsed<ConsoleOptions>(opts =>
                {
                    opts.WorkspacePath = Path.GetFullPath(opts.WorkspacePath, originalWorkingDir);
                    options = opts;
                })
                .WithParsed<HeadlessOptions>(opts => options = opts)
                .WithParsed<CloneOptions>(opts =>
                {
                    opts.WorkspacePath = Path.GetFullPath(opts.WorkspacePath, originalWorkingDir);
                    options = opts;
                })
                .WithParsed<ShowConfigOptions>(opts =>
                {
                    options = opts;
                    Console.WriteLine(InstallationDir.AppsettingsJsonPath());
                    Environment.Exit(0);
                })
                .WithNotParsed(x => Environment.Exit(-1));
            return options;
        }
    }
}
