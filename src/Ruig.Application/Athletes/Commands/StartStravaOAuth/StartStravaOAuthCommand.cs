using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Athletes.Commands.StartStravaOAuth
{
    public sealed record StartStravaOAuthCommand() : IRequest<StartStravaOAuthResult>;
}
