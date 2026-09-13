using System.Globalization;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Gltd.Binary;

/// <summary>
/// The projection of TARGET-SCHEMA.md section 6, in both directions: a GLTD-J document to the body that
/// carries it, refusing whatever the body cannot carry rather than dropping it, and a body back to
/// canonical GLTD-J.
/// </summary>
public static class Projection
{
    /// <summary>Values the body cannot carry, fixed on decode (TARGET-SCHEMA.md section 11, question 11).</summary>
    public const int CodeVersion = 10;

    public const int CodeQuietZone = 16;
    public const int MinorStroke = 2;
    public const int MajorStroke = 3;
    public const int AxisStroke = 4;

    public static (BodyModel? Body, IReadOnlyList<Diagnostic> Diagnostics) ToBody(TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new Encoder(definition).Run();
    }

    public static TargetDefinition FromBody(BodyModel body, string definitionId)
    {
        ArgumentNullException.ThrowIfNull(body);
        int q = WireCodes.QuantumDmm[body.Page.Quantum];

        var size = WireCodes.PageSizeOf(body.Page.PageCode) ?? throw new ArgumentException("Unknown page code.", nameof(body));
        var (width, height) = PageSizes.Standard(size) ?? (
            PageSizes.RollWidth(size) ?? body.Page.Width * q,
            body.Page.Height * q);
        var page = new Page(size, width, height, body.Page.Orientation == 1 ? Orientation.Landscape : Orientation.Portrait);

        // One decoded ink per stored index; an index shared by the fiducials and a disc takes the
        // fiducial role, and the code and text roles do not survive (section 6).
        var inks = new List<Ink>(body.Inks.Count + 1);
        for (int i = 0; i < body.Inks.Count; i++)
        {
            inks.Add(new Ink(InkKey(i), body.Inks[i].ToString(), i == body.Fiducials.InkIndex ? InkRole.Fiducial : InkRole.Artwork));
        }

        bool paperUsed = body.RingSets.Any(set => set.Any(disc => disc.InkIndex == WireCodes.PaperInk))
            || (body.Grids?.Any(g => (g.InkPair & 0xF) == WireCodes.PaperInk || g.InkPair >> 4 == WireCodes.PaperInk) ?? false);
        if (paperUsed)
        {
            inks.Add(new Ink("paper", "#FFFFFF", InkRole.Paper));
        }

        var ringSets = body.RingSets
            .Select((set, i) => new RingSet(SetKey(i), set.Select(disc => new Disc(disc.Diameter * q, InkKey(disc.InkIndex))).ToList()))
            .ToList();

        var bulls = new List<Bull>();
        Cells? cells = null;
        if (body.ExplicitBulls is { } explicitBulls)
        {
            foreach (var b in explicitBulls)
            {
                bulls.Add(new Bull(b.X * q, b.Y * q, SetKey(b.RingSetIndex), null, b.Scoring, null));
            }
        }
        else
        {
            var g = body.Grid!;
            for (int k = 0; k < g.Cols * g.Rows; k++)
            {
                var (row, col) = GridCell(k, g.Order, g.Cols, g.Rows);
                bulls.Add(new Bull((g.OriginX + (col * g.PitchX)) * q, (g.OriginY + (row * g.PitchY)) * q,
                    SetKey(g.RingSetIndex), null, true, null));
            }

            foreach (var s in body.Sighters)
            {
                for (int k = 0; k < s.Count; k++)
                {
                    bulls.Add(new Bull((s.OriginX + (k * s.PitchX)) * q, s.OriginY * q, SetKey(s.RingSetIndex), null, false, null));
                }
            }

            cells = CellsFor(g, q);
        }

        bulls = WithDefaultLabels(bulls);

        var f = body.Fiducials;
        var fiducials = new Fiducials(WireCodes.Schemes[f.Scheme], (FiducialFamily)f.Family, f.MarkerSize * q,
            f.QuietZone * q, InkKey(f.InkIndex), null);

        DataBlock? dataBlock = body.DataBlock is { } d
            ? new DataBlock(d.X * q, d.Y * q, d.Width * q, d.Height * q, (DataBlockLayout)d.Layout, (FieldSet)d.FieldSet,
                d.Reserve * q, InkKey(d.InkIndex), null, null, null)
            : null;

        var c = body.Codes;
        var codes = new Codes(c.Count, CodeVersion, (EcLevel)c.EcLevel, c.ModuleSize * q, CodeQuietZone, CodePlacement.Corners1,
            true, Corners1.Positions(width, height, dataBlock?.Height ?? 0, c.Count, c.ModuleSize * q));

        Tiling? tiling = body.Tiling is { } t
            ? new Tiling(t.Cols, t.Rows, t.SheetWidth * q, t.SheetHeight * q, t.Overlap * q)
            : null;

        var grids = body.Grids?.Select((m, i) =>
        {
            string major = InkKey(m.InkPair >> 4);
            return new MeasurementGrid($"grid{i}", m.CentreX * q, m.CentreY * q, m.Half * q, m.Divisions, m.MajorEvery,
                WireCodes.GridUnitOf(m.Unit)!.Value, m.Distance, m.DistanceUnit == 1 ? DistanceUnit.Metres : DistanceUnit.Yards,
                InkKey(m.InkPair & 0xF), major, major, MinorStroke, MajorStroke, AxisStroke, m.LabelStep, major);
        }).ToList();

        return new TargetDefinition(1, 0, definitionId, definitionId, null, null, null, null, "dmm", page, inks, ringSets,
            bulls, cells, fiducials, codes, null, dataBlock, null, tiling, grids, []);
    }

