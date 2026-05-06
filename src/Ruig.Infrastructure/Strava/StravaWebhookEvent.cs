namespace Ruig.Infrastructure.Strava
{
    public sealed class StravaWebhookEvent
    {
        public Guid Id { get; set; }
        public string ObjectType { get; set; } = default!;
        public long ObjectId { get; set; }
        public string AspectType { get; set; } = default!;
        public long OwnerId { get; set; }
        public long SubscriptionId { get; set; }
        public DateTimeOffset EventTimeUtc { get; set; }
        public string UpdatesJson { get; set; } = "{}";
        public DateTimeOffset ReceivedAtUtc { get; set; }
        public DateTimeOffset? ProcessedAtUtc { get; set; }
        public string? ProcessingError { get; set; }
    }
}
