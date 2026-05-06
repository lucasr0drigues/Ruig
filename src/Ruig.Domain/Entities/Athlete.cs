using Ruig.Domain.Common;
using Ruig.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Domain.Entities
{
    public class Athlete : BaseAuditableEntity
    {
        public string ExternalAthleteId { get; private set; }
        public string? Username { get; private set; }
        public string? Firstname { get; private set; }
        public string? Lastname { get; private set; }
        public string? Bio { get; private set; }
        public string? City { get; private set; }
        public string? State { get; private set; }
        public string? Country { get; private set; }
        public Sex? Sex { get; private set; }
        public DateTime ExternalCreatedAt { get; private set; }
        public DateTime ExternalUpdatedAt { get; private set; }
        public string ProfileMedium { get; private set; }
        public string Profile { get; private set; }
        public DateTimeOffset? LastActivitySyncedAtUtc { get; private set; }

        //private readonly List<Activity> _activities = new();
        //public IReadOnlyCollection<Activity> Activities => _activities.AsReadOnly();

        private Athlete()
        {
            ExternalAthleteId = null!;
            ProfileMedium = string.Empty;
            Profile = string.Empty;
        }

        public Athlete(
            string externalAthleteId,
            string? username,
            string? firstname,
            string? lastname,
            string? bio,
            string? city,
            string? state,
            string? country,
            Sex? sex,
            DateTime externalCreatedAt,
            DateTime externalUpdatedAt,
            string profileMedium,
            string profile)
        {
            if (string.IsNullOrWhiteSpace(externalAthleteId))
                throw new DomainException("External Athlete ID is required");

            ExternalAthleteId = externalAthleteId;
            Username = username;
            Firstname = firstname;
            Lastname = lastname;
            Bio = bio;
            City = city;
            State = state;
            Country = country;
            Sex = sex;
            ExternalCreatedAt = externalCreatedAt;
            ExternalUpdatedAt = externalUpdatedAt;
            ProfileMedium = profileMedium;
            Profile = profile;
        }

        public void UpdateFromExternal(Athlete externalAthlete)
        {
            if (externalAthlete.ExternalAthleteId != ExternalAthleteId)
                throw new DomainException("External Athlete ID cannot be changed");

            Username = externalAthlete.Username;
            Firstname = externalAthlete.Firstname;
            Lastname = externalAthlete.Lastname;
            Bio = externalAthlete.Bio;
            City = externalAthlete.City;
            State = externalAthlete.State;
            Country = externalAthlete.Country;
            Sex = externalAthlete.Sex;
            ExternalCreatedAt = externalAthlete.ExternalCreatedAt;
            ExternalUpdatedAt = externalAthlete.ExternalUpdatedAt;
            ProfileMedium = externalAthlete.ProfileMedium;
            Profile = externalAthlete.Profile;
        }

        public void MarkActivitySyncCompleted(DateTimeOffset syncedAtUtc)
        {
            LastActivitySyncedAtUtc = syncedAtUtc;
        }
    }
}
