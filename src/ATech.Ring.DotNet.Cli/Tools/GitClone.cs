using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
            _ringCfg = ringCfg?.Value ?? throw new NullReferenceException(nameof(ringCfg.Value));
            Logger = logger;
        }

        public string ResolveFullClonePath(IFromGit gitCfg, string rootPathOverride = null)
        {
            if (gitCfg == null) throw new ArgumentNullException(nameof(gitCfg));
            if (gitCfg.SshRepoUrl == null) throw new NullReferenceException(nameof(gitCfg.SshRepoUrl));

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

        public async Task<ExecutionInfo> CloneOrPullAsync(IFromGit gitCfg, CancellationToken token, bool shallow = false, bool defaultBranchOnly = false, string rootPathOverride = null)
        {
            using var _ = Logger.WithScope(gitCfg.SshRepoUrl, Phase.GIT);
            var depthArg = shallow ? "--depth=1" : "";
            var singleBranchArg = defaultBranchOnly ? "--single-branch" : "";
            var cloneFullPath = ResolveFullClonePath(gitCfg, rootPathOverride);

            async Task<ExecutionInfo> CloneAsync()
            {
                Logger.LogDebug("Cloning to {OutputPath}", cloneFullPath);

                return await this.TryAsync(3, TimeSpan.FromSeconds(10),
                    async t =>
                    {
                        var result = await t.RunProcessWaitAsync("clone", singleBranchArg, depthArg, "--", gitCfg.SshRepoUrl, cloneFullPath);
                        Logger.LogInformation(result.IsSuccess ? PhaseStatus.OK : PhaseStatus.FAILED);
                        return result;
                    }, token);
            }

            if (!Directory.Exists(cloneFullPath)) return await CloneAsync();

            var output = await this.RunProcessWaitAsync("-C", cloneFullPath, "status");
            if (output.IsSuccess)
            {
                Logger.LogInformation("Pulling at {OutputPath}", cloneFullPath);
                return await this.TryAsync(3, TimeSpan.FromSeconds(10),
                    async t =>
                    {
                        var result = await t.RunProcessWaitAsync("-C", cloneFullPath, "pull", depthArg);
                        Logger.LogInformation(result.IsSuccess ? PhaseStatus.OK : PhaseStatus.FAILED);
                        return result;
                    }, token);
            }

            var tryLeft = 3;
            while (Directory.Exists(cloneFullPath) && tryLeft > 0)
            {
                try
                {
                    Logger.LogInformation("Deleting an invalid clone at {OutputPath}", cloneFullPath);
                    SafeDelete(cloneFullPath);
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Could not delete {CloneFullPath}", cloneFullPath);
                    await Task.Delay(TimeSpan.FromSeconds(10), token);
                    tryLeft--;
                }
            }
            return await CloneAsync();
        }

        // Git process does the same thing as libgit2sharp https://github.com/libgit2/libgit2sharp/issues/1354
        private static void SafeDelete(string dir)
        {
            foreach (var subdirectory in Directory.EnumerateDirectories(dir))
            {
                SafeDelete(subdirectory);
            }

            foreach (var fileName in Directory.EnumerateFiles(dir))
            {
                var fileInfo = new FileInfo(fileName)
                {
                    Attributes = FileAttributes.Normal
                };
                fileInfo.Delete();
            }
            Directory.Delete(dir);
        }
    }
}
