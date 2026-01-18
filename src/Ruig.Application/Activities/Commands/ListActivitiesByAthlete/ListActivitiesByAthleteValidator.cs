using FluentValidation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Ruig.Application.Activities.Commands.ListActivitiesByAthlete
{
    public sealed class ListActivitiesByAthleteValidator : AbstractValidator<ListActivitiesByAthleteQuery>
    {
        public ListActivitiesByAthleteValidator()
        {
            RuleFor(x => x.AthleteId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);

            RuleFor(x => x)
                .Must(x => x.FromUtc == null || x.ToUtc == null || x.FromUtc <= x.ToUtc)
                .WithMessage("The start date must be less than or equal to the end date");
        }
    }
}
