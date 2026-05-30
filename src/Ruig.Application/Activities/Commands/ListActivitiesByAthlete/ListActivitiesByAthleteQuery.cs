using Ruig.Application.Common.Dispatching;
using Ruig.Application.Common.Models;
using System;

namespace Ruig.Application.Activities.Commands.ListActivitiesByAthlete
{
    public sealed record ListActivitiesByAthleteQuery(
        Guid AthleteId,
        int Page = 1,
        int PageSize = 50,
        DateTime? FromUtc = null,
        DateTime? ToUtc = null) : IRuigRequest<PagedResult<ListActivitiesByAthleteDto>>;
}
