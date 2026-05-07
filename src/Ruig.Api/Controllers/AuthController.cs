using FluentValidation;
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
        private readonly ILogger<AuthController> _logger;

        public AuthController(IMediator mediator, ILogger<AuthController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet("start")]
        public async Task<ActionResult> Start([FromQuery] string githubUsername, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(new StartStravaOAuthCommand(githubUsername), cancellationToken);

                return Ok(result);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { error = "invalid-github-username", message = ex.Message });
            }
        }

        [HttpGet("callback")]
        public async Task<ActionResult> Callback([FromQuery] string code, [FromQuery] string state, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(new CompleteStravaOAuthCommand(code, state), cancellationToken);

                return Redirect($"/setup-complete.html?slug={Uri.EscapeDataString(result.BadgeSlug)}");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Strava OAuth callback rejected: invalid state.");
                return Redirect("/?error=invalid-state");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Strava OAuth callback failed.");
                return Redirect("/?error=connection-failed");
            }
        }
    }
}
