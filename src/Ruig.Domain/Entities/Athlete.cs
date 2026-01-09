using Ruig.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Domain.Entities
{
    public class Athlete : BaseAuditableEntity
    {
        public string ExternalId { get; private set; }
        public string? Username { get; private set; }
        public string? Firstname { get; private set; }
        public string? Lastname { get; private set; }
        public string? Bio { get; private set; }
        public string? City { get; private set; }
        public string? State { get; private set; }
        public string? Country { get; private set; }
        public string? Sex { get; private set; }
        public bool Premium { get; private set; }
        public DateTime ExternalCreatedAt { get; private set; }
        public DateTime ExternalUpdatedAt { get; private set; }
        public string ProfileMedium { get; private set; }
        public string Profile { get; private set; }

        public Athlete(
            string externalId,
            string? username,
            string? firstname,
            string? lastname,
            string? bio,
            string? city,
            string? state,
            string? country,
            string? sex,
            bool premium,
            DateTime externalCreatedAt,
            DateTime externalUpdatedAt,
            string profileMedium,
            string profile)
        {
            if (string.IsNullOrWhiteSpace(externalId))
                throw new DomainException("External ID is required");

            ExternalId = externalId;
            Username = username;
            Firstname = firstname;
            Lastname = lastname;
            Bio = bio;
            City = city;
            State = state;
            Country = country;
            Sex = sex;
            Premium = premium;
            ExternalCreatedAt = externalCreatedAt;
            ExternalUpdatedAt = externalUpdatedAt;
            ProfileMedium = profileMedium;
            Profile = profile;
        }
    }
}
