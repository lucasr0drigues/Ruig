using Microsoft.AspNetCore.Mvc;
using Ruig.Api.Controllers;
using System.Reflection;

namespace Ruig.Api.Tests;

public sealed class BadgesControllerContractTests
{
    [Fact]
    public void BadgesController_UsesExpectedRoute()
    {
        var route = typeof(BadgesController).GetCustomAttribute<RouteAttribute>();

        Assert.Equal("badges", route?.Template);
    }

    [Fact]
    public void GetBadge_RoutesToSlugDotSvgAndProducesSvg()
    {
        var method = typeof(BadgesController).GetMethod(nameof(BadgesController.GetBadge));

        Assert.NotNull(method);

        var http = method!.GetCustomAttribute<HttpGetAttribute>();
        Assert.Equal("{slug}.svg", http?.Template);

        var produces = method.GetCustomAttribute<ProducesAttribute>();
        Assert.NotNull(produces);
        Assert.Contains("image/svg+xml", produces!.ContentTypes);

        var slugParam = method.GetParameters().FirstOrDefault(p => p.Name == "slug");
        Assert.NotNull(slugParam);
        Assert.NotNull(slugParam!.GetCustomAttribute<FromRouteAttribute>());
    }
}
