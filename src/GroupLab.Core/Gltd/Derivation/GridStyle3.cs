using System.Globalization;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Gltd.Derivation;

/// <summary>
/// Measurement grid style 3, design C3: NOTES-FROM-PLANNING.md entries 250 section 3, 251 and 252, chosen by Alan with Jylee and Unholy.
/// Squares of a scope's subtension (0.2 mil, or 0.5 MOA), one click's tick between the lines along the centre cross and the frame, the
/// numbers outside the grid on all four sides and nothing written on it, and a legend above that can be read through the scope. Everything
/// the style fixes is here, so the renderer, the marker rule, the encoder and the tests agree on one set of numbers.
///
/// <para>The lattice is style 2's, the drawn field has its own half-extents across and up and down, and every lattice line inside it is
/// drawn. Weights: the axes and the frame heaviest (2 mm, the drawing's 8 px at 96 an inch); every whole unit heavier on an MOA sheet (a
/// mil sheet's whole units are its axes and frame); every other line fine (0.5 mm, as entry 251 asks, and under style 2's 0.6 mm, which
/// entry 196's touching pair on a crossing was measured against).</para>
/// </summary>
public static class GridStyle3
{
    public const int Style = 3;

    public const int FineStroke = 5;
    public const int WholeStroke = 12;
    public const int HeavyStroke = 20;

    /// <summary>A click's tick: its width, and how far it reaches from the line it stands on (both ways on the cross, inward on the frame).</summary>
    public const int TickStroke = 5;
    public const int TickReach = 30;

    /// <summary>Cap heights of the numbers outside the grid, dmm: every line, and a whole unit, which is larger and bold.</summary>
    public const int NumberCap = 40;
    public const int WholeNumberCap = 46;

    /// <summary>Gap between the frame's outer edge and the numbers.</summary>
    public const int NumberGap = 16;

    /// <summary>The legend's three lines, cap heights in dmm: the unit and distance, the square, the tick. 100 dmm is 0.39 in.</summary>
    public const int LegendCap = 100;
    public const int SquareCap = 80;
    public const int TickCap = 60;

    /// <summary>Baselines of the legend's three lines from the top of the page, clear of the numbers above the grid.</summary>
    public static readonly int[] LegendBaselines = [180, 295, 395];

    /// <summary>Where the numbers above the grid begin, dmm from the top of the page: just below the legend.</summary>
    public const int NumbersTop = 426;

    /// <summary>The grid's centre height when its field reaches <paramref name="halfY"/> up and down and it hangs just below the legend.</summary>
    public static int CentreY(int halfY) => NumbersTop + WholeNumberCap + NumberGap + (HeavyStroke / 2) + halfY;

    /// <summary>The width the legend may use, between the two top codes of corners-1.</summary>
    public const int LegendWidth = 1320;

    /// <summary>The check bar below the numbers: its top this far below the frame, its thickness, and its caption's cap height.</summary>
    public const int BarBelowFrame = 150;
    public const int BarThickness = 12;
    public const int BarCaptionCap = 32;

    /// <summary>Clear space a marker keeps from a number beyond its own quiet zone.</summary>
    public const int MarkerToNumber = 10;

    /// <summary>Font size, dmm, of a cap height, for Helvetica's 718 per 1000 (the bold face's too).</summary>
    public static int FontSizeForCap(int cap) => GridStyle2.FontSizeForCap(cap);

    /// <summary>The lattice lines inside a field of half-extent <paramref name="field"/>: index and offset, ascending.</summary>
    public static IReadOnlyList<(int Index, int Offset)> Lines(MeasurementGrid g, int field) => GridStyle2.Lines(g, field);

    /// <summary>The ticks' offsets: halfway between each pair of neighbouring lines inside the field, one click.</summary>
    public static IReadOnlyList<int> Ticks(MeasurementGrid g, int field)
    {
        var lines = Lines(g, field);
        return [.. lines.Zip(lines.Skip(1), (a, b) => (a.Offset + b.Offset) / 2)];
    }

    /// <summary>0 fine, 1 whole unit, 2 heaviest: the axes, the frame, and a mil sheet's whole mils.</summary>
    public static int Weight(MeasurementGrid g, int index, int offset, int field)
    {
        if (index == 0 || Math.Abs(offset) >= Lines(g, field).Max(l => Math.Abs(l.Offset)))
        {
            return 2;
        }

        return index % (g.WholeEvery ?? int.MaxValue) == 0 ? g.Unit == GridUnit.Mil ? 2 : 1 : 0;
    }

    public static int Stroke(int weight) => weight switch { 2 => HeavyStroke, 1 => WholeStroke, _ => FineStroke };

    /// <summary>True for a whole unit, whose number is larger and bold.</summary>
    public static bool IsWhole(MeasurementGrid g, int index) => index % (g.WholeEvery ?? 1) == 0;

    /// <summary>A line's distance from the aim in the grid's unit: "0", "0.2" ... "1.0" in mils, "0", "0.5", "1" ... "3.5" in MOA.</summary>
    public static string Number(MeasurementGrid g, int index)
    {
        int whole = g.WholeEvery ?? 1;
        double value = Math.Abs(index) / (double)whole;
        return index == 0 ? "0" : value.ToString(g.Unit == GridUnit.Mil ? "0.0" : "0.#", CultureInfo.InvariantCulture);
    }

    /// <summary>The legend's words: the unit and distance, the square's size, and what a tick is.</summary>
    public static (string Heading, string Square, string Tick) Legend(MeasurementGrid g)
    {
        string unit = g.Unit == GridUnit.Moa ? "MOA" : "MIL";
        string distance = string.Create(CultureInfo.InvariantCulture, $"{g.Distance} {(g.DistanceUnit == DistanceUnit.Yards ? "YD" : "M")}");
        int whole = g.WholeEvery ?? 1;
        string square = (1.0 / whole).ToString("0.0##", CultureInfo.InvariantCulture);
        string tick = g.Unit == GridUnit.Moa ? $"1/{2 * whole}" : (0.5 / whole).ToString("0.0##", CultureInfo.InvariantCulture);
        return ($"{unit} · {distance}", $"= {square} {unit}", $"TICK = {tick} {unit} (1 CLICK)");
    }

    /// <summary>The check bar: 4 in on a yard sheet, 10 cm on a metre sheet, with a tick at every inch or centimetre, and its caption.</summary>
    public static (int Length, int Ticks, string Caption) Bar(MeasurementGrid g) => g.DistanceUnit == DistanceUnit.Yards
        ? (1016, 4, "This bar is 4 in. Print at 100 percent, never Fit to page.")
        : (1000, 10, "This bar is 10 cm. Print at 100 percent, never Fit to page.");
}
