using Ruig.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Activities.Commands.ListActivitiesByAthlete
{
    public sealed record ListActivitiesByAthleteDto(
        Guid ActivityId,
        Guid AthleteId,
        string ExternalActivityId,
        string? Name,
        ActivitySport? Sport,
        DateTimeOffset? StartedAtUtc,
        TimeSpan? UtcOffsetAtStart,
        string? DeviceName);
}

