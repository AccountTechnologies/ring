using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ATech.Ring.DotNet.Cli.Infrastructure
{
    public class WebsocketsInitializer : IHostedService
    {
        private readonly WebsocketsHandler _handler;
        private Task _messageLoop;
        public WebsocketsInitializer(WebsocketsHandler handler) => _handler = handler;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _messageLoop = _handler.InitializeAsync(cancellationToken);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_messageLoop != null) await _messageLoop;
        }
    }
}