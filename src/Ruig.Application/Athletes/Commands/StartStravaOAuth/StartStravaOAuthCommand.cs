using MediatR;

namespace Ruig.Application.Athletes.Commands.StartStravaOAuth
{
    public sealed record StartStravaOAuthCommand(string GitHubUsername) : IRequest<StartStravaOAuthResult>;
}
