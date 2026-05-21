using Ruig.Domain.Common;
using Ruig.Domain.Entities;

namespace Ruig.Application.Tests;

public sealed class ActivityTests
{
    [Fact]
    public void Constructor_WithLocalDate_StoresActivityDay()
    {
        var athleteId = Guid.NewGuid();
        var localDate = new DateOnly(2026, 5, 7);

        var activity = new Activity(athleteId, localDate);

        Assert.Equal(athleteId, activity.AthleteId);
        Assert.Equal(localDate, activity.LocalDate);
    }

    [Fact]
    public void Constructor_WithMissingLocalDate_Throws()
    {
        Assert.Throws<DomainException>(() => new Activity(Guid.NewGuid(), default));
    }
}
