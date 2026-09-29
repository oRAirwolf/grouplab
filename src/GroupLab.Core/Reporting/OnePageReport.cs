using System.Globalization;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Pdf;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Reporting;

/// <summary>One row of the one-page report's figures table: the figure, its size at the target, and its angle where the distance is known.</summary>
public sealed record OnePageRow(string Figure, string Size, string Angle);

/// <summary>
/// The one-page report's plot: every shot about the group's centre, each from its own aim point, the square scaled to fill with the group and
/// its grid in whole steps of the person's own unit, stated in <see cref="GridWords"/>. Offsets are in inches, x right and y down.
/// </summary>
public sealed record OnePagePlot(
    IReadOnlyList<ReportShot> Shots,
    PointD Centre,
    bool Aim,
    double? MeanRadiusInches,
    double? CalibreInches,
    double HalfWidthInches,
    double GridInches,
    string GridWords);

/// <summary>
/// Everything the one-page report prints, NOTES-FROM-PLANNING.md entry 280 section 2 (entry 278 feature e, board Report): one dated page on
/// Letter, or A4 where the person's paper is A4, with the picture, the plot, the figures table, the load and equipment line and the
/// confidence sentence. Every word comes from <see cref="OnePageReports.For"/>, so the phone and the desktop print the same page.
/// </summary>
public sealed record OnePageReport(
    string Title,
    string Date,
    PageSize Paper,
    DocumentImage? Picture,
    OnePagePlot Plot,
    IReadOnlyList<OnePageRow> Figures,
    string Equipment,
    string Confidence,
    string Footer);

/// <summary>
/// Builds the one-page report from a marking and lays it out through GroupLab's own <see cref="PdfWriter"/>, the same machinery as the full
/// session report (<see cref="ReportWriter"/>), which stays beside it for everything the analysis screen holds. The figures are
/// <see cref="GroupAnalysis"/>'s counted ones: a shot the shooter left out is on the plot, hollow, and in no figure.
/// </summary>
public static class OnePageReports
{
    private const long Inch = 508;

    private static readonly Rgb Ink = new(20, 20, 20);
    private static readonly Rgb Grey = new(95, 95, 95);
    private static readonly Rgb Rule = new(190, 190, 190);
    private static readonly Rgb GridInk = new(222, 222, 222);
    private static readonly Rgb Shade = new(240, 240, 240);
    private static readonly Rgb PlotGroup = new(0, 122, 77);
    private static readonly Rgb PlotAim = new(0, 85, 212);
    private static readonly Rgb PlotOutline = new(150, 150, 150);

    /// <summary>The paper the report is printed on: Letter where the person's region prints on Letter, and A4 everywhere else.</summary>
    public static PageSize PaperFor(bool letterRegion) => letterRegion ? PageSize.Letter : PageSize.A4;

    /// <summary>The report of <paramref name="state"/> as it stands, named <paramref name="title"/> and dated <paramref name="date"/>.</summary>
    public static OnePageReport For(MarkingState state, string title, string date, UnitSettings units, PageSize paper, DocumentImage? picture, string footer)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(units);
        var report = GroupAnalysis.Analyse(state);
        double? distance = state.ShotDistanceInches;
        string Angle(double inches) => units.AngleText(inches, distance) ?? "";
        var rows = new List<OnePageRow>();
        string confidence;
        if (report.Counted is { } counted)
        {
            rows.Add(new("Shots", string.Create(CultureInfo.InvariantCulture,
                $"{counted.Shots} counted{(report.Excluded > 0 ? $", {report.Excluded} left out by the shooter" : "")}"), ""));
            if (counted.MeanRadius is { } mr)
            {
                rows.Add(new("Mean radius", units.Length(mr.Value), Angle(mr.Value)));
            }

            if (counted.ExtremeSpread is { } es)
            {
                rows.Add(new("Extreme spread, center to center", units.Length(es.Value), Angle(es.Value)));
            }

            if (counted.Cep50 is { } c50)
            {
                rows.Add(new("CEP 50", units.Length(c50.Value), Angle(c50.Value)));
            }

            if (counted.Cep90 is { } c90)
            {
                rows.Add(new("CEP 90", units.Length(c90.Value), Angle(c90.Value)));
            }

            if (counted is { Width: { } w, Height: { } h })
            {
                rows.Add(new("Width × height", $"{units.Number(w)} × {units.Length(h)}", units.AngleText(w, distance) is { } aw ? $"{aw} × {Angle(h)}" : ""));
            }

            if (counted.CentreFromAim is { } c)
            {
                string across = c.X >= 0 ? "right" : "left", down = c.Y >= 0 ? "low" : "high";
                rows.Add(new("Center from aim", $"{units.Length(Math.Abs(c.X))} {across}, {units.Length(Math.Abs(c.Y))} {down}",
                    units.AngleText(Math.Abs(c.X), distance) is { } ax ? $"{ax} {across}, {Angle(Math.Abs(c.Y))} {down}" : ""));
            }

            confidence = Confidence(counted, units);
        }
        else
        {
            confidence = report.Problem ?? "No shots are marked, so there is nothing to be sure of.";
        }

