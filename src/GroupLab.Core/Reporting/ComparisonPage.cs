using System.Globalization;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Pdf;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Reporting;

/// <summary>One load as the exported comparison names it: its name, the line beneath it (its shots and where they came from), and its speed where the record book has one.</summary>
public sealed record ExportedLoad(string Name, string Detail, string? Speed);

/// <summary>One row of the exported comparison's figures: the load, its shots, and each figure, with the 95 percent interval of the two that have one, as the screen words them.</summary>
public sealed record ComparisonRow(string Load, string Shots, string Sigma, string SigmaInterval, string MeanRadius, string MeanRadiusInterval, string ExtremeSpread);

/// <summary>
/// Everything the exported Compare loads page carries, suggestion 61 (Discord, Alan's yes on 2026-11-24): the groups side by side at one
/// scale, each load's figures with their intervals, what the difference means, the tests and what they could detect, and a modest footer
/// naming GroupLab and grouplab.org. Every word comes from <see cref="ComparisonPages.For"/>, so the computer and the phone make the same page.
/// </summary>
public sealed record ComparisonPage(
    string Date,
    PageSize Paper,
    LoadComparisonReport Report,
    IReadOnlyList<ExportedLoad> Loads,
    string Summary,
    IReadOnlyList<ComparisonRow> Rows,
    string GridWords,
    double HalfWidthInches,
    double GridInches,
    IReadOnlyList<string> TopGun,
    string Footer);

/// <summary>
/// Lays the comparison out through GroupLab's own <see cref="PdfWriter"/>, as the one-page report is laid out, and draws the same layout as a
/// picture for posting where a PDF does not show, such as a Discord channel. The PDF takes a second page only when the comparison does not fit
/// on one; the picture is always one, as tall as it needs.
/// </summary>
public static class ComparisonPages
{
    private const long Inch = 508;

    /// <summary>The footer's right-hand words, after the version: modest, a name and where to find it, never a watermark.</summary>
    public const string Branding = "grouplab.org";

    /// <summary>The picture's resolution, dots an inch: a Letter page is 1275 pixels across, sharp in a chat window and small to send.</summary>
    public const double PictureDpi = 150;

    private static readonly Rgb Ink = new(20, 20, 20);
    private static readonly Rgb Grey = new(95, 95, 95);
    private static readonly Rgb Rule = new(190, 190, 190);
    private static readonly Rgb GridInk = new(225, 225, 225);
    private static readonly Rgb Shade = new(242, 242, 242);

    /// <summary>The loads' colors as the light theme draws them on the Compare screen, so the page and the screen name each load alike.</summary>
    private static readonly Rgb[] LoadInks = [new(0x96, 0x5d, 0x12), new(0x2f, 0x6f, 0xc4), new(0x2e, 0x7d, 0x32), new(0x9c, 0x3f, 0x8f), new(0x5f, 0x5a, 0x00)];

    /// <summary>The color load <paramref name="index"/> is drawn in.</summary>
    public static Rgb LoadInk(int index) => LoadInks[index % LoadInks.Length];

    /// <summary>
    /// The page for <paramref name="report"/>, its loads in the order chosen with the screen's own words beneath each, sized in the person's
    /// units and, where every group was shot at one <paramref name="distance"/>, as angles too.
    /// </summary>
    public static ComparisonPage For(LoadComparisonReport report, IReadOnlyList<ExportedLoad> loads, string summary, IReadOnlyList<string> topGun,
        UnitSettings units, double? distance, PageSize paper, string date, string version)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(loads);
        ArgumentNullException.ThrowIfNull(units);
        if (loads.Count != report.Groups.Count)
        {
            throw new ArgumentException("one exported load is needed for each compared group", nameof(loads));
        }

        string Sized(double inches) => units.AngleText(inches, distance) is { } angle ? $"{units.Length(inches)} ({angle})" : units.Length(inches);
        string Interval(Estimate e) => $"95% interval {units.Number(e.Lower)} to {units.Length(e.Upper)}";
        var rows = report.Groups.Select(g => new ComparisonRow(
            g.Name,
            g.Shots.ToString(CultureInfo.InvariantCulture),
            Sized(g.Rayleigh.Sigma.Value),
            Interval(g.Rayleigh.Sigma),
            Sized(g.MeanRadius.Value),
            Interval(g.MeanRadius),
            Sized(g.ExtremeSpread))).ToList();

