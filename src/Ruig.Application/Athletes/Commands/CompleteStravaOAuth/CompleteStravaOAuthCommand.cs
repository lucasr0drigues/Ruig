using Ruig.Application.Common.Dispatching;

namespace Ruig.Application.Athletes.Commands.CompleteStravaOAuth
{
    public sealed record CompleteStravaOAuthCommand(string Code, string State) : IRuigRequest<CompleteStravaOAuthResult>;
}
