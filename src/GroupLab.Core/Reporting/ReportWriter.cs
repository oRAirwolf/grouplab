using System.Globalization;
using System.Text;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Pdf;

namespace GroupLab.Core.Reporting;

/// <summary>
/// Lays a <see cref="SessionReport"/> out on Letter pages and writes it through GroupLab's own <see cref="PdfWriter"/>, NOTES-FROM-PLANNING.md
/// entry 112 section 2. The layout is a flow: page 1 is the particulars, the composite plot beside the headline figures, the zero correction
/// and the two cards; page 2 begins with the shot table; anything that does not fit continues on a further page rather than being cut.
/// Every page is numbered with the sheet and date, since a page of a report gets separated from the others.
/// </summary>
public static class ReportWriter
{
    private const long Inch = 508;
    private const long PageWidth = 17 * Inch / 2;
    private const long PageHeight = 11 * Inch;
    private const long Margin = 3 * Inch / 5;
    private const long Right = PageWidth - Margin;
    private const long Bottom = PageHeight - Margin - (Inch / 4);
    private const long ColumnGap = Inch / 4;

    private static readonly Rgb Ink = new(20, 20, 20);
    private static readonly Rgb Grey = new(95, 95, 95);
    private static readonly Rgb Rule = new(190, 190, 190);
    private static readonly Rgb Shade = new(238, 238, 238);
    private static readonly Rgb Paper = new(255, 255, 255);

    /// <summary>Entry 204: the composite plot's inks as the light theme draws them, so the page and the screen read alike.</summary>
    private static readonly Rgb PlotInk = new(0, 0, 0);
    private static readonly Rgb PlotOutline = new(128, 128, 128);
    private static readonly Rgb PlotBull = new(212, 212, 212);
    private static readonly Rgb PlotGroup = new(0, 122, 77);
    private static readonly Rgb PlotAim = new(0, 85, 212);
    private static readonly Rgb PlotSpread = new(200, 16, 46);

    /// <summary>A font size in points, in the scene's half-dmm.</summary>
    private static long Pt(double points) => (long)Math.Round(points * Inch / 72);

    public static byte[] Write(SessionReport report) => PdfWriter.Write(Pages(report));

    /// <summary>The report's pages as scenes, exposed so tests can read the text each page carries.</summary>
    public static IReadOnlyList<Scene> Pages(SessionReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var flow = new Flow();

        // Page 1.
        flow.Text(report.Title, Pt(16), Ink);
        flow.Text("GroupLab session report", Pt(9), Grey);
        flow.Gap(Pt(6));
        flow.Pairs(report.Particulars);
        flow.Gap(Pt(8));

        long plotSize = 3 * Inch;
        long top = flow.Y;
        DrawPlot(flow.Page, report.Plot, Margin, top, plotSize);
        long plotBottom = top + plotSize + Pt(4);
        var caption = new Flow.Column(flow, Margin, Margin + plotSize, plotBottom);
        foreach (string line in Wrap(report.Plot.Caption, Pt(7.5), plotSize))
        {
            caption.Line(line, Pt(7.5), Grey);
        }

        var figures = new Flow.Column(flow, Margin + plotSize + ColumnGap, Right, top);
        foreach (string line in report.Summary)
        {
            figures.Paragraph(line, Pt(8.5), Ink);
        }

        foreach (var figure in report.Figures)
        {
            figures.Figure(figure);
        }

        flow.Y = Math.Max(caption.Y, figures.Y) + Pt(8);
        flow.Heading("Zero correction");
        flow.Card(report.Zero, withTitle: false);
        foreach (var card in report.Cards)
        {
            flow.Heading(card.Title);
            flow.Card(card, withTitle: false);
        }

        // Page 2.
        flow.NewPage();
        flow.Heading("Shots");
        flow.Table(report.ShotHeadings, report.Shots);
        flow.Heading("Exclusions");
        flow.Lines(report.Exclusions.Count > 0 ? report.Exclusions : ["None. Every shot counts."]);
        flow.Heading("Decisions left unmade");
        flow.Lines(report.Unmade.Count > 0 ? report.Unmade : ["None. Every decision the review queue raised was made before this was accepted."]);
        flow.Heading("Registration");
        flow.Lines(report.Registration);
        flow.Heading("Why");
        foreach (var section in report.Why)
        {
            flow.Subheading(section.Heading);
            flow.Lines(section.Lines);
        }

        flow.Heading("Version and identifiers");
        flow.Lines([.. report.Identity.Select(p => $"{p.Label}: {p.Value}")]);

        string date = report.Particulars.FirstOrDefault(p => p.Label == "Date")?.Value ?? "";
        return flow.Finish(report.Title + (date.Length > 0 ? ", " + date : ""));
    }

