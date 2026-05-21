using MediatR;

namespace Ruig.Application.Badges.Queries.GetBadgeSvg
{
    public sealed record GetBadgeSvgQuery(
        string Slug,
        string? Theme = null,
        string? Accent = null,
        string? Background = null,
        string? Canvas = null) : IRequest<GetBadgeSvgResult>;
}
