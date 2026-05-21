using System.Text.Json.Serialization;

namespace Ruig.Infrastructure.Strava
{
    internal sealed record ActivityDto
    {
        [JsonPropertyName("start_date")]
        public string? StartDate { get; init; }

        [JsonPropertyName("start_date_local")]
        public string? StartDateLocal { get; init; }

        [JsonPropertyName("utc_offset")]
        public double? UtcOffsetSeconds { get; init; }
    }
}
