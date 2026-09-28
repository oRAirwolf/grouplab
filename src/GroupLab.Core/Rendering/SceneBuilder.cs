using System.Globalization;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Gltd.Validation;
using GroupLab.Core.Rendering.Markers;
using GroupLab.Core.Rendering.Pdf;
using Net.Codecrete.QrCodeGenerator;

namespace GroupLab.Core.Rendering;

/// <summary>The print modes of a data block, TARGET-SCHEMA.md section 3.10. The third mode, none, is a different definition.</summary>
public enum DataBlockMode
{
    Blank,
    Filled,
}

/// <summary>
/// Print-time choices, none of which is part of the definition. <see cref="Scale"/> exists to produce the deliberately
/// mis-scaled print of DESIGN.md section 21's Phase 0 gate, and is 1 for every real print. <see cref="PrintNote"/> is a line of
/// text along the bottom edge, NOTES-FROM-PLANNING.md entry 25 section 2, so that a sheet printed at the wrong scale carries the
/// instruction it ignored, and a ruler against the printed edge says whether the scaling was uniform; without it a page is exactly what Phase 0 drew.
/// </summary>
public sealed record RenderOptions(
    DataBlockMode Mode = DataBlockMode.Blank,
    Instance? Instance = null,
    int? TileIndex = null,
    bool AllowInvalid = false,
    double Scale = 1.0,
    string? PrintNote = null,
    bool OneSheet = false);

public sealed record SceneResult(IReadOnlyList<Scene> Pages, string? DefinitionId, IReadOnlyList<Diagnostic> Diagnostics);

/// <summary>
/// Turns a definition into pages of filled primitives, reading nothing but the definition's integers (TARGET-SCHEMA.md
/// rule R5 and section 2). Paint order follows section 3.4's rule that a paper disc reveals what is underneath: the
/// measurement grid first, then cells, then bulls, so a knockout in an aiming mark shows the grid lines below it.
/// </summary>
public static class SceneBuilder
{
    /// <summary>QR symbol versions of TARGET-SCHEMA.md sections 3.8 and 3.11, and the instance symbol's module.</summary>
    public const int InstanceCodeVersion = 11;

    public const int InstanceModuleSize = 4;

    /// <summary>The print instruction the print screen puts on every sheet, in plain words.</summary>
    public const string ActualSizeNote = "Print at actual size, 100 percent. Never fit to page: a scaled sheet loses the spacing it was designed for.";

    /// <summary>
    /// The print note's baseline, 45 dmm above the bottom edge, and its largest size, 18 dmm, both in half-dmm. On every built-in sheet
    /// the band from 41 to 58 dmm is clear: the identifier sits at 74 to 99 dmm and nothing else comes below 70 (PrintNoteTests).
    /// </summary>
    public const long PrintNoteBaselineFromBottom = 90;

    public const long PrintNoteFontSize = 36;

    /// <summary>
    /// The printed name line, NOTES-FROM-PLANNING.md entry 77 section 5: the sheet's name, then <see cref="NameSeparator"/>, then its
    /// identifier, centred with its top <see cref="NameTop"/> dmm below the top edge, the margin the codes keep, at up to
    /// <see cref="NameFontSize"/> half-dmm and never below <see cref="NameMinimumFontSize"/>. It goes only where the analyser never looks and
    /// needs no exclusion box: at least <see cref="NameCellClearance"/> dmm from every bull's cell, which is where render-and-difference
    /// looks for holes, and <see cref="NameItemClearance"/> dmm from anything else printed. A sheet with no such place prints no name.
    /// </summary>
    public const string NameSeparator = " · ";

    public const long NameTop = 136;

    public const long NameFontSize = 50;

    public const long NameMinimumFontSize = 24;

    /// <summary>
    /// Clearance from a cell, dmm: a hole centred on a cell's edge reaches up to about 40 dmm past it, and the difference's closing joins
    /// residue within about 28 dmm, so 60 keeps the name's residue from ever joining a hole's.
    /// </summary>
    public const long NameCellClearance = 60;

    public const long NameItemClearance = 30;

    /// <summary>The printed caption of a load-block field, for a screen that asks for its value.</summary>
    public static string FieldCaption(string key) => Builder.CaptionFor(key);

    public static SceneResult Build(TargetDefinition definition, RenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new Builder(definition, options ?? new RenderOptions()).Run();
    }

    private sealed class Builder(TargetDefinition d, RenderOptions options)
    {
        private static readonly Rgb Black = new(0, 0, 0);

        private static readonly Dictionary<string, string> Captions = new(StringComparer.Ordinal)
        {
            ["date"] = "Date",
            ["distance"] = "Distance",
            ["cartridge"] = "Cartridge",
            ["bullet"] = "Bullet",
            ["powder"] = "Powder and charge",
            ["brass"] = "Brass",
            ["primer"] = "Primer",
            ["seating"] = "Seating depth",
            ["notes"] = "Notes",
        };

        internal static string CaptionFor(string key) => Captions.GetValueOrDefault(key, key);

        private readonly List<Diagnostic> _diagnostics = [];
        private readonly Dictionary<string, Ink> _inks = new(StringComparer.Ordinal);
        private BinaryEncoding? _encoding;

        public SceneResult Run()
        {
            _diagnostics.AddRange(GltdValidator.Validate(d));
            if (!options.AllowInvalid && _diagnostics.Any(x => x.Severity == Severity.Error))
            {
                return Refused("render.invalid", "", "The definition has validation errors, so it is not rendered.");
            }

            var encoded = GltdBinary.Encode(d);
            _diagnostics.AddRange(encoded.Diagnostics);
            if (encoded.Encoding is null)
            {
                return Refused("render.encode", "", "The definition cannot be encoded into the codes it has to print.");
            }

            _encoding = encoded.Encoding;
            foreach (var ink in d.Inks)
            {
                _inks.TryAdd(ink.Key, ink);
            }

            int tiles = d.Tiling is { } t ? t.Cols * t.Rows : 1;
            int[] indices = options.TileIndex is { } only ? [only] : [.. Enumerable.Range(0, tiles)];
            if (indices.Any(i => i < 0 || i >= tiles))
            {
                return Refused("render.tileIndex", "", $"The definition has {tiles} tile(s); tile {options.TileIndex} does not exist.");
            }

            var pages = indices.Select(BuildPage).ToList();
            return _diagnostics.Any(x => x.Severity == Severity.Error && x.Code.StartsWith("render.", StringComparison.Ordinal))
                ? new SceneResult([], _encoding.DefinitionId, _diagnostics)
                : new SceneResult(pages, _encoding.DefinitionId, _diagnostics);
        }

        private SceneResult Refused(string code, string path, string message)
        {
            _diagnostics.Add(Diagnostic.Error(code, path, message));
            return new SceneResult([], null, _diagnostics);
        }

        private void Error(string code, string path, string message) => _diagnostics.Add(Diagnostic.Error(code, path, message));

        private Scene BuildPage(int tile)
        {
            var items = new List<SceneItem>();
            AddMeasurementGrids(items);
            AddCells(items);
            AddBulls(items);
            AddLabels(items);
            AddMarkers(items, tile);
            AddCodes(items, tile);
            AddDataBlock(items);
            AddIdentifier(items, tile);
            AddPrintNote(items);
            AddName(items, tile);
            return new Scene(2L * d.Page.Width, 2L * d.Page.Height, tile, items);
        }

