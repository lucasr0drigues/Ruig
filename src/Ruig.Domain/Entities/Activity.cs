using Ruig.Domain.Common;
using Ruig.Domain.Enums;
using Ruig.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Domain.Entities
{
    public class Activity : BaseEntity
    {
        public Guid AthleteId { get; init; }
        public string ExternalActivityId { get; private set; }
        public string? Name { get; private set; }
        public ActivitySport? Sport { get; private set; }
        public double? DistanceMeters { get; private set; }
        public int? MovingTimeSeconds { get; private set; }
        public int? ElapsedTimeSeconds { get; private set; }
        public double? TotalElevationGainMeters { get; private set; }
        public DateTimeOffset? StartedAtUtc { get; private set; }
        public TimeSpan? UtcOffsetAtStart { get; private set; }
        public string? DeviceName { get; private set; }
        public ActivityMap? Map { get; private set; }

        private Activity() { }

        public Activity(
            Guid athleteId,
            string externalActivityId,
            string? name,
            ActivitySport? sport,
            double? distanceMeters,
            int? movingTimeSeconds,
            int? elapsedTimeSeconds,
            double? totalElevationGainMeters,
            DateTimeOffset? startedAtUtc,
            TimeSpan? utcOffsetAtStart,
            string? deviceName,
            string? externalMapId,
            string? summaryPolyline)
        {
            if (externalActivityId == null)
                throw new DomainException("External Activity ID is required.");

            AthleteId = athleteId;
            ExternalActivityId = externalActivityId;
            Name = name;
            Sport = sport;
            DistanceMeters = distanceMeters;
            MovingTimeSeconds = movingTimeSeconds;
            ElapsedTimeSeconds = elapsedTimeSeconds;
            TotalElevationGainMeters = totalElevationGainMeters;
            StartedAtUtc = startedAtUtc;
            UtcOffsetAtStart = utcOffsetAtStart;
            DeviceName = deviceName;

            Map = summaryPolyline is null ? null : ActivityMap.Create(externalMapId, summaryPolyline);
        }
    }
}
