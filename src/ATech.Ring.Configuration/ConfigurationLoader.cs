using System.IO;
using ATech.Ring.Configuration.Interfaces;
using Tomlyn;

namespace ATech.Ring.Configuration;

public class ConfigurationLoader : IConfigurationLoader
{
    public T Load<T>(string path) where T : class, new() => Toml.ToModel<T>(File.ReadAllText(path), path, TomlConfig.Settings);
}
