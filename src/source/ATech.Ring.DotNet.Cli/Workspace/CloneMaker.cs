using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Logging;
using ATech.Ring.DotNet.Cli.Windows.Tools;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Workspace
{
    public class CloneMaker : ICloneMaker
    {
        private readonly ILogger<CloneMaker> _logger;
        private readonly IConfigurator _configurator;
        private readonly GitClone _gitClone;

        public CloneMaker(ILogger<CloneMaker> logger, IConfigurator configurator, GitClone gitClone)
        {
            _logger = logger;
            _configurator = configurator;
            _gitClone = gitClone;
        }

        public async Task CloneWorkspaceRepos(string workspacePath, string outputDir = null, CancellationToken token = default)
        {
            using var _ = _logger.WithScope(nameof(CloneMaker), Phase.GIT);
            await _configurator.LoadAsync(new ConfiguratorPaths { WorkspacePath = workspacePath }, token);
            var haveValidGitUrl = _configurator.Current.Values.OfType<IFromGit>().ToLookup(x => !string.IsNullOrWhiteSpace(x.SshRepoUrl));

            foreach (var invalidCfg in haveValidGitUrl[false].Cast<IRunnableConfig>())
            {
                _logger.LogInformation("{parameter} is not specified for {runnableId}. Skipping.", nameof(IFromGit.SshRepoUrl), invalidCfg.Id);
            }

            foreach (var gitCfg in haveValidGitUrl[true].GroupBy(x => _gitClone.ResolveFullClonePath(x, outputDir)).Select(x => x.First()))
            {
                var output = await _gitClone.CloneOrPullAsync(gitCfg, rootPathOverride: outputDir);
                if (output.IsSuccess) continue;
                break;
            }
        }
    }
}
