using FluentValidation;

namespace Ruig.Application.Athletes.Commands.StartStravaOAuth
{
    public sealed class StartStravaOAuthValidator : AbstractValidator<StartStravaOAuthCommand>
    {
        public StartStravaOAuthValidator()
        {
            RuleFor(x => x.GitHubUsername)
                .NotEmpty()
                .MaximumLength(39)
                .Matches("^[a-zA-Z0-9](?:[a-zA-Z0-9]|-(?=[a-zA-Z0-9])){0,38}$")
                .WithMessage("GitHub username must contain only letters, digits, and non-consecutive hyphens.");
        }
    }
}
