namespace ATech.Ring.DotNet.Cli.Infrastructure;

using System;
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
    private readonly HashSet<WsClient> _clients = new(new WsClientEqualityComparer());
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

    private Task BroadcastAsync(Message m)
    {
        var tasks = new List<Task>();
        foreach (var client in _clients.Where(c => c.IsOpen))
        {
            tasks.Add(client.SendAsync(m));
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
                while (await _queue.WaitToReadAsync(_appLifetime.ApplicationStopped))
                {
                    try
                    {
                        await _queue.DequeueAsync(BroadcastAsync);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, _appLifetime.ApplicationStopping);

            _appLifetime.ApplicationStopping.Register(async () =>
            {
                await _server.TerminateAsync(default);
                _queue.Complete();
                await messageLoop;
                foreach (var c in _clients)
                {
                    await c.DisposeAsync();
                }
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
        WsClient? client = null;
        client = await CreateClient(clientId, await createSocket());
        await _server.ConnectAsync(t);
        await client.ListenAsync(Dispatch, t);
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

    public async Task<WsClient> CreateClient(Guid key, WebSocket socket)
    {
        foreach (var c in _clients.Where(x => !x.IsOpen))
        {
            _clients.Remove(c);
            await c.DisposeAsync();
        }
        var wsClient = new WsClient(_logger, key, socket);
        if (!_clients.Add(wsClient)) throw new InvalidOperationException($"Client already exists: {key}");
        return wsClient;
    }
}
