using Ruig.Application.Heatmaps.Models;

namespace Ruig.Application.Common.Interfaces
{
    public interface IBadgeSvgRenderer
    {
        string Render(Heatmap heatmap);
    }
}
