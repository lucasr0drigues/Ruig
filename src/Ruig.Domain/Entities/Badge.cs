using Ruig.Domain.Common;
using System;

namespace Ruig.Domain.Entities
{
    public class Badge : BaseAuditableEntity
    {
        public string Slug { get; private set; }
        public Guid AthleteId { get; init; }
        public string GitHubUsername { get; private set; }
        public bool IsEnabled { get; private set; }
        public string Theme { get; private set; }
        public string AccentColor { get; private set; }

        private Badge()
        {
            Slug = null!;
            GitHubUsername = null!;
            Theme = null!;
            AccentColor = null!;
        }

        public Badge(Guid athleteId, string slug, string gitHubUsername, string theme, string accentColor)
        {
            if (athleteId == Guid.Empty)
                throw new DomainException("Athlete ID is required.");

            if (string.IsNullOrWhiteSpace(slug))
                throw new DomainException("Badge slug is required.");

            if (string.IsNullOrWhiteSpace(gitHubUsername))
                throw new DomainException("GitHub username is required.");

            if (string.IsNullOrWhiteSpace(theme))
                throw new DomainException("Badge theme is required.");

            if (string.IsNullOrWhiteSpace(accentColor))
                throw new DomainException("Badge accent color is required.");

            AthleteId = athleteId;
            Slug = slug;
            GitHubUsername = gitHubUsername;
            Theme = theme;
            AccentColor = accentColor;
            IsEnabled = true;
        }

        public void Disable() => IsEnabled = false;

        public void Enable() => IsEnabled = true;

        public void RotateSlug(string newSlug)
        {
            if (string.IsNullOrWhiteSpace(newSlug))
                throw new DomainException("Badge slug is required.");

            Slug = newSlug;
        }

        public void UpdateGitHubUsername(string gitHubUsername)
        {
            if (string.IsNullOrWhiteSpace(gitHubUsername))
                throw new DomainException("GitHub username is required.");

            GitHubUsername = gitHubUsername;
        }

        public void UpdateAppearance(string theme, string accentColor)
        {
            if (string.IsNullOrWhiteSpace(theme))
                throw new DomainException("Badge theme is required.");

            if (string.IsNullOrWhiteSpace(accentColor))
                throw new DomainException("Badge accent color is required.");

            Theme = theme;
            AccentColor = accentColor;
        }
    }
}
