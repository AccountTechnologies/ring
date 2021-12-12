using ATech.Ring.Configuration.Interfaces;
using Nett;

namespace ATech.Ring.Configuration
{
    public class ConfigurationLoader : IConfigurationLoader
    {
        public T Load<T>(string path) => Toml.ReadFile<T>(path, TomlConfig.Settings);
    }
}