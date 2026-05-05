using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Infrastructure.Strava
{
    public sealed class StravaOptions
    {
        public const string SectionName = "Strava";

        public string ClientId { get; init; } = default!;
        public string ClientSecret { get; init; } = default!;
        public string RedirectUri { get; init; } = default!;
        public string AuthorizeBaseUrl { get; init; } = "https://www.strava.com/oauth/authorize";
        public string TokenUrl { get; init; } = "https://www.strava.com/oauth/token";
        public string ApiBaseUrl { get; init; } = "https://www.strava.com/api/v3";
    }
}
