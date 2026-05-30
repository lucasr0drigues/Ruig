using Ruig.Application.Common.Dispatching;

namespace Ruig.Application.Athletes.Commands.StartStravaOAuth
{
    public sealed record StartStravaOAuthCommand(string GitHubUsername) : IRuigRequest<StartStravaOAuthResult>;
}
