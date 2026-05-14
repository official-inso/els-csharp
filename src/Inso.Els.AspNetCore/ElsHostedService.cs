using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Inso.Els.AspNetCore
{
    /// <summary>
    /// Flushes and closes the ELS client when the host shuts down so pending
    /// batches are sent (or buffered to disk) before the process exits.
    /// </summary>
    internal sealed class ElsHostedService : IHostedService
    {
        private readonly IElsClient _client;

        public ElsHostedService(IElsClient client)
        {
            _client = client;
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _client.CloseAsync(cancellationToken);
        }
    }
}
