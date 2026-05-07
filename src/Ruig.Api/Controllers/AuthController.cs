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

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
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
            catch (InvalidOperationException)
            {
                return Redirect("/?error=invalid-state");
            }
            catch (Exception)
            {
                return Redirect("/?error=connection-failed");
            }
        }
    }
}
