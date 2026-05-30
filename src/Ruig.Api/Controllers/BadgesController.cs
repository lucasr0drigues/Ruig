using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Ruig.Application.Badges.Queries.GetBadgeSvg;
using Ruig.Application.Common.Dispatching;
using System.Text;

namespace Ruig.Api.Controllers
{
    [ApiController]
    [Route("badges")]
    public sealed class BadgesController : ControllerBase
    {
        private readonly IRuigDispatcher _dispatcher;

        public BadgesController(IRuigDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpGet("{slug}.svg")]
        [Produces("image/svg+xml")]
        public async Task<IActionResult> GetBadge(
            [FromRoute] string slug,
            [FromQuery] string? theme,
            [FromQuery] string? accent,
            [FromQuery] string? bg,
            [FromQuery] string? canvas,
            CancellationToken cancellationToken)
        {
            GetBadgeSvgResult result;

            try
            {
                result = await _dispatcher.Send(new GetBadgeSvgQuery(slug, theme, accent, bg, canvas), cancellationToken);
            }
            catch (BadgeNotFoundException)
            {
                return NotFound();
            }

            Response.Headers.CacheControl = "public, max-age=600";
            Response.Headers["X-Heatmap-Range-From"] = result.RangeFrom.ToString("yyyy-MM-dd");
            Response.Headers["X-Heatmap-Range-To"] = result.RangeTo.ToString("yyyy-MM-dd");

            return File(Encoding.UTF8.GetBytes(result.Svg), "image/svg+xml");
        }
    }
}