    /// <summary>
    /// The characters WinAnsiEncoding lacks, in the words the screen means by them, so nothing on paper turns into a question mark; any other
    /// character outside Latin-1 is refused by <see cref="Printable"/>, which the tests hold every report string to.
    /// </summary>
    public static string Plain(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var s = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            s.Append(c switch
            {
                '\u2013' or '\u2212' or '\u2010' or '\u2011' => "-",
                '\u2018' or '\u2019' => "'",
                '\u201c' or '\u201d' => "\"",
                '\u2026' => "...",
                '\u2264' => "<=",
                '\u2265' => ">=",
                '\u2248' => "about",
                '\u03c3' => "sigma",
                '\u203a' or '\u25b8' => ">",
                '\u25be' => "v",
                '\u2032' => "'",
                '\u2033' => "\"",
                '\u00a0' or '\u2009' or '\u202f' => " ",
                _ => c.ToString(),
            });
        }

        return s.ToString();
    }

    /// <summary>Whether a string survives WinAnsiEncoding once made plain: no character becomes a question mark it was not.</summary>
    public static bool Printable(string text) => Plain(text).All(c => c == '?' || HelveticaMetrics.ToWinAnsi(c) != '?');

    /// <summary>Greedy word wrap at the Helvetica advance widths; a word longer than the line is left whole.</summary>
    internal static IReadOnlyList<string> Wrap(string text, long size, long width)
    {
        var lines = new List<string>();
        foreach (string paragraph in Plain(text).Split('\n'))
        {
            var line = new StringBuilder();
            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && HelveticaMetrics.TextWidth(candidate, size) > width)
                {
                    lines.Add(line.ToString());
                    line.Clear().Append(word);
                }
                else
                {
                    line.Clear().Append(candidate);
                }
            }

            lines.Add(line.ToString());
        }

        return lines;
    }

    /// <summary>
    /// The composite plot in a square, drawn as the screen draws it (entry 204): the bull's rings as wide pale bands behind everything, each
    /// shot a black dot inside a grey outline the bullet's size (half strength, as on screen), the CEP circles in green, the extreme spread
    /// in red, and the group centre's green lines and the aim point's blue ones across the whole square. The page has no dashed stroke, so
    /// CEP 50 is a ring of dots, CEP 90 a solid band and CEP 95 a ring of dashes made of dots, and the caption says which is which. The
    /// toggles are the screen's. The square spans the rings or the shots, whichever is wider.
    /// </summary>
    private static void DrawPlot(List<SceneItem> page, ReportPlot plot, long left, long top, long size)
    {
        double half = size / 2.0;
        long cx = left + (size / 2), cy = top + (size / 2);
        double calibre = plot.CalibreInches ?? 0.1;
        var shown = plot.Shown ?? PlotMarks.Default;
        double? cep50 = shown.Cep50 ? plot.Cep50Inches : null, cep90 = shown.Cep90 ? plot.Cep90Inches : null, cep95 = shown.Cep95 ? plot.Cep95Inches : null;
        double extent = plot.Shots.Select(s => Math.Sqrt((s.OffsetInches.X * s.OffsetInches.X) + (s.OffsetInches.Y * s.OffsetInches.Y)) + (calibre / 2))
            .Concat(plot.Discs.Select(d => d.DiameterInches / 2))
            .Concat([cep95 ?? 0, cep90 ?? 0, cep50 ?? 0, 0.25])
            .Max() * 1.08;
        double k = half / extent;
        long X(double inches) => cx + (long)Math.Round(inches * k);
        long Y(double inches) => cy + (long)Math.Round(inches * k);
        long R(double inches) => Math.Max(1, (long)Math.Round(inches * k));

        // The rings: each disc's edge a wide pale band, background to everything else.
        foreach (var disc in plot.Discs.Where(d => !d.Paper))
        {
            long r = R(disc.DiameterInches / 2);
            page.Add(new DiscBand(SceneLayer.Bulls, PlotBull, cx, cy, r + 10, Math.Max(0, r - 10)));
        }

        // The frame, a hairline in grey.
        long hair = 3;
        page.Add(new RectFill(SceneLayer.MeasurementGrid, Rule, left, top, size, hair));
        page.Add(new RectFill(SceneLayer.MeasurementGrid, Rule, left, top + size - hair, size, hair));
        page.Add(new RectFill(SceneLayer.MeasurementGrid, Rule, left, top, hair, size));
        page.Add(new RectFill(SceneLayer.MeasurementGrid, Rule, left + size - hair, top, hair, size));

        foreach (var shot in plot.Shots)
        {
            long r = R(calibre / 2), dot = 16, sx = X(shot.OffsetInches.X), sy = Y(shot.OffsetInches.Y);
            if (plot.CalibreInches is not null)
            {
                page.Add(new DiscBand(SceneLayer.Labels, PlotOutline, sx, sy, r + 2, Math.Max(0, r - 2)));
            }

            page.Add(new DiscBand(SceneLayer.Labels, shot.Excluded ? PlotOutline : PlotInk, sx, sy, dot, shot.Excluded ? dot - 6 : 0));
            page.Add(new TextRun(SceneLayer.Labels, Ink, sx + dot + 8, sy - dot, Pt(6), Plain(shot.Label), TextAnchor.Left));
        }

        if (plot.Centre is { } centre)
        {
            long gx = X(centre.X), gy = Y(centre.Y);
            if (cep50 is { } r50)
            {
                Dashes(page, PlotGroup, gx, gy, R(r50), 0, 30);
            }

            if (cep90 is { } r90)
            {
                long outer = R(r90);
                page.Add(new DiscBand(SceneLayer.Labels, PlotGroup, gx, gy, outer + 5, Math.Max(0, outer - 5)));
            }

            if (cep95 is { } r95)
            {
                Dashes(page, PlotGroup, gx, gy, R(r95), 60, 36);
            }
        }

        if (shown.Spread && plot.Spread is var (from, to))
        {
            double length = Math.Sqrt(Math.Pow(X(to.X) - X(from.X), 2) + Math.Pow(Y(to.Y) - Y(from.Y), 2));
            int steps = Math.Max(1, (int)(length / 6));
            for (int i = 0; i <= steps; i++)
            {
                // Dashes of 60 with gaps of 30, made of overlapping dots.
                if ((i * 6) % 90 < 60)
                {
                    double f = (double)i / steps;
                    page.Add(new DiscBand(SceneLayer.Labels, PlotSpread, X(from.X + ((to.X - from.X) * f)), Y(from.Y + ((to.Y - from.Y) * f)), 6, 0));
                }
            }
        }

        // The aim point's blue lines and the group centre's green ones, across the whole square.
        long line = 6;
        page.Add(new RectFill(SceneLayer.Labels, PlotAim, left, cy - (line / 2), size, line));
        page.Add(new RectFill(SceneLayer.Labels, PlotAim, cx - (line / 2), top, line, size));
        if (plot.Centre is { } groupCentre)
        {
            page.Add(new RectFill(SceneLayer.Labels, PlotGroup, left, Y(groupCentre.Y) - (line / 2), size, line));
            page.Add(new RectFill(SceneLayer.Labels, PlotGroup, X(groupCentre.X) - (line / 2), top, line, size));
        }

        string scale = string.Create(CultureInfo.InvariantCulture, $"square {2 * extent * plot.LengthPerInch:0.00} {plot.LengthSymbol} across");
        page.Add(new TextRun(SceneLayer.Labels, Grey, left + size - 20, top + size - 20, Pt(6.5), scale, TextAnchor.Right));
    }

    /// <summary>
    /// A circle drawn as dots, the page having no dashed stroke: with <paramref name="dash"/> 0 a dot every <paramref name="gap"/>, and
    /// otherwise dashes that long, made of overlapping dots, with gaps between.
    /// </summary>
    private static void Dashes(List<SceneItem> page, Rgb colour, long cx, long cy, long radius, long dash, long gap)
    {
        double circumference = 2 * Math.PI * radius;
        double period = dash + gap;
        const double step = 6;
        for (double along = 0; along < circumference; along += dash == 0 ? period : step)
        {
            if (dash > 0 && along % period > dash)
            {
                continue;
            }

            double angle = along / radius;
            page.Add(new DiscBand(SceneLayer.Labels, colour, cx + (long)Math.Round(radius * Math.Cos(angle)), cy + (long)Math.Round(radius * Math.Sin(angle)), dash == 0 ? 7 : 5, 0));
        }
    }

    /// <summary>A cursor down a sequence of pages, starting a new one when the next block does not fit.</summary>
    private sealed class Flow
    {
        private readonly List<List<SceneItem>> pages = [[]];

        public long Y { get; set; } = Margin;

        public List<SceneItem> Page => pages[^1];

        public void NewPage()
        {
            pages.Add([]);
            Y = Margin;
        }

        /// <summary>Makes room for <paramref name="height"/>, on a new page if this one has too little left.</summary>
        public void Need(long height)
        {
            if (Y + height > Bottom && Y > Margin)
            {
                NewPage();
            }
        }

        public void Gap(long height) => Y += height;

        public void Text(string text, long size, Rgb colour)
        {
            foreach (string line in Wrap(text, size, Right - Margin))
            {
                Need(Leading(size));
                Y += Leading(size);
                Page.Add(new TextRun(SceneLayer.Labels, colour, Margin, Y - (size / 4), size, line, TextAnchor.Left));
            }
        }

        public void Heading(string text)
        {
            Need(Pt(30));
            Gap(Pt(6));
            Text(text, Pt(11), Ink);
            Page.Add(new RectFill(SceneLayer.Labels, Rule, Margin, Y + 6, Right - Margin, 3));
            Gap(Pt(4));
        }

        public void Subheading(string text)
        {
            Need(Pt(24));
            Gap(Pt(3));
            Text(text, Pt(9), Ink);
        }

        public void Lines(IEnumerable<string> lines)
        {
            foreach (string line in lines)
            {
                Text(line, Pt(8.5), Grey);
                Gap(Pt(1.5));
            }
        }

        public void Pairs(IReadOnlyList<ReportPair> pairs)
        {
            // Two columns of label and value, the labels grey.
            long column = (Right - Margin) / 2, labelWidth = Pt(62);
            for (int i = 0; i < pairs.Count; i += 2)
            {
                long height = 0;
                for (int j = i; j < Math.Min(i + 2, pairs.Count); j++)
                {
                    long x = Margin + ((j - i) * column);
                    var wrapped = Wrap(pairs[j].Value, Pt(9), column - labelWidth - ColumnGap);
                    Need(wrapped.Count * Leading(Pt(9)));
                    Page.Add(new TextRun(SceneLayer.Labels, Grey, x, Y + Leading(Pt(9)) - (Pt(9) / 4), Pt(9), Plain(pairs[j].Label), TextAnchor.Left));
                    for (int l = 0; l < wrapped.Count; l++)
                    {
                        Page.Add(new TextRun(SceneLayer.Labels, Ink, x + labelWidth, Y + ((l + 1) * Leading(Pt(9))) - (Pt(9) / 4), Pt(9), wrapped[l], TextAnchor.Left));
                    }

                    height = Math.Max(height, wrapped.Count * Leading(Pt(9)));
                }

                Y += height;
            }
        }

        public void Card(ReportCard card, bool withTitle)
        {
            if (withTitle)
            {
                Subheading(card.Title);
            }

            Text(card.Verdict, Pt(10), Ink);
            Gap(Pt(1.5));
            Lines(card.Evidence);
        }

        public void Table(IReadOnlyList<string> headings, IReadOnlyList<ReportRow> rows)
        {
            long width = Right - Margin, cell = width / Math.Max(1, headings.Count), size = Pt(8.5), row = Leading(size) + Pt(2);
            void Cells(IReadOnlyList<string> texts, Rgb colour)
            {
                for (int c = 0; c < texts.Count; c++)
                {
                    long x = c == 0 ? Margin + Pt(4) : Margin + ((c + 1) * cell) - Pt(6);
                    Page.Add(new TextRun(SceneLayer.Labels, colour, x, Y + row - Pt(4), size, Plain(texts[c]), c == 0 ? TextAnchor.Left : TextAnchor.Right));
                }
            }

            Need(2 * row);
            Cells(headings, Grey);
            Y += row;
            Page.Add(new RectFill(SceneLayer.Labels, Rule, Margin, Y, width, 3));
            for (int i = 0; i < rows.Count; i++)
            {
                if (Y + row > Bottom)
                {
                    NewPage();
                    Cells(headings, Grey);
                    Y += row;
                }

                if (i % 2 == 1)
                {
                    Page.Add(new RectFill(SceneLayer.Cells, Shade, Margin, Y, width, row));
                }

                Cells(rows[i].Cells, rows[i].Struck ? Grey : Ink);
                if (rows[i].Struck)
                {
                    Page.Add(new RectFill(SceneLayer.Labels, Grey, Margin, Y + (row / 2) + Pt(0.5), width, 4));
                }

                Y += row;
            }
        }

        public IReadOnlyList<Scene> Finish(string running)
        {
            int count = pages.Count;
            return [.. pages.Select((items, i) =>
            {
                items.Add(new TextRun(SceneLayer.Name, Grey, Margin, PageHeight - (Margin / 2), Pt(7.5), Plain(running), TextAnchor.Left));
                items.Add(new TextRun(SceneLayer.Name, Grey, Right, PageHeight - (Margin / 2), Pt(7.5), FormattableString.Invariant($"page {i + 1} of {count}"), TextAnchor.Right));
                return new Scene(PageWidth, PageHeight, 0, items);
            })];
        }

        private static long Leading(long size) => size * 5 / 4;

        /// <summary>A column beside another block on the same page, with its own cursor.</summary>
        public sealed class Column(Flow flow, long left, long right, long top)
        {
            public long Y { get; private set; } = top;

            public void Line(string text, long size, Rgb colour)
            {
                Y += Leading(size);
                flow.Page.Add(new TextRun(SceneLayer.Labels, colour, left, Y - (size / 4), size, Plain(text), TextAnchor.Left));
            }

            public void Paragraph(string text, long size, Rgb colour)
            {
                foreach (string line in Wrap(text, size, right - left))
                {
                    Line(line, size, colour);
                }

                Y += Pt(3);
            }

            /// <summary>One figure: the label left and the value right on one line, the details beneath, and a hairline under the row.</summary>
            public void Figure(ReportFigure figure)
            {
                long size = Pt(12);
                Y += Leading(size);
                flow.Page.Add(new TextRun(SceneLayer.Labels, Grey, left, Y - (size / 4), Pt(9), Plain(figure.Label), TextAnchor.Left));
                flow.Page.Add(new TextRun(SceneLayer.Labels, Ink, right, Y - (size / 4), size, Plain(figure.Value), TextAnchor.Right));
                foreach (string detail in figure.Details)
                {
                    foreach (string line in Wrap(detail, Pt(7.5), right - left))
                    {
                        Line(line, Pt(7.5), Grey);
                    }
                }

                Y += Pt(3);
                flow.Page.Add(new RectFill(SceneLayer.Labels, Rule, left, Y, right - left, 2));
                Y += Pt(3);
            }
        }
    }
}
