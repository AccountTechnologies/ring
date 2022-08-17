namespace ATech.Ring.DotNet.Cli.Infrastructure;

using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Logging;
using ATech.Ring.Protocol.v2;
using Microsoft.Extensions.Logging;

public delegate Task<Ack> Dispatch(Message m, CancellationToken t);

public sealed class WsClient : IAsyncDisposable
{
    public Guid Id { get; }
    private readonly ILogger<WebsocketsHandler> _logger;
    private WebSocket Ws { get; }
    private Task _backgroundAwaiter = Task.CompletedTask;
    private readonly CancellationTokenSource _localCts = new();
    private readonly Channel<Task<Ack>> _channel = Channel.CreateUnbounded<Task<Ack>>();
    public WsClient(ILogger<WebsocketsHandler> logger, Guid id, WebSocket ws)
    {
        _logger = logger;
        Id = id;
        Ws = ws;
    }

    public bool IsOpen => Ws.State == WebSocketState.Open;
    public Task SendAsync(Message m)
    {
        try
        {
            if (!_logger.IsEnabled(LogLevel.Debug)) return Ws.SendMessageAsync(m);
            var displayMessage = m.ToString();
            var task = Ws.SendMessageAsync(m);
            _logger.LogDebug("{m} > {id} ({TaskId})", displayMessage, Id, task.Id);
            task.ContinueWith(_ => _logger.LogDebug(">> {m} > {id} ({TaskId})", displayMessage, Id, task.Id), TaskContinuationOptions.OnlyOnRanToCompletion);
            return task;
        }
        catch (WebSocketException wse)
        {
            _logger.LogDebug("Exception {exception}", wse);
            return Task.CompletedTask;
        }
    }

    private async Task AckLongRunning(CancellationToken token)
    {
        try
        {
            while (await _channel.Reader.WaitToReadAsync(token))
            {

                while (_channel.Reader.TryPeek(out var peek))
                {
                    if (peek.IsCompleted)
                    {
                        if (_channel.Reader.TryRead(out var task)) await Ws.SendAckAsync(await task, token);
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(500), token);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
    }

    public async Task ListenAsync(Dispatch dispatch, CancellationToken t)
    {
        try
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(t, _localCts.Token);
            _backgroundAwaiter = Task.Run(() => AckLongRunning(cts.Token), cts.Token);
            await Ws.ListenAsync(YieldOrQueueLongRunning, cts.Token);
            _logger.LogInformation("Client disconnected ({Id}) ({WebSocketState})", Id, Ws.State);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client disconnected ({Id}) ({WebSocketState})", Id, Ws.State);
        }
        catch (WebSocketException wx)
        {
            _logger.LogInformation("Client {Id} aborted the connection.", Id);
            _logger.LogDebug(wx, "Exception details");
        }
        finally
        {
            _logger.LogDebug("Closing websocket ({Id}) ({WebSocketState})", Id, Ws.State);
            await Ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, default);
            _logger.LogDebug("Closed websocket ({Id}) ({WebSocketState})", Id, Ws.State);
        }

        Task<Ack>? YieldOrQueueLongRunning(ref Message message, CancellationToken token)
        {
            try
            {
                var (type, payload) = message;
                using (_logger.WithProtocolScope(type))
                {
                    if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("< {Payload}", payload.AsUtf8String());

                    var backgroundTask = dispatch(message, token);
                    if (backgroundTask.IsCompleted) return backgroundTask;
                    else _channel.Writer.TryWrite(backgroundTask);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Listener terminating");
                _channel.Writer.TryWrite(Task.FromResult(Ack.Terminating));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error");
                _channel.Writer.TryWrite(Task.FromResult(Ack.ServerError));
            }
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _localCts.Cancel();
            await _backgroundAwaiter;
            Ws.Dispose();
        }
        catch (WebSocketException wx)
        {
            _logger.LogDebug(wx, "Error on disposing WsClient");
        }
    }
}
