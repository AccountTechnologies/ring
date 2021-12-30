namespace ATech.Ring.Configuration.Interfaces;

public interface IConfigurationTreeReader
{
    WorkspaceConfig GetConfigTree(ConfiguratorPaths paths);
}
