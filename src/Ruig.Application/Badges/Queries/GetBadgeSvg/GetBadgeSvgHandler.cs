using MediatR;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Heatmaps.Queries.GetHeatmap;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ruig.Application.Badges.Queries.GetBadgeSvg
{
    public sealed class GetBadgeSvgHandler : IRequestHandler<GetBadgeSvgQuery, GetBadgeSvgResult>
    {
        private static readonly TimeSpan HeatmapWindow = TimeSpan.FromDays(365);

        private readonly IBadgeRepository _badgeRepository;
        private readonly IBadgeSvgRenderer _renderer;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IMediator _mediator;

        public GetBadgeSvgHandler(
            IBadgeRepository badgeRepository,
            IBadgeSvgRenderer renderer,
            IDateTimeProvider dateTimeProvider,
            IMediator mediator)
        {
            _badgeRepository = badgeRepository;
            _renderer = renderer;
            _dateTimeProvider = dateTimeProvider;
            _mediator = mediator;
        }

        public async Task<GetBadgeSvgResult> Handle(GetBadgeSvgQuery request, CancellationToken cancellationToken)
        {
            var badge = await _badgeRepository.GetBySlugAsync(request.Slug, cancellationToken);

            if (badge is null || !badge.IsEnabled)
                throw new BadgeNotFoundException(request.Slug);

            var nowUtc = NormalizeUtc(_dateTimeProvider.UtcNow);
            var to = DateOnly.FromDateTime(nowUtc);
            var from = to.AddDays(-(int)HeatmapWindow.TotalDays + 1);

            var heatmap = await _mediator.Send(
                new GetHeatmapQuery(badge.GitHubUsername, badge.AthleteId, from, to),
                cancellationToken);

            var theme = request.ThemeOverride ?? badge.Theme;
            var accent = request.AccentOverride ?? badge.AccentColor;

            var svg = _renderer.Render(new BadgeRenderRequest(heatmap, badge.GitHubUsername, theme, accent));

            return new GetBadgeSvgResult(badge.Slug, svg, from, to, new DateTimeOffset(nowUtc));
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc
                ? value
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }
}
