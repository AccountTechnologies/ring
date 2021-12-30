using System;
using System.Reflection;
using static System.IO.Path;

namespace ATech.Ring.DotNet.Cli.Infrastructure
{
    internal static class InstallationDir
    {
        internal static string Path =>
            GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException("Can't determine the executing assembly location");
        internal static string FilePath(string fileName) => Combine(Path, fileName);
        internal static string AppsettingsJsonPath(string? variant = null)
        {
            var variantString = variant is string v ? $".{v}" : string.Empty;
            return FilePath($"appsettings{variantString}.json");
        }
    }
}
