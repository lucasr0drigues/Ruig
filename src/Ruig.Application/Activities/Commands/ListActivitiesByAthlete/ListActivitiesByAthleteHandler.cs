using MediatR;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Activities.Commands.ListActivitiesByAthlete
{
    public sealed class ListActivitiesByAthleteHandler : IRequestHandler<ListActivitiesByAthleteQuery, PagedResult<ListActivitiesByAthleteDto>>
    {
        private readonly IActivityRepository _activityRepository;

        public ListActivitiesByAthleteHandler(IActivityRepository activityRepository)
        {
            _activityRepository = activityRepository;
        }

        public Task<PagedResult<ListActivitiesByAthleteDto>> Handle(ListActivitiesByAthleteQuery request, CancellationToken cancellationToken)
        {
            return _activityRepository.ListByAthleteIdAsync(request.AthleteId, request.Page, request.PageSize, request.FromUtc, request.ToUtc, cancellationToken);
        }
    }
}
