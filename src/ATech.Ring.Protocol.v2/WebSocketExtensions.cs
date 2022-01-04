using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace ATech.Ring.Protocol.v2;
public static class WebSocketExtensions
{
    public static async Task SendAckAsync(this WebSocket s, Ack status, CancellationToken token = default)
    {
        if (s.State != WebSocketState.Open) return;
        await s.SendAsync(new ArraySegment<byte>(new Message(M.ACK, (byte)status).Bytes.ToArray()), WebSocketMessageType.Binary, true, token).ConfigureAwait(false);
    }

    public static Task SendMessageAsync(this WebSocket s, Message m, CancellationToken token = default)
    {
        if (s.State != WebSocketState.Open) return Task.CompletedTask;
        return s.SendAsync(new ArraySegment<byte>(m.Bytes.SliceUntilNull().ToArray()), WebSocketMessageType.Binary, true, token);
    }

    public static async Task ListenAsync(this WebSocket webSocket, HandleMessage onReceived, CancellationToken token=default)
    {
        WebSocketReceiveResult? result = null;
        do
        {
            var buffer = ArrayPool<byte>.Shared.Rent(Constants.MaxMessageSize);
            try
            {
                Array.Clear(buffer);
                result = await webSocket.ReceiveAsync(buffer, token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }
                if (!result.EndOfMessage) await webSocket.SendAckAsync(Ack.ExpectedEndOfMessage, token);

                static Task? OnReceived(ReadOnlySpan<byte> buffer, HandleMessage onReceived, CancellationToken token)
                {
                    var m = new Message(buffer.SliceUntilNull());
                    return onReceived(ref m, token);
                }

                var maybeAck = OnReceived(buffer, onReceived, token);
                if (maybeAck is Task<Ack> ack)
                {
                    await webSocket.SendAckAsync(await ack, token);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, true);
            }

        } while (result is not null && !result.CloseStatus.HasValue && !token.IsCancellationRequested);
    }

    public delegate Task? HandleMessage(ref Message message, CancellationToken token);
}
