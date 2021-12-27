namespace ATech.Ring.Configuration.Interfaces;

public interface IConfigurationLoader
{
    T Load<T>(string path);
}
