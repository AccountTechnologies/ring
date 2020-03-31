using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Logging;
using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Infrastructure
{
    public class WebsocketsHandler
    {
        private readonly ConcurrentDictionary<Guid, Task<WebSocket>> _clients = new ConcurrentDictionary<Guid, Task<WebSocket>>();
        private readonly IReceiver<IRingEvent> _queue;
        private readonly IServer _server;
        private readonly ILogger<WebsocketsHandler> _logger;

        public WebsocketsHandler(IReceiver<IRingEvent> queue, IServer server, ILogger<WebsocketsHandler> logger)
        {
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
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(25, token);
                    if (!_queue.TryDequeue(out var @event)) continue;
                    var m = @event.AsMessage();

                    var all = _clients.ToList();

                    foreach (var (id,task) in all)
                    {
                        var ws = await task;
                        if (ws.State == WebSocketState.Open) continue;
                        await TryRemoveAsync(id);
                    }
                        
                    await Task.WhenAll(_clients.Values.Select(async t =>
                    {
                        var ws = await t;
                        try
                        {
                            _logger.LogDebug($"{m}");
                            await ws.SendMessageAsync(m, token);

                        }
                        catch (WebSocketException wse)
                        {
                            _logger.LogDebug("Exception {exception}", wse);
                        }
                    }));
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Shutting down");
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {exception}", ex);
            }
        }

        public async Task ListenAsync(Guid clientId, Func<Task<WebSocket>> createSocket, CancellationToken t)
        {
            var ws = await GetOrAddAsync(clientId, createSocket);
            await _server.ConnectAsync(t);

            try
            {
                await ws.ListenAsync(async (message, token) =>
                {
                    try
                    {
                        var (type, payload) = message;
                        using (_logger.WithProtocolScope(type))
                        {
                            _logger.LogDebug("{Payload}", payload);
                            return await Dispatch(message, token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Listener terminating");
                        return Ack.Terminating;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Server error");
                        return Ack.ServerError;
                    }
                }, t);

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

        public Task<WebSocket> GetOrAddAsync(Guid key, Func<Task<WebSocket>> create)
        {
            return _clients.GetOrAdd(key, k => create());
        }

        public async Task<(bool isSuccess, WebSocket webSocket)> TryRemoveAsync(Guid key)
        {
            var isRemoved = _clients.TryRemove(key, out var task);
            return (isSuccess: isRemoved, isRemoved ? await task : null);
        }
    }
}
