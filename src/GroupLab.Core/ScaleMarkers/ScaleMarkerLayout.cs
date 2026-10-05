using System.Globalization;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.ScaleMarkers;

/// <summary>The kinds of thing in a photo whose true size GroupLab knows, NOTES-FROM-PLANNING.md entry 365 (concepts A to D).</summary>
public enum MarkerKind
{
    /// <summary>A: an L shaped piece placed near one of the target's corners, two codes on it (entry 375: anywhere close, cut roughly).</summary>
    Bracket,

    /// <summary>B: a printed strip with a code at each end, 10.000 in apart.</summary>
    InchBar,

    /// <summary>B on A4: the same strip with its codes 250 mm apart.</summary>
    MetricBar,

    /// <summary>C: one code sticker on a target board, where it is was measured once.</summary>
    Sticker,

    /// <summary>D: a bank card, nothing printed.</summary>
    Card,

    /// <summary>Entry 372: a thermal printer's scale label, rows of two codes across its width (<see cref="ScaleLabels"/>).</summary>
    Label,
}

/// <summary>One printed code of a marker: its identifier and its four corners in its piece's own millimetres, top left first, clockwise.</summary>
public sealed record MarkerTag(int Id, MarkerKind Kind, int Piece, PointD Centre, double Side)
{
    /// <summary>The corners in the piece's millimetres, x right and y down as printed, in the order the detector returns them.</summary>
    public PointD[] Corners =>
    [
        new(Centre.X - (Side / 2), Centre.Y - (Side / 2)), new(Centre.X + (Side / 2), Centre.Y - (Side / 2)),
        new(Centre.X + (Side / 2), Centre.Y + (Side / 2)), new(Centre.X - (Side / 2), Centre.Y + (Side / 2)),
    ];
}

/// <summary>
/// Entry 365 section 0: the identifiers and the geometry of every scale marker GroupLab prints. AprilTag tag36h11 is the sheets' family
/// (docs/FIDUCIAL-DECISION.md); the sheets number their markers from 0 in raster order and the built-in library's largest assembly reaches 150,
/// so the top 32 of the family's 587 codes, 555 to 586, are kept for markers: a marker can never be taken for a sheet's code or a sheet's for a
/// marker's. The validator warns of any sheet whose markers would reach them.
/// <para>
/// Each piece is described in its own millimetres, x right and y down as it is printed, which is how its codes are found again: two codes
/// of one bracket or one bar are a rigid body whose shape is known exactly, wherever on the page it was printed.
/// </para>
/// </summary>
public static class ScaleMarkerLayout
{
    /// <summary>The first identifier kept for markers: entry 372's scale labels from 470, entry 365's markers from 555.</summary>
    public const int First = ScaleLabels.First;

    /// <summary>The last: tag36h11 holds 587 codes, 0 to 586.</summary>
    public const int Last = 586;

    /// <summary>Brackets 1 to 4 (555 to 562), two codes each; inch bars 1 and 2 (563 to 566); metric bars 1 and 2 (567 to 570); four sets of four board stickers (571 to 586).</summary>
    public const int BracketFirst = 555, InchBarFirst = 563, MetricBarFirst = 567, StickerFirst = 571;

    public static bool IsMarker(int id) => id is >= First and <= Last;

    /// <summary>A code's modules, 8 by 8 with its black border, true where inked: what is printed and what a test or trial draws.</summary>
    public static bool[,] Inked(int id) => Rendering.Markers.Tag36h11.InkedModules(id);

    public const int Modules = Rendering.Markers.Tag36h11.Modules;

    /// <summary>
    /// A bracket's arm: 22 mm wide, reaching 81 mm along the target's edge from its corner, a 16 mm code near its end with 3 mm of white each
    /// side. Two pairs nest as square frames 125 mm across, one above the other on Letter or A4. Larger codes on wider, shorter arms (22 mm
    /// on 28 by 70 mm) measured worse on computer-made photos, 0.15 against 0.04 percent on a 12 in target: their white border was too thin.
    /// </summary>
    public const double ArmWidth = 22, ArmLength = 81, BracketCode = 16;

    /// <summary>Where a bracket's code sits along its arm, from the corner: the arm's end less half its width.</summary>
    public const double BracketCodeAt = ArmLength - (ArmWidth / 2);

    /// <summary>
    /// A bar's codes, their centres 10.000 in (254 mm) apart on the inch bar and 250 mm on the metric one. The inch bar's codes are 12 mm, the
    /// most a Letter page sideways holds inside a quarter inch margin at each end (254 + 12 = 266 of 279.4 mm); A4 has room for 16 mm.
    /// </summary>
    public const double InchBarCode = 12, MetricBarCode = 16, InchBarLength = 254, MetricBarLength = 250, BarWidth = 24;

    /// <summary>A board sticker's code, 30 mm.</summary>
    public const double StickerCode = 30;

