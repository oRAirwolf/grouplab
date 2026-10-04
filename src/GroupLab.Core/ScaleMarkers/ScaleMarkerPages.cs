using System.Globalization;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Imaging;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Markers;

namespace GroupLab.Core.ScaleMarkers;

/// <summary>The paper a marker page is printed on.</summary>
public enum MarkerPaper
{
    Letter,
    A4,

    /// <summary>A 4 by 6 inch thermal label: the board stickers of set A (entry 358's label printers).</summary>
    Label4x6,
}

/// <summary>
/// Entry 365, the pages that are printed: the corner brackets (A), the scale bars (B) and the board stickers (C), drawn as the concepts show
/// them (boards Brackets, Bar and Backer), at actual size. They are pages, not targets: nothing on them is shot at and nothing reads them as a
/// sheet, so they are drawn here rather than kept in the library, and print, save as PDF and go to a label printer's own app the way a sheet's
/// pages do. Every code is a tag36h11 code from the range kept for markers, and every piece says in print what it is and its size.
/// </summary>
public static class ScaleMarkerPages
{
    private const double U = 20; // half-dmm a millimetre

    private static readonly Rgb Black = new(0, 0, 0), Grey = new(128, 128, 128);

    /// <summary>The page's size in millimetres.</summary>
    public static (double Width, double Height) Size(MarkerPaper paper, bool sideways = false)
    {
        var (w, h) = paper switch
        {
            MarkerPaper.A4 => (210.0, 297.0),
            MarkerPaper.Label4x6 => (101.6, 152.4),
            _ => (215.9, 279.4),
        };
        return sideways ? (h, w) : (w, h);
    }

    /// <summary>What a page is called, for a file name and a heading.</summary>
    public static string Title(MarkerKind kind, MarkerPaper paper) => (kind switch
    {
        MarkerKind.Bracket => "GroupLab corner brackets",
        MarkerKind.InchBar or MarkerKind.MetricBar => "GroupLab scale bars",
        _ => "GroupLab board stickers",
    }) + paper switch { MarkerPaper.A4 => ", A4", MarkerPaper.Label4x6 => ", 4x6 label", _ => ", Letter" };

    /// <summary>A: the four brackets, nested in pairs as two square frames, with the instructions in the frames' middles.</summary>
    public static Scene Brackets(MarkerPaper paper)
    {
        var (w, h) = Size(paper == MarkerPaper.A4 ? MarkerPaper.A4 : MarkerPaper.Letter);
        var items = new List<SceneItem>();
        double frame = (2 * ScaleMarkerLayout.ArmWidth) + ScaleMarkerLayout.ArmLength, gap = 8;
        double x0 = (w - frame) / 2, y0 = (h - (2 * frame) - gap) / 2, yb = y0 + frame + gap;
        double near = ScaleMarkerLayout.ArmWidth, far = frame - ScaleMarkerLayout.ArmWidth;
        Bracket(items, 1, new PointD(x0 + near, y0 + near));
        Bracket(items, 3, new PointD(x0 + far, y0 + far));
        Bracket(items, 2, new PointD(x0 + far, yb + near));
        Bracket(items, 4, new PointD(x0 + near, yb + far));
        Lines(items, x0 + near + 3, y0 + near + 8, 3,
        [
            ("GroupLab corner brackets", true),
            ("Print at Actual size (100%).", false),
            ("Never Fit to page.", false),
            ("Cut out the four L shapes on", false),
            ("the dashed lines. Card stock", false),
            ("lasts longer.", false),
            ("Lay each flat against its", false),
            ("corner of the target:", false),
            ("1 top left, 2 top right,", false),
            ("3 bottom right, 4 bottom left.", false),
        ]);
        Lines(items, x0 + near + 3, yb + near + 8, 3,
        [
            ("The inside corner of each L", false),
            ("is the target's corner.", false),
            ("A piece may tuck slightly under", false),
            ("the target's edge, but it must", false),
            ("lie flat on the same surface.", false),
            ("Keep every code uncovered.", false),
            ("Without a printer check the", false),
            ("scale can be off by up to", false),
            ("1.5 percent (Settings, Printers).", false),
        ]);
        return new Scene((long)Math.Round(w * U), (long)Math.Round(h * U), 0, items);
    }

