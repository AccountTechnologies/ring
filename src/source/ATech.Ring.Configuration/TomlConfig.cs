using Nett;

namespace ATech.Ring.Configuration
{
    public static class TomlConfig
    {
        public static TomlSettings Settings { get; } = TomlSettings.Create(cfg =>
            cfg.ConfigurePropertyMapping(p => p.UseTargetPropertySelector(x => x.IgnoreCase)));
    }
}