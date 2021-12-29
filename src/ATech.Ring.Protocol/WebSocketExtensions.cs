using ATech.Ring.Protocol.Events;
using System;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ATech.Ring.Protocol
{
    public static class WebSocketExtensions
    {
        internal static ArraySegment<byte> AsSegment(this Message message)
        {
            MemoryMarshal.TryGetArray<byte>(message, out var segment);
            return segment;
        }

        public static async Task SendAckAsync(this WebSocket s, Ack status, CancellationToken token = default)
            => await s.SendAsync(new AckEvent(status).AsMessage().AsSegment(), WebSocketMessageType.Binary, true, token).ConfigureAwait(false);

        public static async Task SendMessageAsync(this WebSocket s, Message m, CancellationToken token = default)
        {
            await s.SendAsync(m.AsSegment(), WebSocketMessageType.Binary, true, token).ConfigureAwait(false);
        }

        public static async Task ListenAsync(this WebSocket webSocket, Func<Message, CancellationToken, Task> onReceived, CancellationToken token)
        {
            WebSocketReceiveResult result;
            do
            {
                var buffer = new byte[Constants.MaxMessageSize];
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Requested by the client", token);
                    return;
                }
                if (!result.EndOfMessage) await webSocket.SendAckAsync(Ack.ExpectedEndOfMessage, token);

                await onReceived(buffer.AsMemory(), token);

            } while (!result.CloseStatus.HasValue && !token.IsCancellationRequested);
        }
    }
}
