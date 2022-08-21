using System.Diagnostics;

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
            if (path != null) return Path.GetFullPath(path, originalWorkingDir);

            var defaultFilePath = Path.GetFullPath(DefaultFileName, originalWorkingDir);
            if (!File.Exists(defaultFilePath))
            {
                throw new FileNotFoundException($"{DefaultFileName} was not found in the current directory. Did you mean to pass workspace file path via the --workspace CLI option?", DefaultFileName);
            }
            return defaultFilePath;
        }

        void EnsureConfigOverrideFile(string path, string scope)
        {
            var dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(dir!);

            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, File.ReadAllBytes(Directories.Installation.AppsettingsPath()));
                Console.WriteLine($"Config file (scope: {scope}) created: {path}");
            }
            else
            {
                Console.WriteLine($"Config file (scope: {scope}) already exists: {path}");
            }
        }
        
        BaseOptions options = new ConsoleOptions { IsDebug = false };
        Parser.Default
            .ParseArguments<
                ConsoleOptions,
                HeadlessOptions,
                CloneOptions,
                ConfigPath,
                ConfigDump,
                ConfigCreate>(args)
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
            .WithParsed<ConfigDump>(opts =>
            {
                options = opts;
            })
            .WithParsed<ConfigPath>(opts =>
            {
                var path =
                    opts.Local ? Directories.Working(originalWorkingDir).AppsettingsPath :
                    opts.User ? Directories.User.AppsettingsPath :
                    opts.Global ? Directories.Installation.AppsettingsPath() : throw new ArgumentOutOfRangeException();

                Console.WriteLine(path);
                Environment.Exit(0);
            })
            .WithParsed<ConfigCreate>(opts =>
            {
                if (opts.Local)
                {
                    EnsureConfigOverrideFile(Directories.Working(originalWorkingDir).AppsettingsPath, "local");
                }

                if (opts.User)
                {
                    EnsureConfigOverrideFile(Directories.User.AppsettingsPath, "user");
                }

                Environment.Exit(0);
            })
            .WithNotParsed(_ => Environment.Exit(-1));
        return options;
    }
}
