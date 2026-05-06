using System;

namespace Ruig.Application.Badges.Queries.GetBadgeSvg
{
    public sealed record GetBadgeSvgResult(
        string Slug,
        string Svg,
        DateOnly RangeFrom,
        DateOnly RangeTo,
        DateTimeOffset GeneratedAtUtc);
}