        /// <summary>The print instruction centred along the bottom edge, when the print asks for one, shrunk to fit inside 300 dmm of each side.</summary>
        private void AddPrintNote(List<SceneItem> items)
        {
            if (string.IsNullOrWhiteSpace(options.PrintNote))
            {
                return;
            }

            long size = HelveticaMetrics.FitFontSize(options.PrintNote, (2L * d.Page.Width) - 1200, PrintNoteFontSize);
            if (size < 24)
            {
                Error("render.printNoteTooWide", "", "The print note does not fit along the bottom of the page at a legible size.");
                return;
            }

            items.Add(new TextRun(SceneLayer.PrintNote, RoleColour(InkRole.Text), d.Page.Width, (2L * d.Page.Height) - PrintNoteBaselineFromBottom, size, options.PrintNote, TextAnchor.Centre));
        }

        /// <summary>The colour an ink key lays, or null for the paper knockout, which lays none (section 3.3).</summary>
        private Rgb? Colour(string key)
        {
            if (!_inks.TryGetValue(key, out var ink))
            {
                Error("render.unresolvedInk", "", $"No ink has key \"{key}\".");
                return null;
            }

            return ink.Role == InkRole.Paper ? null : ParseRgb(ink.Srgb);
        }

        /// <summary>
        /// Section 6: a renderer working from GLTD-J uses a declared ink of the role; a decode has none, and prints in
        /// ink index 0, the first non-paper colour.
        /// </summary>
        private Rgb RoleColour(InkRole role) =>
            d.Inks.FirstOrDefault(i => i.Role == role) is { } declared ? ParseRgb(declared.Srgb)
            : d.Inks.FirstOrDefault(i => i.Role != InkRole.Paper) is { } first ? ParseRgb(first.Srgb)
            : Black;

        private string? FirstArtworkKey() => d.Inks.FirstOrDefault(i => i.Role != InkRole.Paper)?.Key;

        private void AddMeasurementGrids(List<SceneItem> items)
        {
            foreach (var g in d.Grids ?? [])
            {
                if (g.StyleOrDefault == GridStyle2.Style)
                {
                    AddStyle2Grid(items, g);
                    continue;
                }

                if (g.StyleOrDefault == GridStyle3.Style)
                {
                    AddStyle3Grid(items, g);
                    continue;
                }

                if (g.StyleOrDefault == GridStyle4.Style)
                {
                    AddCheckPage(items);
                    continue;
                }

                var xs = MeasurementGridLines.Positions(g.CentreX, g.Half, g.Divisions);
                var ys = MeasurementGridLines.Positions(g.CentreY, g.Half, g.Divisions);
                long left = 2L * (g.CentreX - g.Half), right = 2L * (g.CentreX + g.Half);
                long top = 2L * (g.CentreY - g.Half), bottom = 2L * (g.CentreY + g.Half);
                string? major = g.MajorInk ?? FirstArtworkKey();
                string? minor = g.MinorInk ?? FirstArtworkKey();
                string? axis = g.AxisInk ?? major;

                // Heavier lines are drawn after lighter ones, so a crossing shows the heavier weight.
                foreach (int weightClass in (int[])[0, 1, 2])
                {
                    for (int i = -g.Divisions; i <= g.Divisions; i++)
                    {
                        int lineClass = i == 0 ? 2 : i % g.MajorEvery == 0 ? 1 : 0;
                        if (lineClass != weightClass)
                        {
                            continue;
                        }

                        var (key, stroke) = lineClass switch
                        {
                            2 => (axis, g.AxisStroke ?? Projection.AxisStroke),
                            1 => (major, g.MajorStroke ?? Projection.MajorStroke),
                            _ => (minor, g.MinorStroke ?? Projection.MinorStroke),
                        };
                        if (key is null || Colour(key) is not { } colour)
                        {
                            continue;
                        }

                        long x = 2L * xs[i + g.Divisions], y = 2L * ys[i + g.Divisions];
                        items.Add(new RectFill(SceneLayer.MeasurementGrid, colour, x - stroke, top, 2L * stroke, bottom - top));
                        items.Add(new RectFill(SceneLayer.MeasurementGrid, colour, left, y - stroke, right - left, 2L * stroke));
                    }
                }

                AddGridLabels(items, g, xs, ys);
            }
        }

        /// <summary>
        /// A style 2 grid (<see cref="GridStyle2"/>): the lattice lines inside the field in three weights, a label on every major line, the
        /// line broken where it would cross a label, and the scale statement with its ruler above the field.
        /// </summary>
        private void AddStyle2Grid(List<SceneItem> items, MeasurementGrid g)
        {
            string? major = g.MajorInk ?? FirstArtworkKey();
            string? minor = g.MinorInk ?? FirstArtworkKey();
            string? axis = g.AxisInk ?? major;
            string? labelKey = g.LabelInk ?? major;
            if (major is null || minor is null || axis is null || Colour(major) is not { } majorColour)
            {
                return;
            }

            int halfX = g.HalfX, halfY = g.HalfY;
            var columns = GridStyle2.Lines(g, halfX);
            var rows = GridStyle2.Lines(g, halfY);
            long left = 2L * (g.CentreX - halfX), right = 2L * (g.CentreX + halfX);
            long top = 2L * (g.CentreY - halfY), bottom = 2L * (g.CentreY + halfY);

            // The labels first, in dmm boxes, so the lines can leave room for them.
            var labels = new List<(TextRun Run, (long X0, long Y0, long X1, long Y1) Box)>();
            Rgb? labelColour = labelKey is null ? null : Colour(labelKey);
            long size = 2L * GridStyle2.FontSizeForCap(GridStyle2.LabelCap);
            long firstLabel = (GridStyle2.WholeStroke / 2) + GridStyle2.LabelGap;
            if (g.LabelStep is > 0 && labelColour is { } lc)
            {
                // Each label is centred on its own line, like the figures on a ruler, and the line is broken behind it; a label at the
                // field's edge moves inward to stay on the paper. Across the axis below it for the upright lines, beside the upright
                // axis for the level ones, so the two sets never meet and neither reaches the aiming ring.
                foreach (var (i, offset) in columns.Where(c => c.Index != 0 && c.Index % g.LabelStep.Value == 0))
                {
                    string text = GridStyle2.Label(g, i);
                    long width = HelveticaMetrics.TextWidth(text, size) / 2;
                    long x0 = Centred(offset, width, halfX);
                    long capTop = g.CentreY + firstLabel;
                    labels.Add((new TextRun(SceneLayer.MeasurementGrid, lc, 2 * (g.CentreX + x0), 2 * (capTop + GridStyle2.LabelCap), size, text, TextAnchor.Left),
                        (g.CentreX + x0, capTop, g.CentreX + x0 + width, capTop + GridStyle2.LabelCap)));
                }

                foreach (var (i, offset) in rows.Where(r => r.Index != 0 && r.Index % g.LabelStep.Value == 0))
                {
                    string text = GridStyle2.Label(g, i);
                    long width = HelveticaMetrics.TextWidth(text, size) / 2;
                    long x = g.CentreX + firstLabel;
                    long capTop = g.CentreY + Centred(offset, GridStyle2.LabelCap, halfY);
                    labels.Add((new TextRun(SceneLayer.MeasurementGrid, lc, 2 * x, 2 * (capTop + GridStyle2.LabelCap), size, text, TextAnchor.Left),
                        (x, capTop, x + width, capTop + GridStyle2.LabelCap)));
                }
            }

            var clear = labels.Select(l => (2 * (l.Box.X0 - GridStyle2.LabelMargin), 2 * (l.Box.Y0 - GridStyle2.LabelMargin),
                2 * (l.Box.X1 + GridStyle2.LabelMargin), 2 * (l.Box.Y1 + GridStyle2.LabelMargin))).ToList();

            // Heavier lines are drawn after lighter ones, so a crossing shows the heavier weight.
            foreach (int weight in (int[])[0, 1, 2])
            {
                var (key, stroke) = weight switch
                {
                    2 => (axis, (long)GridStyle2.WholeStroke),
                    1 => (major, (long)GridStyle2.MajorStroke),
                    _ => (minor, (long)GridStyle2.FineStroke),
                };
                if (Colour(key) is not { } colour)
                {
                    continue;
                }

                foreach (var (_, offset) in columns.Where(c => GridStyle2.Weight(g, c.Index) == weight))
                {
                    long x = 2L * (g.CentreX + offset);
                    AddBroken(items, colour, x - stroke, x + stroke, top, bottom, vertical: true, clear);
                }

                foreach (var (_, offset) in rows.Where(r => GridStyle2.Weight(g, r.Index) == weight))
                {
                    long y = 2L * (g.CentreY + offset);
                    AddBroken(items, colour, y - stroke, y + stroke, left, right, vertical: false, clear);
                }
            }

            items.AddRange(labels.Select(l => l.Run));
            AddScaleStatement(items, g, majorColour);
        }