    /// <summary>Row and column of the k-th grid bull under the grid order byte of section 5.2.</summary>
    internal static (int Row, int Col) GridCell(int k, int order, int cols, int rows) => order switch
    {
        1 => (k % rows, k / rows),
        2 => (k / cols, (k / cols) % 2 == 0 ? k % cols : cols - 1 - (k % cols)),
        _ => (k / cols, k % cols),
    };

    private static string InkKey(int index) => index == WireCodes.PaperInk ? "paper" : $"ink{index}";

    private static string SetKey(int index) => $"set{index}";

    /// <summary>
    /// Cells are emitted where the bull grid defines them as integers: both pitches positive and even, and
    /// the cell origin half a pitch before the first bull not negative. Otherwise there are no cells.
    /// </summary>
    private static Cells? CellsFor(BodyGrid g, int q)
    {
        int pitchX = g.PitchX * q, pitchY = g.PitchY * q;
        int originX = (g.OriginX * q) - (pitchX / 2), originY = (g.OriginY * q) - (pitchY / 2);
        if (pitchX <= 0 || pitchY <= 0 || pitchX % 2 != 0 || pitchY % 2 != 0 || originX < 0 || originY < 0)
        {
            return null;
        }

        return new Cells(CellsMode.Grid, null, null, null, null, new CellGrid(originX, originY, pitchX, pitchY, g.Cols, g.Rows), null);
    }

    /// <summary>Scoring bulls sequential from 1 and sighters S1 upward, both in array order (section 5.2).</summary>
    internal static List<Bull> WithDefaultLabels(IReadOnlyList<Bull> bulls)
    {
        int scoring = 0, sighters = 0;
        return bulls.Select(b => b with { Label = DefaultLabel(b.Scoring, ref scoring, ref sighters) }).ToList();
    }

    private static string DefaultLabel(bool scoring, ref int scoringCount, ref int sighterCount) =>
        scoring
            ? (++scoringCount).ToString(CultureInfo.InvariantCulture)
            : "S" + (++sighterCount).ToString(CultureInfo.InvariantCulture);

    private sealed record ParametricLayout(BodyGrid Grid, List<BodySighterRow> Sighters);

