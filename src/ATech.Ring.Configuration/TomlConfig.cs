using Tomlyn;

namespace ATech.Ring.Configuration;

public static class TomlConfig
{
    public static TomlModelOptions Settings { get; } = new(){ ConvertPropertyName = x => x.ToLower() };
}
