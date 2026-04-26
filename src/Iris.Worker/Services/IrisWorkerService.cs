using Microsoft.Extensions.Hosting;

namespace Iris.Worker.Services
{
    internal sealed class IrisWorkerService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
    }
}