    private sealed class Encoder(TargetDefinition d)
    {
        private readonly List<Diagnostic> _diagnostics = [];
        private readonly Dictionary<string, byte> _inkIndex = new(StringComparer.Ordinal);
        private readonly List<string> _colours = [];
        private readonly Dictionary<string, byte> _ringIndex = new(StringComparer.Ordinal);

        public (BodyModel?, IReadOnlyList<Diagnostic>) Run()
        {
            MapInks();
            MapRingSets();
            if (HasErrors)
            {
                return (null, _diagnostics);
            }

            var layout = InferParametric();
            CheckLabels();
            CheckCells(layout);
            if (layout is null)
            {
                CheckExplicitBulls();
            }

            if (d.Fiducials is null)
            {
                Refuse("encode.missingBlock", "/fiducials", "Every GLTD-B body carries a fiducial block (section 5.2).");
            }

            if (d.Codes is null)
            {
                Refuse("encode.missingBlock", "/codes", "Every GLTD-B body carries a code block (section 5.2).");
            }

            var fiducialScheme = CheckFiducials();
            CheckCodes();
            CheckDataBlock();
            var gridInks = CheckGrids();

            int quantumCode = layout is null ? ChooseExplicitQuantum() : 0;
            if (HasErrors)
            {
                return (null, _diagnostics);
            }

            int q = WireCodes.QuantumDmm[quantumCode];
            ushort Q(int value) => checked((ushort)(value / q));

            var page = d.Page;
            var size = page.Size;
            if ((PageSizes.Standard(size) is { } standard && (standard.Width != page.Width || standard.Height != page.Height))
                || (PageSizes.RollWidth(size) is { } rollWidth && rollWidth != page.Width))
            {
                size = PageSize.Custom;
            }

            byte pageCode = WireCodes.PageCode(size);
            var bodyPage = new BodyPage(pageCode,
                pageCode == 0 ? Q(page.Width) : (ushort)0,
                pageCode == 0 || pageCode >= 7 ? Q(page.Height) : (ushort)0,
                page.Orientation == Orientation.Landscape ? (byte)1 : (byte)0,
                (byte)quantumCode);

            var inks = _colours.Select(ParseRgb).ToList();
            var ringSets = d.RingSets
                .Select(set => (IReadOnlyList<BodyDisc>)set.Discs.Select(disc => new BodyDisc(Q(disc.Diameter), _inkIndex[disc.Ink])).ToList())
                .ToList();

            var bulls = layout is null
                ? d.Bulls.Select(b => new BodyBull(Q(b.X), Q(b.Y), _ringIndex[b.RingSet], b.Scoring)).ToList()
                : null;

            var f = d.Fiducials!;
            var fiducials = new BodyFiducials(fiducialScheme, WireCodes.FamilyCode(f.Family), Q(f.MarkerSize),
                checked((byte)(f.QuietZone / q)), _inkIndex[f.Ink]);

            var c = d.Codes!;
            var codes = new BodyCodes((byte)c.Count, (byte)c.EcLevel, checked((byte)(c.ModuleSize / q)), 0);

            BodyDataBlock? dataBlock = d.DataBlock is { } db
                ? new BodyDataBlock(Q(db.X), Q(db.Y), Q(db.Width), Q(db.Height), (byte)db.Layout, (byte)db.FieldSet,
                    Q(db.Reserve), _inkIndex[db.Ink])
                : null;

            BodyTiling? tiling = d.Tiling is { } t
                ? new BodyTiling((byte)t.Cols, (byte)t.Rows, Q(t.SheetWidth), Q(t.SheetHeight), Q(t.Overlap))
                : null;

            var grids = d.Grids?.Select((m, i) => new BodyMeasurementGrid(Q(m.CentreX), Q(m.CentreY), Q(m.Half),
                (byte)m.Divisions, (byte)m.MajorEvery, WireCodes.GridUnitCode(m.Unit), (ushort)m.Distance,
                m.DistanceUnit == DistanceUnit.Metres ? (byte)1 : (byte)0, gridInks[i], WireCodes.StandardGridStyle,
                (byte)(m.LabelStep ?? 0))).ToList();

            var model = new BodyModel(bodyPage, inks, ringSets, layout?.Grid, layout?.Sighters ?? [], bulls,
                fiducials, codes, dataBlock, tiling, grids);
            return (model, _diagnostics);
        }

