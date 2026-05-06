using System;

namespace Ruig.Application.Athletes.Commands.CompleteStravaOAuth
{
    public sealed record CompleteStravaOAuthResult(
        Guid AthleteId,
        string GitHubUsername,
        string BadgeSlug);
}