    /// <summary>The four sets of board stickers, one set a board.</summary>
    public static readonly char[] Sets = ['A', 'B', 'C', 'D'];

    /// <summary>
    /// Bracket <paramref name="piece"/> (1 top left, 2 top right, 3 bottom right, 4 bottom left), its origin the inside corner of the L, which is
    /// the target's corner: the arms run outside the target's two edges, away from it, so x and y point into the target for piece 1.
    /// </summary>
    public static MarkerTag[] Bracket(int piece)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(piece, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(piece, 4);
        var (sx, sy) = Signs(piece);
        double half = ArmWidth / 2;
        int id = BracketFirst + (2 * (piece - 1));
        return
        [
            new MarkerTag(id, MarkerKind.Bracket, piece, new PointD(sx * BracketCodeAt, -sy * half), BracketCode),
            new MarkerTag(id + 1, MarkerKind.Bracket, piece, new PointD(-sx * half, sy * BracketCodeAt), BracketCode),
        ];
    }

    /// <summary>The direction into the target from bracket <paramref name="piece"/>'s corner, along x and y.</summary>
    public static (int X, int Y) Signs(int piece) => piece switch
    {
        1 => (1, 1),
        2 => (-1, 1),
        3 => (-1, -1),
        _ => (1, -1),
    };

    /// <summary>The L's outline in its own millimetres, for the cut line: the outer corner first, then round.</summary>
    public static PointD[] BracketOutline(int piece)
    {
        var (sx, sy) = Signs(piece);
        PointD P(double x, double y) => new(sx * x, sy * y);
        return [P(-ArmWidth, -ArmWidth), P(ArmLength, -ArmWidth), P(ArmLength, 0), P(0, 0), P(0, ArmLength), P(-ArmWidth, ArmLength)];
    }

    /// <summary>Bar <paramref name="number"/> (1 or 2), inch or metric: its first code at the origin and its second along x.</summary>
    public static MarkerTag[] Bar(bool metric, int number)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(number, 2);
        int id = (metric ? MetricBarFirst : InchBarFirst) + (2 * (number - 1));
        var kind = metric ? MarkerKind.MetricBar : MarkerKind.InchBar;
        double length = metric ? MetricBarLength : InchBarLength;
        double code = metric ? MetricBarCode : InchBarCode;
        return [new MarkerTag(id, kind, number, new PointD(0, 0), code), new MarkerTag(id + 1, kind, number, new PointD(length, 0), code)];
    }

    /// <summary>Sticker <paramref name="number"/> (1 to 4) of set <paramref name="set"/> (A to D), alone in its own millimetres.</summary>
    public static MarkerTag Sticker(char set, int number)
    {
        int s = Array.IndexOf(Sets, set);
        ArgumentOutOfRangeException.ThrowIfNegative(s);
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(number, 4);
        return new MarkerTag(StickerFirst + (4 * s) + (number - 1), MarkerKind.Sticker, (4 * s) + number, new PointD(0, 0), StickerCode);
    }

    /// <summary>Which printed code an identifier is, or null for a sheet's.</summary>
    public static MarkerTag? Tag(int id) => id switch
    {
        >= BracketFirst and < InchBarFirst => Bracket(((id - BracketFirst) / 2) + 1)[(id - BracketFirst) % 2],
        >= InchBarFirst and < MetricBarFirst => Bar(false, ((id - InchBarFirst) / 2) + 1)[(id - InchBarFirst) % 2],
        >= MetricBarFirst and < StickerFirst => Bar(true, ((id - MetricBarFirst) / 2) + 1)[(id - MetricBarFirst) % 2],
        >= StickerFirst and <= Last => Sticker(Sets[(id - StickerFirst) / 4], ((id - StickerFirst) % 4) + 1),
        _ => ScaleLabels.Tag(id),
    };

    /// <summary>The set a sticker belongs to.</summary>
    public static char SetOf(int id) => Sets[(id - StickerFirst) / 4];

    /// <summary>An ID-1 card (ISO/IEC 7810): 85.60 by 53.98 mm, its corners rounded about 3.18 mm, 0.76 mm thick.</summary>
    public const double CardWidth = 85.60, CardHeight = 53.98, CardRadius = 3.18, CardThickness = 0.76;

    /// <summary>The name a person reads for a piece, "corner bracket 2" or "scale bar 1".</summary>
    public static string Name(MarkerKind kind, int piece) => kind switch
    {
        MarkerKind.Bracket => string.Create(CultureInfo.InvariantCulture, $"corner bracket {piece}"),
        MarkerKind.InchBar or MarkerKind.MetricBar => string.Create(CultureInfo.InvariantCulture, $"scale bar {piece}"),
        MarkerKind.Sticker => string.Create(CultureInfo.InvariantCulture, $"board sticker {Sets[(piece - 1) / 4]}{((piece - 1) % 4) + 1}"),
        MarkerKind.Label => "scale label",
        _ => "bank card",
    };
}
