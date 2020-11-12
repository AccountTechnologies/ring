using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Logging;
using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Infrastructure
{
    public class WebsocketsHandler
    {
        private readonly ConcurrentDictionary<Guid, WsClient> _clients = new ConcurrentDictionary<Guid, WsClient>();
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IReceiver<IRingEvent> _queue;
        private readonly IServer _server;
        private readonly ILogger<WebsocketsHandler> _logger;

        public WebsocketsHandler(IHostApplicationLifetime appLifetime, IReceiver<IRingEvent> queue, IServer server, ILogger<WebsocketsHandler> logger)
        {
            _appLifetime = appLifetime;
            _queue = queue;
            _server = server;
            _logger = logger;
        }

        public async Task InitializeAsync(CancellationToken token)
        {
            try
            {
                using var _ = _logger.WithProtocolScope(PhaseStatus.OK);
                await _server.InitializeAsync(token);

                var messageLoop = Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            var @event = await _queue.WaitForNextAsync(_appLifetime.ApplicationStopping);
                            var m = @event.AsMessage();

                            foreach (var (id, client) in _clients.ToList())
                            {
                                if (await client.IsOpenAsync()) continue;
                                await TryRemoveAsync(id);
                            }

                            await Task.WhenAll(_clients.Select(async x =>
                            {
                                try
                                {
                                    var (id, client) = x;
                                    _logger.LogDebug($"{m} > {id}");
                                    await (await client.Ws).SendMessageAsync(m, default);

                                }
                                catch (WebSocketException wse)
                                {
                                    _logger.LogDebug("Exception {exception}", wse);
                                }
                            }));
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }, _appLifetime.ApplicationStopping);

                _appLifetime.ApplicationStopping.Register(() =>
                {
                    // blocking calls here to force server terminate before Kestrel Websockets die 
                    _server.TerminateAsync(default).GetAwaiter().GetResult();
                    messageLoop.GetAwaiter().GetResult();
                }, true);
                await messageLoop;
            }
            catch (OperationCanceledException)
            {
                using var _ = _logger.WithHostScope(Phase.DESTROY);
                _logger.LogInformation("Shutting down");
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {exception}", ex);
            }
        }

        public async Task ListenAsync(Guid clientId, Func<Task<WebSocket>> createSocket, CancellationToken t)
        {
            var client = GetOrAddAsync(clientId, createSocket);
            await _server.ConnectAsync(t);

            try
            {
                await client.ListenAsync(Dispatch, t);

            }
            catch (WebSocketException wse)
            {
                await TryRemoveAsync(clientId);
                _logger.LogInformation("ClientId {clientId} ({errorCode}) {message}", clientId, wse.WebSocketErrorCode, wse.Message);
                _logger.LogDebug("Exception {wse}", wse);
            }
        }

        private async Task<Ack> Dispatch(Message m, CancellationToken token)
        {
            return m switch
            {
                (M.LOAD, var path) => await _server.LoadAsync(path, token),
                (M.UNLOAD, _) => await _server.UnloadAsync(token),
                (M.TERMINATE, _) => await _server.TerminateAsync(token),
                (M.START, _) => await _server.StartAsync(token),
                (M.STOP, _) => await _server.StopAsync(token),
                (M.RUNNABLE_INCLUDE, var runnableId) => await _server.IncludeAsync(runnableId, token),
                (M.RUNNABLE_EXCLUDE, var runnableId) => await _server.ExcludeAsync(runnableId, token),
                (M.WORKSPACE_INFO_RQ, _) => _server.RequestWorkspaceInfo(),
                (M.PING, _) => Ack.Alive,
                _ => Ack.NotSupported
            };
        }

        public WsClient GetOrAddAsync(Guid key, Func<Task<WebSocket>> create)
        {
            return _clients.GetOrAdd(key, k => new WsClient(_logger, create()));
        }

        public async Task<(bool isSuccess, WebSocket webSocket)> TryRemoveAsync(Guid key)
        {
            var isRemoved = _clients.TryRemove(key, out var client);

            return (isSuccess: isRemoved, isRemoved ? (await client.Ws) : null);
        }
    }
}
