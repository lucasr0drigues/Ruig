using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Infrastructure.Strava
{
    public sealed class StravaOptions
    {
        public string ClientId { get; init; } = default!;
        public string ClientSecret { get; init; } = default!;
        public string RedirectUri { get; init; } = default!;
        public string AuthorizeBaseUrl { get; init; } = default!;
        public string TokenUrl { get; init; } = default!;
        public string ApiBaseUrl { get; init; } = default!;
    }
}
