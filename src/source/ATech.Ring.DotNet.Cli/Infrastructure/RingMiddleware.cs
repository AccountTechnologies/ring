using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Infrastructure
{
    public class RingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly WebsocketsHandler _socketManager;

        public RingMiddleware(RequestDelegate next, WebsocketsHandler broadcast)
        {
            _next = next;
            _socketManager = broadcast;
        }

        public async Task Invoke(HttpContext context)
        {
            var clientId = Guid.Empty;
            try
            {
                if (!await context.ShouldHandle(_next)) return;
                var log = context.Logger();

                const string clientIdKey = "clientId";
                if (!context.Request.Query.ContainsKey(clientIdKey))
                {
                    var errorSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await errorSocket.CloseOutputAsync(WebSocketCloseStatus.ProtocolError, "clientId (uuid) expected in query string.", context.RequestAborted);
                    return;
                    
                }
                if (!Guid.TryParse(context.Request.Query[clientIdKey], out clientId))
                {
                    var errorSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await errorSocket.CloseOutputAsync(WebSocketCloseStatus.ProtocolError, "clientId is not a valid Uuid / Guid.", context.RequestAborted);
                    return;
                }

                using (log.WithProtocolScope(PhaseStatus.OK))
                {
                    log.LogInformation($"Client {clientId} connected");
                    await _socketManager.ListenAsync(clientId, () => context.WebSockets.AcceptWebSocketAsync(), context.RequestAborted);
                }
            }
            catch (OperationCanceledException)
            {
                context.Logger().LogInformation($"Client {clientId} disconnected", clientId);
            }
            catch (WebSocketException ex)
            {
                if (await _socketManager.TryRemoveAsync(clientId) is (true, var ws))
                {
                    ws.Dispose();
                }
                context.Logger().LogInformation($"Client {clientId} disconnected", clientId);
                context.Logger().LogDebug("Exception: {ex}", ex);
            }
            catch (Exception ex)
            {
                context.Logger().LogError("Unhandled: {ex}", ex);
            }
        }
    }
}
