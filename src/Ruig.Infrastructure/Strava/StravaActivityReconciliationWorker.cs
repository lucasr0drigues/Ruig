using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ruig.Infrastructure.Strava
{
    public sealed class StravaActivityReconciliationWorker : BackgroundService
    {
        private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan Delay = TimeSpan.FromDays(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StravaActivityReconciliationWorker> _logger;

        public StravaActivityReconciliationWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<StravaActivityReconciliationWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(InitialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var reconciliationService = scope.ServiceProvider.GetRequiredService<StravaActivityReconciliationService>();
                    var syncedAthletes = await reconciliationService.ReconcileAsync(stoppingToken);

                    _logger.LogInformation("Reconciled Strava activities for {SyncedAthletes} athletes.", syncedAthletes);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reconcile Strava activities.");
                }

                await Task.Delay(Delay, stoppingToken);
            }
        }
    }
}
