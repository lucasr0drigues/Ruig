using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using Ruig.Infrastructure.Strava;
using System.Text.Json.Serialization;

namespace Ruig.Api.Controllers
{
    [ApiController]
    [EnableRateLimiting("strava-webhooks")]
    [Route("webhooks/strava")]
    public sealed class StravaWebhooksController : ControllerBase
    {
        private readonly IStravaWebhookEventStore _eventStore;
        private readonly StravaOptions _options;

        public StravaWebhooksController(
            IStravaWebhookEventStore eventStore,
            IOptions<StravaOptions> options)
        {
            _eventStore = eventStore;
            _options = options.Value;
        }

        [HttpGet]
        public ActionResult<IDictionary<string, string>> VerifySubscription(
            [FromQuery(Name = "hub.mode")] string? mode,
            [FromQuery(Name = "hub.verify_token")] string? verifyToken,
            [FromQuery(Name = "hub.challenge")] string? challenge)
        {
            if (string.IsNullOrWhiteSpace(challenge))
                return BadRequest("Missing hub.challenge");

            if (!string.Equals(mode, "subscribe", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Unsupported hub.mode");

            if (!IsValidVerifyToken(verifyToken))
                return Unauthorized();

            return Ok(new Dictionary<string, string>
            {
                ["hub.challenge"] = challenge
            });
        }

        [HttpPost]
        public async Task<ActionResult> ReceiveEvent(
            [FromBody] StravaWebhookEventRequest request,
            CancellationToken cancellationToken)
        {
            if (!IsValidSubscription(request.SubscriptionId))
                return Unauthorized();

            await _eventStore.SaveAsync(
                new StravaWebhookEventMessage(
                    request.ObjectType,
                    request.ObjectId,
                    request.AspectType,
                    request.OwnerId,
                    request.SubscriptionId,
                    request.EventTime,
                    request.Updates ?? new Dictionary<string, string>()),
                cancellationToken);

            return Ok();
        }

        private bool IsValidVerifyToken(string? verifyToken)
        {
            return !string.IsNullOrWhiteSpace(_options.WebhookVerifyToken) &&
                   string.Equals(_options.WebhookVerifyToken, verifyToken, StringComparison.Ordinal);
        }

        private bool IsValidSubscription(long subscriptionId)
        {
            return _options.WebhookSubscriptionId <= 0 ||
                   _options.WebhookSubscriptionId == subscriptionId;
        }
    }

    public sealed record StravaWebhookEventRequest(
        [property: JsonPropertyName("object_type")] string ObjectType,
        [property: JsonPropertyName("object_id")] long ObjectId,
        [property: JsonPropertyName("aspect_type")] string AspectType,
        [property: JsonPropertyName("owner_id")] long OwnerId,
        [property: JsonPropertyName("subscription_id")] long SubscriptionId,
        [property: JsonPropertyName("event_time")] long EventTime,
        [property: JsonPropertyName("updates")] Dictionary<string, string>? Updates);
}
