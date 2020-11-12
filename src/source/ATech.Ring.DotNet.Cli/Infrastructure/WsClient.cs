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
    public class WsClient
    {
        private readonly ILogger<WebsocketsHandler> _logger;
        public Task<WebSocket> Ws { get; }
        public WsClient(ILogger<WebsocketsHandler> logger, Task<WebSocket> ws)
        {
            _logger = logger;
            Ws = ws;
        }

        public ConcurrentQueue<Task<Ack>> TaskQueue { get; } = new ConcurrentQueue<Task<Ack>>();

        public async Task<bool> IsOpenAsync() => (await Ws).State == WebSocketState.Open;

        public async Task ListenAsync(Func<Message, CancellationToken, Task<Ack>> dispatch, CancellationToken t)
        {
            await (await Ws).ListenAsync(async (message, token) =>
            {
                try
                {
                    var (type, payload) = message;
                    using (_logger.WithProtocolScope(type))
                    {
                        _logger.LogDebug("< {Payload}", payload);

                        while (TaskQueue.TryPeek(out var peek) && peek.IsCompleted)
                        {
                            if (TaskQueue.TryDequeue(out var task)) await (await Ws).SendAckAsync(await task, token);
                        }

                        var backgroundTask = dispatch(message, token);
                        if (backgroundTask.IsCompleted) await (await Ws).SendAckAsync(await backgroundTask, token);
                        else TaskQueue.Enqueue(backgroundTask);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Listener terminating");
                    TaskQueue.Enqueue(Task.FromResult(Ack.Terminating));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Server error");
                    TaskQueue.Enqueue(Task.FromResult(Ack.ServerError));
                }
            }, t);
        }
    }
}