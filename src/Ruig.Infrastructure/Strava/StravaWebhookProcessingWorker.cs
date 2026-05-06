using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ruig.Infrastructure.Strava
{
    public sealed class StravaWebhookProcessingWorker : BackgroundService
    {
        private static readonly TimeSpan Delay = TimeSpan.FromSeconds(15);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StravaWebhookProcessingWorker> _logger;

        public StravaWebhookProcessingWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<StravaWebhookProcessingWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<StravaWebhookProcessor>();
                    await processor.ProcessPendingAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process Strava webhook events.");
                }

                await Task.Delay(Delay, stoppingToken);
            }
        }
    }
}
