namespace ATech.Ring.Configuration.Interfaces
{
    public interface IFromGit
    {
        string SshRepoUrl { get; }
        string CloneFullPath { get; set; }
    }
}