        /// <summary>
        /// A style 3 grid (<see cref="GridStyle3"/>, design C3 of entry 251): every lattice line inside the field in three weights, a click's
        /// tick halfway between the lines along the centre cross and inward from the frame, each line's distance from the aim written
        /// outside the frame on all four sides and nothing inside it, the legend above between the codes, and the check bar below.
        /// </summary>
        private void AddStyle3Grid(List<SceneItem> items, MeasurementGrid g)
        {
            string? major = g.MajorInk ?? FirstArtworkKey();
            string? minor = g.MinorInk ?? FirstArtworkKey();
            string? axis = g.AxisInk ?? major;
            if (major is null || minor is null || axis is null || Colour(major) is not { } ink)
            {
                return;
            }

            int halfX = g.HalfX, halfY = g.HalfY;
            var columns = GridStyle3.Lines(g, halfX);
            var rows = GridStyle3.Lines(g, halfY);
            long left = 2L * (g.CentreX - halfX), right = 2L * (g.CentreX + halfX);
            long top = 2L * (g.CentreY - halfY), bottom = 2L * (g.CentreY + halfY);

            // The aim's white centre stays white: the centre cross is left out inside it, where the drawing has paper.
            var clear = new List<(long X0, long Y0, long X1, long Y1)>();
            foreach (var bull in d.Bulls.Where(b => b.X == g.CentreX && b.Y == g.CentreY))
            {
                if (d.RingSets.FirstOrDefault(r => r.Key == bull.RingSet)?.Discs.Where(disc => Colour(disc.Ink) is null).Select(disc => disc.Diameter).DefaultIfEmpty(0).Max() is > 0 and var white)
                {
                    clear.Add((2L * g.CentreX - white, 2L * g.CentreY - white, 2L * g.CentreX + white, 2L * g.CentreY + white));
                }
            }

            foreach (int weight in (int[])[0, 1, 2])
            {
                var (key, stroke) = weight switch
                {
                    2 => (axis, (long)GridStyle3.HeavyStroke),
                    1 => (major, (long)GridStyle3.WholeStroke),
                    _ => (minor, (long)GridStyle3.FineStroke),
                };
                if (Colour(key) is not { } colour)
                {
                    continue;
                }

                foreach (var (i, offset) in columns.Where(c => GridStyle3.Weight(g, c.Index, c.Offset, halfX) == weight))
                {
                    long x = 2L * (g.CentreX + offset);
                    AddBroken(items, colour, x - stroke, x + stroke, top - (weight == 2 ? stroke : 0), bottom + (weight == 2 ? stroke : 0), vertical: true, clear);
                }

                foreach (var (i, offset) in rows.Where(r => GridStyle3.Weight(g, r.Index, r.Offset, halfY) == weight))
                {
                    long y = 2L * (g.CentreY + offset);
                    AddBroken(items, colour, y - stroke, y + stroke, left, right, vertical: false, clear);
                }
            }

            // One click's tick halfway between the lines: across the centre cross both ways, and inward from each side of the frame.
            long tickHalf = GridStyle3.TickStroke, reach = 2L * GridStyle3.TickReach;
            foreach (int tick in GridStyle3.Ticks(g, halfX))
            {
                long x = 2L * (g.CentreX + tick);
                items.Add(new RectFill(SceneLayer.MeasurementGrid, ink, x - tickHalf, (2L * g.CentreY) - reach, 2 * tickHalf, 2 * reach));
                items.Add(new RectFill(SceneLayer.MeasurementGrid, ink, x - tickHalf, top, 2 * tickHalf, reach));
                items.Add(new RectFill(SceneLayer.MeasurementGrid, ink, x - tickHalf, bottom - reach, 2 * tickHalf, reach));
            }

            foreach (int tick in GridStyle3.Ticks(g, halfY))
            {
                long y = 2L * (g.CentreY + tick);
                items.Add(new RectFill(SceneLayer.MeasurementGrid, ink, (2L * g.CentreX) - reach, y - tickHalf, 2 * reach, 2 * tickHalf));
                items.Add(new RectFill(SceneLayer.MeasurementGrid, ink, left, y - tickHalf, reach, 2 * tickHalf));
                items.Add(new RectFill(SceneLayer.MeasurementGrid, ink, right - reach, y - tickHalf, reach, 2 * tickHalf));
            }

            // Each line's distance from the aim, outside the frame on all four sides; a whole unit larger and bold.
            long outer = GridStyle3.HeavyStroke / 2, gap = GridStyle3.NumberGap;
            foreach (var (i, offset) in columns)
            {
                bool whole = GridStyle3.IsWhole(g, i);
                int cap = whole ? GridStyle3.WholeNumberCap : GridStyle3.NumberCap;
                long size = 2L * GridStyle3.FontSizeForCap(cap);
                string text = GridStyle3.Number(g, i);
                long x = 2L * (g.CentreX + offset);
                items.Add(new TextRun(SceneLayer.MeasurementGrid, ink, x, 2L * (g.CentreY - halfY - outer - gap), size, text, TextAnchor.Centre, whole));
                items.Add(new TextRun(SceneLayer.MeasurementGrid, ink, x, 2L * (g.CentreY + halfY + outer + gap + cap), size, text, TextAnchor.Centre, whole));
            }

            foreach (var (i, offset) in rows)
            {
                bool whole = GridStyle3.IsWhole(g, i);
                int cap = whole ? GridStyle3.WholeNumberCap : GridStyle3.NumberCap;
                long size = 2L * GridStyle3.FontSizeForCap(cap);
                string text = GridStyle3.Number(g, i);
                long baseline = (2L * (g.CentreY + offset)) + cap;
                items.Add(new TextRun(SceneLayer.MeasurementGrid, ink, 2L * (g.CentreX - halfX - outer - gap), baseline, size, text, TextAnchor.Right, whole));
                items.Add(new TextRun(SceneLayer.MeasurementGrid, ink, 2L * (g.CentreX + halfX + outer + gap), baseline, size, text, TextAnchor.Left, whole));
            }

            AddStyle3Legend(items, g, ink);
            AddStyle3Bar(items, g, ink);
        }

