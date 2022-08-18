using System.Threading;
using System.Threading.Tasks;

namespace ATech.Ring.DotNet.Cli.Workspace;

public interface ICloneMaker
{
    Task CloneWorkspaceRepos(string workspacePath, string outputDir, CancellationToken token = default);
}