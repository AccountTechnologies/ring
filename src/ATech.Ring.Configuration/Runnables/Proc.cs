namespace ATech.Ring.Configuration.Runnables;

public class Proc : RunnableConfigBase
{
    public string Path { get; set; }
    public string[] Env { get; set; } = { };
    public override string Id => Path;
}
