namespace Ruig.Application.Activities.Commands.ListActivitiesByAthlete
{
    public sealed record ListActivitiesByAthleteDto(
        Guid ActivityId,
        Guid AthleteId,
        DateOnly LocalDate);
}
