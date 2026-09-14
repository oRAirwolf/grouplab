using GroupLab.Core.Gltd.Binary;

namespace GroupLab.Core.Rendering;

/// <summary>
/// What each item of a page belongs to. Conformance test 27 compares the blank and filled print modes of one definition
/// on every layer except <see cref="DataBlockContent"/>, which is the only thing the two modes may draw differently.
/// </summary>
public enum SceneLayer
{
    MeasurementGrid,
    Cells,
    Bulls,
    Labels,
    Markers,
    Codes,
    DataBlockFrame,
    DataBlockContent,
    Identifier,

    /// <summary>The print instruction along the bottom edge, drawn only when a print asks for it (NOTES-FROM-PLANNING.md entry 25 section 2).</summary>
    PrintNote,
}

public enum TextAnchor
{
    Left,
    Centre,
    Right,
}

/// <summary>
/// One filled primitive of a page. Every coordinate is an integer in half-dmm, twice the dmm of TARGET-SCHEMA.md
/// section 2, so that a disc of odd diameter or a stroke of odd width still has integer edges and nothing is rounded
/// between the definition and the page (conformance test 40). A paper ink is never an item: it lays no ink (section 3.3).
/// </summary>
public abstract record SceneItem(SceneLayer Layer, Rgb Colour);

/// <summary>
/// The ink between two concentric circles, or a whole disc when <see cref="InnerRadius"/> is 0. A ring set of
/// section 3.4 becomes a sequence of these: each band is one disc minus the next, so an annulus's ink extent is
/// exactly the difference of two declared diameters and there is no stroke width for a graphics stack to interpret.
/// In half-dmm a radius equals the declared diameter in dmm.
/// </summary>
public sealed record DiscBand(SceneLayer Layer, Rgb Colour, long CentreX, long CentreY, long OuterRadius, long InnerRadius)
    : SceneItem(Layer, Colour);

/// <summary>An axis-aligned filled rectangle, used for marker and QR modules, rules and grid lines.</summary>
public sealed record RectFill(SceneLayer Layer, Rgb Colour, long X, long Y, long Width, long Height) : SceneItem(Layer, Colour);

/// <summary>A line of Helvetica text. <see cref="X"/> is the anchor point and <see cref="Baseline"/> the baseline.</summary>
public sealed record TextRun(SceneLayer Layer, Rgb Colour, long X, long Baseline, long FontSize, string Text, TextAnchor Anchor)
    : SceneItem(Layer, Colour);

/// <summary>One printed sheet, in half-dmm from its top-left corner. A tiled definition has one scene per tile.</summary>
public sealed record Scene(long Width, long Height, int TileIndex, IReadOnlyList<SceneItem> Items)
{
    public const int UnitsPerDmm = 2;
}