        /// <summary>
        /// The legend above the grid, between the two top codes, in bold large enough to read through the scope at the sheet's distance:
        /// the unit and distance, a square drawn beside what it is, and a tick drawn beside what it is.
        /// </summary>
        private void AddStyle3Legend(List<SceneItem> items, MeasurementGrid g, Rgb ink)
        {
            var (heading, square, tick) = GridStyle3.Legend(g);
            long centre = 2L * g.CentreX, width = 2L * GridStyle3.LegendWidth;
            long Fit(string text, int cap, long room)
            {
                long size = 2L * GridStyle3.FontSizeForCap(cap);
                long wide = HelveticaMetrics.TextWidth(text, size, bold: true);
                return wide <= room ? size : size * room / wide;
            }

            long headingSize = Fit(heading, GridStyle3.LegendCap, width);
            items.Add(new TextRun(SceneLayer.MeasurementGrid, ink, centre, 2L * GridStyle3.LegendBaselines[0], headingSize, heading, TextAnchor.Centre, true));

            // A swatch the height of the capitals it stands beside, then the words.
            void Swatched(string text, int cap, long baseline, Action<long, long> swatch)
            {
                long side = 2L * cap, space = side / 3;
                long size = Fit(text, cap, width - side - space);
                long total = side + space + HelveticaMetrics.TextWidth(text, size, bold: true);
                long start = centre - (total / 2);
                swatch(start, side);
                items.Add(new TextRun(SceneLayer.MeasurementGrid, ink, start + side + space, baseline, size, text, TextAnchor.Left, true));
            }

            long squareBaseline = 2L * GridStyle3.LegendBaselines[1];
            Swatched(square, GridStyle3.SquareCap, squareBaseline, (x, side) =>
            {
                long line = 2L * GridStyle3.FineStroke, y = squareBaseline - side;
                items.Add(new RectFill(SceneLayer.MeasurementGrid, ink, x, y, side, line));
                items.Add(new RectFill(SceneLayer.MeasurementGrid, ink, x, squareBaseline - line, side, line));
                items.Add(new RectFill(SceneLayer.MeasurementGrid, ink, x, y, line, side));
                items.Add(new RectFill(SceneLayer.MeasurementGrid, ink, x + side - line, y, line, side));
            });

            long tickBaseline = 2L * GridStyle3.LegendBaselines[2];
            Swatched(tick, GridStyle3.TickCap, tickBaseline, (x, side) =>
            {
                long middle = tickBaseline - (side / 2), line = GridStyle3.HeavyStroke;
                items.Add(new RectFill(SceneLayer.MeasurementGrid, ink, x, middle - (line / 2), side, line));
                items.Add(new RectFill(SceneLayer.MeasurementGrid, ink, x + (side / 2) - GridStyle3.TickStroke, middle - (side / 2), 2L * GridStyle3.TickStroke, side));
            });
        }

