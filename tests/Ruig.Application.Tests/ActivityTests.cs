using Ruig.Domain.Entities;
using Ruig.Domain.Enums;

namespace Ruig.Application.Tests;

public sealed class ActivityTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingPolyline_LeavesMapNull(string? polyline)
    {
        var activity = new Activity(
            athleteId: Guid.NewGuid(),
            externalActivityId: "1",
            name: "Manual entry",
            sport: ActivitySport.Run,
            distanceMeters: 5000,
            movingTimeSeconds: 1800,
            elapsedTimeSeconds: 1900,
            totalElevationGainMeters: 0,
            startedAtUtc: new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero),
            utcOffsetAtStart: TimeSpan.Zero,
            visibility: ActivityVisibility.Everyone,
            deviceName: null,
            externalMapId: null,
            summaryPolyline: polyline);

        Assert.Null(activity.Map);
    }

    [Fact]
    public void Constructor_WithRealPolyline_BuildsMap()
    {
        var activity = new Activity(
            athleteId: Guid.NewGuid(),
            externalActivityId: "1",
            name: "GPS run",
            sport: ActivitySport.Run,
            distanceMeters: 5000,
            movingTimeSeconds: 1800,
            elapsedTimeSeconds: 1900,
            totalElevationGainMeters: 50,
            startedAtUtc: new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero),
            utcOffsetAtStart: TimeSpan.Zero,
            visibility: ActivityVisibility.Everyone,
            deviceName: "Garmin",
            externalMapId: "a123",
            summaryPolyline: "abcXYZ");

        Assert.NotNull(activity.Map);
        Assert.Equal("a123", activity.Map!.ExternalMapId);
        Assert.Equal("abcXYZ", activity.Map.SummaryPolyline);
    }
}
