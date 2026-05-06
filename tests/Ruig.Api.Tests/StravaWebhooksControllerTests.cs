using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Ruig.Api.Controllers;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using Ruig.Infrastructure.Strava;

namespace Ruig.Api.Tests;

public sealed class StravaWebhooksControllerTests
{
    [Fact]
    public void VerifySubscription_WithValidToken_ReturnsChallenge()
    {
        var controller = CreateController();

        var result = controller.VerifySubscription("subscribe", "verify-token", "challenge-value");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IDictionary<string, string>>(ok.Value);
        Assert.Equal("challenge-value", payload["hub.challenge"]);
    }

    [Fact]
    public void VerifySubscription_WithInvalidToken_ReturnsUnauthorized()
    {
        var controller = CreateController();

        var result = controller.VerifySubscription("subscribe", "bad-token", "challenge-value");

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task ReceiveEvent_StoresMessageAndReturnsOk()
    {
        var eventStore = new FakeWebhookEventStore();
        var controller = CreateController(eventStore);

        var result = await controller.ReceiveEvent(
            new StravaWebhookEventRequest(
                "activity",
                987,
                "create",
                123,
                456,
                1_800_000_000,
                new Dictionary<string, string>()),
            CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.NotNull(eventStore.SavedMessage);
        Assert.Equal("activity", eventStore.SavedMessage.ObjectType);
        Assert.Equal(987, eventStore.SavedMessage.ObjectId);
    }

    private static StravaWebhooksController CreateController(FakeWebhookEventStore? eventStore = null)
    {
        return new StravaWebhooksController(
            eventStore ?? new FakeWebhookEventStore(),
            Options.Create(new StravaOptions
            {
                WebhookVerifyToken = "verify-token"
            }));
    }

    private sealed class FakeWebhookEventStore : IStravaWebhookEventStore
    {
        public StravaWebhookEventMessage? SavedMessage { get; private set; }

        public Task SaveAsync(StravaWebhookEventMessage message, CancellationToken cancellationToken)
        {
            SavedMessage = message;
            return Task.CompletedTask;
        }
    }
}
