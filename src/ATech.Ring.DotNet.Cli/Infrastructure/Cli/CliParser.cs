namespace ATech.Ring.DotNet.Cli.Infrastructure.Cli;

using System;
using System.IO;
using CommandLine;

public static class CliParser
{
    public const string DefaultFileName = "ring.toml";
    public static BaseOptions GetOptions(string[] args, string originalWorkingDir)
    {
        string WorkspacePathOrDefault(string? path)
        {
            if (path is string p) return Path.GetFullPath(path, originalWorkingDir);

            var defaultFilePath = Path.GetFullPath(DefaultFileName, originalWorkingDir);
            if (!File.Exists(defaultFilePath))
            {
                throw new FileNotFoundException($"{DefaultFileName} was not found in the current directory. Did you mean to pass workspace file path via the --workspace CLI option?", DefaultFileName);
            }
            return defaultFilePath;
        }

        BaseOptions options = new ConsoleOptions { IsDebug = false };
        Parser.Default.ParseArguments<ConsoleOptions, HeadlessOptions, CloneOptions, ShowConfigOptions>(args)
            .WithParsed<ConsoleOptions>(opts =>
            {
                opts.WorkspacePath = WorkspacePathOrDefault(opts.WorkspacePath);
                options = opts;
            })
            .WithParsed<HeadlessOptions>(opts => options = opts)
            .WithParsed<CloneOptions>(opts =>
            {
                opts.WorkspacePath = WorkspacePathOrDefault(opts.WorkspacePath);
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