        private bool HasErrors => _diagnostics.Exists(x => x.Severity == Severity.Error);

        private void Refuse(string code, string path, string message) => _diagnostics.Add(Diagnostic.Error(code, path, message));

        private void MapInks()
        {
            for (int i = 0; i < d.Inks.Count; i++)
            {
                var ink = d.Inks[i];
                byte index;
                if (ink.Role == InkRole.Paper)
                {
                    index = WireCodes.PaperInk;
                }
                else
                {
                    int existing = _colours.IndexOf(ink.Srgb);
                    if (existing < 0)
                    {
                        _colours.Add(ink.Srgb);
                        existing = _colours.Count - 1;
                    }

                    index = (byte)existing;
                }

                if (!_inkIndex.TryAdd(ink.Key, index))
                {
                    Refuse("encode.duplicateKey", $"/inks/{i}/key", $"Ink key \"{ink.Key}\" is used twice.");
                }
            }

            if (_colours.Count > 15)
            {
                Refuse("encode.tooManyColours", "/inks",
                    $"{_colours.Count} distinct colours exceeds the 15 an ink index can address (section 3.3).");
            }
        }

        private void MapRingSets()
        {
            for (int i = 0; i < d.RingSets.Count; i++)
            {
                var set = d.RingSets[i];
                if (!_ringIndex.TryAdd(set.Key, (byte)i))
                {
                    Refuse("encode.duplicateKey", $"/ringSets/{i}/key", $"Ring set key \"{set.Key}\" is used twice.");
                }

                for (int k = 0; k < set.Discs.Count; k++)
                {
                    ResolveInk(set.Discs[k].Ink, $"/ringSets/{i}/discs/{k}/ink", allowPaper: true);
                }
            }

            for (int i = 0; i < d.Bulls.Count; i++)
            {
                if (!_ringIndex.ContainsKey(d.Bulls[i].RingSet))
                {
                    Refuse("encode.unresolvedRingSet", $"/bulls/{i}/ringSet", $"No ring set has key \"{d.Bulls[i].RingSet}\".");
                }
            }
        }

        private byte ResolveInk(string key, string path, bool allowPaper)
        {
            if (!_inkIndex.TryGetValue(key, out byte index))
            {
                Refuse("encode.unresolvedInk", path, $"No ink has key \"{key}\".");
                return 0;
            }

            if (index == WireCodes.PaperInk && !allowPaper)
            {
                Refuse("encode.paperInk", path, "The paper knockout lays no ink and cannot be used here.");
            }

            return index;
        }

