using Ruig.Domain.Common;

namespace Ruig.Domain.Entities
{
    public class Athlete : BaseAuditableEntity
    {
        public string? Firstname { get; private set; }
        public string? Lastname { get; private set; }
        public DateTimeOffset? LastActivitySyncedAtUtc { get; private set; }

        private Athlete()
        {
        }

        public Athlete(string? firstname, string? lastname)
        {
            Firstname = firstname;
            Lastname = lastname;
        }

        public void UpdateFromExternal(Athlete externalAthlete)
        {
            Firstname = externalAthlete.Firstname;
            Lastname = externalAthlete.Lastname;
        }

        public void MarkActivitySyncCompleted(DateTimeOffset syncedAtUtc)
        {
            LastActivitySyncedAtUtc = syncedAtUtc;
        }
    }
}
