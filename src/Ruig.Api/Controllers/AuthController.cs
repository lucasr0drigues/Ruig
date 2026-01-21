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
        public async Task<ActionResult> Start(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new StartStravaOAuthCommand(), cancellationToken);

            return Ok(result);
        }

        [HttpGet("callback")]
        public async Task<ActionResult> Callback([FromQuery] string code, [FromQuery] string state, CancellationToken cancellationToken)
        {
            var athleteId = await _mediator.Send(new CompleteStravaOAuthCommand(code, state), cancellationToken);

            return Ok(new { athleteId });
        }
    }
}
