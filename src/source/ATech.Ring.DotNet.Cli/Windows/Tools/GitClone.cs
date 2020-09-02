using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using ATech.Ring.DotNet.Cli.Infrastructure;
using ATech.Ring.DotNet.Cli.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ATech.Ring.DotNet.Cli.Windows.Tools
{
    public class GitClone : ITool
    {
        private readonly RingConfiguration _ringCfg;
        public string ExePath { get; set; } = "git";
        public string[] DefaultArgs { get; set; }
        public ILogger<ITool> Logger { get; }
        public GitClone(ILogger<GitClone> logger, IOptions<RingConfiguration> ringCfg)
        {
            _ringCfg = ringCfg?.Value ?? throw new ArgumentNullException(nameof(ringCfg.Value));
            Logger = logger;
        }

        public string ResolveFullClonePath(IFromGit gitCfg, string rootPathOverride = null)
        {
            if (gitCfg == null) throw new ArgumentNullException(nameof(gitCfg));
            if (gitCfg.SshRepoUrl == null) throw new ArgumentNullException(nameof(gitCfg.SshRepoUrl));

            var chunks = gitCfg.SshRepoUrl.Split(":");

            if (chunks.Length != 2)
                throw new InvalidOperationException(
                    $"Git Ssh Url is expected to be split by a colon into two parts. {gitCfg.SshRepoUrl}");

            var pathChunks = new List<string> { rootPathOverride ?? _ringCfg.GitCloneRootPath };
            var inRepoPath = chunks[1].Replace(".git", "").Split("/");
            pathChunks.AddRange(inRepoPath);
            var targetPath = Environment.ExpandEnvironmentVariables(Path.Combine(pathChunks.ToArray()));
            return Path.IsPathRooted(targetPath) ? targetPath : Path.GetFullPath(targetPath);
        }

        public async Task<ExecutionInfo> CloneOrPullAsync(IFromGit gitCfg, string rootPathOverride = null)
        {
            using var _ = Logger.WithScope(gitCfg.SshRepoUrl, Phase.GIT);

            var cloneFullPath = ResolveFullClonePath(gitCfg, rootPathOverride);

            if (!Directory.Exists(cloneFullPath))
            {
                Logger.LogDebug("Cloning to {OutputPath}", cloneFullPath);
                var result = await this.RunProcessWaitAsync("clone", gitCfg.SshRepoUrl, cloneFullPath);
                Logger.LogInformation(result.IsSuccess ? PhaseStatus.OK : PhaseStatus.FAILED);
                return result;
            }

            var output = await this.RunProcessWaitAsync("-C", cloneFullPath, "status");
            if (output.IsSuccess)
            {
                Logger.LogInformation("Pulling at {OutputPath}", cloneFullPath);
                var result = await this.RunProcessWaitAsync("-C", cloneFullPath, "pull");
                Logger.LogInformation(result.IsSuccess ? PhaseStatus.OK : PhaseStatus.FAILED);
                return result;
            }

            var tryLeft = 2;
            while (Directory.Exists(cloneFullPath) && tryLeft > 0)
            {
                try
                {
                    Logger.LogInformation("Deleting an invalid clone at {OutputPath}", cloneFullPath);
                    Directory.Delete(cloneFullPath, true);
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Could not delete {CloneFullPath}", cloneFullPath);
                    await Task.Delay(4000);
                    tryLeft--;
                }
            }
            return await this.RunProcessWaitAsync("clone", gitCfg.SshRepoUrl, cloneFullPath);
        }
    }
}
