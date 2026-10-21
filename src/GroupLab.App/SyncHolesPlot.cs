using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GroupLab.Core.Imaging;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 463 section 2 (2A): one version's holes, small, for the side by side of a session changed on two devices:
/// each counted hole a dot about its own aim point, the cross at the aim, on one scale with the other version so a moved or added hole shows.
/// Shared with the phone, which compiles this file too.
/// </summary>
public sealed class SyncHolesPlot : Control
{
    private readonly IReadOnlyList<PointD> holes;
    private readonly double reach;

    /// <param name="holes">This version's holes, in inches about their aim points.</param>
    /// <param name="reach">The farthest hole of both versions, so the two plots share a scale.</param>
    public SyncHolesPlot(IReadOnlyList<PointD> holes, double reach)
    {
        this.holes = holes ?? [];
        this.reach = reach > 0 ? reach : 1;
        Width = 160;
        Height = 160;
        Avalonia.Automation.AutomationProperties.SetName(this, $"{this.holes.Count} holes");
    }

    /// <summary>The farthest hole from its aim point in both versions, the shared scale.</summary>
    public static double Reach(IEnumerable<PointD> a, IEnumerable<PointD> b) =>
        a.Concat(b).Select(p => Math.Sqrt((p.X * p.X) + (p.Y * p.Y))).DefaultIfEmpty(1).Max();

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var size = Bounds.Size;
        var centre = new Point(size.Width / 2, size.Height / 2);
        double scale = (Math.Min(size.Width, size.Height) / 2 - 8) / reach;
        // The theme's own field border and text colours, so the plot follows the theme; where a host has neither, nothing is drawn.
        if (!this.TryFindResource("TextControlBorderBrush", out var b) || b is not IBrush line || !this.TryFindResource("TextControlForeground", out var f) || f is not IBrush dot)
        {
            return;
        }

        var pen = new Pen(line, 1);
        context.DrawRectangle(null, pen, new Rect(size).Deflate(0.5));
        context.DrawLine(pen, new Point(centre.X - 6, centre.Y), new Point(centre.X + 6, centre.Y));
        context.DrawLine(pen, new Point(centre.X, centre.Y - 6), new Point(centre.X, centre.Y + 6));
        foreach (var hole in holes)
        {
            // Offsets are in the picture's own directions, down positive, as the shot table reads them (it shows the negative as up).
            context.DrawEllipse(dot, null, new Point(centre.X + (hole.X * scale), centre.Y + (hole.Y * scale)), 3, 3);
        }
    }
}