        /// <summary>
        /// Recognises a parametric layout: the scoring bulls first, forming a complete grid in one of the
        /// three orders, then sighter rows. The lowest matching order code is canonical.
        /// </summary>
        private ParametricLayout? InferParametric()
        {
            var bulls = d.Bulls;
            int n = 0;
            while (n < bulls.Count && bulls[n].Scoring)
            {
                n++;
            }

            if (n == 0 || bulls.Skip(n).Any(b => b.Scoring) || bulls.Take(n).Any(b => b.RingSet != bulls[0].RingSet))
            {
                return null;
            }

            var xs = bulls.Take(n).Select(b => b.X).Distinct().Order().ToList();
            var ys = bulls.Take(n).Select(b => b.Y).Distinct().Order().ToList();
            int cols = xs.Count, rows = ys.Count;
            if (cols * rows != n || cols > 255 || rows > 255 || Step(xs) is not { } pitchX || Step(ys) is not { } pitchY)
            {
                return null;
            }

            var declared = d.Cells?.Grid;
            if (cols == 1)
            {
                pitchX = declared?.PitchX ?? (rows > 1 ? pitchY : 0);
            }

            if (rows == 1)
            {
                pitchY = declared?.PitchY ?? (cols > 1 ? pitchX : 0);
            }

            int order = -1;
            for (int o = 0; o <= 2 && order < 0; o++)
            {
                bool matches = true;
                for (int k = 0; k < n && matches; k++)
                {
                    var (row, col) = GridCell(k, o, cols, rows);
                    matches = bulls[k].X == xs[0] + (col * pitchX) && bulls[k].Y == ys[0] + (row * pitchY);
                }

                if (matches)
                {
                    order = o;
                }
            }

            if (order < 0)
            {
                return null;
            }

            var sighters = new List<BodySighterRow>();
            for (int i = n; i < bulls.Count;)
            {
                int j = i;
                while (j < bulls.Count && bulls[j].Y == bulls[i].Y && bulls[j].RingSet == bulls[i].RingSet)
                {
                    j++;
                }

                int count = j - i;
                int pitch = count == 1 ? pitchX : bulls[i + 1].X - bulls[i].X;
                if (count > 255 || pitch < 0 || pitch > ushort.MaxValue)
                {
                    return null;
                }

                for (int k = 0; k < count; k++)
                {
                    if (bulls[i + k].X != bulls[i].X + (k * pitch))
                    {
                        return null;
                    }
                }

                sighters.Add(new BodySighterRow((byte)count, (ushort)bulls[i].X, (ushort)bulls[i].Y, (ushort)pitch, _ringIndex[bulls[i].RingSet]));
                i = j;
            }

            if (sighters.Count > 4 || pitchX > ushort.MaxValue || pitchY > ushort.MaxValue)
            {
                return null;
            }

            var grid = new BodyGrid((byte)cols, (byte)rows, (ushort)xs[0], (ushort)ys[0], (ushort)pitchX, (ushort)pitchY,
                _ringIndex[bulls[0].RingSet], (byte)order);
            return new ParametricLayout(grid, sighters);
        }

        /// <summary>The common step of sorted distinct values: 0 for one value, null when uneven.</summary>
        private static int? Step(List<int> values)
        {
            if (values.Count == 1)
            {
                return 0;
            }

            int step = values[1] - values[0];
            for (int i = 2; i < values.Count; i++)
            {
                if (values[i] - values[i - 1] != step)
                {
                    return null;
                }
            }

            return step;
        }

        private void CheckLabels()
        {
            var expected = WithDefaultLabels(d.Bulls);
            for (int i = 0; i < d.Bulls.Count; i++)
            {
                var bull = d.Bulls[i];
                if (bull.Label is not null && bull.Label != expected[i].Label)
                {
                    Refuse("encode.labelBlock", $"/bulls/{i}/label",
                        $"Label \"{bull.Label}\" differs from the default \"{expected[i].Label}\", which needs the label block; " +
                        "it has no byte layout yet (TARGET-SCHEMA.md section 11, question 10).");
                }

                if (bull.LabelOffset is not null)
                {
                    Refuse("encode.labelBlock", $"/bulls/{i}/labelOffset",
                        "A label offset needs the label block, which has no byte layout yet (question 10).");
                }
            }
        }

