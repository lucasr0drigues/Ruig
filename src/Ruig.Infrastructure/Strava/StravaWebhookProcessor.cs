using Microsoft.EntityFrameworkCore;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Domain.Entities;
using Ruig.Infrastructure.Common.Persistance;
using System.Globalization;
using System.Text.Json;

namespace Ruig.Infrastructure.Strava
{
    public sealed class StravaWebhookProcessor
    {
        private const int BatchSize = 25;

        private readonly AppDbContext _dbContext;
        private readonly IAthleteRepository _athleteRepository;
        private readonly IStravaActivitySyncService _activitySyncService;
        private readonly IStravaTokenStore _tokenStore;
        private readonly IDateTimeProvider _dateTimeProvider;

        public StravaWebhookProcessor(
            AppDbContext dbContext,
            IAthleteRepository athleteRepository,
            IStravaActivitySyncService activitySyncService,
            IStravaTokenStore tokenStore,
            IDateTimeProvider dateTimeProvider)
        {
            _dbContext = dbContext;
            _athleteRepository = athleteRepository;
            _activitySyncService = activitySyncService;
            _tokenStore = tokenStore;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
        {
            var events = await _dbContext.StravaWebhookEvents
                .Where(e => e.ProcessedAtUtc == null)
                .OrderBy(e => e.EventTimeUtc)
                .ThenBy(e => e.ReceivedAtUtc)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            foreach (var webhookEvent in events)
            {
                await ProcessEventAsync(webhookEvent, cancellationToken);
            }

            return events.Count;
        }

        private async Task ProcessEventAsync(StravaWebhookEvent webhookEvent, CancellationToken cancellationToken)
        {
            try
            {
                if (IsActivityEvent(webhookEvent))
                {
                    await ProcessActivityEventAsync(webhookEvent, cancellationToken);
                }
                else if (IsAthleteDeauthorizationEvent(webhookEvent))
                {
                    await _tokenStore.RevokeByStravaAthleteIdAsync(
                        webhookEvent.ObjectId,
                        GetUtcNow(),
                        cancellationToken);
                }

                webhookEvent.ProcessedAtUtc = GetUtcNow();
                webhookEvent.ProcessingError = null;
            }
            catch (Exception ex)
            {
                webhookEvent.ProcessingError = ex.Message;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task ProcessActivityEventAsync(StravaWebhookEvent webhookEvent, CancellationToken cancellationToken)
        {
            var athlete = await _athleteRepository.GetByExternalIdAsync(
                webhookEvent.OwnerId.ToString(CultureInfo.InvariantCulture),
                cancellationToken);

            if (athlete is null)
                return;

            if (IsDeleteEvent(webhookEvent) || IsPrivateUpdate(webhookEvent))
            {
                await _activitySyncService.MarkActivityDeletedAsync(athlete.Id, webhookEvent.ObjectId, cancellationToken);
                return;
            }

            if (IsCreateEvent(webhookEvent) || IsUpdateEvent(webhookEvent))
                await _activitySyncService.SyncActivityAsync(athlete.Id, webhookEvent.ObjectId, cancellationToken);
        }

        private static bool IsActivityEvent(StravaWebhookEvent webhookEvent)
        {
            return string.Equals(webhookEvent.ObjectType, "activity", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAthleteDeauthorizationEvent(StravaWebhookEvent webhookEvent)
        {
            if (!string.Equals(webhookEvent.ObjectType, "athlete", StringComparison.OrdinalIgnoreCase))
                return false;

            var updates = GetUpdates(webhookEvent);
            return updates.TryGetValue("authorized", out var authorized) &&
                   string.Equals(authorized, "false", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCreateEvent(StravaWebhookEvent webhookEvent)
        {
            return string.Equals(webhookEvent.AspectType, "create", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUpdateEvent(StravaWebhookEvent webhookEvent)
        {
            return string.Equals(webhookEvent.AspectType, "update", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDeleteEvent(StravaWebhookEvent webhookEvent)
        {
            return string.Equals(webhookEvent.AspectType, "delete", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPrivateUpdate(StravaWebhookEvent webhookEvent)
        {
            if (!IsUpdateEvent(webhookEvent))
                return false;

            var updates = GetUpdates(webhookEvent);
            return updates.TryGetValue("private", out var isPrivate) &&
                   string.Equals(isPrivate, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, string> GetUpdates(StravaWebhookEvent webhookEvent)
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(webhookEvent.UpdatesJson) ?? [];
        }

        private DateTimeOffset GetUtcNow()
        {
            var utcNow = _dateTimeProvider.UtcNow.Kind == DateTimeKind.Utc
                ? _dateTimeProvider.UtcNow
                : DateTime.SpecifyKind(_dateTimeProvider.UtcNow, DateTimeKind.Utc);

            return new DateTimeOffset(utcNow);
        }
    }
}
