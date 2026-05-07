using MediatR;

namespace Ruig.Application.Badges.Queries.GetBadgeSvg
{
    public sealed record GetBadgeSvgQuery(
        string Slug,
        string? ThemeOverride = null,
        string? AccentOverride = null) : IRequest<GetBadgeSvgResult>;
}