        private void CheckCells(ParametricLayout? layout)
        {
            if (d.Cells is not { } cells)
            {
                return;
            }

            if (cells.Drawn == true)
            {
                Refuse("encode.cellBlock", "/cells/drawn", "Drawn cells need the cell block, which has no byte layout yet (question 10).");
            }

            if (cells.Polygons is not null)
            {
                Refuse("encode.cellBlock", "/cells/polygons", "Explicit cell polygons need the cell block, which has no byte layout yet (question 10).");
            }

            if (cells.Mode == CellsMode.None && d.Bulls.Count == 1)
            {
                return;
            }

            if (cells.Mode != CellsMode.Grid || layout is null)
            {
                Refuse("encode.cellBlock", "/cells/mode",
                    $"Cells in mode \"{GltdNames.CellsMode.NameOf(cells.Mode)}\" on this layout need the cell block, which has no byte layout yet (question 10).");
                return;
            }

            var g = layout.Grid;
            if (cells.Grid is not { } cg)
            {
                Refuse("encode.cellBlock", "/cells", "Grid cells without cells.grid need the cell block (question 10).");
            }
            else if (cg.Cols != g.Cols || cg.Rows != g.Rows || cg.PitchX != g.PitchX || cg.PitchY != g.PitchY
                || (2 * cg.OriginX) + cg.PitchX != 2 * g.OriginX || (2 * cg.OriginY) + cg.PitchY != 2 * g.OriginY)
            {
                Refuse("encode.cellsInconsistent", "/cells/grid",
                    $"cells.grid does not describe the bull grid, which is {g.Cols} by {g.Rows} at pitch {g.PitchX} by {g.PitchY} " +
                    $"from the bull at ({g.OriginX}, {g.OriginY}); the body carries only the bull grid.");
            }
        }

        private void CheckExplicitBulls()
        {
            for (int i = 0; i < d.Bulls.Count; i++)
            {
                if (_ringIndex.TryGetValue(d.Bulls[i].RingSet, out byte index) && index > 7)
                {
                    Refuse("encode.explicitRingSet", $"/bulls/{i}/ringSet",
                        $"An explicit layout packs a 3-bit ring set index (section 5.5), so ring set {index} cannot be used.");
                }
            }
        }

        private byte CheckFiducials()
        {
            if (d.Fiducials is not { } f)
            {
                return 0;
            }

            int scheme = Array.IndexOf(WireCodes.Schemes, f.Scheme);
            if (scheme < 0)
            {
                Refuse("encode.unknownScheme", "/fiducials/scheme", $"\"{f.Scheme}\" is not a fiducial scheme this encoder knows.");
            }
            else if (scheme == 0)
            {
                Refuse("encode.explicitMarkers", "/fiducials/scheme",
                    "Explicit marker placement has no byte layout: the fiducial block of section 5.2 carries a scheme byte but no marker list.");
            }

            ResolveInk(f.Ink, "/fiducials/ink", allowPaper: false);
            return (byte)Math.Max(scheme, 0);
        }

        private void CheckCodes()
        {
            if (d.Codes is not { } c)
            {
                return;
            }

            if (c.Placement == CodePlacement.Explicit)
            {
                Refuse("encode.explicitCodes", "/codes/placement",
                    "Explicit code placement has no byte layout (TARGET-SCHEMA.md section 11, question 13), so the positions could not be carried.");
            }

            if (!Corners1.Supports(c.Count))
            {
                Refuse("encode.codeCount", "/codes/count", $"corners-1 places 0, 2 or 4 codes, not {c.Count}.");
            }

            if (c.Version is { } version && version != CodeVersion)
            {
                Refuse("encode.notCarried", "/codes/version",
                    $"The body carries no code version and a decoder assumes {CodeVersion} (question 11), so {version} would not survive.");
            }

            if (c.QuietZone is { } quietZone && quietZone != CodeQuietZone)
            {
                Refuse("encode.notCarried", "/codes/quietZone",
                    $"The body carries no code quiet zone and a decoder assumes {CodeQuietZone} (question 11), so {quietZone} would not survive.");
            }

            if (c.HumanReadableId == false)
            {
                Refuse("encode.notCarried", "/codes/humanReadableId",
                    "The body carries no humanReadableId and a decoder assumes true (question 11).");
            }
        }

        private void CheckDataBlock()
        {
            if (d.DataBlock is not { } b)
            {
                return;
            }

            if (b.Layout == DataBlockLayout.Explicit || b.FieldSet == FieldSet.Explicit || b.Fields is not null)
            {
                Refuse("encode.explicitDataBlock", "/dataBlock",
                    "Explicit data block layouts and field sets have no complete byte layout: section 5.2 gives neither the " +
                    "width of the key-list length prefix nor anywhere to carry field labels.");
            }

            ResolveInk(b.Ink, "/dataBlock/ink", allowPaper: false);
        }

