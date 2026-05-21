using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ruig.Application.Badges
{
    public sealed record BadgePalette(string Key, string Label, IReadOnlyList<string> LevelFills);

    public sealed record BadgeAccent(string Key, string Label, string Color);

    public sealed record BadgeBackground(string Key, string Label, string Color);

    public sealed record BadgeCanvas(string Key, string Label, string Color);

    public static class BadgeStyleCatalog
    {
        public const string DefaultThemeKey = "purple";
        public const string DefaultAccentKey = "strava";
        public const string DefaultBackgroundKey = "slate";
        public const string DefaultCanvasKey = "none";

        public static readonly IReadOnlyDictionary<string, BadgePalette> Themes = new ReadOnlyDictionary<string, BadgePalette>(
            new Dictionary<string, BadgePalette>(StringComparer.OrdinalIgnoreCase)
            {
                ["purple"] = new("purple", "Obsidian violet",
                    new[] { "#26262e", "#4c1d95", "#7c3aed", "#a78bfa", "#c4b5fd" }),
                ["green"] = new("green", "GitHub green",
                    new[] { "#161b22", "#0e4429", "#006d32", "#26a641", "#39d353" }),
                ["blue"] = new("blue", "Ocean",
                    new[] { "#161b22", "#0c2d6b", "#1e40af", "#3b82f6", "#93c5fd" }),
                ["amber"] = new("amber", "Sunrise",
                    new[] { "#161b22", "#7c2d12", "#c2410c", "#f97316", "#fdba74" }),
                ["pink"] = new("pink", "Magenta",
                    new[] { "#161b22", "#831843", "#be185d", "#ec4899", "#f9a8d4" }),
                ["mono"] = new("mono", "Monochrome",
                    new[] { "#1f1f24", "#3a3a44", "#5e5e6a", "#9696a6", "#e1e1e8" })
            });

        public static readonly IReadOnlyDictionary<string, BadgeAccent> Accents = new ReadOnlyDictionary<string, BadgeAccent>(
            new Dictionary<string, BadgeAccent>(StringComparer.OrdinalIgnoreCase)
            {
                ["strava"]  = new("strava",  "Strava orange", "#fc4c02"),
                ["cyan"]    = new("cyan",    "Cyan",          "#22d3ee"),
                ["amber"]   = new("amber",   "Amber",         "#fbbf24"),
                ["emerald"] = new("emerald", "Emerald",       "#10b981"),
                ["magenta"] = new("magenta", "Magenta",       "#ec4899"),
                ["white"]   = new("white",   "White",         "#f4f4f5")
            });

        // Color used for cells with no GitHub contributions. Independent from the active-ramp
        // theme so users can pair any palette with any "empty day" colour (e.g. dark ramp on
        // a light README, or transparent to inherit whatever background the SVG sits on).
        public static readonly IReadOnlyDictionary<string, BadgeBackground> Backgrounds = new ReadOnlyDictionary<string, BadgeBackground>(
            new Dictionary<string, BadgeBackground>(StringComparer.OrdinalIgnoreCase)
            {
                ["slate"]       = new("slate",       "Slate",       "#26262e"),
                ["ink"]         = new("ink",         "Ink",         "#0c0c0e"),
                ["graphite"]    = new("graphite",    "Graphite",    "#1f1f24"),
                ["snow"]        = new("snow",        "Snow",        "#ebedf0"),
                ["paper"]       = new("paper",       "Paper",       "#f4f4f5"),
                ["transparent"] = new("transparent", "Transparent", "none")
            });

        // Full SVG canvas backdrop (a single rect drawn behind the heatmap).
        // "none" leaves the SVG transparent — what users get unless they opt in.
        public static readonly IReadOnlyDictionary<string, BadgeCanvas> Canvases = new ReadOnlyDictionary<string, BadgeCanvas>(
            new Dictionary<string, BadgeCanvas>(StringComparer.OrdinalIgnoreCase)
            {
                ["none"]   = new("none",   "None",   "none"),
                ["github"] = new("github", "GitHub", "#0d1117"),
                ["slate"]  = new("slate",  "Slate",  "#15151b"),
                ["ink"]    = new("ink",    "Ink",    "#0c0c0e"),
                ["snow"]   = new("snow",   "Snow",   "#ebedf0"),
                ["paper"]  = new("paper",  "Paper",  "#f4f4f5")
            });

        public static BadgePalette ResolveTheme(string? key)
        {
            if (!string.IsNullOrWhiteSpace(key) && Themes.TryGetValue(key, out var palette))
                return palette;

            return Themes[DefaultThemeKey];
        }

        public static BadgeAccent ResolveAccent(string? key)
        {
            if (!string.IsNullOrWhiteSpace(key) && Accents.TryGetValue(key, out var accent))
                return accent;

            return Accents[DefaultAccentKey];
        }

        public static BadgeBackground ResolveBackground(string? key)
        {
            if (!string.IsNullOrWhiteSpace(key) && Backgrounds.TryGetValue(key, out var bg))
                return bg;

            return Backgrounds[DefaultBackgroundKey];
        }

        public static BadgeCanvas ResolveCanvas(string? key)
        {
            if (!string.IsNullOrWhiteSpace(key) && Canvases.TryGetValue(key, out var canvas))
                return canvas;

            return Canvases[DefaultCanvasKey];
        }

        public static bool IsValidTheme(string? key)
            => !string.IsNullOrWhiteSpace(key) && Themes.ContainsKey(key);

        public static bool IsValidAccent(string? key)
            => !string.IsNullOrWhiteSpace(key) && Accents.ContainsKey(key);

        public static bool IsValidBackground(string? key)
            => !string.IsNullOrWhiteSpace(key) && Backgrounds.ContainsKey(key);

        public static bool IsValidCanvas(string? key)
            => !string.IsNullOrWhiteSpace(key) && Canvases.ContainsKey(key);
    }
}