        return new OnePageReport(title, date, paper, picture, Plot(state, units), rows, Equipment(state, units), confidence, footer);
    }

    /// <summary>
    /// The confidence sentence: how far the true mean radius can lie from the measured one at this many shots, the analysis's own interval
    /// with the coverage it actually has; or, below the shots a spread needs, the analysis's own sentence saying so.
    /// </summary>
    public static string Confidence(GroupFigures counted, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(counted);
        ArgumentNullException.ThrowIfNull(units);
        if (counted.DispersionWithheld is { } withheld)
        {
            return withheld;
        }

        return counted.MeanRadius switch
        {
            { Lower: { } lower, Upper: { } upper, Coverage: { } coverage } => string.Create(CultureInfo.InvariantCulture,
                $"From {counted.Shots} shots, the true mean radius of this rifle and load lies between {units.Length(lower)} and {units.Length(upper)}, with {100 * coverage:0.0} percent confidence; more shots would narrow it."),
            { IntervalUnavailable: { } why } => string.Create(CultureInfo.InvariantCulture, $"From {counted.Shots} shots there is no interval for the mean radius: {why}."),
            _ => string.Create(CultureInfo.InvariantCulture, $"From {counted.Shots} shots."),
        };
    }

    /// <summary>The load and equipment line: rifle with its click value, barrel, load, caliber and distance, those recorded.</summary>
    public static string Equipment(MarkingState state, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(units);
        var parts = new List<string>();
        if (state.Rifle is { } rifle)
        {
            parts.Add($"Rifle: {rifle.Name}, {rifle.DescribeClick()}");
        }

        if (!string.IsNullOrWhiteSpace(state.Barrel))
        {
            parts.Add("Barrel: " + state.Barrel);
        }

        if (!string.IsNullOrWhiteSpace(state.Load))
        {
            parts.Add("Load: " + state.Load);
        }

        if (state.Calibre is { } calibre)
        {
            parts.Add("Caliber: " + calibre.Name);
        }

        if (state.ShotDistanceInches is { } d)
        {
            parts.Add("Distance: " + units.DistanceText(d));
        }

        return parts.Count == 0 ? "No rifle, load, caliber or distance was recorded with this target." : string.Join("; ", parts) + ".";
    }

    /// <summary>The steps a grid may take in each unit, in that unit: the first that gives no more than ten squares across is used.</summary>
    private static double[] Steps(LinearUnit unit) => unit switch
    {
        LinearUnit.Millimetre => [1, 2, 5, 10, 20, 50, 100, 200, 500],
        LinearUnit.Centimetre => [0.1, 0.2, 0.5, 1, 2, 5, 10, 20, 50],
        _ => [0.1, 0.2, 0.25, 0.5, 1, 2, 5, 10, 20],
    };

    /// <summary>The plot: every shot but sighters, about the counted shots' centre, the square a whole number of grid steps each side of it.</summary>
    public static OnePagePlot Plot(MarkingState state, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(units);
        var shots = state.Shots.Where(s => s.IsShot && !GroupAnalysis.OnSighter(state, s)).ToList();
        var offsets = GroupAnalysis.CompositeOffsets(state, shots);
        if (offsets.Count != shots.Count)
        {
            offsets = [];
            shots = [];
        }

        var labels = ShotLabels.For(state).ToDictionary(l => l.ShotId, l => l.Text);
        var plotted = shots.Select((s, i) => new ReportShot(labels.GetValueOrDefault(s.Id) ?? (i + 1).ToString(CultureInfo.InvariantCulture), offsets[i], s.Exclusion is not null)).ToList();
        var counted = plotted.Where(p => !p.Excluded).Select(p => p.OffsetInches).ToList();
        var centre = counted.Count > 0 ? GroupStatistics.Centre(counted) : new PointD(0, 0);
        double? calibre = state.Calibre?.DiameterInches;
        double? meanRadius = GroupAnalysis.Analyse(state).Counted?.MeanRadius?.Value;
        // Scaled to fill with the shots that count; a shot left out far away is said to be off the plot rather than shrinking the group.
        var filling = plotted.Any(p => !p.Excluded) ? plotted.Where(p => !p.Excluded) : plotted;
        double reach = filling.Select(p => Math.Sqrt(Math.Pow(p.OffsetInches.X - centre.X, 2) + Math.Pow(p.OffsetInches.Y - centre.Y, 2)) + ((calibre ?? 0) / 2))
            .Concat([meanRadius ?? 0, 0.1]).Max() * 1.15;

        // The grid in the person's own unit: the smallest step giving at most ten squares across, and the square a whole number of steps.
        double perInch = UnitSettings.FromInches(1, units.Linear);
        double step = Steps(units.Linear).Select(s => s / perInch).FirstOrDefault(s => 2 * reach / s <= 10, Steps(units.Linear)[^1] / perInch);
        double half = Math.Ceiling(reach / step) * step;
        string stepWords = (step * perInch).ToString("0.##", CultureInfo.InvariantCulture) + " " + UnitSettings.Symbol(units.Linear);
        string across = (2 * half * perInch).ToString("0.##", CultureInfo.InvariantCulture) + " " + UnitSettings.Symbol(units.Linear);
        string words = $"Grid squares {stepWords}; the plot is {across} across, centered on the group.";
        return new OnePagePlot(plotted, centre, GroupAnalysis.HasOrigin(state, shots), meanRadius, calibre, half, step, words);
    }

    /// <summary>The PDF: one page.</summary>
    public static byte[] Write(OnePageReport report) => PdfWriter.Write([Page(report)]);

    /// <summary>The name a saved or shared report is given: the title and the date.</summary>
    public static string FileName(OnePageReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        string name = $"{report.Title} {report.Date} one page";
        return string.Concat(name.Split(Path.GetInvalidFileNameChars())).Trim();
    }

    /// <summary>A font size in points, in the scene's half-dmm.</summary>
    private static long Pt(double points) => (long)Math.Round(points * Inch / 72);

    private static long Leading(long size) => size * 5 / 4;

    /// <summary>The page as a scene, exposed so tests can read what it carries and where.</summary>
    public static Scene Page(OnePageReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var (widthDmm, heightDmm) = PageSizes.Standard(report.Paper) ?? PageSizes.Standard(PageSize.Letter)!.Value;
        long width = widthDmm * Scene.UnitsPerDmm, height = heightDmm * Scene.UnitsPerDmm;
        long margin = 3 * Inch / 5, right = width - margin, gap = Inch / 4;
        var items = new List<SceneItem>();
        long y = margin;

        void Text(string text, long size, Rgb colour, long left, long wrapWidth, bool bold = false)
        {
            foreach (string line in ReportWriter.Wrap(text, size, wrapWidth))
            {
                y += Leading(size);
                items.Add(new TextRun(SceneLayer.Labels, colour, left, y - (size / 4), size, line, TextAnchor.Left, Bold: bold));
            }
        }

        void Heading(string text)
        {
            y += Pt(8);
            Text(text, Pt(11), Ink, margin, right - margin);
            items.Add(new RectFill(SceneLayer.Labels, Rule, margin, y + 6, right - margin, 3));
            y += Pt(4);
        }

        // The title and the date: a page of a report gets separated from everything else.
        Text(report.Title, Pt(16), Ink, margin, right - margin, bold: true);
        Text($"GroupLab one-page report, dated {report.Date}", Pt(9), Grey, margin, right - margin);
        y += Pt(8);

        // The picture and the plot side by side, each in a square.
        long box = (right - margin - gap) / 2, top = y;
        Picture(items, report.Picture, margin, top, box);
        DrawPlot(items, report.Plot, right - box, top, box);
        y = top + box;
        long captionTop = y;
        Text(report.Plot.GridWords + " Black: the shots that count; hollow: left out by the shooter. Green circle: the mean radius about the group's center."
            + (report.Plot.Aim ? " Blue cross: the aim point." : " No aim point was marked."), Pt(7.5), Grey, right - box, box);
        long plotCaption = y;
        y = captionTop;
        Text(report.Picture is null ? "No picture: the shots were entered without one." : "The picture as it was measured.", Pt(7.5), Grey, margin, box);
        y = Math.Max(y, plotCaption);

        // The figures table.
        Heading("Figures");
        long size = Pt(9.5), row = Leading(size) + Pt(3);
        long sizeColumn = margin + ((right - margin) * 70 / 100), angleColumn = right - Pt(4);
        items.Add(new TextRun(SceneLayer.Labels, Grey, margin + Pt(4), y + row - Pt(5), Pt(8), "Figure", TextAnchor.Left));
        items.Add(new TextRun(SceneLayer.Labels, Grey, sizeColumn, y + row - Pt(5), Pt(8), "At the target", TextAnchor.Right));
        items.Add(new TextRun(SceneLayer.Labels, Grey, angleColumn, y + row - Pt(5), Pt(8), "Angle", TextAnchor.Right));
        y += row;
        for (int i = 0; i < report.Figures.Count; i++)
        {
            var figure = report.Figures[i];
            if (i % 2 == 0)
            {
                items.Add(new RectFill(SceneLayer.Cells, Shade, margin, y, right - margin, row));
            }

            items.Add(new TextRun(SceneLayer.Labels, Grey, margin + Pt(4), y + row - Pt(5), size, ReportWriter.Plain(figure.Figure), TextAnchor.Left));
            items.Add(new TextRun(SceneLayer.Labels, Ink, sizeColumn, y + row - Pt(5), size, ReportWriter.Plain(figure.Size), TextAnchor.Right, Bold: figure.Figure == "Mean radius"));
            items.Add(new TextRun(SceneLayer.Labels, Ink, angleColumn, y + row - Pt(5), size, ReportWriter.Plain(figure.Angle), TextAnchor.Right));
            y += row;
        }

        Heading("Load and equipment");
        Text(report.Equipment, Pt(9.5), Ink, margin, right - margin);
        Heading("How sure");
        Text(report.Confidence, Pt(9.5), Ink, margin, right - margin);

        items.Add(new TextRun(SceneLayer.Name, Grey, margin, height - (margin / 2), Pt(7.5), ReportWriter.Plain($"{report.Title}, {report.Date}"), TextAnchor.Left));
        items.Add(new TextRun(SceneLayer.Name, Grey, right, height - (margin / 2), Pt(7.5), ReportWriter.Plain(report.Footer), TextAnchor.Right));
        return new Scene(width, height, 0, items);
    }

    /// <summary>The picture fitted inside the square, its shape kept, with a hairline round the square.</summary>
    private static void Picture(List<SceneItem> items, DocumentImage? picture, long left, long top, long size)
    {
        Frame(items, left, top, size);
        if (picture is null || picture.PixelWidth <= 0 || picture.PixelHeight <= 0)
        {
            return;
        }

        double k = Math.Min((double)size / picture.PixelWidth, (double)size / picture.PixelHeight);
        long w = (long)Math.Round(picture.PixelWidth * k), h = (long)Math.Round(picture.PixelHeight * k);
        items.Add(new ImageBox(SceneLayer.Labels, left + ((size - w) / 2), top + ((size - h) / 2), w, h, picture.Jpeg, picture.PixelWidth, picture.PixelHeight));
    }

    private static void Frame(List<SceneItem> items, long left, long top, long size)
    {
        const long hair = 3;
        items.Add(new RectFill(SceneLayer.MeasurementGrid, Rule, left, top, size, hair));
        items.Add(new RectFill(SceneLayer.MeasurementGrid, Rule, left, top + size - hair, size, hair));
        items.Add(new RectFill(SceneLayer.MeasurementGrid, Rule, left, top, hair, size));
        items.Add(new RectFill(SceneLayer.MeasurementGrid, Rule, left + size - hair, top, hair, size));
    }

    /// <summary>The plot in a square: the grid about the group's centre, the mean radius circle, the aim point, and every shot.</summary>
    private static void DrawPlot(List<SceneItem> items, OnePagePlot plot, long left, long top, long size)
    {
        double k = size / (2 * plot.HalfWidthInches);
        long cx = left + (size / 2), cy = top + (size / 2);
        long X(double inches) => cx + (long)Math.Round((inches - plot.Centre.X) * k);
        long Y(double inches) => cy + (long)Math.Round((inches - plot.Centre.Y) * k);
        long R(double inches) => Math.Max(1, (long)Math.Round(inches * k));

        int lines = (int)Math.Round(plot.HalfWidthInches / plot.GridInches);
        for (int i = -lines; i <= lines; i++)
        {
            long at = (long)Math.Round(i * plot.GridInches * k);
            long thick = i == 0 ? 5 : 3;
            items.Add(new RectFill(SceneLayer.MeasurementGrid, i == 0 ? Rule : GridInk, cx + at - (thick / 2), top, thick, size));
            items.Add(new RectFill(SceneLayer.MeasurementGrid, i == 0 ? Rule : GridInk, left, cy + at - (thick / 2), size, thick));
        }

        Frame(items, left, top, size);
        if (plot.MeanRadiusInches is { } mr)
        {
            long r = R(mr);
            items.Add(new DiscBand(SceneLayer.Labels, PlotGroup, cx, cy, r + 5, Math.Max(0, r - 5)));
        }

        // The group's centre, a small green cross.
        items.Add(new RectFill(SceneLayer.Labels, PlotGroup, cx - 40, cy - 3, 80, 6));
        items.Add(new RectFill(SceneLayer.Labels, PlotGroup, cx - 3, cy - 40, 6, 80));
        if (plot.Aim)
        {
            long ax = X(0), ay = Y(0);
            if (ax > left + 40 && ax < left + size - 40 && ay > top + 40 && ay < top + size - 40)
            {
                items.Add(new RectFill(SceneLayer.Labels, PlotAim, ax - 60, ay - 4, 120, 8));
                items.Add(new RectFill(SceneLayer.Labels, PlotAim, ax - 4, ay - 60, 8, 120));
            }
            else
            {
                items.Add(new TextRun(SceneLayer.Labels, PlotAim, left + 20, top + size - 20, Pt(6.5), "The aim point is off the plot.", TextAnchor.Left));
            }
        }

        int off = 0;
        foreach (var shot in plot.Shots)
        {
            long sx = X(shot.OffsetInches.X), sy = Y(shot.OffsetInches.Y), dot = 18;
            if (sx < left || sx > left + size || sy < top || sy > top + size)
            {
                off++;
                continue;
            }

            if (plot.CalibreInches is { } calibre)
            {
                long r = R(calibre / 2);
                items.Add(new DiscBand(SceneLayer.Labels, PlotOutline, sx, sy, r + 2, Math.Max(0, r - 2)));
            }

            items.Add(new DiscBand(SceneLayer.Labels, shot.Excluded ? PlotOutline : Ink, sx, sy, dot, shot.Excluded ? dot - 6 : 0));
            items.Add(new TextRun(SceneLayer.Labels, Ink, sx + dot + 8, sy - dot, Pt(6), ReportWriter.Plain(shot.Label), TextAnchor.Left));
        }

        if (off > 0)
        {
            items.Add(new TextRun(SceneLayer.Labels, Grey, left + size - 20, top + Pt(9), Pt(6.5),
                off == 1 ? "1 shot left out lies off the plot." : string.Create(CultureInfo.InvariantCulture, $"{off} shots left out lie off the plot."), TextAnchor.Right));
        }
    }
}
