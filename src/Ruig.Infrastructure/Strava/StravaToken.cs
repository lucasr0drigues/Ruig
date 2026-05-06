using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Infrastructure.Strava
{
    public sealed class StravaToken
    {
        public Guid Id { get; set; }
        public Guid AthleteId { get; set; }
        public long StravaAthleteId { get; set; }
        public string AccessToken { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
        public DateTimeOffset ExpiresAtUtc { get; set; }
        public string Scope { get; set; } = string.Empty;
        public DateTimeOffset? RevokedAtUtc { get; set; }
    }
}
