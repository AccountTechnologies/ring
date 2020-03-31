using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using ATech.Ring.Vsix.Interfaces;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Vsix.Client.Commands;
using Task = System.Threading.Tasks.Task;

namespace ATech.Ring.Vsix.Client
{
    public class RingClient : IRingClient, IDisposable
    {
        private readonly Uri _ringServerUri;
        private readonly IReceiver<IRingCommand> _receiver;
        private readonly Action<string> _logger;
        private ClientWebSocket _socket = new ClientWebSocket();
        private CancellationTokenSource _cts;
        private bool _isConnected;

        public RingClient(Uri ringServerUri, IReceiver<IRingCommand> receiver, Action<string> logger)
        {
            _ringServerUri = ringServerUri;
            _receiver = receiver;
            _logger = logger;
        }

        public async Task<Task> ConnectAsync(Func<IRingEvent, Task> dispatchAsync, CancellationToken token)
        {
            try
            {
                _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                _socket = new ClientWebSocket();

                await _socket.ConnectAsync(new Uri(_ringServerUri, $"?clientId={Guid.NewGuid()}"), _cts.Token);

                _isConnected = true;
                return Task.Factory.StartNew(async () =>
                    await Task.WhenAll(ListenAsync(dispatchAsync, _cts.Token),
                                   PublishAsync(_cts.Token)), _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            catch (OperationCanceledException)
            {
                _socket?.Dispose();
                return Task.CompletedTask;
            }
        }

        public async Task DisconnectAsync()
        {
            if (!_isConnected) return;
            await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "disconnecting", _cts.Token);
            _socket?.Dispose();
            _cts.Cancel();
        }

        public void Dispose()
        {
            _socket.Dispose();
        }

        private async Task ListenAsync(Func<IRingEvent, Task> dispatchAsync, CancellationToken token)
        {
            await _socket.ListenAsync(async (message, ct) =>
            {
                try
                {
                    await dispatchAsync(message.AsEvent());
                }
                catch (Exception ex)
                {
                    _logger($"Exception: {ex}");
                    _logger(message.ToString());
                }
            }, token);
        }

        private async Task PublishAsync(CancellationToken token)
        {
            await _socket.PublishAsync((out Message m, CancellationToken ct) =>
            {
                m = Message.Empty;
                var @false = Task.FromResult(false);

                try
                {
                    if (!_receiver.TryDequeue(out var cmd)) return @false;
                    m = cmd.AsMessage();
                    return Task.FromResult(true);
                }
                catch (Exception ex)
                {
                    _logger($"Exception: {ex}");
                    return @false;
                }
            }, token);
        }
    }
}