    /// <summary>B: two bars on a sideways page, inch bars on Letter and metric bars on A4.</summary>
    public static Scene Bars(MarkerPaper paper)
    {
        bool metric = paper == MarkerPaper.A4;
        var (w, h) = Size(metric ? MarkerPaper.A4 : MarkerPaper.Letter, sideways: true);
        var items = new List<SceneItem>();
        double length = metric ? ScaleMarkerLayout.MetricBarLength : ScaleMarkerLayout.InchBarLength;
        double left = (w - length) / 2;
        Lines(items, left, 22, 3.2,
        [
            ("GroupLab scale bars", true),
            ((metric ? "Print on A4" : "Print on Letter") + " turned sideways, at Actual size (100%), never Fit to page. Cut along the dashed lines.", false),
            ("Lay a bar flat beside the target, on the same surface, codes uncovered. One bar gives the scale; two in an L give the camera's", false),
            ("angle too; two end to end give " + (metric ? "500 mm" : "20 inches") + " for a poster. Without a printer check the scale can be off by up to 1.5 percent.", false),
        ]);
        Bar(items, metric, 1, new PointD(left, 90), w);
        Bar(items, metric, 2, new PointD(left, 155), w);
        return new Scene((long)Math.Round(w * U), (long)Math.Round(h * U), 0, items);
    }

    /// <summary>C: the stickers, every set on Letter or A4 (a row a set), set A alone on a 4 by 6 label.</summary>
    public static Scene Stickers(MarkerPaper paper)
    {
        var (w, h) = Size(paper);
        var items = new List<SceneItem>();
        const double pitch = 46, cell = 44;
        bool label = paper == MarkerPaper.Label4x6;
        char[] sets = label ? ['A'] : ScaleMarkerLayout.Sets;
        double x0 = (w - ((2 * pitch) + (label ? 0 : 2 * pitch) - (pitch - cell))) / 2;
        double y0 = label ? 26 : 44;
        Lines(items, x0, label ? 8 : 16, label ? 2.6 : 3.2, label
            ? [("GroupLab board stickers, set A", true), ("Stick all four near the corners of one board.", false)]
            : [("GroupLab board stickers", true), ("Print at Actual size (100%), or on adhesive labels. Each row is one set: put the four of one set", false),
                ("on one target board, near its corners, where no target will cover them.", false)]);
        for (int s = 0; s < sets.Length; s++)
        {
            for (int i = 0; i < 4; i++)
            {
                double cx = x0 + ((label ? i % 2 : i) * pitch), cy = y0 + ((label ? i / 2 : s) * pitch);
                Dashes(items, [new(cx, cy), new(cx + cell, cy), new(cx + cell, cy + cell), new(cx, cy + cell)]);
                var tag = ScaleMarkerLayout.Sticker(sets[s], i + 1);
                Tag(items, tag.Id, new PointD(cx + (cell / 2), cy + 19), tag.Side);
                Text(items, cx + (cell / 2), cy + 40, 2.6, string.Create(CultureInfo.InvariantCulture, $"GroupLab board sticker {sets[s]}{i + 1}"), TextAnchor.Centre);
            }
        }

        double notes = y0 + ((label ? 2 : sets.Length) * pitch) + 6;
        Lines(items, x0, notes, label ? 2.6 : 3.2, label
            ? [("Then on Targets, Scale markers, choose Measure a", false), ("board, and photograph the board with any GroupLab", false), ("sheet on it, once.", false)]
            : [("Then on Targets, Scale markers, choose Measure a board, and photograph the board once with any GroupLab sheet", false),
                ("on it. Every photo of a target on that board then has its scale, with nothing placed or typed. If the board bends", false),
                ("or swells, GroupLab says so and asks you to measure it again.", false)]);
        return new Scene((long)Math.Round(w * U), (long)Math.Round(h * U), 0, items);
    }

    /// <summary>The page as a PDF at actual size, as it is printed or shared.</summary>
    public static byte[] Pdf(MarkerKind kind, MarkerPaper paper) => Rendering.Pdf.PdfWriter.Write(Pages(kind, paper));

    /// <summary>A file name for the page.</summary>
    public static string FileName(MarkerKind kind, MarkerPaper paper) => (kind switch
    {
        MarkerKind.Bracket => "grouplab-corner-brackets",
        MarkerKind.InchBar or MarkerKind.MetricBar => "grouplab-scale-bars",
        _ => "grouplab-board-stickers",
    }) + paper switch { MarkerPaper.A4 => "-a4", MarkerPaper.Label4x6 => "-4x6", _ => "-letter" };

