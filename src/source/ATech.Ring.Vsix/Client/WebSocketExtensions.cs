using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ATech.Ring.Protocol;
using System.Net.WebSockets;
using System.Threading;

namespace ATech.Ring.Vsix.Client
{
    public static class WebSocketExtensions
    {
        public static async System.Threading.Tasks.Task PublishAsync(this WebSocket webSocket, TryGetNextMessage tryGetNextAsync, CancellationToken token = default)
        {
            while (!webSocket.CloseStatus.HasValue && !token.IsCancellationRequested)
            {
                while (await tryGetNextAsync(out var msg, token))
                {
                    try
                    {
                        await webSocket.SendMessageAsync(msg, token);
                    }
                    catch (WebSocketException)
                    {
                        break;
                    }
                }

                await System.Threading.Tasks.Task.Delay(25, token);
            }
        }

        public static async System.Threading.Tasks.Task ListenAsync(this ClientWebSocket webSocket, ClientHandler onReceived, CancellationToken token)
        {
            WebSocketReceiveResult result;
            do
            {
                try
                {
                    using var ms = new MemoryStream(Constants.MaxMessageSize);
                    do
                    {
                        var buffer = new ArraySegment<byte>(new byte[Constants.MaxMessageSize]);
                        result = await webSocket.ReceiveAsync(buffer, token);
                        await ms.WriteAsync(buffer.Array, buffer.Offset, result.Count, token);

                    } while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);
                    await ms.FlushAsync(token);
                    await onReceived(ms.ToArray().AsMemory(), token);
                }
                catch (WebSocketException)
                {
                    break;
                }
            } while (!result.CloseStatus.HasValue && !token.IsCancellationRequested);
        }
    }
    public delegate System.Threading.Tasks.Task<bool> TryGetNextMessage(out Message message, CancellationToken token);
    public delegate System.Threading.Tasks.Task ClientHandler(Message message, CancellationToken token);
}