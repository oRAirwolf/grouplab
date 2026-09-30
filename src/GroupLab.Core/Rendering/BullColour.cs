using GroupLab.Core.Gltd.Binary;

namespace GroupLab.Core.Rendering;

/// <summary>The color a sheet's bulls are printed in, NOTES-FROM-PLANNING.md entry 297: black, the default, blue or red.</summary>
public enum BullColour
{
    Black,
    Blue,
    Red,
}

/// <summary>
/// Bulls in black, blue or red, NOTES-FROM-PLANNING.md entry 297, with the hues and ink of its section 7 and entry 309 section 4 (Alan:
/// "Colors: Approved"). Only the bulls, their rings and their numbers take the color, and a zeroing grid's lines and figures, which are its
/// bulls; the corner codes, the markers, the cells' lines, the title and the load block stay black, so registration and code reading are
/// unchanged. Lines, rings and numbers print in the full color, a red near #D22630 and a blue near #1F5FBF; large solid areas, the C bull's
/// diamond and filled centers, print as a 60 percent tint, which saves ink where most of it goes and lets a bullet hole show dark against
/// it where solid black hides one. Nothing is drawn thicker or larger than the black version, so a color never costs more ink than black.
/// <para>
/// The detector is not told the color: it is found from the photograph (<c>RenderDifferenceHoleDetector</c>), because a color carried in
/// the sheet's codes would make every colored sheet a different definition, and the color is an option, not a new target.
/// </para>
/// </summary>
public static class BullColours
{
    /// <summary>Every choice, in the order it is offered.</summary>
    public static IReadOnlyList<BullColour> All { get; } = [BullColour.Black, BullColour.Blue, BullColour.Red];

    /// <summary>The full color of lines, rings and numbers.</summary>
    public static Rgb Of(BullColour colour) => colour switch
    {
        BullColour.Red => new Rgb(0xD2, 0x26, 0x30),
        BullColour.Blue => new Rgb(0x1F, 0x5F, 0xBF),
        _ => new Rgb(0, 0, 0),
    };

    /// <summary>The share of the full color a large solid area prints at.</summary>
    public const double SolidTint = 0.6;

    /// <summary>
    /// The width, in half-dmm, from which a band or a rectangle counts as a large solid area rather than a line: 3 mm. The rings' lines are
    /// at most about 1 mm; a filled center or the C bull's diamond is a centimeter or more.
    /// </summary>
    public const long SolidFrom = 60;

    /// <summary>The layers the color reaches: the bulls, their numbers, and a zeroing grid's lines and figures.</summary>
    public static bool Coloured(SceneLayer layer) => layer is SceneLayer.Bulls or SceneLayer.Labels or SceneLayer.MeasurementGrid;

    /// <summary>The word for a color, as the settings file and the screens say it.</summary>
    public static string Name(BullColour colour) => colour switch
    {
        BullColour.Red => "red",
        BullColour.Blue => "blue",
        _ => "black",
    };

    /// <summary>A color from its word, black for anything else.</summary>
    public static BullColour Parse(string? word) => word?.Trim().ToLowerInvariant() switch
    {
        "red" => BullColour.Red,
        "blue" => BullColour.Blue,
        _ => BullColour.Black,
    };

    /// <summary>Whether an item is a large solid area, which a color prints as a tint.</summary>
    public static bool Solid(SceneItem item) => item switch
    {
        DiscBand band => band.OuterRadius - band.InnerRadius >= SolidFrom,
        RectFill rect => rect.Width >= SolidFrom && rect.Height >= SolidFrom,
        _ => false,
    };

    /// <summary>
    /// A page in <paramref name="colour"/>: each ink of the colored layers mixed from paper toward the color by how dark it was, the full color
    /// for black, and a large solid area at <see cref="SolidTint"/> of that. Black returns the page as it is.
    /// </summary>
    public static Scene Apply(Scene page, BullColour colour)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (colour == BullColour.Black)
        {
            return page;
        }

        var full = Of(colour);
        return page with
        {
            Items = [.. page.Items.Select(item => Coloured(item.Layer) && item is not ImageBox
                ? Recolour(item, Mix(full, Darkness(item.Colour) * (Solid(item) ? SolidTint : 1)))
                : item)],
        };
    }

    /// <summary>How dark an ink is, 0 for paper white to 1 for black, by its luminance.</summary>
    public static double Darkness(Rgb ink) => 1 - (((0.299 * ink.R) + (0.587 * ink.G) + (0.114 * ink.B)) / 255.0);

    /// <summary>The color <paramref name="share"/> of the way from paper white to <paramref name="full"/>, as a halftone of it prints.</summary>
    public static Rgb Mix(Rgb full, double share)
    {
        byte Channel(byte c) => (byte)Math.Clamp(Math.Round(255 - (share * (255 - c))), 0, 255);
        return new Rgb(Channel(full.R), Channel(full.G), Channel(full.B));
    }

    private static SceneItem Recolour(SceneItem item, Rgb ink) => item switch
    {
        DiscBand band => band with { Colour = ink },
        RectFill rect => rect with { Colour = ink },
        TextRun text => text with { Colour = ink },
        _ => item,
    };
}
