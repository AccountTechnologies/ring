namespace ATech.Ring.Configuration.Interfaces;

/// <summary>
/// Inidcates a particular runnable has a corresponding source code in a Git repository
/// </summary>
public interface IFromGit
{
    string SshRepoUrl { get; }
}
