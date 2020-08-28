using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Windows.Tools
{
    public class GitClone : ITool
    {
        public string ExePath { get; set; } = "git";
        public string[] DefaultArgs { get; set; }
        public ILogger<ITool> Logger { get; }
        public GitClone(ILogger<GitClone> logger) => Logger = logger;

        public async Task<ExecutionInfo> CloneAsync(IFromGit gitCfg)
        {
            if (gitCfg?.SshRepoUrl == null)
            {
                throw new ArgumentNullException(nameof(gitCfg));
            }
            var chunks = gitCfg.SshRepoUrl.Split(":");
            if (chunks.Length != 2)
            {
                throw new InvalidOperationException($"Git Ssh Url is expected to be split by a colon into two parts. {gitCfg.SshRepoUrl}");
            }

            var pathChunks = new List<string> { Path.GetTempPath(), "ring", "repos" };
            var inRepoPath = chunks[1].Replace(".git", "").Split("/");
            pathChunks.AddRange(inRepoPath);
            gitCfg.CloneFullPath ??= Path.Combine(pathChunks.ToArray());

            if (Directory.Exists(gitCfg.CloneFullPath)) 
            {
                var output = await this.RunProcessWaitAsync("-C", gitCfg.CloneFullPath, "status");
                if (output.IsSuccess) {
                    return await this.RunProcessWaitAsync("-C", gitCfg.CloneFullPath, "pull");
                } 
                else 
                {
                    var tryLeft = 2;
                    while (Directory.Exists(gitCfg.CloneFullPath) && tryLeft > 0) 
                    {
                        try
                        {
                            Directory.Delete(gitCfg.CloneFullPath, true);
                            break;
                        } catch(Exception ex)
                        {
                            Logger.LogError(ex, "Could not delete {CloneFullPath}", gitCfg.CloneFullPath);
                            await Task.Delay(4000);
                            tryLeft--;
                        }
                    }
                }
            }
            return await this.RunProcessWaitAsync("clone", gitCfg.SshRepoUrl, gitCfg.CloneFullPath);                
        }
    }
}
