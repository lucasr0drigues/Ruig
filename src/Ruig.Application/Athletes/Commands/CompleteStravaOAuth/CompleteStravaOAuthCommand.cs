using MediatR;

namespace Ruig.Application.Athletes.Commands.CompleteStravaOAuth
{
    public sealed record CompleteStravaOAuthCommand(string Code, string State) : IRequest<CompleteStravaOAuthResult>;
}
