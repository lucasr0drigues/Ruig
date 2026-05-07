using MediatR;

namespace Ruig.Application.Athletes.Commands.StartStravaOAuth
{
    public sealed record StartStravaOAuthCommand(
        string GitHubUsername,
        string? Theme = null,
        string? AccentColor = null) : IRequest<StartStravaOAuthResult>;
}
