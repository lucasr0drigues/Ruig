using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ruig.Application.Athletes.Commands.CompleteStravaOAuth;
using Ruig.Application.Athletes.Commands.StartStravaOAuth;

namespace Ruig.Api.Controllers
{
    [ApiController]
    [Route("auth/strava")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("start")]
        public async Task<ActionResult> Start([FromQuery] string githubUsername, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new StartStravaOAuthCommand(githubUsername), cancellationToken);

            return Ok(result);
        }

        [HttpGet("callback")]
        public async Task<ActionResult> Callback([FromQuery] string code, [FromQuery] string state, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new CompleteStravaOAuthCommand(code, state), cancellationToken);

            var badgeUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/badges/{result.BadgeSlug}.svg";
            var markdown = $"![Ruig heatmap]({badgeUrl})";

            return Ok(new
            {
                athleteId = result.AthleteId,
                gitHubUsername = result.GitHubUsername,
                badgeSlug = result.BadgeSlug,
                badgeUrl,
                markdown
            });
        }
    }
}
