using System.IO;
using System.Runtime.InteropServices;

namespace ATech.Ring.DotNet.Cli.Infrastructure;

using System;
using System.Reflection;
using static Path;

internal static class Directories
{
    internal static readonly InstallationDir Installation = new();
    internal static readonly UserSettingsDir User = new();
    internal static WorkingDir Working(string path) => new (path);

    internal static string GetOsPath()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" :
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows" : 
            throw new NotSupportedException("Platform not supported");
    }
}

internal class InstallationDir
{
    private static string Path =>
        GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        ?? throw new InvalidOperationException("Can't determine the executing assembly location");

    internal string SettingsPath => Combine(Path, $"app.{Directories.GetOsPath()}.toml");
    internal string LoggingPath => Combine(Path,  $"logging.{Directories.GetOsPath()}.toml");
}

internal class UserSettingsDir
{
    private string Path => Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".ring");
    internal string SettingsPath => Combine(Path, "settings.toml");
}

internal class WorkingDir
{
    private readonly string _path;
    internal WorkingDir(string path) => _path = path;
    private string Path => Combine(_path, ".ring");
    internal string SettingsPath => Combine(Path, "settings.toml");
}
