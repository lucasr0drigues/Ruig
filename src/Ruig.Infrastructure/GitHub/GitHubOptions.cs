using System;

namespace Ruig.Infrastructure.GitHub
{
    public sealed class GitHubOptions
    {
        public const string SectionName = "GitHub";

        public string GraphQLUrl { get; init; } = "https://api.github.com/graphql";
        public string AccessToken { get; init; } = string.Empty;
        public string UserAgent { get; init; } = "Ruig";
        public TimeSpan CacheTtl { get; init; } = TimeSpan.FromHours(1);
    }
}
