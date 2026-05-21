using Ruig.Domain.Common;

namespace Ruig.Domain.Entities
{
    public class Activity : BaseEntity
    {
        public Guid AthleteId { get; init; }
        public DateOnly LocalDate { get; private set; }

        private Activity()
        {
        }

        public Activity(Guid athleteId, DateOnly localDate)
        {
            if (athleteId == Guid.Empty)
                throw new DomainException("Athlete ID is required.");

            if (localDate == default)
                throw new DomainException("Local date is required.");

            AthleteId = athleteId;
            LocalDate = localDate;
        }
    }
}
