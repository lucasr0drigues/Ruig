using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using Ruig.Infrastructure.Common.Persistance;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Ruig.Infrastructure.Strava
{
    public sealed class StravaWebhookEventStore : IStravaWebhookEventStore
    {
        private readonly AppDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;

        public StravaWebhookEventStore(AppDbContext dbContext, IDateTimeProvider dateTimeProvider)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task SaveAsync(StravaWebhookEventMessage message, CancellationToken cancellationToken)
        {
            var eventTimeUtc = DateTimeOffset.FromUnixTimeSeconds(message.EventTimeUnixSeconds);

            if (await EventExistsAsync(message, eventTimeUtc, cancellationToken))
                return;

            var webhookEvent = new StravaWebhookEvent
            {
                Id = Guid.NewGuid(),
                ObjectType = message.ObjectType,
                ObjectId = message.ObjectId,
                AspectType = message.AspectType,
                OwnerId = message.OwnerId,
                SubscriptionId = message.SubscriptionId,
                EventTimeUtc = eventTimeUtc,
                UpdatesJson = JsonSerializer.Serialize(message.Updates),
                ReceivedAtUtc = GetUtcNow()
            };

            await _dbContext.StravaWebhookEvents.AddAsync(webhookEvent, cancellationToken);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _dbContext.ChangeTracker.Clear();
            }
        }

        private Task<bool> EventExistsAsync(
            StravaWebhookEventMessage message,
            DateTimeOffset eventTimeUtc,
            CancellationToken cancellationToken)
        {
            return _dbContext.StravaWebhookEvents.AnyAsync(
                e => e.ObjectType == message.ObjectType
                    && e.ObjectId == message.ObjectId
                    && e.AspectType == message.AspectType
                    && e.EventTimeUtc == eventTimeUtc,
                cancellationToken);
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            for (var current = ex.InnerException; current is not null; current = current.InnerException)
            {
                var sqlState = current.GetType().GetProperty("SqlState")?.GetValue(current) as string;
                if (string.Equals(sqlState, "23505", StringComparison.Ordinal))
                    return true;
            }

            return false;
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
