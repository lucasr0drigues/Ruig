namespace Ruig.Application.Common.Interfaces.Strava.Models
{
    public sealed record StravaActivityResponse(
        string? StartDate,
        string? StartDateLocal,
        double? UtcOffsetSeconds);
}
