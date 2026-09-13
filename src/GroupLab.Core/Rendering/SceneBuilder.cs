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
/// mis-scaled print of DESIGN.md section 21's Phase 0 gate, and is 1 for every real print.
/// </summary>
public sealed record RenderOptions(
    DataBlockMode Mode = DataBlockMode.Blank,
    Instance? Instance = null,
    int? TileIndex = null,
    bool AllowInvalid = false,
    double Scale = 1.0);

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
            return new Scene(2L * d.Page.Width, 2L * d.Page.Height, tile, items);
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
                        long inner = j + 1 < set.Discs.Count ? set.Discs[j + 1].Diameter : 0;
                        items.Add(new DiscBand(SceneLayer.Bulls, colour, 2L * bull.X, 2L * bull.Y, set.Discs[k].Diameter, inner));
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

            string text = _encoding!.DefinitionId;
            if (d.Tiling is { } t)
            {
                text += string.Create(CultureInfo.InvariantCulture, $"   tile {tile + 1} of {t.Cols * t.Rows}");
            }

            items.Add(new TextRun(SceneLayer.Identifier, RoleColour(InkRole.Text), d.Page.Width, (2L * d.Page.Height) - 160, 50, text, TextAnchor.Centre));
        }

        private static Rgb ParseRgb(string srgb) => new(
            byte.Parse(srgb.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(srgb.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(srgb.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }
}
