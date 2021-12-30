namespace ATech.Ring.DotNet.Cli.CsProj
{
    public interface ICsProjContext
    {
        string CsProjPath { get; set; }
        string WorkingDir { get; set; }
        string TargetFramework { get; set; }
        string TargetRuntime { get; set; }
        string EntryAssemblyPath { get; set; }
    }
}