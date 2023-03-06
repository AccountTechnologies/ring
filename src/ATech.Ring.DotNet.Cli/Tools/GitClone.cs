using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using ATech.Ring.DotNet.Cli.Infrastructure;
using ATech.Ring.DotNet.Cli.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ATech.Ring.DotNet.Cli.Tools;

public class GitClone : ITool
{
    private readonly RingConfiguration _ringCfg;
    public string Command { get; set; } = "git";
    public string[] DefaultArgs { get; set; } = Array.Empty<string>();
    public ILogger<ITool> Logger { get; }
    public GitClone(ILogger<GitClone> logger, IOptions<RingConfiguration> ringCfg)
    {
        _ringCfg = ringCfg?.Value ?? throw new NullReferenceException(nameof(ringCfg.Value));
        Logger = logger;
    }

    public string ResolveFullClonePath(IFromGit gitCfg, string? rootPathOverride = null)
    {
        if (gitCfg == null) throw new ArgumentNullException(nameof(gitCfg));
        if (gitCfg.SshRepoUrl == null) throw new NullReferenceException(nameof(gitCfg.SshRepoUrl));

        var chunks = gitCfg.SshRepoUrl.Split(":");

        if (chunks.Length != 2)
            throw new InvalidOperationException(
                $"Git Ssh Url is expected to be split by a colon into two parts. {gitCfg.SshRepoUrl}");

        var pathChunks = new List<string> { rootPathOverride ?? _ringCfg.Git.ClonePath };
        var inRepoPath = chunks[1].Replace(".git", "").Split("/");
        pathChunks.AddRange(inRepoPath);
        var targetPath = Path.Combine(pathChunks.ToArray());
        targetPath = Env.ExpandPath(targetPath);
        return Path.IsPathRooted(targetPath) ? targetPath : Path.GetFullPath(targetPath);
    }

    private Func<CancellationToken,Task<ExecutionInfo>> Git(params string[] args)
    {
        return (token) => this.TryAsync(3, TimeSpan.FromSeconds(10), t => t.RunProcessWaitAsync(args, token), token);
    }

    public async Task<ExecutionInfo> CloneOrPullAsync(IFromGit gitCfg, CancellationToken token, bool shallow = false, bool defaultBranchOnly = false, string? rootPathOverride = null)
    {
        using var _ = Logger.WithScope(gitCfg.SshRepoUrl, Phase.GIT);
        var depthArg = shallow ? "--depth=1" : "";
        var singleBranchArg = defaultBranchOnly ? "--single-branch" : "";
        var repoFullPath = ResolveFullClonePath(gitCfg, rootPathOverride);

        async Task<ExecutionInfo> CloneAsync()
        {
            Logger.LogDebug("Cloning to {OutputPath}", repoFullPath);
            var result = await Git("clone", singleBranchArg, depthArg, "--", gitCfg.SshRepoUrl, repoFullPath)(token);
            Logger.LogInformation(result.IsSuccess ? PhaseStatus.OK : PhaseStatus.FAILED);
            return result;
        }

        if (!Directory.Exists(repoFullPath)) return await CloneAsync();

        var output = await Git("-C", repoFullPath, "status", "--short", "--branch")(token);
       
        if (output.IsSuccess)
        {
            Logger.LogInformation("Updating repository at {OutputPath}", repoFullPath);

            if (shallow)
            {
                var remoteBranchName = Regex.Match(output.Output, @".*\.\.\.([^\s]+).*");
                if (remoteBranchName.Success)
                {
                    await Git("-C", repoFullPath, "fetch", depthArg)(token);
                    await Git("-C", repoFullPath, "reset", "--hard", remoteBranchName.Groups[1].Value)(token);
                    return await Git("-C", repoFullPath, "clean", "-fdx")(token);
                }
                throw new InvalidOperationException($"Could not get branch name from git status output: {output.Output}");
            }

            var result = await Git("-C", repoFullPath, "pull", depthArg)(token);
            Logger.LogInformation(result.IsSuccess ? PhaseStatus.OK : PhaseStatus.FAILED);
            return result;
        }

        var tryLeft = 3;
        while (Directory.Exists(repoFullPath) && tryLeft > 0)
        {
            try
            {
                Logger.LogInformation("Deleting an invalid clone at {OutputPath}", repoFullPath);
                SafeDelete(repoFullPath);
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Could not delete {CloneFullPath}", repoFullPath);
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
