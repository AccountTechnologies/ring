using ATech.Ring.Vsix.Interfaces;
using Microsoft.VisualStudio.Threading;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using ATech.Ring.Protocol.Events;
using ATech.Ring.Vsix.Components;

namespace ATech.Ring.Vsix
{
    public class RingManager : IDisposable
    {
        private readonly IRingClient _ringClient;
        private readonly Action<string> _writeLog;
        private const string ProcessName = "ring";
        private Task _clientTask;
        private CancellationTokenSource _cts;

        public RingManager(IRingClient ringClient, Action<string> writeLog)
        {
            _ringClient = ringClient;
            _writeLog = writeLog;
        }

        public async Task ConnectAsync(Func<IRingEvent, Task> dispatchAsync, CancellationToken token)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            await EnsureServerAsync(_cts.Token);
            await SetupClientAsync(dispatchAsync, token);
        }

        public async Task DisconnectAsync() => await _ringClient.DisconnectAsync();
        public async Task DestroyAsync()
        {
            if (_clientTask == null) return;
            await _clientTask;
        }

        public void Dispose()
        {
            if (_ringClient is IDisposable d) d.Dispose();
        }

        private async Task SetupClientAsync(Func<IRingEvent, Task> dispatchAsync, CancellationToken token)
        {
            try
            {
                var attempt = 0;
                do
                {
                    try
                    {
                        _clientTask = await _ringClient.ConnectAsync(dispatchAsync, token);
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        _writeLog("Dispatched cancelled");
                    }
                    catch (Exception ex)
                    {
                        _cts.Cancel();
                        _writeLog($"Connecting... #{attempt}");
                        Debug.WriteLine(ex);
                        attempt++;
                    }

                } while (attempt < 3);
            }
            catch (Exception ex)
            {
                _writeLog($"Exception: {ex}");
            }
        }

        public async Task EnsureServerAsync(CancellationToken token)
        {
            var ringProcess = Process.GetProcessesByName(ProcessName).SingleOrDefault();
            if (ringProcess != null) return;

            try
            {
#if DEBUG
#else
                ProcessExtensions.TerminateIISExpress(_writeLog);
#endif
                var s = new ProcessStartInfo
                {
#if DEBUG
                    FileName = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\ATech.Ring.DotNet.Cli\bin\Debug\win7-x64\publish\ring.exe")),
                    Arguments = @"headless -d",
#else
                    FileName = ProcessName,
                    Arguments = "headless",
#endif
                    CreateNoWindow = false,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Minimized
                };

                var p = Process.Start(s);
                if (p == null) throw new Exception("Could not start ring");
                const int maxStartupSeconds = 10;
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(maxStartupSeconds));
                try
                {
                    if (await p.WaitForExitAsync(cts.Token) != 0)
                    {
                        _writeLog($"ring server failed to start. Exit code: {p.ExitCode}");
                    }
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("Ring is running ok.");
                }
            }
            catch (Win32Exception wx)
            {
                _writeLog($"Looks like ring.exe might be missing. Exception: {wx}");

            }
            catch (Exception ex)
            {
                _writeLog($"Unexpected boom: {ex}");
            }
        }
    }
}