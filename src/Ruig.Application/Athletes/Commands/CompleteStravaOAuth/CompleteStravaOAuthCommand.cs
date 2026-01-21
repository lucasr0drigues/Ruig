using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Athletes.Commands.CompleteStravaOAuth
{
    public sealed record CompleteStravaOAuthCommand(string Code, string State) : IRequest<Guid>;
}
