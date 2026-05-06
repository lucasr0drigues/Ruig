using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Ruig.Application.Common.Interfaces.GitHub;
using Ruig.Application.Common.Interfaces.GitHub.Models;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Ruig.Infrastructure.GitHub
{
    public sealed class CachedGitHubContributionsService : IGitHubContributionsService
    {
        private readonly IGitHubContributionsClient _client;
        private readonly IMemoryCache _cache;
        private readonly GitHubOptions _options;

        public CachedGitHubContributionsService(
            IGitHubContributionsClient client,
            IMemoryCache cache,
            IOptions<GitHubOptions> options)
        {
            _client = client;
            _cache = cache;
            _options = options.Value;
        }

        public async Task<GitHubContributionCalendar> GetContributionsAsync(
            string username,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("GitHub username is required", nameof(username));

            var cacheKey = BuildCacheKey(username, fromUtc, toUtc);

            if (_cache.TryGetValue<GitHubContributionCalendar>(cacheKey, out var cached) && cached is not null)
                return cached;

            var result = await _client.GetContributionsAsync(username, fromUtc, toUtc, cancellationToken);

            var ttl = _options.CacheTtl > TimeSpan.Zero ? _options.CacheTtl : TimeSpan.FromHours(1);
            _cache.Set(cacheKey, result, ttl);

            return result;
        }

        private static string BuildCacheKey(string username, DateTimeOffset fromUtc, DateTimeOffset toUtc)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"github:contrib:{username.ToLowerInvariant()}:{fromUtc.UtcTicks}:{toUtc.UtcTicks}");
        }
    }
}
