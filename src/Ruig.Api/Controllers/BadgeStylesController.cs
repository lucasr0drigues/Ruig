using Microsoft.AspNetCore.Mvc;
using Ruig.Application.Badges;
using System.Linq;

namespace Ruig.Api.Controllers
{
    [ApiController]
    [Route("badges/styles")]
    public sealed class BadgeStylesController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var themes = BadgeStyleCatalog.Themes
                .OrderBy(kvp => kvp.Key == BadgeStyleCatalog.DefaultThemeKey ? 0 : 1)
                .ThenBy(kvp => kvp.Key)
                .Select(kvp => new
                {
                    key = kvp.Value.Key,
                    label = kvp.Value.Label,
                    levels = kvp.Value.LevelFills,
                    isDefault = kvp.Key == BadgeStyleCatalog.DefaultThemeKey
                });

            var accents = BadgeStyleCatalog.Accents
                .OrderBy(kvp => kvp.Key == BadgeStyleCatalog.DefaultAccentKey ? 0 : 1)
                .ThenBy(kvp => kvp.Key)
                .Select(kvp => new
                {
                    key = kvp.Value.Key,
                    label = kvp.Value.Label,
                    color = kvp.Value.Color,
                    isDefault = kvp.Key == BadgeStyleCatalog.DefaultAccentKey
                });

            var backgrounds = BadgeStyleCatalog.Backgrounds
                .OrderBy(kvp => kvp.Key == BadgeStyleCatalog.DefaultBackgroundKey ? 0 : 1)
                .ThenBy(kvp => kvp.Key)
                .Select(kvp => new
                {
                    key = kvp.Value.Key,
                    label = kvp.Value.Label,
                    color = kvp.Value.Color,
                    isDefault = kvp.Key == BadgeStyleCatalog.DefaultBackgroundKey
                });

            var canvases = BadgeStyleCatalog.Canvases
                .OrderBy(kvp => kvp.Key == BadgeStyleCatalog.DefaultCanvasKey ? 0 : 1)
                .ThenBy(kvp => kvp.Key)
                .Select(kvp => new
                {
                    key = kvp.Value.Key,
                    label = kvp.Value.Label,
                    color = kvp.Value.Color,
                    isDefault = kvp.Key == BadgeStyleCatalog.DefaultCanvasKey
                });

            return Ok(new { themes, accents, backgrounds, canvases });
        }
    }
}
