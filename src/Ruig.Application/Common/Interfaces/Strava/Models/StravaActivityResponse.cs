using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Common.Interfaces.Strava.Models
{
    public sealed record StravaActivityResponse(
        long Id,
        string? Name,
        string? SportType,
        double? DistanceMeters,
        int? MovingTimeSeconds,
        int? ElapsedTimeSeconds,
        double? TotalElevationGainMeters,
        string? StartDate,
        string? StartDateLocal,
        double? UtcOffsetSeconds,
        string? Timezone,
        string? DeviceName,
        bool? IsPrivate,
        string? Visibility,
        StravaActivityMapResponse? Map);
}
