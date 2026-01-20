using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Athletes.Commands.StartStravaOAuth
{
    public sealed record StartStravaOAuthResult(string AuthorizationUrl, string State);
}