    /// <summary>Every page of a kind on one paper (one page each).</summary>
    public static IReadOnlyList<Scene> Pages(MarkerKind kind, MarkerPaper paper) => kind switch
    {
        MarkerKind.Bracket => [Brackets(paper)],
        MarkerKind.InchBar or MarkerKind.MetricBar => [Bars(paper)],
        _ => [Stickers(paper)],
    };

    private static void Bracket(List<SceneItem> items, int piece, PointD corner)
    {
        var (sx, sy) = ScaleMarkerLayout.Signs(piece);
        PointD At(PointD p) => new(corner.X + p.X, corner.Y + p.Y);
        Dashes(items, [.. ScaleMarkerLayout.BracketOutline(piece).Select(At)]);

        // The inside edges, where the target's edges lie, in a solid line.
        double a = ScaleMarkerLayout.ArmLength, t = 0.6;
        Box(items, At(new PointD(0, 0)), At(new PointD(sx * a, -sy * t)), SceneLayer.MeasurementGrid);
        Box(items, At(new PointD(0, 0)), At(new PointD(-sx * t, sy * a)), SceneLayer.MeasurementGrid);
        foreach (var tag in ScaleMarkerLayout.Bracket(piece))
        {
            Tag(items, tag.Id, At(tag.Centre), tag.Side);
        }

        double half = ScaleMarkerLayout.ArmWidth / 2;
        var digit = At(new PointD(-sx * half, -sy * half));
        Text(items, digit.X, digit.Y + 4.3, 12, piece.ToString(CultureInfo.InvariantCulture), TextAnchor.Centre, bold: true);
        var tags = ScaleMarkerLayout.Bracket(piece);
        double apart = Math.Sqrt(Math.Pow(tags[0].Centre.X - tags[1].Centre.X, 2) + Math.Pow(tags[0].Centre.Y - tags[1].Centre.Y, 2));
        var line = At(new PointD(sx * 3, -sy * half));
        var anchor = sx > 0 ? TextAnchor.Left : TextAnchor.Right;
        Text(items, line.X, line.Y - 1.6, 2.6, string.Create(CultureInfo.InvariantCulture, $"GroupLab corner bracket {piece} of 4"), anchor, bold: true);
        Text(items, line.X, line.Y + 3.4, 2.4, string.Create(CultureInfo.InvariantCulture, $"codes {apart:0.00} mm apart"), anchor);
    }

    private static void Bar(List<SceneItem> items, bool metric, int number, PointD start, double pageWidth)
    {
        double length = metric ? ScaleMarkerLayout.MetricBarLength : ScaleMarkerLayout.InchBarLength, half = ScaleMarkerLayout.BarWidth / 2;
        Dashes(items, [new(2, start.Y - half), new(pageWidth - 2, start.Y - half)], closed: false);
        Dashes(items, [new(2, start.Y + half), new(pageWidth - 2, start.Y + half)], closed: false);
        foreach (var tag in ScaleMarkerLayout.Bar(metric, number))
        {
            Tag(items, tag.Id, new PointD(start.X + tag.Centre.X, start.Y + tag.Centre.Y), tag.Side);
        }

        // The ruler along the top: quarter inches, or every 5 mm, the whole units longest and numbered.
        double step = metric ? 5 : 25.4 / 4;
        int every = metric ? 2 : 4;
        double top = start.Y - half;
        for (int i = 0; i * step <= length + 1e-9; i++)
        {
            double x = start.X + (i * step);
            bool whole = i % every == 0;

            // Nothing is drawn over a code: the ruler stops a millimetre short of each, whose centres are its two ends.
            double code = metric ? ScaleMarkerLayout.MetricBarCode : ScaleMarkerLayout.InchBarCode;
            if (i * step < (code / 2) + 1 || length - (i * step) < (code / 2) + 1)
            {
                continue;
            }

            double tick = whole ? 5 : metric || i % 2 == 0 ? 3.5 : 2.2;
            Box(items, new PointD(x - 0.15, top), new PointD(x + 0.15, top + tick), SceneLayer.MeasurementGrid);
            int unit = i / every;
            if (whole && unit > 0 && x < start.X + length - 6)
            {
                Text(items, x, top + 8.6, 2.8, unit.ToString(CultureInfo.InvariantCulture), TextAnchor.Centre);
            }
        }

        string apart = metric ? "250 mm" : "10.000 in";
        Text(items, start.X + (length / 2), start.Y + 9.4, 3, string.Create(CultureInfo.InvariantCulture,
            $"GroupLab scale bar {number} of 2 · code centers {apart} apart · {(metric ? "cm" : "inches")}"), TextAnchor.Centre);
    }