        // One scale for every group, each about its own center: the farthest shot from its group's center, and a little room.
        double reach = report.Groups.SelectMany(g => g.Offsets.Select(o => Math.Sqrt(Math.Pow(o.X - g.Centre.X, 2) + Math.Pow(o.Y - g.Centre.Y, 2))))
            .Concat(report.Groups.Select(g => g.MeanRadius.Value)).Concat([0.1]).Max() * 1.15;
        var (half, step, words) = OnePageReports.Grid(reach, units);
        return new ComparisonPage(date, paper, report, loads, summary, rows,
            $"Each load's shots about their own center, all at one scale; grid squares {words}.", half, step, topGun,
            $"Made with GroupLab {version}, {Branding}");
    }

    /// <summary>
    /// What a load was chronographed at, for the comparison, entry 131 section 10: the muzzle velocity and its spread where the record book has
    /// them, and null where it does not. A spread GroupLab worked out from readings says where it came from, because a measured spread and a
    /// typed one are not the same claim (entry 115 section 3).
    /// </summary>
    public static string? Speed(RecordBook book, string? loadName, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(units);
        if (loadName is null || book.FindLoad(loadName) is not { MuzzleVelocityFps: { } fps } load)
        {
            return null;
        }

        string speed = units.Speed(fps);
        return load.MuzzleVelocitySdFps is not { } sd
            ? speed
            : speed + ", SD " + units.Speed(sd) + (load.MuzzleVelocitySdFrom is { Length: > 0 } from ? " from " + from : "");
    }

    /// <summary>The PDF: one page, or two where the comparison does not fit on one.</summary>
    public static byte[] Write(ComparisonPage page) => PdfWriter.Write(Pages(page));

    /// <summary>The PDF's pages as scenes, exposed so tests can read what each carries.</summary>
    public static IReadOnlyList<Scene> Pages(ComparisonPage page) => Lay(page, paged: true);

    /// <summary>The whole comparison as one scene, a page wide and as tall as it needs, which the picture is drawn from.</summary>
    public static Scene Tall(ComparisonPage page) => Lay(page, paged: false)[0];

    /// <summary>The picture: <see cref="Tall"/> drawn in color at <see cref="PictureDpi"/>, as a PNG.</summary>
    public static byte[] Picture(ComparisonPage page)
    {
        var (width, height, bgr) = SceneRasterizer.RasterizeBgr(Tall(page), PictureDpi, words: true);
        return ColourPng.Write(width, height, bgr, PictureDpi);
    }

    /// <summary>The name a saved or shared comparison is given: the loads compared and the date.</summary>
    public static string FileName(ComparisonPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        string loads = string.Join(" vs ", page.Loads.Select(l => l.Name));
        if (loads.Length > 60)
        {
            loads = string.Create(CultureInfo.InvariantCulture, $"{page.Loads.Count} loads");
        }

        return string.Concat($"Compare loads {loads} {page.Date}".Split(Path.GetInvalidFileNameChars())).Trim();
    }

    private static long Pt(double points) => (long)Math.Round(points * Inch / 72);

    private static long Leading(long size) => size * 5 / 4;

    /// <summary>A block of the page: its height, and how to draw it with its top at a given place.</summary>
    private sealed record Block(long Height, Action<List<SceneItem>, long> Draw);

    private static IReadOnlyList<Scene> Lay(ComparisonPage page, bool paged)
    {
        ArgumentNullException.ThrowIfNull(page);
        var (widthDmm, heightDmm) = PageSizes.Standard(page.Paper) ?? PageSizes.Standard(PageSize.Letter)!.Value;
        long width = widthDmm * Scene.UnitsPerDmm, height = heightDmm * Scene.UnitsPerDmm;
        long margin = 3 * Inch / 5, right = width - margin;
        var blocks = Blocks(page, margin, right);

        // The footer sits in the bottom margin of every page, and of the picture.
        void Footer(List<SceneItem> items, long bottom)
        {
            items.Add(new TextRun(SceneLayer.Name, Grey, margin, bottom - (margin / 2), Pt(7.5), ReportWriter.Plain($"Compare loads, {page.Date}"), TextAnchor.Left));
            items.Add(new TextRun(SceneLayer.Name, Grey, right, bottom - (margin / 2), Pt(7.5), ReportWriter.Plain(page.Footer), TextAnchor.Right));
        }

        var scenes = new List<Scene>();
        if (!paged)
        {
            var items = new List<SceneItem>();
            long y = margin;
            foreach (var block in blocks)
            {
                block.Draw(items, y);
                y += block.Height;
            }

            long tall = Math.Max(y + margin, Inch * 3);
            Footer(items, tall);
            return [new Scene(width, tall, 0, items)];
        }

        var current = new List<SceneItem>();
        long at = margin;
        foreach (var block in blocks)
        {
            if (at + block.Height > height - margin && at > margin)
            {
                Footer(current, height);
                scenes.Add(new Scene(width, height, 0, current));
                current = [];
                at = margin;
            }

            block.Draw(current, at);
            at += block.Height;
        }

        Footer(current, height);
        scenes.Add(new Scene(width, height, 0, current));
        return scenes;
    }

    private static List<Block> Blocks(ComparisonPage page, long margin, long right)
    {
        var report = page.Report;
        long width = right - margin;
        var blocks = new List<Block>();

        // Words wrapped to the width, one block, so a paragraph never splits across pages.
        Block Words(string words, long size, Rgb colour, bool bold = false, long before = 0, long left = 0)
        {
            var lines = ReportWriter.Wrap(words, size, width - left);
            return new Block(before + (lines.Count * Leading(size)), (items, top) =>
            {
                long y = top + before;
                foreach (string line in lines)
                {
                    y += Leading(size);
                    items.Add(new TextRun(SceneLayer.Labels, colour, margin + left, y - (size / 4), size, line, TextAnchor.Left, Bold: bold));
                }
            });
        }

        Block Heading(string words) => new(Pt(8) + Leading(Pt(11)) + Pt(6), (items, top) =>
        {
            long y = top + Pt(8) + Leading(Pt(11));
            items.Add(new TextRun(SceneLayer.Labels, Ink, margin, y - (Pt(11) / 4), Pt(11), ReportWriter.Plain(words), TextAnchor.Left));
            items.Add(new RectFill(SceneLayer.Labels, Rule, margin, y + 6, width, 3));
        });

        blocks.Add(Words("Compare loads", Pt(16), Ink, bold: true));
        blocks.Add(Words($"GroupLab comparison, dated {page.Date}", Pt(9), Grey));
        blocks.Add(Words(page.Summary, Pt(9.5), Ink, before: Pt(6)));

        // The groups side by side, each in a square, then each load's name in its color and its line beneath.
        int n = report.Groups.Count;
        long gap = Inch / 6, cell = Math.Min((width - ((n - 1) * gap)) / n, 2 * Inch);
        var names = Enumerable.Range(0, n).Select(i => ReportWriter.Wrap(page.Loads[i].Name, Pt(9.5), cell)).ToList();
        var details = Enumerable.Range(0, n).Select(i => ReportWriter.Wrap(
            page.Loads[i].Detail + (page.Loads[i].Speed is { } speed ? "; " + speed : ""), Pt(7.5), cell)).ToList();
        long under = names.Max(l => l.Count) * Leading(Pt(9.5)) + (details.Max(l => l.Count) * Leading(Pt(7.5)));
        blocks.Add(new Block(Pt(10) + cell + Pt(4) + under, (items, top) =>
        {
            long squareTop = top + Pt(10);
            for (int i = 0; i < n; i++)
            {
                long left = margin + (i * (cell + gap));
                DrawGroup(items, report.Groups[i], LoadInk(i), left, squareTop, cell, page.HalfWidthInches, page.GridInches);
                long y = squareTop + cell + Pt(4);
                foreach (string line in names[i])
                {
                    y += Leading(Pt(9.5));
                    items.Add(new TextRun(SceneLayer.Labels, LoadInk(i), left, y - (Pt(9.5) / 4), Pt(9.5), line, TextAnchor.Left, Bold: true));
                }

                foreach (string line in details[i])
                {
                    y += Leading(Pt(7.5));
                    items.Add(new TextRun(SceneLayer.Labels, Grey, left, y - (Pt(7.5) / 4), Pt(7.5), line, TextAnchor.Left));
                }
            }
        }));
        blocks.Add(Words(page.GridWords + " A ring in each load's color: its mean radius about its center.", Pt(7.5), Grey, before: Pt(4)));

        // The figures, a row a load: the load's name over two lines where it needs them, each figure, and the interval beneath it.
        blocks.Add(Heading("Figures, each with its 95 percent interval"));
        long size = Pt(8.5), small = Pt(7), row = Leading(size) + Leading(small) + Pt(5);
        long[] columns = [margin + Pt(4), margin + (width * 31 / 100), margin + (width * 38 / 100), margin + (width * 59 / 100), margin + (width * 80 / 100)];
        long Room(int c) => (c + 1 < columns.Length ? columns[c + 1] : right) - columns[c] - Pt(6);
        string[] headings = ["Load", "Shots", "Sigma", "Mean radius", "Extreme spread"];
        blocks.Add(new Block(Leading(Pt(7.5)) + Pt(4) + (row * page.Rows.Count), (items, top) =>
        {
            long y = top + Leading(Pt(7.5));
            for (int c = 0; c < headings.Length; c++)
            {
                items.Add(new TextRun(SceneLayer.Labels, Grey, columns[c], y, Pt(7.5), headings[c], TextAnchor.Left));
            }

            y += Pt(4);
            for (int r = 0; r < page.Rows.Count; r++)
            {
                var cells = page.Rows[r];
                if (r % 2 == 0)
                {
                    items.Add(new RectFill(SceneLayer.Cells, Shade, margin, y, width, row));
                }

                long first = y + Pt(2) + Leading(size) - (size / 4), second = first + Leading(small);
                var name = ReportWriter.Wrap(cells.Load, size, Room(0));
                items.Add(new TextRun(SceneLayer.Labels, LoadInk(r), columns[0], first, size, Fit(name[0], size, Room(0)), TextAnchor.Left, Bold: true));
                if (name.Count > 1)
                {
                    items.Add(new TextRun(SceneLayer.Labels, LoadInk(r), columns[0], second, small, Fit(string.Join(" ", name.Skip(1)), small, Room(0)), TextAnchor.Left, Bold: true));
                }

                string[] values = [cells.Shots, cells.Sigma, cells.MeanRadius, cells.ExtremeSpread];
                string[] beneath = ["", cells.SigmaInterval, cells.MeanRadiusInterval, ""];
                for (int c = 1; c < headings.Length; c++)
                {
                    items.Add(new TextRun(SceneLayer.Labels, Ink, columns[c], first, size, Fit(ReportWriter.Plain(values[c - 1]), size, Room(c)), TextAnchor.Left));
                    if (beneath[c - 1].Length > 0)
                    {
                        items.Add(new TextRun(SceneLayer.Labels, Grey, columns[c], second, small, Fit(ReportWriter.Plain(beneath[c - 1]), small, Room(c)), TextAnchor.Left));
                    }
                }

                y += row;
            }
        }));

        // What the difference means: the comparison's own verdict, which never ranks by the point estimates.
        blocks.Add(Heading("What the difference means"));
        blocks.Add(Words(report.Headline, Pt(10), Ink, bold: true));
        foreach (string line in report.Plain.Count > 0 ? report.Plain : report.Explanation)
        {
            blocks.Add(Words(line, Pt(9), Ink, before: Pt(3)));
        }

        blocks.Add(Heading("Tests run"));
        foreach (var test in report.Tests)
        {
            string p = test.PValue < 0.001 ? "p < 0.001" : "p = " + test.PValue.ToString("0.000", CultureInfo.InvariantCulture);
            blocks.Add(Words($"{test.Name}, {p}: {test.Verdict}", Pt(9), Ink, before: Pt(3)));
            blocks.Add(Words(test.Power, Pt(7.5), Grey, left: Pt(10)));
        }

        if (n > 2)
        {
            blocks.Add(Heading("Details: every pair's sigma ratio"));
            foreach (var pair in report.Pairs)
            {
                blocks.Add(Words(string.Create(CultureInfo.InvariantCulture,
                    $"{report.Groups[pair.First].Name} against {report.Groups[pair.Second].Name}: {pair.Ratio.Value:0.00}, 95% interval {pair.Ratio.Lower:0.00} to {pair.Ratio.Upper:0.00}; p = {pair.PValue:0.000}, {pair.HolmPValue:0.000} after Holm's adjustment"),
                    Pt(8), Ink, before: Pt(2)));
            }
        }

        if (page.TopGun.Count > 0)
        {
            blocks.Add(Heading("Top Gun"));
            foreach (string line in page.TopGun)
            {
                blocks.Add(Words(line, Pt(9), Ink, before: Pt(2)));
            }

            blocks.Add(Words(Ballistics.TopGun.Note, Pt(7.5), Grey, before: Pt(2)));
        }

        if (report.Resolve.Count > 0)
        {
            blocks.Add(Heading("Shots to resolve a difference"));
            blocks.Add(Words(string.Join("; ", report.Resolve.Select(r => string.Create(CultureInfo.InvariantCulture,
                $"{100 * (r.Ratio - 1):0} percent: {r.ShotsPerLoad} shots per load"))) + ". With 80 percent power at the 5 percent level, by the F test on the Rayleigh sigmas.",
                Pt(8), Ink, before: Pt(2)));
        }

        return blocks;
    }

    /// <summary>A cell's words, cut with an ellipsis where they are wider than the column.</summary>
    private static string Fit(string words, long size, long width)
    {
        if (HelveticaMetrics.TextWidth(words, size) <= width)
        {
            return words;
        }

        string cut = words;
        while (cut.Length > 1 && HelveticaMetrics.TextWidth(cut + "...", size) > width)
        {
            cut = cut[..^1];
        }

        return cut.TrimEnd() + "...";
    }

    /// <summary>One group in its square: the grid about its center, its mean radius ring and its shots, in its color.</summary>
    private static void DrawGroup(List<SceneItem> items, ComparedGroup group, Rgb ink, long left, long top, long size, double halfWidth, double grid)
    {
        double k = size / (2 * halfWidth);
        long cx = left + (size / 2), cy = top + (size / 2);
        int lines = (int)Math.Round(halfWidth / grid);
        for (int i = -lines; i <= lines; i++)
        {
            long at = (long)Math.Round(i * grid * k), thick = i == 0 ? 5 : 3;
            items.Add(new RectFill(SceneLayer.MeasurementGrid, i == 0 ? Rule : GridInk, cx + at - (thick / 2), top, thick, size));
            items.Add(new RectFill(SceneLayer.MeasurementGrid, i == 0 ? Rule : GridInk, left, cy + at - (thick / 2), size, thick));
        }

        const long hair = 3;
        items.Add(new RectFill(SceneLayer.MeasurementGrid, Rule, left, top, size, hair));
        items.Add(new RectFill(SceneLayer.MeasurementGrid, Rule, left, top + size - hair, size, hair));
        items.Add(new RectFill(SceneLayer.MeasurementGrid, Rule, left, top, hair, size));
        items.Add(new RectFill(SceneLayer.MeasurementGrid, Rule, left + size - hair, top, hair, size));

        long r = Math.Max(1, (long)Math.Round(group.MeanRadius.Value * k));
        items.Add(new DiscBand(SceneLayer.Labels, ink, cx, cy, r + 5, Math.Max(0, r - 5)));
        foreach (var shot in group.Offsets)
        {
            long sx = cx + (long)Math.Round((shot.X - group.Centre.X) * k), sy = cy + (long)Math.Round((shot.Y - group.Centre.Y) * k);
            if (sx >= left && sx <= left + size && sy >= top && sy <= top + size)
            {
                items.Add(new DiscBand(SceneLayer.Labels, ink, sx, sy, 20, 0));
            }
        }
    }
}
