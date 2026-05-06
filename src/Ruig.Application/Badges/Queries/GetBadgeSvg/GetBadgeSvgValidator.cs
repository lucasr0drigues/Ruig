using FluentValidation;

namespace Ruig.Application.Badges.Queries.GetBadgeSvg
{
    public sealed class GetBadgeSvgValidator : AbstractValidator<GetBadgeSvgQuery>
    {
        public GetBadgeSvgValidator()
        {
            RuleFor(x => x.Slug)
                .NotEmpty()
                .MaximumLength(64)
                .Matches("^[a-zA-Z0-9_-]+$")
                .WithMessage("Slug must only contain letters, digits, '-' and '_'.");
        }
    }
}
