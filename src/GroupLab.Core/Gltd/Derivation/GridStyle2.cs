using System.Globalization;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Gltd.Derivation;

/// <summary>
/// Measurement grid style 2, NOTES-FROM-PLANNING.md entries 226 and 227 section 1 and question 59: the zeroing grids redrawn so they can be
/// read through a scope. Everything the style fixes is here, so the renderer, the marker rule and the tests agree on one set of numbers.
///
/// <para>The lattice is the style 1 lattice (lines at <c>round(half * i / divisions)</c>), but only the lines inside the drawn field are
/// drawn, and the field has its own half-extents across and up and down. Lines come in three weights: every line, every
/// <c>majorEvery</c>-th line, and every <c>wholeEvery</c>-th line, which is one whole unit (a mil or an MOA) and includes the axes.</para>
///
/// <para>The weights follow the visibility rule of entry 226 section 3 as far as a line can: at 100 yd one arcminute seen through a 6x
/// scope is 44 dmm on the paper and through a 10x scope 27 dmm. The whole-unit lines are 30 dmm (1.1 arcminutes at 10x, 0.7 at 6x), the
/// half-unit lines 20 dmm and the fine lines 6 dmm, three to seven times the old weights. The fine lines are for counting at higher power,
/// and at 10 dmm a touching pair of .308 holes with one of them on a crossing of two fine lines was found as one hole. A dark line on white is seen well below the
/// arcminute a letter needs, and applying 3 to 4 arcminutes to a line's width would make it wider than half a fine square at 6x and hide
/// the holes on it, so the rule is applied in full to what has to be recognised rather than merely seen: the labels (130 dmm tall,
/// 3.0 arcminutes at 6x and 4.9 at 10x) and the aiming ring (200 dmm, 4.5 arcminutes at 6x).</para>
/// </summary>
public static class GridStyle2
{
    public const int Style = 2;

    public const int FineStroke = 6;
    public const int MajorStroke = 20;
    public const int WholeStroke = 30;

    /// <summary>Cap height of the line labels, dmm.</summary>
    public const int LabelCap = 130;

    /// <summary>Cap height of the scale statement above the grid, dmm; it is read at the bench, not through the scope.</summary>
    public const int StatementCap = 36;

    /// <summary>Clear space kept around a label: a grid line that would cross it is broken there.</summary>
    public const int LabelMargin = 15;

    /// <summary>Gap between an axis and the labels beside it.</summary>
    public const int LabelGap = 30;

    /// <summary>The statement's bottom sits this far above the field, clear of the marker row field-ring-1 puts there.</summary>
    public const int StatementClearance = 110;

    public const int RulerThickness = 12;
    public const int RulerTick = 20;
    public const int RulerTickWidth = 8;

    /// <summary>The page width the statement may use, between the two top codes of corners-1.</summary>
    public const int StatementWidth = 1320;

    /// <summary>Font size, dmm, of a cap height, for Helvetica's 718 per 1000.</summary>
    public static int FontSizeForCap(int cap) => (int)Math.Round(cap * 1000.0 / 718.0);

    /// <summary>One unit of the grid at its stated distance, in dmm; null for a unit that has no angle.</summary>
    public static double? UnitDmm(MeasurementGrid g)
    {
        double distanceDmm = g.Distance * (g.DistanceUnit == DistanceUnit.Yards ? 9144.0 : 10000.0);
        return g.Unit switch
        {
            GridUnit.Moa => distanceDmm * Math.Tan(Math.PI / 180.0 / 60.0),
            GridUnit.Mil => distanceDmm * 0.001,
            _ => null,
        };
    }

    /// <summary>The lattice lines inside a field of half-extent <paramref name="field"/>: index and offset, ascending.</summary>
    public static IReadOnlyList<(int Index, int Offset)> Lines(MeasurementGrid g, int field)
    {
        var offsets = MeasurementGridLines.Offsets(g.Half, g.Divisions);
        var lines = new List<(int, int)>();
        for (int i = -g.Divisions; i <= g.Divisions; i++)
        {
            int offset = Math.Sign(i) * offsets[Math.Abs(i)];
            if (Math.Abs(offset) <= field)
            {
                lines.Add((i, offset));
            }
        }

        return lines;
    }

    /// <summary>0 fine, 1 major, 2 whole unit, which the axes are.</summary>
    public static int Weight(MeasurementGrid g, int index) =>
        index % (g.WholeEvery ?? int.MaxValue) == 0 ? 2 : index % g.MajorEvery == 0 ? 1 : 0;

    public static int Stroke(int weight) => weight switch { 2 => WholeStroke, 1 => MajorStroke, _ => FineStroke };

    /// <summary>A line's value in the grid's unit: "0.5" and "1.0" where there are half units, "1", "2" and "3" where there are not.</summary>
    public static string Label(MeasurementGrid g, int index)
    {
        int whole = g.WholeEvery ?? 1;
        double value = Math.Abs(index) / (double)whole;
        return g.MajorEvery < whole ? value.ToString("0.0##", CultureInfo.InvariantCulture) : value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string UnitName(MeasurementGrid g) => g.Unit == GridUnit.Moa ? "MOA" : "mil";

    private static string Length(MeasurementGrid g, double dmm) => g.DistanceUnit == DistanceUnit.Yards
        ? (dmm / 254.0).ToString("0.00", CultureInfo.InvariantCulture) + " in"
        : (dmm / 100.0).ToString("0.0#", CultureInfo.InvariantCulture) + " cm";

    private static string Units(MeasurementGrid g, double units) => units.ToString("0.##", CultureInfo.InvariantCulture) + " " + UnitName(g);

    /// <summary>The ruler printed under the statement: 4 in on a yard sheet, 10 cm on a metre sheet, with a tick at every inch or centimetre.</summary>
    public static (int Length, int Ticks, string Words) Ruler(MeasurementGrid g) =>
        g.DistanceUnit == DistanceUnit.Yards ? (1016, 4, "4 in") : (1000, 10, "10 cm");

    /// <summary>
    /// The scale statement, three lines: what a small square is, what the heavier lines are, and the check that the page printed at full
    /// size. Every number is worked out from the grid block, so it cannot disagree with the lines.
    /// </summary>
    public static IReadOnlyList<string> Statement(MeasurementGrid g)
    {
        if (UnitDmm(g) is not { } unit)
        {
            return [];
        }

        int whole = g.WholeEvery ?? 1;
        string distance = string.Create(CultureInfo.InvariantCulture, $"{g.Distance} {(g.DistanceUnit == DistanceUnit.Yards ? "yd" : "m")}");
        double fine = 1.0 / whole, major = (double)g.MajorEvery / whole;
        string first = $"Each small square is {Units(g, fine)}: {Length(g, fine * unit)} at {distance}";
        string second = g.MajorEvery < whole
            ? $"Bold lines every {Units(g, major)} ({Length(g, major * unit)}), heaviest every {Units(g, 1)} ({Length(g, unit)})"
            : $"Heaviest lines every {Units(g, 1)} ({Length(g, unit)})";
        string third = $"Print at 100 percent. Check with a ruler: the bar below is {Ruler(g).Words} long";
        return [first, second, third];
    }
}
