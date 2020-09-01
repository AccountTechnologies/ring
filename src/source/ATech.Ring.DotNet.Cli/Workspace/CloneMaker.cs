using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Windows.Tools;

namespace ATech.Ring.DotNet.Cli.Workspace
{
    public class CloneMaker : ICloneMaker
    {
        private readonly IConfigurator _configurator;
        private readonly GitClone _gitClone;

        public CloneMaker(IConfigurator configurator, GitClone gitClone)
        {
            _configurator = configurator;
            _gitClone = gitClone;
        }

        public async Task CloneWorkspaceRepos(string workspacePath, string outputDir = null, CancellationToken token = default)
        {
            await _configurator.LoadAsync(new ConfiguratorPaths { WorkspacePath = workspacePath }, token);
            var gitConfigs = _configurator.Current.Values.OfType<IFromGit>();
            foreach (var gitCfg in gitConfigs.GroupBy(x => _gitClone.ResolveFullClonePath(x, outputDir)).Select(x => x.First()))
            {
                var output = await _gitClone.CloneOrPullAsync(gitCfg, outputDir);
                if (output.IsSuccess) continue;
                break;
            }
        }
    }
}
