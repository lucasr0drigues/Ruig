using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Common.Interfaces.Strava.Models
{
    public sealed record StravaWebhookEventMessage(
        string ObjectType,
        long ObjectId,
        string AspectType,
        long OwnerId,
        long SubscriptionId,
        long EventTimeUnixSeconds,
        IReadOnlyDictionary<string, string> Updates);
}
