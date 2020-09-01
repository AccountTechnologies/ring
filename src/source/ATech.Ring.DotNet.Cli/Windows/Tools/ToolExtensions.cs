using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Windows.Tools
{
    public static class ToolExtensions
    {
        public static Task<ExecutionInfo> RunProcessWaitAsync(this ITool tool, params object[] args)
            => tool.RunProcessCoreAsync(args: args, wait: true);

        public static Task<ExecutionInfo> RunProcessAsync(this ITool tool, params object[] args)
            => tool.RunProcessCoreAsync(args);

        public static Task<ExecutionInfo> RunProcessAsync(this ITool tool, string workingDirectory, IDictionary<string, string> envVars, params object[] args)
            => tool.RunProcessCoreAsync(args, envVars: envVars, workingDirectory: workingDirectory);

        public static Task<ExecutionInfo> RunProcessAsync(this ITool tool, Action<string> onErrorData, IDictionary<string, string> envVars, params object[] args)
            => tool.RunProcessCoreAsync(args: args, onErrorData: onErrorData, envVars: envVars);

        private static async Task<ExecutionInfo> RunProcessCoreAsync(this ITool tool,
                                                               IEnumerable<object> args,
                                                               bool wait = false,
                                                               string workingDirectory = null,
                                                               IDictionary<string, string> envVars = null,
                                                               Action<string> onErrorData = null)
        {
            var procUid = Guid.NewGuid().ToString("n").Remove(10);
            try
            {
                var allArgs = string.Join(" ", (tool.DefaultArgs ?? new object[] { }).Concat(args.Select(x => x.ToString())));
                var sb = new StringBuilder();
                void OnData(object _, DataReceivedEventArgs x) => sb.AppendLine(x.Data);
                void OnError(object _, DataReceivedEventArgs x)
                {
                    if (string.IsNullOrWhiteSpace(x.Data)) return;
                    tool.Logger.LogError(x.Data);
                    onErrorData?.Invoke(x.Data);
                }

                var s = new ProcessStartInfo
                {
                    FileName = tool.ExePath,
                    Arguments = allArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                };
                if (envVars != null)
                {
                    foreach (var (key, value) in envVars)
                    {
                        if (s.EnvironmentVariables.ContainsKey(key)) s.EnvironmentVariables[key] = value;
                        else s.EnvironmentVariables.Add(key, value);
                    }
                }

                if (workingDirectory != null) s.WorkingDirectory = workingDirectory;
                var ringWorkingDir = Directory.GetCurrentDirectory();

                tool.Logger.LogDebug("{procUid} - Starting process: {Tool} {Args} ({ProcessWorkingDir})", procUid, tool.ExePath, allArgs, workingDirectory ?? ringWorkingDir);


                var title = Console.Title;
                var p = Process.Start(s);

                if (p == null)
                {
                    tool.Logger.LogError("{procUid} - Process failed: {Tool} {Args} ({ProcessWorkingDir})", procUid, tool.ExePath, allArgs, workingDirectory ?? ringWorkingDir);
                    return new ExecutionInfo();
                }

                p.TrackAsChild();
                p.EnableRaisingEvents = true;

                p.OutputDataReceived += OnData;
                p.ErrorDataReceived += OnError;

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                tool.Logger.LogDebug("{procUid} - Process started: {Pid}", procUid, p.Id);

                var tcs = new TaskCompletionSource<ExecutionInfo>();

                void OnExit(object sender, EventArgs _)
                {
                    if (!(sender is Process e)) return;
                    e.OutputDataReceived -= OnData;
                    e.ErrorDataReceived -= OnError;
                    e.Exited -= OnExit;
                    tcs.SetResult(new ExecutionInfo { Pid = e.Id, Output = sb.ToString().Trim('\r', '\n', ' ', '\t'), ExitCode = e.ExitCode });
                    e.Dispose();
                }

                p.Exited += OnExit;

                ExecutionInfo result;

                if (wait)
                {
                    p.WaitForExit();
                    result = await tcs.Task;
                }
                else
                {
                    result = new ExecutionInfo { Pid = p.Id, Output = sb.ToString().Trim('\r', '\n', ' ', '\t') };
                }

                Console.Title = title;

                return result;
            }
            catch (Exception ex)
            {
                tool.Logger.LogCritical(ex, "{procUid} - Unhandled error when starting process", procUid);
                return new ExecutionInfo();
            }
        }
    }
}