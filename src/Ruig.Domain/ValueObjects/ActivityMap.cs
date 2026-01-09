using Ruig.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Domain.ValueObjects
{
    public sealed class ActivityMap
    {
        public string? ExternalMapId { get; }
        public string SummaryPolyline { get; }

        private ActivityMap(string? externalMapId, string summaryPolyline)
        {
            ExternalMapId = externalMapId;
            SummaryPolyline = summaryPolyline;
        }

        public static ActivityMap Create(string? externalMapId, string summaryPolyline)
        {
            if (string.IsNullOrWhiteSpace(summaryPolyline))
                throw new DomainException("Summary Polyline is required");

            return new ActivityMap(externalMapId, summaryPolyline);
        }
    }
}
