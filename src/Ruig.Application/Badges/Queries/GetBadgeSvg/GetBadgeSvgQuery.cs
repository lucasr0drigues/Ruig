using MediatR;

namespace Ruig.Application.Badges.Queries.GetBadgeSvg
{
    public sealed record GetBadgeSvgQuery(string Slug) : IRequest<GetBadgeSvgResult>;
}
