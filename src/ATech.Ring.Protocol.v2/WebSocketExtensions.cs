using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace ATech.Ring.Protocol.v2;
public static class WebSocketExtensions
{

    public static async Task SendAckAsync(this WebSocket s, Ack status, CancellationToken token = default)
        => await s.SendAsync(new byte[] { (byte)status }, WebSocketMessageType.Binary, true, token).ConfigureAwait(false);

    public static Task SendMessageAsync(this WebSocket s, Message m, CancellationToken token = default)
    {
        return s.SendAsync(new ArraySegment<byte>(m.Bytes.ToArray()), WebSocketMessageType.Binary, true, token);
    }

    public static async Task ListenAsync(this WebSocket webSocket, HandleMessage onReceived, CancellationToken token)
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
                    var m = new Message(buffer);
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
