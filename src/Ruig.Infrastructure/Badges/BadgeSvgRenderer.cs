using Ruig.Application.Badges;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.GitHub.Models;
using Ruig.Application.Heatmaps.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Ruig.Infrastructure.Badges
{
    public sealed class BadgeSvgRenderer : IBadgeSvgRenderer
    {
        private const int CellSize = 11;
        private const int CellGap = 3;
        private const int CellStride = CellSize + CellGap;
        private const int Padding = 14;
        private const int FooterGap = 14;
        private const int FooterHeight = 20;
        private const int CornerRadius = 2;
        private const float StravaStrokeWidth = 1.5f;

        // Space reserved for axis labels.
        private const int LeftLabelWidth = 24;       // weekday labels (Mon/Wed/Fri)
        private const int TopLabelHeight = 16;       // month labels
        private const int MonthLabelMinGapPx = 32;   // skip labels that would crowd the previous one

        private const string TextColor = "#a8a8b3";
        private const string TextStrong = "#e8e8ec";
        private const string FontStack = "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";

        // Indexed by 1-based month number (index 0 unused).
        private static readonly string[] MonthAbbreviations =
        {
            string.Empty,
            "Jan", "Feb", "Mar", "Apr", "May", "Jun",
            "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
        };

        public string Render(BadgeRenderRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var heatmap = request.Heatmap;
            ArgumentNullException.ThrowIfNull(heatmap);

            var palette = BadgeStyleCatalog.ResolveTheme(request.ThemeKey);
            var accent = BadgeStyleCatalog.ResolveAccent(request.AccentKey);
            var background = BadgeStyleCatalog.ResolveBackground(request.BackgroundKey);
            var canvas = BadgeStyleCatalog.ResolveCanvas(request.CanvasKey);
            var emptyFill = background.Color;

            var gridStart = StartOfWeek(heatmap.From);
            var gridEnd = EndOfWeek(heatmap.To);
            var totalDays = gridEnd.DayNumber - gridStart.DayNumber + 1;
            var weeks = totalDays / 7;

            var gridWidth = weeks * CellStride - CellGap;
            var gridHeight = 7 * CellStride - CellGap;

            var gridOriginX = Padding + LeftLabelWidth;
            var gridOriginY = Padding + TopLabelHeight;

            var width = gridOriginX + gridWidth + Padding;
            var height = gridOriginY + gridHeight + FooterGap + FooterHeight + Padding;

            var sb = new StringBuilder(weeks * 7 * 80 + 1024);

            sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" ")
              .Append(CultureInfo.InvariantCulture, $"viewBox=\"0 0 {width} {height}\" ")
              .Append(CultureInfo.InvariantCulture, $"width=\"{width}\" height=\"{height}\" ")
              .Append("role=\"img\" aria-label=\"Ruig heatmap badge for ")
              .Append(WebUtility.HtmlEncode(request.GitHubUsername))
              .Append("\">");

            sb.Append("<style>")
              .Append(".ruig-text{font-family:").Append(FontStack).Append(";}")
              .Append("</style>");

            // Optional full-canvas backdrop ("card" effect).
            if (!string.Equals(canvas.Color, "none", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(CultureInfo.InvariantCulture,
                    $"<rect x=\"0\" y=\"0\" width=\"{width}\" height=\"{height}\" rx=\"6\" ry=\"6\" fill=\"{canvas.Color}\"/>");
            }

            // Month labels (top axis).
            AppendMonthLabels(sb, gridStart, weeks, gridOriginX, gridOriginY);

            // Weekday labels (left axis: Mon, Wed, Fri).
            AppendWeekdayLabels(sb, gridOriginX, gridOriginY);

            // Cells.
            var dayLookup = BuildDayLookup(heatmap);

            for (var date = gridStart; date <= gridEnd; date = date.AddDays(1))
            {
                var weekIndex = (date.DayNumber - gridStart.DayNumber) / 7;
                var dayOfWeek = (int)date.DayOfWeek;
                var x = gridOriginX + weekIndex * CellStride;
                var y = gridOriginY + dayOfWeek * CellStride;

                var fill = emptyFill;
                var hasStrava = false;
                var count = 0;

                if (date >= heatmap.From && date <= heatmap.To && dayLookup.TryGetValue(date, out var day))
                {
                    fill = day.GitHubLevel == GitHubContributionLevel.None
                        ? emptyFill
                        : palette.LevelFills[(int)day.GitHubLevel];
                    hasStrava = day.HasStravaActivity;
                    count = day.GitHubContributionCount;
                }

                sb.Append("<rect ")
                  .Append(CultureInfo.InvariantCulture, $"x=\"{x}\" y=\"{y}\" ")
                  .Append(CultureInfo.InvariantCulture, $"width=\"{CellSize}\" height=\"{CellSize}\" ")
                  .Append(CultureInfo.InvariantCulture, $"rx=\"{CornerRadius}\" ry=\"{CornerRadius}\" ")
                  .Append(CultureInfo.InvariantCulture, $"fill=\"{fill}\"");

                if (hasStrava)
                {
                    sb.Append(CultureInfo.InvariantCulture,
                        $" stroke=\"{accent.Color}\" stroke-width=\"{StravaStrokeWidth.ToString(CultureInfo.InvariantCulture)}\"");
                }

                sb.Append("><title>")
                  .Append(CultureInfo.InvariantCulture, $"{date:yyyy-MM-dd}: {count} GitHub, {(hasStrava ? "Strava activity" : "no Strava")}")
                  .Append("</title></rect>");
            }

            // Footer row: username on the left, legend on the right.
            var footerY = gridOriginY + gridHeight + FooterGap;
            var footerTextY = footerY + 10;
            var legendCellSize = 9;
            var legendCellGap = 3;
            var legendCellStride = legendCellSize + legendCellGap;
            var legendCellsCount = palette.LevelFills.Count;

            // Right-anchor: build legend right-to-left.
            var rightX = gridOriginX + gridWidth;

            // Strava legend chip on the far right.
            var stravaLabel = "Strava";
            var stravaLabelWidth = ApproxTextWidth(stravaLabel);
            var stravaChipX = rightX - stravaLabelWidth;
            sb.Append(CultureInfo.InvariantCulture,
                $"<text class=\"ruig-text\" x=\"{rightX}\" y=\"{footerTextY}\" text-anchor=\"end\" font-size=\"10\" fill=\"{TextColor}\">")
              .Append(stravaLabel)
              .Append("</text>");

            var stravaDotCx = stravaChipX - 12;
            var stravaDotCy = footerTextY - 3;
            sb.Append(CultureInfo.InvariantCulture,
                $"<rect x=\"{stravaDotCx - legendCellSize / 2}\" y=\"{stravaDotCy - legendCellSize / 2}\" width=\"{legendCellSize}\" height=\"{legendCellSize}\" rx=\"2\" fill=\"none\" stroke=\"{accent.Color}\" stroke-width=\"{StravaStrokeWidth.ToString(CultureInfo.InvariantCulture)}\"/>");

            // Less ▢▢▢▢▢ More cluster, anchored to the left of the Strava chip.
            var moreLabel = "More";
            var moreLabelWidth = ApproxTextWidth(moreLabel);
            var clusterRightX = stravaDotCx - legendCellSize / 2 - 12;
            var moreX = clusterRightX;
            sb.Append(CultureInfo.InvariantCulture,
                $"<text class=\"ruig-text\" x=\"{moreX}\" y=\"{footerTextY}\" text-anchor=\"end\" font-size=\"10\" fill=\"{TextColor}\">More</text>");

            var legendCellsRightX = moreX - moreLabelWidth - 4;
            var legendCellsLeftX = legendCellsRightX - (legendCellsCount * legendCellStride - legendCellGap);
            var legendCellY = footerTextY - 8;

            for (var i = 0; i < legendCellsCount; i++)
            {
                var x = legendCellsLeftX + i * legendCellStride;
                var legendFill = i == 0 ? emptyFill : palette.LevelFills[i];
                sb.Append(CultureInfo.InvariantCulture,
                    $"<rect x=\"{x}\" y=\"{legendCellY}\" width=\"{legendCellSize}\" height=\"{legendCellSize}\" rx=\"2\" fill=\"{legendFill}\"/>");
            }

            var lessX = legendCellsLeftX - 6;
            sb.Append(CultureInfo.InvariantCulture,
                $"<text class=\"ruig-text\" x=\"{lessX}\" y=\"{footerTextY}\" text-anchor=\"end\" font-size=\"10\" fill=\"{TextColor}\">Less</text>");

            // Username on the left of the footer (anti-theft signature), aligned with the grid.
            var displayName = TruncateForFooter(request.GitHubUsername, lessX - ApproxTextWidth("Less") - gridOriginX - 8);
            sb.Append(CultureInfo.InvariantCulture,
                $"<text class=\"ruig-text\" x=\"{gridOriginX}\" y=\"{footerTextY}\" font-size=\"11\" fill=\"{TextStrong}\" font-weight=\"600\">")
              .Append("GitHub: ")
              .Append("<tspan font-weight=\"400\" fill=\"")
              .Append(TextColor)
              .Append("\">")
              .Append(WebUtility.HtmlEncode(displayName))
              .Append("</tspan>")
              .Append("</text>");

            sb.Append("</svg>");

            return sb.ToString();
        }

        private static void AppendMonthLabels(StringBuilder sb, DateOnly gridStart, int weeks, int gridOriginX, int gridOriginY)
        {
            // Month label baseline — sits just above the grid.
            var labelY = gridOriginY - 5;
            var lastLabelMonth = -1;
            int? lastLabelX = null;

            for (var w = 0; w < weeks; w++)
            {
                var weekFirstDay = gridStart.AddDays(w * 7);
                var weekLastDay = weekFirstDay.AddDays(6);

                // The label belongs to the column where the new month "starts" (the
                // first week whose last day is in a different month than the previous week's).
                var month = weekLastDay.Month;
                if (month == lastLabelMonth) continue;

                lastLabelMonth = month;

                var x = gridOriginX + w * CellStride;

                // Skip labels that would crowd the previous one.
                if (lastLabelX is int prevX && x - prevX < MonthLabelMinGapPx) continue;
                lastLabelX = x;

                var monthName = MonthAbbreviations[month];

                sb.Append(CultureInfo.InvariantCulture,
                    $"<text class=\"ruig-text\" x=\"{x}\" y=\"{labelY}\" font-size=\"10\" fill=\"{TextColor}\">")
                  .Append(monthName)
                  .Append("</text>");
            }
        }

        private static void AppendWeekdayLabels(StringBuilder sb, int gridOriginX, int gridOriginY)
        {
            // GitHub-style: only show Mon, Wed, Fri.
            // DayOfWeek: Sunday=0, Monday=1, Tuesday=2, Wednesday=3, Thursday=4, Friday=5, Saturday=6
            var entries = new (string Label, int Row)[]
            {
                ("Mon", 1),
                ("Wed", 3),
                ("Fri", 5),
            };

            // Right-anchor the labels just to the left of the grid.
            var labelX = gridOriginX - 4;

            foreach (var (label, row) in entries)
            {
                var rowCenterY = gridOriginY + row * CellStride + (CellSize / 2);
                // Tweak for visual centering with 9px font.
                var textY = rowCenterY + 3;

                sb.Append(CultureInfo.InvariantCulture,
                    $"<text class=\"ruig-text\" x=\"{labelX}\" y=\"{textY}\" text-anchor=\"end\" font-size=\"9\" fill=\"{TextColor}\">")
                  .Append(label)
                  .Append("</text>");
            }
        }

        private static Dictionary<DateOnly, HeatmapDay> BuildDayLookup(Heatmap heatmap)
        {
            var lookup = new Dictionary<DateOnly, HeatmapDay>(heatmap.Days.Count);
            foreach (var day in heatmap.Days)
                lookup[day.Date] = day;
            return lookup;
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

        private static int ApproxTextWidth(string text)
        {
            // Rough monospace-ish estimate; good enough for layout anchoring.
            return (int)Math.Ceiling(text.Length * 6.0);
        }

        private static string TruncateForFooter(string username, int maxPixelWidth)
        {
            if (maxPixelWidth <= 0) return string.Empty;
            var charsThatFit = Math.Max(3, maxPixelWidth / 6);
            if (username.Length <= charsThatFit) return username;
            return username[..(charsThatFit - 1)] + "…";
        }
    }
}
