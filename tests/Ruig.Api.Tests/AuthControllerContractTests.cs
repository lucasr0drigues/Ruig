using Microsoft.AspNetCore.Mvc;
using Ruig.Api.Controllers;
using System.Reflection;

namespace Ruig.Api.Tests;

public sealed class AuthControllerContractTests
{
    [Fact]
    public void AuthController_UsesExpectedStravaRoute()
    {
        var route = typeof(AuthController).GetCustomAttribute<RouteAttribute>();

        Assert.Equal("auth/strava", route?.Template);
    }

    [Fact]
    public void Start_RequiresGitHubUsernameQueryParameter()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Start));
        var parameters = method?.GetParameters();

        Assert.NotNull(parameters);
        Assert.Contains(parameters, p => p.Name == "githubUsername" && p.GetCustomAttribute<FromQueryAttribute>() is not null);
    }

    [Fact]
    public void Callback_RequiresCodeAndStateQueryParameters()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Callback));
        var parameters = method?.GetParameters();

        Assert.NotNull(parameters);
        Assert.Contains(parameters, p => p.Name == "code" && p.GetCustomAttribute<FromQueryAttribute>() is not null);
        Assert.Contains(parameters, p => p.Name == "state" && p.GetCustomAttribute<FromQueryAttribute>() is not null);
    }
}