        /// <summary>
        /// Grid style 4, the printer check page of entries 272 and 273, as Alan approved it: the rulers, the crosshairs' arms and the dashed
        /// guides between them, the card outline and every word, all derived by <see cref="GridStyle4"/> from the page size. The crosshairs'
        /// circles are the definition's own bulls, drawn with the rest of the bulls.
        /// </summary>
        private void AddCheckPage(List<SceneItem> items)
        {
            var ink = RoleColour(InkRole.Text);
            const SceneLayer layer = SceneLayer.MeasurementGrid;
            long line = GridStyle4.LineStroke;
            void Rect(long x, long y, long w, long h) => items.Add(new RectFill(layer, ink, 2 * x, 2 * y, 2 * w, 2 * h));
            void Text(long x, long baseline, int cap, string text, TextAnchor anchor, bool bold = false, long room = 0)
            {
                long size = 2L * GridStyle3.FontSizeForCap(cap);
                long wide = HelveticaMetrics.TextWidth(text, size, bold);
                if (room > 0 && wide > 2 * room)
                {
                    size = size * 2 * room / wide;
                }

                items.Add(new TextRun(layer, ink, 2 * x, 2 * baseline, size, text, anchor, bold));
            }

            void Dashed(long x0, long y0, long x1, long y1)
            {
                const long dash = 20, gap = 16;
                bool across = y0 == y1;
                for (long s = across ? x0 : y0, end = across ? x1 : y1; s < end; s += dash + gap)
                {
                    long e = Math.Min(s + dash, end);
                    if (across)
                    {
                        Rect(s, y0 - 1, e - s, 2);
                    }
                    else
                    {
                        Rect(x0 - 1, s, 2, e - s);
                    }
                }
            }

            var page = d.Page;
            int middle = page.Width / 2;

            // The title block, between the two top codes.
            Text(middle, 150, 44, GridStyle4.Title, TextAnchor.Centre, bold: true);
            Text(middle, 225, 24, GridStyle4.PrintAtActualSize, TextAnchor.Centre);
            Text(middle, 290, 20, GridStyle4.PageName(page), TextAnchor.Centre);

            // The ruler down the side, tick to tick, its words beside its top.
            var (dx, top, bottom) = GridStyle4.RulerDown(page);
            Rect(dx - (line / 2), top, line, bottom - top);
            Rect(dx - (GridStyle4.EndTick / 2), top - (line / 2), GridStyle4.EndTick, line);
            Rect(dx - (GridStyle4.EndTick / 2), bottom - (line / 2), GridStyle4.EndTick, line);
            Text(dx + 45, 400, 20, "DOWN THIS SIDE, " + GridStyle4.RulerLabel(GridStyle4.RulerDownDmm), TextAnchor.Left, room: 1300);

            // The ruler across the bottom.
            var (ay, left, right) = GridStyle4.RulerAcross(page);
            Rect(left, ay - (line / 2), right - left, line);
            Rect(left - (line / 2), ay - (GridStyle4.EndTick / 2), line, GridStyle4.EndTick);
            Rect(right - (line / 2), ay - (GridStyle4.EndTick / 2), line, GridStyle4.EndTick);
            Text((left + right) / 2, ay - 50, 20, GridStyle4.RulerLabel(GridStyle4.RulerAcrossDmm), TextAnchor.Centre);

            // The crosshairs' arms, and the dashed guides between their centers.
            var cross = GridStyle4.Crosshairs(page);
            foreach (var c in cross)
            {
                Rect(c.X - GridStyle4.CrossArm, c.Y - (line / 2), 2 * GridStyle4.CrossArm, line);
                Rect(c.X - (line / 2), c.Y - GridStyle4.CrossArm, line, 2 * GridStyle4.CrossArm);
            }

            Dashed(cross[0].X + GridStyle4.CrossArm + 20, cross[0].Y, cross[1].X - GridStyle4.CrossArm - 20, cross[1].Y);
            Dashed(cross[1].X, cross[1].Y + GridStyle4.CrossArm + 20, cross[1].X, cross[2].Y - GridStyle4.CrossArm - 20);
            Text((cross[0].X + cross[1].X) / 2, cross[0].Y - 40, 20, GridStyle4.CaliperLabel, TextAnchor.Centre);
            Text(cross[2].X - GridStyle4.CrossArm - 30, cross[2].Y + 12, 20, "DOWN, " + GridStyle4.CaliperLabel, TextAnchor.Right, room: 1400);

            // The card outline, a gap of white outside the card's size so the card's edges lie on paper, and its words.
            var card = GridStyle4.Card(page);
            long o = GridStyle4.OutlineStroke, gap = GridStyle4.OutlineGap, w = GridStyle4.CardWidthDmm, h = GridStyle4.CardHeightDmm;
            Rect(card.X - gap - o, card.Y - gap - o, w + (2 * (gap + o)), o);
            Rect(card.X - gap - o, card.Y + h + gap, w + (2 * (gap + o)), o);
            Rect(card.X - gap - o, card.Y - gap, o, h + (2 * gap));
            Rect(card.X + w + gap, card.Y - gap, o, h + (2 * gap));
            Text(card.X - gap - o, card.Y - gap - o - 22, 20, "CARD", TextAnchor.Left, bold: true);
            long cx = card.X + (w / 2);
            Text(cx, card.Y + (h / 2) - 40, 28, GridStyle4.CardHeading, TextAnchor.Centre, bold: true, room: w - 60);
            Text(cx, card.Y + (h / 2) + 30, 22, GridStyle4.CardSize, TextAnchor.Centre);
            Text(cx, card.Y + (h / 2) + 90, 20, GridStyle4.CardNote, TextAnchor.Centre);

            // The four instructions under the card, each a bold lead and its words, wrapped to the room beside the dashed guide.
            long x0 = 300, room = cross[1].X - 80 - x0, baselineY = 1760;
            Text(x0, baselineY, 28, GridStyle4.InstructionsHeading, TextAnchor.Left, bold: true, room: room);
            long size = 2L * GridStyle3.FontSizeForCap(22);
            foreach (var (lead, words) in GridStyle4.Instructions)
            {
                baselineY += 80;
                long leadWidth = (HelveticaMetrics.TextWidth(lead + " ", size, bold: true) + 1) / 2;
                items.Add(new TextRun(layer, ink, 2 * x0, 2 * baselineY, size, lead, TextAnchor.Left, true));
                var lineWords = new List<string>();
                long start = x0 + leadWidth;
                foreach (string word in words.Split(' '))
                {
                    string trial = string.Join(' ', lineWords.Append(word));
                    if (lineWords.Count > 0 && (HelveticaMetrics.TextWidth(trial, size) / 2) > room - (start - x0))
                    {
                        items.Add(new TextRun(layer, ink, 2 * start, 2 * baselineY, size, string.Join(' ', lineWords), TextAnchor.Left));
                        lineWords.Clear();
                        baselineY += 55;
                    }

                    lineWords.Add(word);
                }

                items.Add(new TextRun(layer, ink, 2 * start, 2 * baselineY, size, string.Join(' ', lineWords), TextAnchor.Left));
            }
        }

        /// <summary>The check bar below the numbers: 4 in or 10 cm, a tick at every inch or centimetre, and a line saying so.</summary>
        private void AddStyle3Bar(List<SceneItem> items, MeasurementGrid g, Rgb ink)
        {
            var (length, ticks, caption) = GridStyle3.Bar(g);
            long barTop = g.CentreY + g.HalfY + (GridStyle3.HeavyStroke / 2) + GridStyle3.BarBelowFrame;
            long barLeft = g.CentreX - (length / 2);
            items.Add(new RectFill(SceneLayer.MeasurementGrid, ink, 2 * barLeft, 2 * barTop, 2L * length, 2L * GridStyle3.BarThickness));
            for (int k = 0; k <= ticks; k++)
            {
                long x = (2 * barLeft) + (2L * length * k / ticks);
                long tick = (k == 0 || k == ticks || (ticks == 10 && k == 5)) ? 2L * GridStyle2.RulerTick : GridStyle2.RulerTick;
                items.Add(new RectFill(SceneLayer.MeasurementGrid, ink, Math.Clamp(x - GridStyle2.RulerTickWidth, 2 * barLeft, (2 * (barLeft + length)) - (2 * GridStyle2.RulerTickWidth)),
                    (2 * barTop) - tick, 2L * GridStyle2.RulerTickWidth, tick));
            }

            long size = 2L * GridStyle3.FontSizeForCap(GridStyle3.BarCaptionCap);
            long baseline = 2L * (barTop + GridStyle3.BarThickness + 20 + GridStyle3.BarCaptionCap);
            items.Add(new TextRun(SceneLayer.MeasurementGrid, RoleColour(InkRole.Text), 2L * g.CentreX, baseline, size, caption, TextAnchor.Centre));
        }

        /// <summary>
        /// Where a label of <paramref name="extent"/> centred on a line at <paramref name="offset"/> starts, kept inside the field and clear
        /// of the line along its edge, so the edge of the field is never broken.
        /// </summary>
        private static long Centred(int offset, long extent, int field)
        {
            long inset = (GridStyle2.WholeStroke / 2) + (2 * GridStyle2.LabelMargin);
            return Math.Clamp(offset - (extent / 2), -field + inset, field - inset - extent);
        }

        /// <summary>A line from <paramref name="from"/> to <paramref name="to"/> along its length, in half-dmm, left out where it crosses a clear box.</summary>
        private static void AddBroken(List<SceneItem> items, Rgb colour, long across0, long across1, long from, long to, bool vertical,
            IReadOnlyList<(long X0, long Y0, long X1, long Y1)> clear)
        {
            var gaps = clear
                .Where(b => vertical ? b.X0 < across1 && across0 < b.X1 : b.Y0 < across1 && across0 < b.Y1)
                .Select(b => vertical ? (b.Y0, b.Y1) : (b.X0, b.X1))
                .OrderBy(b => b.Item1)
                .ToList();
            long at = from;
            foreach (var (g0, g1) in gaps.Append((to, to)))
            {
                long end = Math.Min(g0, to);
                if (end > at)
                {
                    items.Add(vertical
                        ? new RectFill(SceneLayer.MeasurementGrid, colour, across0, at, across1 - across0, end - at)
                        : new RectFill(SceneLayer.MeasurementGrid, colour, at, across0, end - at, across1 - across0));
                }

                at = Math.Max(at, g1);
            }
        }

