using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Rendering;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 226 section 5.1: a large target that a flatbed cannot take whole, printed on one large page as its tiles laid
/// side by side, with dashed cut lines between them. Each piece is a whole tile, with its own markers and codes, so it registers and names
/// itself on its own and the pieces pool as tiled sheets do. No bull crosses a cut line, because no bull crosses a tile's edge.
/// </summary>
public static class CutSheet
{
    /// <summary>The line, half-dmm: 1 mm wide, 4 mm dashes 2 mm apart, in a grey that prints on any printer and reads as a guide.</summary>
    private const long Width = 2 * 10, Dash = 2 * 40, Gap = 2 * 20;

    private static readonly Rgb Grey = new(128, 128, 128);

    /// <summary>Why a definition cannot be printed this way, or null when it can.</summary>
    public static string? Refusal(TargetDefinition definition) => definition.Tiling switch
    {
        null => "Only a sheet made of tiles can be printed as one large page with cut lines: this one is a single sheet.",
        { Overlap: not 0 } => "These tiles overlap when they are assembled, so they cannot be cut apart from one page.",
        _ => null,
    };

    /// <summary>The tiles' scenes, one per tile in tile order, laid out as the assembly and joined into one page with the cut lines.</summary>
    public static Scene Compose(TargetDefinition definition, IReadOnlyList<Scene> tiles)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(tiles);
        if (Refusal(definition) is { } refusal)
        {
            throw new ArgumentException(refusal, nameof(definition));
        }

        var t = definition.Tiling!;
        long w = 2L * t.SheetWidth, h = 2L * t.SheetHeight;
        var items = new List<SceneItem>();
        foreach (var tile in tiles)
        {
            long dx = w * (tile.TileIndex % t.Cols), dy = h * (tile.TileIndex / t.Cols);
            items.AddRange(tile.Items.Select(item => Moved(item, dx, dy)));
        }

        for (int c = 1; c < t.Cols; c++)
        {
            Dashes(items, (c * w) - (Width / 2), 0, vertical: true, t.Rows * h);
        }

        for (int r = 1; r < t.Rows; r++)
        {
            Dashes(items, 0, (r * h) - (Width / 2), vertical: false, t.Cols * w);
        }

        return new Scene(t.Cols * w, t.Rows * h, 0, items);
    }

    private static SceneItem Moved(SceneItem item, long dx, long dy) => item switch
    {
        RectFill r => r with { X = r.X + dx, Y = r.Y + dy },
        DiscBand b => b with { CentreX = b.CentreX + dx, CentreY = b.CentreY + dy },
        TextRun s => s with { X = s.X + dx, Baseline = s.Baseline + dy },
        _ => throw new InvalidOperationException($"A cut sheet cannot move a {item.GetType().Name}."),
    };

    private static void Dashes(List<SceneItem> items, long x, long y, bool vertical, long length)
    {
        for (long at = 0; at < length; at += Dash + Gap)
        {
            long run = Math.Min(Dash, length - at);
            items.Add(vertical
                ? new RectFill(SceneLayer.CutLines, Grey, x, y + at, Width, run)
                : new RectFill(SceneLayer.CutLines, Grey, x + at, y, run, Width));
        }
    }
}
