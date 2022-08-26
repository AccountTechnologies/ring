using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using ATech.Ring.DotNet.Cli.Tools.Windows;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Tools;

public static class ToolExtensions
{
    public static async Task<T> TryAsync<T>(int times, TimeSpan backOffInterval, Func<Task<T>> func, Predicate<T> until, CancellationToken token)
        where T: new()
    {
        var result = new T();
        var triesLeft = times;
        while (triesLeft > 0)
        {
            result = await func();
            if (until(result)) return result;
            try
            {
                await Task.Delay(backOffInterval, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            triesLeft--;
        }

        return result;
    }

    public static async Task<ExecutionInfo> TryAsync(this ITool t, int times, TimeSpan backOffInterval,
        Func<ITool, Task<ExecutionInfo>> func, CancellationToken token)
        => await TryAsync(times, backOffInterval, () => func(t), r => r.IsSuccess, token);

    public static Task<ExecutionInfo> RunProcessWaitAsync(this ITool tool, CancellationToken token)
        => tool.RunProcessCoreAsync(args: null, wait: true, token: token);

    public static Task<ExecutionInfo> RunProcessWaitAsync(this ITool tool, object[] args, CancellationToken token)
        => tool.RunProcessCoreAsync(args: args, wait: true, token: token);

    public static Task<ExecutionInfo> RunProcessAsync(this ITool tool, object[] args, CancellationToken token)
        => tool.RunProcessCoreAsync(args, token: token);

    public static Task<ExecutionInfo> RunProcessAsync(this ITool tool, string workingDirectory, IDictionary<string, string>? envVars, object[]? args, CancellationToken token)
        => tool.RunProcessCoreAsync(args, envVars: envVars, workingDirectory: workingDirectory, token: token);

    public static Task<ExecutionInfo> RunProcessAsync(this ITool tool, Action<string> onErrorData, IDictionary<string, string>? envVars, object[]? args, CancellationToken token)
        => tool.RunProcessCoreAsync(args: args, onErrorData: onErrorData, envVars: envVars, token: token);

    private static async Task<ExecutionInfo> RunProcessCoreAsync(this ITool tool,
        IEnumerable<object>? args,
        bool wait = false,
        string? workingDirectory = null,
        IDictionary<string, string>? envVars = null,
        Action<string>? onErrorData = null, CancellationToken token=default)
    {
        var procUid = Guid.NewGuid().ToString("n").Remove(10);
        try
        {
            var allArgs = string.Join(" ", (tool.DefaultArgs ?? Array.Empty<object>()).Concat((args ?? Array.Empty<object>()).Select(x => x.ToString())));
            var sb = new StringBuilder();
            void OnData(object _, DataReceivedEventArgs x) => sb.AppendLine(x.Data);
            void OnError(object _, DataReceivedEventArgs x)
            {
                if (string.IsNullOrWhiteSpace(x.Data)) return;
                tool.Logger.LogInformation(x.Data);
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

            var p = Process.Start(s);

            if (p == null)
            {
                tool.Logger.LogError("{procUid} - Process failed: {Tool} {Args} ({ProcessWorkingDir})", procUid, tool.ExePath, allArgs, workingDirectory ?? ringWorkingDir);
                return new ExecutionInfo();
            }
               
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                p.TrackAsChild();
            }
            else
            {
                // not sure yet what can be done for Linux/Darwin to support it
            }
               
            p.EnableRaisingEvents = true;
            p.OutputDataReceived += OnData;
            p.ErrorDataReceived += OnError;

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            tool.Logger.LogDebug("{procUid} - Process started: {Pid}", procUid, p.Id);

            var tcs = new TaskCompletionSource<ExecutionInfo>();

            token.Register(() =>
            {
                if (tcs.TrySetCanceled())
                {
                    try
                    {
                        p.Kill();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                }
            });

            void OnExit(object? sender, EventArgs _)
            {
                if (sender is not Process e) return;
                e.OutputDataReceived -= OnData;
                e.ErrorDataReceived -= OnError;
                e.Exited -= OnExit;
                tcs.TrySetResult(new ExecutionInfo(e.Id, e.ExitCode, sb.ToString().Trim('\r', '\n', ' ', '\t')));
                e.Dispose();
            }

            p.Exited += OnExit;

            ExecutionInfo result;

            if (wait)
            {
                result = await tcs.Task;
            }
            else
            {
                result = new ExecutionInfo(p.Id, null, sb.ToString().Trim('\r', '\n', ' ', '\t'));
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            tool.Logger.LogInformation("Forcefully terminated process {procUid}", procUid);
            return ExecutionInfo.Empty;
        }
        catch (Exception ex)
        {
            tool.Logger.LogCritical(ex, "{procUid} - Unhandled error when starting process", procUid);
            return ExecutionInfo.Empty;
        }
    }
}