        /// <summary>
        /// The style 2 scale statement and ruler, centred above the field and clear of the marker row: three lines saying what the squares
        /// are at the stated distance, then a bar of 4 in or 10 cm with a tick at every inch or centimetre, to measure the print with.
        /// </summary>
        private void AddScaleStatement(List<SceneItem> items, MeasurementGrid g, Rgb colour)
        {
            var lines = GridStyle2.Statement(g);
            if (lines.Count == 0)
            {
                return;
            }

            Rgb text = RoleColour(InkRole.Text);
            long barBottom = g.CentreY - g.HalfY - GridStyle2.StatementClearance;
            long barTop = barBottom - GridStyle2.RulerThickness;
            var (length, ticks, _) = GridStyle2.Ruler(g);
            long barLeft = g.CentreX - (length / 2);
            items.Add(new RectFill(SceneLayer.MeasurementGrid, colour, 2 * barLeft, 2 * barTop, 2L * length, 2L * GridStyle2.RulerThickness));
            for (int k = 0; k <= ticks; k++)
            {
                long x = (2 * barLeft) + (2L * length * k / ticks);
                long tick = (k == 0 || k == ticks || (ticks == 10 && k == 5)) ? 2L * GridStyle2.RulerTick : GridStyle2.RulerTick;
                items.Add(new RectFill(SceneLayer.MeasurementGrid, colour, Math.Clamp(x - (GridStyle2.RulerTickWidth / 2 * 2), 2 * barLeft, (2 * (barLeft + length)) - GridStyle2.RulerTickWidth * 2),
                    (2 * barTop) - tick, 2L * GridStyle2.RulerTickWidth, tick));
            }

            long maximum = 2L * GridStyle2.FontSizeForCap(GridStyle2.StatementCap);
            long lineHeight = 2L * GridStyle2.StatementCap * 3 / 2;
            long baseline = (2 * barTop) - (2L * GridStyle2.RulerTick) - (2L * GridStyle2.StatementCap / 2);
            for (int n = lines.Count - 1; n >= 0; n--)
            {
                long size = HelveticaMetrics.FitFontSize(lines[n], 2L * GridStyle2.StatementWidth, maximum);
                items.Add(new TextRun(SceneLayer.MeasurementGrid, text, 2L * g.CentreX, baseline, size, lines[n], TextAnchor.Centre));
                baseline -= lineHeight;
            }
        }

        /// <summary>
        /// Labels in the grid's unit, beside the axes inside the field (docs/SPEC-ERRATA.md C6). The value of a line is
        /// its offset divided by one unit at the stated distance; it is text for the eye and places nothing.
        /// </summary>
        private void AddGridLabels(List<SceneItem> items, MeasurementGrid g, IReadOnlyList<int> xs, IReadOnlyList<int> ys)
        {
            double distanceDmm = g.Distance * (g.DistanceUnit == DistanceUnit.Yards ? 9144.0 : 10000.0);
            double? unitDmm = g.Unit switch
            {
                GridUnit.Moa => distanceDmm * Math.Tan(Math.PI / 180.0 / 60.0),
                GridUnit.Mil => distanceDmm * 0.001,
                _ => null,
            };
            if (g.LabelStep is not > 0 || unitDmm is null)
            {
                return;
            }

            string? labelKey = g.LabelInk ?? g.MajorInk ?? FirstArtworkKey();
            if (labelKey is null || Colour(labelKey) is not { } colour)
            {
                return;
            }

            const long fontSize = 40;
            long cap = fontSize * HelveticaMetrics.CapHeight / 1000;
            long axis = g.AxisStroke ?? Projection.AxisStroke;
            for (int i = g.LabelStep.Value; i <= g.Divisions; i += g.LabelStep.Value)
            {
                int offset = xs[i + g.Divisions] - g.CentreX;
                string text = Math.Round(offset / unitDmm.Value, 1).ToString("0.#", CultureInfo.InvariantCulture);
                foreach (int sign in (int[])[-1, 1])
                {
                    items.Add(new TextRun(SceneLayer.MeasurementGrid, colour, 2L * (g.CentreX + (sign * offset)),
                        (2L * g.CentreY) + axis + 20 + cap, fontSize, text, TextAnchor.Centre));
                    items.Add(new TextRun(SceneLayer.MeasurementGrid, colour, (2L * g.CentreX) - axis - 20,
                        (2L * ys[(sign * i) + g.Divisions]) + (cap / 2), fontSize, text, TextAnchor.Right));
                }
            }
        }

        /// <summary>Drawn cell boundaries only (section 3.6), clipped at the sheet edge like any other artwork.</summary>
        private void AddCells(List<SceneItem> items)
        {
            if (d.Cells is not { Drawn: true } cells || BullLayout.Recognise(d) is not { } layout)
            {
                return;
            }

            string? key = cells.Ink ?? FirstArtworkKey();
            if (key is null || Colour(key) is not { } colour)
            {
                return;
            }

            var g = layout.Grid;
            long stroke = cells.Stroke ?? 3;
            long x0 = (2L * g.OriginX) - g.PitchX, y0 = (2L * g.OriginY) - g.PitchY;
            long x1 = x0 + (2L * g.Cols * g.PitchX), y1 = y0 + (2L * g.Rows * g.PitchY);
            long width = 2L * d.Page.Width, height = 2L * d.Page.Height;
            for (int k = 0; k <= g.Cols; k++)
            {
                long x = x0 + (2L * k * g.PitchX);
                AddClipped(items, colour, x - stroke, y0, 2 * stroke, y1 - y0, width, height);
            }

            for (int k = 0; k <= g.Rows; k++)
            {
                long y = y0 + (2L * k * g.PitchY);
                AddClipped(items, colour, x0, y - stroke, x1 - x0, 2 * stroke, width, height);
            }
        }

        private static void AddClipped(List<SceneItem> items, Rgb colour, long x, long y, long w, long h, long pageWidth, long pageHeight)
        {
            long left = Math.Max(0, x), top = Math.Max(0, y);
            long right = Math.Min(pageWidth, x + w), bottom = Math.Min(pageHeight, y + h);
            if (right > left && bottom > top)
            {
                items.Add(new RectFill(SceneLayer.Cells, colour, left, top, right - left, bottom - top));
            }
        }