        private List<byte> CheckGrids()
        {
            var pairs = new List<byte>();
            for (int i = 0; i < (d.Grids?.Count ?? 0); i++)
            {
                var g = d.Grids![i];
                string path = $"/grids/{i}";
                byte minor = g.MinorInk is null ? (byte)0 : ResolveInk(g.MinorInk, $"{path}/minorInk", allowPaper: true);
                byte major = g.MajorInk is null ? (byte)0 : ResolveInk(g.MajorInk, $"{path}/majorInk", allowPaper: true);
                if ((g.MinorInk is null || g.MajorInk is null) && _colours.Count == 0)
                {
                    Refuse("encode.unresolvedInk", path, "A grid without explicit inks takes ink index 0, and there is none.");
                }

                if (g.AxisInk is not null && ResolveInk(g.AxisInk, $"{path}/axisInk", allowPaper: true) != major)
                {
                    Refuse("encode.notCarried", $"{path}/axisInk", "Grid style 1 draws the axes in the major ink (question 11).");
                }

                if (g.LabelInk is not null && ResolveInk(g.LabelInk, $"{path}/labelInk", allowPaper: true) != major)
                {
                    Refuse("encode.notCarried", $"{path}/labelInk", "Grid style 1 draws the labels in the major ink (question 11).");
                }

                if (g.MinorStroke is not (null or MinorStroke) || g.MajorStroke is not (null or MajorStroke) || g.AxisStroke is not (null or AxisStroke))
                {
                    Refuse("encode.notCarried", path,
                        $"Grid style 1 is strokes of {MinorStroke}, {MajorStroke} and {AxisStroke} dmm (question 11); other weights would not survive.");
                }

                pairs.Add((byte)(minor | (major << 4)));
            }

            return pairs;
        }

        /// <summary>The smallest quantum that divides every length the body stores in quanta and fits each field.</summary>
        private int ChooseExplicitQuantum()
        {
            var lengths = new List<int>();
            var bytes = new List<int>();
            if (d.Page.Size == PageSize.Custom || PageSizes.Standard(d.Page.Size) is null)
            {
                lengths.Add(d.Page.Width);
                lengths.Add(d.Page.Height);
            }

            lengths.AddRange(d.RingSets.SelectMany(s => s.Discs).Select(disc => disc.Diameter));
            if (d.Fiducials is { } f)
            {
                lengths.Add(f.MarkerSize);
                bytes.Add(f.QuietZone);
            }

            if (d.Codes is { } c)
            {
                bytes.Add(c.ModuleSize);
            }

            if (d.DataBlock is { } b)
            {
                lengths.AddRange([b.X, b.Y, b.Width, b.Height, b.Reserve]);
            }

            if (d.Tiling is { } t)
            {
                lengths.AddRange([t.SheetWidth, t.SheetHeight, t.Overlap]);
            }

            foreach (var g in d.Grids ?? [])
            {
                lengths.AddRange([g.CentreX, g.CentreY, g.Half]);
            }

            var coordinates = d.Bulls.SelectMany(b => new[] { b.X, b.Y }).ToList();
            for (int code = 0; code < WireCodes.QuantumDmm.Length; code++)
            {
                int q = WireCodes.QuantumDmm[code];
                if (lengths.Concat(bytes).Concat(coordinates).All(v => v % q == 0)
                    && coordinates.All(v => v / q <= 0xFFF)
                    && bytes.All(v => v / q <= byte.MaxValue))
                {
                    return code;
                }
            }

            Refuse("encode.explicitQuantum", "/bulls",
                "No quantum of 0.1, 0.2, 0.5 or 1.0 mm both divides every length and keeps every bull within 4095 quanta (section 5.4).");
            return 0;
        }

        private static Rgb ParseRgb(string srgb) => new(
            byte.Parse(srgb.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(srgb.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(srgb.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }
}
