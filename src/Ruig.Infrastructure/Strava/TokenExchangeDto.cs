using Ruig.Application.Activities.Commands.ListActivitiesByAthlete;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Infrastructure.Strava
{
    public sealed record TokenExchangeDto(string Access_Token, string Refresh_Token, long Expires_At, string? Scope, AthleteDto athlete);
}