        /// <summary>
        /// Section 3.4: each disc is painted outermost first, so the ink of disc i is the ring between it and disc i+1.
        /// Adjacent discs of the same colour merge into one band, and a paper band is not drawn at all.
        /// </summary>
        private void AddBulls(List<SceneItem> items)
        {
            var sets = d.RingSets.GroupBy(s => s.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            foreach (var bull in d.Bulls)
            {
                if (!sets.TryGetValue(bull.RingSet, out var set))
                {
                    continue;
                }

                var colours = set.Discs.Select(disc => Colour(disc.Ink)).ToList();
                for (int k = 0; k < set.Discs.Count;)
                {
                    int j = k;
                    while (j + 1 < set.Discs.Count && colours[j + 1] == colours[k])
                    {
                        j++;
                    }

                    if (colours[k] is { } colour)
                    {
                        var outer = set.Discs[k];
                        var within = j + 1 < set.Discs.Count ? set.Discs[j + 1] : null;
                        items.Add(new DiscBand(SceneLayer.Bulls, colour, 2L * bull.X, 2L * bull.Y, outer.Diameter, within?.Diameter ?? 0,
                            outer.Shape, outer.Rotation, within?.Shape ?? DiscShape.Circle, within?.Rotation ?? 0));
                    }

                    k = j + 1;
                }
            }
        }

        private void AddLabels(List<SceneItem> items)
        {
            if (!LabelLayout.Printed(d))
            {
                return;
            }

            var outer = d.RingSets.GroupBy(s => s.Key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First().Discs.Max(disc => disc.Diameter), StringComparer.Ordinal);
            var colour = RoleColour(InkRole.Text);
            foreach (var bull in d.Bulls)
            {
                if (outer.TryGetValue(bull.RingSet, out int diameter) && LabelLayout.Run(bull, diameter, colour) is { } run)
                {
                    items.Add(run);
                }
            }
        }

        /// <summary>
        /// Markers for one tile, with the identifiers section 3.7 assigns over the assembly. FIDUCIAL-DECISION.md
        /// section 8 requires modules printed solid, which filled rectangles on an integer half-dmm grid are.
        /// </summary>
        private void AddMarkers(List<SceneItem> items, int tile)
        {
            if (d.Fiducials is not { } f)
            {
                return;
            }

            if (f.Family != FiducialFamily.AprilTag36h11)
            {
                Error("render.markerFamily", "/fiducials/family", "Only apriltag-36h11 markers can be rendered.");
                return;
            }

            if (f.MarkerSize % 4 != 0)
            {
                Error("render.markerModule", "/fiducials/markerSize",
                    $"A {f.MarkerSize} dmm marker has a module of {f.MarkerSize / 8.0} dmm, which is not a whole half-dmm.");
                return;
            }

            IReadOnlyList<Marker> markers;
            if (f.Scheme == "explicit")
            {
                markers = f.Markers ?? [];
            }
            else if (FiducialDerivation.Derive(d).Markers is { } derived)
            {
                markers = MarkerIds.Assign(derived.Positions, d.Tiling, tile, MarkerIds.DictionarySize(f.Family)).Markers;
            }
            else
            {
                return;
            }

            if (Colour(f.Ink) is not { } colour)
            {
                return;
            }

            long module = f.MarkerSize / 4;
            foreach (var marker in markers)
            {
                var inked = Tag36h11.InkedModules(marker.Id);
                AddModules(items, SceneLayer.Markers, colour, (2L * marker.X) - f.MarkerSize, (2L * marker.Y) - f.MarkerSize, module,
                    Tag36h11.Modules, (row, col) => inked[row, col]);
            }
        }

        /// <summary>
        /// Every code on a sheet carries the same replicated frame (section 5.1), with the tile index of this page in its
        /// header. The symbol version and error correction level are fixed, not chosen to fit, so the footprint is the one
        /// <c>corners-1</c> reserved.
        /// </summary>
        private void AddCodes(List<SceneItem> items, int tile)
        {
            if (d.Codes is not { Count: > 0 } c)
            {
                return;
            }

            byte[] frame = FrameCodec.Replicated(_encoding!.Body, _encoding.BlockFlags, (byte)tile);
            var positions = c.Placement == CodePlacement.Corners1
                ? Corners1.Positions(d.Page.Width, d.Page.Height, d.DataBlock?.Height ?? 0, c.Count, c.ModuleSize)
                : c.Positions;
            var symbol = Symbol(frame, (QrCode.Ecc)c.EcLevel, c.Version ?? Projection.CodeVersion, "/codes");
            if (symbol is null)
            {
                return;
            }

            var colour = RoleColour(InkRole.Code);
            foreach (var p in positions)
            {
                AddSymbol(items, SceneLayer.Codes, colour, symbol, p.X, p.Y, c.ModuleSize);
            }
        }

        private QrCode? Symbol(byte[] payload, QrCode.Ecc level, int version, string path)
        {
            try
            {
                return QrCode.EncodeSegments([DataSegment.MakeSegment(DataSegmentMode.Binary,new ArraySegment<byte>(payload))], level, version, version, false);
            }
            catch (DataTooLongException)
            {
                Error("render.codeCapacity", path, $"A {payload.Length}-byte frame does not fit a version {version} symbol at level {level}.");
                return null;
            }
        }

        /// <summary>Centres a symbol on (<paramref name="centreX"/>, <paramref name="centreY"/>) in dmm; its quiet zone is left unprinted.</summary>
        private static void AddSymbol(List<SceneItem> items, SceneLayer layer, Rgb colour, QrCode symbol, int centreX, int centreY, int moduleSize)
        {
            long module = 2L * moduleSize;
            long half = symbol.Size * (long)moduleSize;
            AddModules(items, layer, colour, (2L * centreX) - half, (2L * centreY) - half, module, symbol.Size, (row, col) => symbol.GetModule(col, row));
        }

        /// <summary>Inked modules as one rectangle per horizontal run, which keeps every edge on the module grid.</summary>
        private static void AddModules(List<SceneItem> items, SceneLayer layer, Rgb colour, long left, long top, long module, int size, Func<int, int, bool> inked)
        {
            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < size;)
                {
                    if (!inked(row, col))
                    {
                        col++;
                        continue;
                    }

                    int start = col;
                    while (col < size && inked(row, col))
                    {
                        col++;
                    }

                    items.Add(new RectFill(layer, colour, left + (start * module), top + (row * module), (col - start) * module, module));
                }
            }
        }

