namespace ATech.Ring.DotNet.Cli.Infrastructure;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Logging;
using ATech.Ring.Protocol.v2;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class WebsocketsHandler
{
    private readonly ConcurrentDictionary<Guid, WsClient> _clients = new();
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IReceiver _queue;
    private readonly IServer _server;
    private readonly ILogger<WebsocketsHandler> _logger;

    public WebsocketsHandler(IHostApplicationLifetime appLifetime, IReceiver queue, IServer server, ILogger<WebsocketsHandler> logger)
    {
        _appLifetime = appLifetime;
        _queue = queue;
        _server = server;
        _logger = logger;
    }


    private Task BroadcastAsync((Guid id, WebSocket ws)[] clients, Message m)
    {
        Task SendAsync(WebSocket ws, Guid id, Message m)
        {
            try
            {
                if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("{m} > {id}", m.ToString(), id);
                return ws.SendMessageAsync(m, default);
            }
            catch (WebSocketException wse)
            {
                _logger.LogDebug("Exception {exception}", wse);
                return Task.CompletedTask;
            }
        }

        var tasks = new List<Task>();
        foreach (var (id, ws) in clients)
        {
            tasks.Add(SendAsync(ws, id, m));
        }

        return Task.WhenAll(tasks.ToArray());
    }

    public async Task InitializeAsync(CancellationToken token)
    {
        try
        {
            using var _ = _logger.WithProtocolScope(PhaseStatus.OK);
            await _server.InitializeAsync(token);

            var messageLoop = Task.Run(async () =>
            {
                while (await _queue.WaitToReadAsync(_appLifetime.ApplicationStopping))
                {
                    try
                    {
                        foreach (var (id, client) in _clients.ToList())
                        {
                            if (await client.IsOpenAsync()) continue;
                            await TryRemoveAsync(id);
                        }

                        var all = await Task.WhenAll(_clients.Select(async x =>
                        {
                            var (id, client) = x;
                            return (id, await client.Ws);
                        }));

                        await BroadcastAsync(all, _queue.Dequeue());
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

    private Task<Ack> Dispatch(Message m, CancellationToken token)
    {
        Task<Ack> Dispatch(Message m)
        {
            return m switch
            {
                (M.LOAD, var path) => _server.LoadAsync(path.AsUtf8String(), token),
                (M.UNLOAD, _) => _server.UnloadAsync(token),
                (M.TERMINATE, _) => _server.TerminateAsync(token),
                (M.START, _) => _server.StartAsync(token),
                (M.STOP, _) => _server.StopAsync(token),
                (M.RUNNABLE_INCLUDE, var runnableId) => _server.IncludeAsync(runnableId.AsUtf8String(), token),
                (M.RUNNABLE_EXCLUDE, var runnableId) => _server.ExcludeAsync(runnableId.AsUtf8String(), token),
                (M.WORKSPACE_INFO_RQ, _) => Task.FromResult(_server.RequestWorkspaceInfo()),
                (M.PING, _) => Task.FromResult(Ack.Alive),
                _ => Task.FromResult(Ack.NotSupported)
            };
        }

        return Dispatch(m);
    }

    public WsClient GetOrAddAsync(Guid key, Func<Task<WebSocket>> create)
    {
        return _clients.GetOrAdd(key, k => new WsClient(_logger, create()));
    }

    public async Task<(bool isSuccess, WebSocket webSocket)> TryRemoveAsync(Guid key)
    {
        var isRemoved = _clients.TryRemove(key, out var client);
        if (isRemoved) await client.DisposeAsync();
        return (isSuccess: isRemoved, isRemoved ? (await client.Ws) : null);
    }
}
