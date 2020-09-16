using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace ATech.Ring.DotNet.Cli.Infrastructure
{
    internal static class ConfigurationExtensions
    {
        internal static IConfigurationBuilder AddUserSettingsFile(this IConfigurationBuilder b)
        {
            var userSettingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ATech", "ring");
            var userSettingsFilePath = Path.Combine(userSettingsDir, "appsettings.json");
            b.AddJsonFile(userSettingsFilePath, optional: true);
            return b;
        }
    }
}