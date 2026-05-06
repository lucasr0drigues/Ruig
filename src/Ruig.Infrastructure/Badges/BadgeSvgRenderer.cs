using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.GitHub.Models;
using Ruig.Application.Heatmaps.Models;
using System;
using System.Globalization;
using System.Text;

namespace Ruig.Infrastructure.Badges
{
    public sealed class BadgeSvgRenderer : IBadgeSvgRenderer
    {
        private const int CellSize = 11;
        private const int CellGap = 3;
        private const int CellStride = CellSize + CellGap;
        private const int Padding = 16;
        private const int FooterHeight = 24;
        private const int CornerRadius = 2;
        private const float StravaStrokeWidth = 1.5f;

        private const string StravaStrokeColor = "#fc4c02";

        private static readonly string[] LevelFills =
        {
            "#ebedf0",
            "#9be9a8",
            "#40c463",
            "#30a14e",
            "#216e39"
        };

        public string Render(Heatmap heatmap)
        {
            ArgumentNullException.ThrowIfNull(heatmap);

            var gridStart = StartOfWeek(heatmap.From);
            var gridEnd = EndOfWeek(heatmap.To);
            var totalDays = gridEnd.DayNumber - gridStart.DayNumber + 1;
            var weeks = totalDays / 7;

            var gridWidth = weeks * CellStride - CellGap;
            var gridHeight = 7 * CellStride - CellGap;
            var width = Padding * 2 + gridWidth;
            var height = Padding * 2 + gridHeight + FooterHeight;

            var sb = new StringBuilder(weeks * 7 * 80);

            sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" ")
              .Append(CultureInfo.InvariantCulture, $"viewBox=\"0 0 {width} {height}\" ")
              .Append(CultureInfo.InvariantCulture, $"width=\"{width}\" height=\"{height}\" ")
              .Append("role=\"img\" aria-label=\"GitHub contributions and Strava activity heatmap\">");

            sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\" />");

            var dayLookup = BuildDayLookup(heatmap);

            for (var date = gridStart; date <= gridEnd; date = date.AddDays(1))
            {
                var weekIndex = (date.DayNumber - gridStart.DayNumber) / 7;
                var dayOfWeek = (int)date.DayOfWeek;
                var x = Padding + weekIndex * CellStride;
                var y = Padding + dayOfWeek * CellStride;

                var fill = LevelFills[0];
                var hasStrava = false;

                if (date >= heatmap.From && date <= heatmap.To && dayLookup.TryGetValue(date, out var day))
                {
                    fill = LevelFills[(int)day.GitHubLevel];
                    hasStrava = day.HasStravaActivity;
                }

                sb.Append("<rect ")
                  .Append(CultureInfo.InvariantCulture, $"x=\"{x}\" y=\"{y}\" ")
                  .Append(CultureInfo.InvariantCulture, $"width=\"{CellSize}\" height=\"{CellSize}\" ")
                  .Append(CultureInfo.InvariantCulture, $"rx=\"{CornerRadius}\" ry=\"{CornerRadius}\" ")
                  .Append(CultureInfo.InvariantCulture, $"fill=\"{fill}\"");

                if (hasStrava)
                {
                    sb.Append(CultureInfo.InvariantCulture,
                        $" stroke=\"{StravaStrokeColor}\" stroke-width=\"{StravaStrokeWidth.ToString(CultureInfo.InvariantCulture)}\"");
                }

                sb.Append(CultureInfo.InvariantCulture,
                    $"><title>{date:yyyy-MM-dd}: {SafeCount(day: dayLookup, date)} GitHub, {(hasStrava ? "Strava activity" : "no Strava")}</title></rect>");
            }

            var footerY = Padding + gridHeight + 14;
            sb.Append("<text font-family=\"-apple-system, Segoe UI, sans-serif\" font-size=\"10\" fill=\"#586069\" ")
              .Append(CultureInfo.InvariantCulture, $"x=\"{Padding}\" y=\"{footerY}\">")
              .Append(CultureInfo.InvariantCulture, $"{heatmap.TotalGitHubContributions} contributions · {heatmap.TotalStravaActivityDays} active days")
              .Append("</text>");

            sb.Append("</svg>");

            return sb.ToString();
        }

        private static System.Collections.Generic.Dictionary<DateOnly, HeatmapDay> BuildDayLookup(Heatmap heatmap)
        {
            var lookup = new System.Collections.Generic.Dictionary<DateOnly, HeatmapDay>(heatmap.Days.Count);

            foreach (var day in heatmap.Days)
                lookup[day.Date] = day;

            return lookup;
        }

        private static string SafeCount(System.Collections.Generic.Dictionary<DateOnly, HeatmapDay> day, DateOnly date)
        {
            return day.TryGetValue(date, out var entry)
                ? entry.GitHubContributionCount.ToString(CultureInfo.InvariantCulture)
                : "0";
        }

        private static DateOnly StartOfWeek(DateOnly date)
        {
            var offset = (int)date.DayOfWeek;
            return date.AddDays(-offset);
        }

        private static DateOnly EndOfWeek(DateOnly date)
        {
            var offset = 6 - (int)date.DayOfWeek;
            return date.AddDays(offset);
        }
    }
}
