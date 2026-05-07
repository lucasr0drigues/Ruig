using Ruig.Application.Heatmaps.Models;

namespace Ruig.Application.Common.Interfaces
{
    public sealed record BadgeRenderRequest(
        Heatmap Heatmap,
        string GitHubUsername,
        string ThemeKey,
        string AccentKey,
        string BackgroundKey);
}
