﻿using ATech.Ring.DotNet.Cli.Logging;

namespace ATech.Ring.DotNet.Cli.Infrastructure;

using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Infrastructure.Cli;
using ATech.Ring.Protocol.v2;
using Microsoft.Extensions.Logging;

public class ConsoleClient
{
    private readonly ILogger<ConsoleClient> _logger;
    private readonly ServeOptions _options;
    private Task _clientTask = Task.CompletedTask;
    private ClientWebSocket? _clientSocket;
    private static readonly Guid ClientId = Guid.Parse("842fcc9e-c1bb-420d-b1e7-b3465aafa4e2");
    public ConsoleClient(ILogger<ConsoleClient> logger, ServeOptions options)
    {
        _logger = logger;
        _options = options;
    }

    public Task StartAsync(CancellationToken token)
    {
        if (_options is not ConsoleOptions consoleOpts) return Task.CompletedTask;

        _clientTask = Task.Run(async () =>
        {
            _clientSocket = new ClientWebSocket();
            try
            {
                using (_logger.WithHostScope(Phase.INIT))
                {
                    if (consoleOpts.StartupDelaySeconds > 0)
                    {
                        _logger.LogDebug("Delaying startup by: {StartupDelaySeconds} seconds",
                            consoleOpts.StartupDelaySeconds);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(consoleOpts.StartupDelaySeconds), token);
                await _clientSocket.ConnectAsync(new Uri($"ws://localhost:{_options.Port}/ws?clientId={ClientId}"), token);
                await _clientSocket.SendMessageAsync(new Message(M.LOAD, consoleOpts.WorkspacePath), token);
                await _clientSocket.SendMessageAsync(M.START, token);

            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {exception}", ex);
            }

        }, token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken token)
    {
        try
        {
            if (_options is not ConsoleOptions) return;
            await _clientTask;
            if (_clientSocket is { } s) await s.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "terminating", token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Console client terminating");
        }
    }
}