        /// <summary>
        /// The data block of section 3.10. Rules and captions are the same in both print modes; only the typed values and
        /// the reserved square's content differ, on <see cref="SceneLayer.DataBlockContent"/>. The square holds the
        /// identifier and serial as text when blank, and also when filled if it is under 280 dmm; a filled square of 280
        /// dmm or more holds the GLTD-I instance code of section 3.11.
        /// </summary>
        private void AddDataBlock(List<SceneItem> items)
        {
            if (d.DataBlock is not { } block || block.Layout == DataBlockLayout.Explicit || block.FieldSet == FieldSet.Explicit)
            {
                return;
            }

            var geometry = DataBlockCells.Derive(block);
            long border = block.Border ?? 2;
            if (border > 0 && Colour(block.Ink) is { } rule)
            {
                Outline(items, rule, 2L * block.X, 2L * block.Y, 2L * block.Width, 2L * block.Height, 2 * border);
                foreach (var cell in geometry.Cells.Append(geometry.Reserve).OfType<DataFieldCell>())
                {
                    Outline(items, rule, 2L * cell.X, 2L * cell.Y, 2L * cell.Width, 2L * cell.Height, 2 * border);
                }
            }

            const long captionSize = 36;
            var captionColour = Colour(block.LabelInk ?? block.Ink);
            foreach (var cell in geometry.Cells)
            {
                if (captionColour is { } cc)
                {
                    items.Add(new TextRun(SceneLayer.DataBlockFrame, cc, (2L * cell.X) + 30, (2L * cell.Y) + 50, captionSize,
                        Captions.GetValueOrDefault(cell.Key, cell.Key), TextAnchor.Left));
                }
            }

            var instance = options.Instance ?? d.Instance;
            var textColour = Colour(block.Ink) ?? Black;
            if (options.Mode == DataBlockMode.Filled)
            {
                foreach (var cell in geometry.Cells)
                {
                    string? value = instance?.Values?.FirstOrDefault(v => v.Key == cell.Key).Value;
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    long size = HelveticaMetrics.FitFontSize(value, (2L * cell.Width) - 60, 60);
                    if (size < 24)
                    {
                        Error("render.valueTooWide", $"/instance/values/{cell.Key}",
                            $"\"{cell.Key}\" does not fit its {cell.Width} dmm field at a legible size.");
                        continue;
                    }

                    items.Add(new TextRun(SceneLayer.DataBlockContent, textColour, (2L * cell.X) + 30,
                        (2L * (cell.Y + cell.Height)) - 30, size, value, TextAnchor.Left));
                }
            }

            if (geometry.Reserve is not { } square)
            {
                return;
            }

            if (options.Mode == DataBlockMode.Filled && block.Reserve >= InstanceCodec.MinimumReserveForCode)
            {
                if (instance is null)
                {
                    Error("render.instanceMissing", "/instance", "A filled data block needs instance data for its instance code.");
                    return;
                }

                var encoded = InstanceCodec.Encode(instance, block.FieldSet, _encoding!.DefinitionId);
                _diagnostics.AddRange(encoded.Diagnostics);
                if (encoded.Frame is { } frame && Symbol(frame, QrCode.Ecc.Quartile, InstanceCodeVersion, "/instance") is { } symbol)
                {
                    AddSymbol(items, SceneLayer.DataBlockContent, textColour, symbol,
                        square.X + (square.Width / 2), square.Y + (square.Height / 2), InstanceModuleSize);
                }

                return;
            }

            string id = _encoding!.DefinitionId;
            string[] lines = [id[..12], id[13..], "Serial " + (instance?.Serial ?? "____")];
            long lineSize = lines.Min(l => HelveticaMetrics.FitFontSize(l, (2L * square.Width) - 80, 40));
            long leading = lineSize * 6 / 5;
            long firstBaseline = (2L * square.Y) + square.Height - leading + (lineSize * HelveticaMetrics.CapHeight / 2000);
            for (int i = 0; i < lines.Length; i++)
            {
                items.Add(new TextRun(SceneLayer.DataBlockContent, textColour, (2L * square.X) + square.Width,
                    firstBaseline + (i * leading), lineSize, lines[i], TextAnchor.Centre));
            }
        }

        /// <summary>A rule of width <paramref name="w"/> laid inside the edge of a rectangle, so its ink stays within the declared box.</summary>
        private static void Outline(List<SceneItem> items, Rgb colour, long x, long y, long width, long height, long w)
        {
            items.Add(new RectFill(SceneLayer.DataBlockFrame, colour, x, y, width, w));
            items.Add(new RectFill(SceneLayer.DataBlockFrame, colour, x, y + height - w, width, w));
            items.Add(new RectFill(SceneLayer.DataBlockFrame, colour, x, y + w, w, height - (2 * w)));
            items.Add(new RectFill(SceneLayer.DataBlockFrame, colour, x + width - w, y + w, w, height - (2 * w)));
        }

        /// <summary>
        /// The human-readable identifier of section 3.8, the recovery path when every code is destroyed, centred 80 dmm
        /// above the bottom edge (docs/SPEC-ERRATA.md C6). A tile also names its place in the assembly.
        /// </summary>
        private void AddIdentifier(List<SceneItem> items, int tile)
        {
            if (d.Codes is null || d.Codes.HumanReadableId == false)
            {
                return;
            }

            items.Add(new TextRun(SceneLayer.Identifier, RoleColour(InkRole.Text), d.Page.Width, (2L * d.Page.Height) - 160, 50, IdentifierText(tile), TextAnchor.Centre));
        }

        /// <summary>The identifier as printed, with a tile's place in its assembly.</summary>
        private string IdentifierText(int tile) => d.Tiling is { } t
            ? _encoding!.DefinitionId + string.Create(CultureInfo.InvariantCulture, $"   tile {tile + 1} of {t.Cols * t.Rows}")
            : _encoding!.DefinitionId;

        /// <summary>The name line of <see cref="NameSeparator"/>'s summary, at the largest size that fits where the analyser never looks, or nothing.</summary>
        private void AddName(List<SceneItem> items, int tile)
        {
            if (string.IsNullOrWhiteSpace(d.Name))
            {
                return;
            }

            string text = d.Name + NameSeparator + IdentifierText(tile);
            var cells = BullCells.Of(d);
            var printed = items.Select(Bounds).ToList();
            for (long size = NameFontSize; size >= NameMinimumFontSize; size -= 2)
            {
                double width = HelveticaMetrics.TextWidth(text, size) / 2.0, top = NameTop, bottom = top + (0.93 * size / 2.0);
                double left = (d.Page.Width - width) / 2.0, right = left + width;
                if (left < NameTop
                    || cells.Any(c => Overlaps((left, top, right, bottom), (c.X - c.HalfWidth, c.Y - c.HalfHeight, c.X + c.HalfWidth, c.Y + c.HalfHeight), NameCellClearance))
                    || printed.Any(b => Overlaps((left, top, right, bottom), b, NameItemClearance)))
                {
                    continue;
                }

                long baseline = (2 * NameTop) + (long)Math.Ceiling(0.72 * size);
                items.Add(new TextRun(SceneLayer.Name, RoleColour(InkRole.Text), d.Page.Width, baseline, size, text, TextAnchor.Centre));
                return;
            }
        }

        private static bool Overlaps((double Left, double Top, double Right, double Bottom) a, (double Left, double Top, double Right, double Bottom) b, double gap) =>
            a.Left < b.Right + gap && b.Left < a.Right + gap && a.Top < b.Bottom + gap && b.Top < a.Bottom + gap;

        /// <summary>An item's extent in dmm; a text run's is its glyph box, cap height 0.72 and descender 0.21 of its size.</summary>
        private static (double Left, double Top, double Right, double Bottom) Bounds(SceneItem item) => item switch
        {
            DiscBand disc => ((disc.CentreX - disc.OuterRadius) / 2.0, (disc.CentreY - disc.OuterRadius) / 2.0, (disc.CentreX + disc.OuterRadius) / 2.0, (disc.CentreY + disc.OuterRadius) / 2.0),
            RectFill rect => (rect.X / 2.0, rect.Y / 2.0, (rect.X + rect.Width) / 2.0, (rect.Y + rect.Height) / 2.0),
            TextRun run => TextBounds(run),
            _ => throw new InvalidOperationException($"No extent for {item.GetType().Name}."),
        };

        private static (double Left, double Top, double Right, double Bottom) TextBounds(TextRun run)
        {
            double width = HelveticaMetrics.TextWidth(run.Text, run.FontSize) / 2.0, x = run.X / 2.0, baseline = run.Baseline / 2.0, size = run.FontSize / 2.0;
            double left = run.Anchor switch { TextAnchor.Centre => x - (width / 2), TextAnchor.Right => x - width, _ => x };
            return (left, baseline - (0.72 * size), left + width, baseline + (0.21 * size));
        }

        private static Rgb ParseRgb(string srgb) => new(
            byte.Parse(srgb.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(srgb.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(srgb.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }
}
