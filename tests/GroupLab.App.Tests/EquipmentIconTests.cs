using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using GroupLab.App.Theme;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 167: the Equipment icon is a tilted cartridge. Alan, on the rifle it replaced: "it is bad and looks like a
/// lego rpg." A long thin object at 16 pixels has no room for its proportions, so the icon draws a detail of the thing rather than the whole.
/// </summary>
public class EquipmentIconTests
{
    /// <summary>
    /// The baked path stays inside the 16 pixel square with room to spare, because the first tilted render clipped its tip and rim at the
    /// corners. It is two shapes, the bullet and the case, since the gap between them is what reads as a loaded cartridge and not a bottle.
    /// </summary>
    [AvaloniaFact]
    public void TheCartridgeSitsInsideItsSquareAndKeepsItsTwoParts()
    {
        var geometry = StreamGeometry.Parse(Icons.Equipment);
        var bounds = geometry.Bounds;
        Assert.True(bounds.Left >= 0.5 && bounds.Top >= 0.5 && bounds.Right <= 15.9 && bounds.Bottom <= 15.9,
            $"the Equipment icon runs to {bounds}, and the 16 pixel square clips anything outside 0.5 to 15.9");
        Assert.Equal(2, Icons.Equipment.Split('M', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    /// <summary>
    /// And it is what the rail draws, filled in the foreground like every other destination, so the contrast the theme tests hold for the
    /// rail's text is the icon's contrast too, in the selected state and out of it.
    /// </summary>
    [AvaloniaFact]
    public void TheRailDrawsIt()
    {
        var (window, _, _) = Entry109Tests.Sheet();
        try
        {
            var drawn = window.GetLogicalDescendants().OfType<PathIcon>()
                .Where(p => p.Data is { } d && Math.Abs(d.Bounds.Width - StreamGeometry.Parse(Icons.Equipment).Bounds.Width) < 1e-6
                    && Math.Abs(d.Bounds.Height - StreamGeometry.Parse(Icons.Equipment).Bounds.Height) < 1e-6)
                .ToList();
            Assert.NotEmpty(drawn);
            Assert.All(drawn, p => Assert.False(p.IsSet(PathIcon.ForegroundProperty), "the icon carries a colour of its own instead of the theme's"));
        }
        finally
        {
            window.Close();
        }
    }
}
