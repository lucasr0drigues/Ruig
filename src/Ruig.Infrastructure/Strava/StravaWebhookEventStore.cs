using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using Ruig.Infrastructure.Common.Persistance;
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
            var webhookEvent = new StravaWebhookEvent
            {
                Id = Guid.NewGuid(),
                ObjectType = message.ObjectType,
                ObjectId = message.ObjectId,
                AspectType = message.AspectType,
                OwnerId = message.OwnerId,
                SubscriptionId = message.SubscriptionId,
                EventTimeUtc = DateTimeOffset.FromUnixTimeSeconds(message.EventTimeUnixSeconds),
                UpdatesJson = JsonSerializer.Serialize(message.Updates),
                ReceivedAtUtc = GetUtcNow()
            };

            await _dbContext.StravaWebhookEvents.AddAsync(webhookEvent, cancellationToken);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                _dbContext.ChangeTracker.Clear();
            }
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
