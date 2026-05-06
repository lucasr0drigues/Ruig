using System;

namespace Ruig.Application.Badges.Queries.GetBadgeSvg
{
    public sealed class BadgeNotFoundException : Exception
    {
        public BadgeNotFoundException(string slug)
            : base($"Badge '{slug}' was not found or is disabled.")
        {
            Slug = slug;
        }

        public string Slug { get; }
    }
}
