using System.Text.Json.Serialization;

namespace Ruig.Infrastructure.Strava
{
    internal sealed record ActivityDto
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("sport_type")]
        public string? SportType { get; init; }

        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("distance")]
        public double? DistanceMeters { get; init; }

        [JsonPropertyName("moving_time")]
        public int? MovingTimeSeconds { get; init; }

        [JsonPropertyName("elapsed_time")]
        public int? ElapsedTimeSeconds { get; init; }

        [JsonPropertyName("total_elevation_gain")]
        public double? TotalElevationGainMeters { get; init; }

        [JsonPropertyName("start_date")]
        public string? StartDate { get; init; }

        [JsonPropertyName("start_date_local")]
        public string? StartDateLocal { get; init; }

        [JsonPropertyName("utc_offset")]
        public double? UtcOffsetSeconds { get; init; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; init; }

        [JsonPropertyName("device_name")]
        public string? DeviceName { get; init; }

        [JsonPropertyName("private")]
        public bool? IsPrivate { get; init; }

        [JsonPropertyName("visibility")]
        public string? Visibility { get; init; }

        [JsonPropertyName("map")]
        public ActivityMapDto? Map { get; init; }
    }
}
