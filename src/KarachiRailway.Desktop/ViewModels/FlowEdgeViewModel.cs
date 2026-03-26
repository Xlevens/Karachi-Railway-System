using System.Windows;
using System.Windows.Media;

namespace KarachiRailway.Desktop.ViewModels;

/// <summary>
/// Represents a directed connector between two flow-diagram nodes.
/// Stores pre-computed polyline waypoints and arrowhead polygon for XAML binding.
/// </summary>
public class FlowEdgeViewModel
{
    public required string FromId { get; init; }
    public required string ToId   { get; init; }
    public string? Label { get; init; }

    /// <summary>Waypoints for the Polyline element.</summary>
    public PointCollection Points { get; } = new();

    /// <summary>Three-point polygon (tip, wing-left, wing-right) for the arrowhead.</summary>
    public PointCollection ArrowheadPoints { get; private set; } = new();

    public double LabelLeft { get; private set; }
    public double LabelTop  { get; private set; }
    public bool   HasLabel  => !string.IsNullOrEmpty(Label);

    /// <summary>
    /// Computes arrowhead geometry and label position from the waypoints.
    /// Must be called after all Points have been added.
    /// </summary>
    public void Build()
    {
        if (Points.Count >= 2)
        {
            var end  = Points[Points.Count - 1];
            var prev = Points[Points.Count - 2];

            double angle = Math.Atan2(end.Y - prev.Y, end.X - prev.X);
            const double sz   = 9.0;
            const double wing = Math.PI / 6.0; // 30°

            ArrowheadPoints = new PointCollection(new[]
            {
                end,
                new Point(end.X - sz * Math.Cos(angle - wing), end.Y - sz * Math.Sin(angle - wing)),
                new Point(end.X - sz * Math.Cos(angle + wing), end.Y - sz * Math.Sin(angle + wing)),
            });
        }

        if (HasLabel && Points.Count >= 2)
        {
            int  mid = Points.Count / 2;
            var  p0  = Points[mid > 0 ? mid - 1 : 0];
            var  p1  = Points[mid];
            LabelLeft = (p0.X + p1.X) / 2.0 + 4;
            LabelTop  = (p0.Y + p1.Y) / 2.0 - 14;
        }
    }
}
