using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Infrastructure.Cli;
using ATech.Ring.Protocol;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Infrastructure
{
    public class ConsoleClient : IHostedService
    {
        private readonly ILogger<ConsoleClient> _logger;
        private readonly BaseOptions _options;
        private Task _clientTask = Task.CompletedTask;
        private ClientWebSocket _clientSocket;
        private static readonly Guid ClientId = Guid.Parse("842fcc9e-c1bb-420d-b1e7-b3465aafa4e2");
        public ConsoleClient(ILogger<ConsoleClient> logger, BaseOptions options)
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
                    await _clientSocket.ConnectAsync(new Uri($"ws://localhost:{_options.Port}/ws?clientId={ClientId}"), token);
                    await _clientSocket.SendMessageAsync(Message.FromString(M.LOAD, consoleOpts.WorkspacePath), token);
                    await _clientSocket.SendMessageAsync(Message.From(M.START), token);

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Exception: {ex}");
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
                await _clientSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "terminating", token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Console client terminating");
            }
        }
    }
}
