using FluentValidation;

namespace Ruig.Application.Heatmaps.Queries.GetHeatmap
{
    public sealed class GetHeatmapValidator : AbstractValidator<GetHeatmapQuery>
    {
        public GetHeatmapValidator()
        {
            RuleFor(x => x.GitHubUsername).NotEmpty();
            RuleFor(x => x.AthleteId).NotEmpty();

            RuleFor(x => x)
                .Must(x => x.From <= x.To)
                .WithMessage("'From' must be less than or equal to 'To'.");
        }
    }
}
