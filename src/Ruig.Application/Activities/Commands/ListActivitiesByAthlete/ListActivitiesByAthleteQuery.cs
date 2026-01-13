using MediatR;
using Ruig.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Activities.Commands.ListActivitiesByAthlete
{
    public sealed record ListActivitiesByAthleteQuery(
        Guid AthleteId,
        int Page = 1,
        int PageSize = 50,
        DateTime? FromUtc = null,
        DateTime? ToUtc = null) : IRequest<PagedResult<ListActivitiesByAthleteDto>>;
}
