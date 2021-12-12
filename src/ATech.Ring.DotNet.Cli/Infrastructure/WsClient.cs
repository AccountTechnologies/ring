using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Logging;
using ATech.Ring.Protocol;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Infrastructure
{
    public sealed class WsClient : IAsyncDisposable
    {
        private readonly ILogger<WebsocketsHandler> _logger;
        public Task<WebSocket> Ws { get; }
        private Task _backgroundAwaiter = Task.CompletedTask;
        private readonly CancellationTokenSource _localCts = new CancellationTokenSource();
        private ConcurrentQueue<Task<Ack>> _taskQueue = new ConcurrentQueue<Task<Ack>>();
        public WsClient(ILogger<WebsocketsHandler> logger, Task<WebSocket> ws)
        {
            _logger = logger;
            Ws = ws;
        }

        public async Task<bool> IsOpenAsync() => (await Ws).State == WebSocketState.Open;

        public async Task ListenAsync(Func<Message, CancellationToken, Task<Ack>> dispatch, CancellationToken t)
        {
            var aggregatedCts = CancellationTokenSource.CreateLinkedTokenSource(t, _localCts.Token);
            _backgroundAwaiter = Task.Run(async () => 
            {
                while (true) {
                    try 
                    {
                        while (_taskQueue.TryPeek(out var peek) && peek.IsCompleted)
                        {
                            if (_taskQueue.TryDequeue(out var task)) await (await Ws).SendAckAsync(await task, aggregatedCts.Token);
                        }
                        await Task.Delay(500, aggregatedCts.Token);

                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, aggregatedCts.Token);

            await (await Ws).ListenAsync(async (message, token) =>
            {
                try
                {
                    var (type, payload) = message;
                    using (_logger.WithProtocolScope(type))
                    {
                        if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("< {Payload}", payload);

                        var backgroundTask = dispatch(message, token);
                        if (backgroundTask.IsCompleted) await (await Ws).SendAckAsync(await backgroundTask, token);
                        else _taskQueue.Enqueue(backgroundTask);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Listener terminating");
                    _taskQueue.Enqueue(Task.FromResult(Ack.Terminating));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Server error");
                    _taskQueue.Enqueue(Task.FromResult(Ack.ServerError));
                }
            }, aggregatedCts.Token);
        }

        public async ValueTask DisposeAsync()
        {
            _localCts.Cancel();
            await _backgroundAwaiter;
        }
    }
}