    /// <summary>A tag36h11 code, its centre and side in millimetres, every module a whole number of half-dmm so a label printer can snap it.</summary>
    private static void Tag(List<SceneItem> items, int id, PointD centre, double side)
    {
        long size = (long)Math.Round(side * U), module = size / Tag36h11.Modules;
        long left = (long)Math.Round(centre.X * U) - (size / 2), top = (long)Math.Round(centre.Y * U) - (size / 2);
        var inked = Tag36h11.InkedModules(id);
        for (int row = 0; row < Tag36h11.Modules; row++)
        {
            for (int col = 0; col < Tag36h11.Modules;)
            {
                if (!inked[row, col])
                {
                    col++;
                    continue;
                }

                int first = col;
                while (col < Tag36h11.Modules && inked[row, col])
                {
                    col++;
                }

                items.Add(new RectFill(SceneLayer.Markers, Black, left + (first * module), top + (row * module), (col - first) * module, module)
                {
                    Module = new ModuleCell(module, Tag36h11.Modules, first, row),
                });
            }
        }
    }

    private static void Box(List<SceneItem> items, PointD a, PointD b, SceneLayer layer)
    {
        long x = (long)Math.Round(Math.Min(a.X, b.X) * U), y = (long)Math.Round(Math.Min(a.Y, b.Y) * U);
        long w = Math.Max(1, (long)Math.Round(Math.Abs(b.X - a.X) * U)), h = Math.Max(1, (long)Math.Round(Math.Abs(b.Y - a.Y) * U));
        items.Add(new RectFill(layer, Black, x, y, w, h));
    }

    /// <summary>A dashed grey line round the polygon (or along it where it is not closed), axis-aligned sides only: where to cut.</summary>
    private static void Dashes(List<SceneItem> items, IReadOnlyList<PointD> corners, bool closed = true)
    {
        const double dash = 3, gap = 2, weight = 0.3;
        int sides = closed ? corners.Count : corners.Count - 1;
        for (int i = 0; i < sides; i++)
        {
            var a = corners[i];
            var b = corners[(i + 1) % corners.Count];
            double length = Math.Abs(b.X - a.X) + Math.Abs(b.Y - a.Y);
            double dx = Math.Sign(b.X - a.X), dy = Math.Sign(b.Y - a.Y);
            for (double s = 0; s < length; s += dash + gap)
            {
                double e = Math.Min(length, s + dash);
                var p = new PointD(a.X + (dx * s) - (dy == 0 ? 0 : weight / 2), a.Y + (dy * s) - (dx == 0 ? 0 : weight / 2));
                var q = new PointD(a.X + (dx * e) + (dy == 0 ? 0 : weight / 2), a.Y + (dy * e) + (dx == 0 ? 0 : weight / 2));
                long x = (long)Math.Round(Math.Min(p.X, q.X) * U), y = (long)Math.Round(Math.Min(p.Y, q.Y) * U);
                items.Add(new RectFill(SceneLayer.CutLines, Grey, x, y, Math.Max(1, (long)Math.Round(Math.Abs(q.X - p.X) * U)), Math.Max(1, (long)Math.Round(Math.Abs(q.Y - p.Y) * U))));
            }
        }
    }

    private static void Text(List<SceneItem> items, double x, double baseline, double size, string text, TextAnchor anchor, bool bold = false) =>
        items.Add(new TextRun(SceneLayer.Labels, Black, (long)Math.Round(x * U), (long)Math.Round(baseline * U), (long)Math.Round(size * U), text, anchor, bold));

    private static void Lines(List<SceneItem> items, double x, double top, double size, IReadOnlyList<(string Text, bool Bold)> lines)
    {
        double y = top;
        foreach (var (text, bold) in lines)
        {
            Text(items, x, y, bold ? size * 1.3 : size, text, TextAnchor.Left, bold);
            y += (bold ? size * 1.3 : size) * 1.45;
        }
    }
}
