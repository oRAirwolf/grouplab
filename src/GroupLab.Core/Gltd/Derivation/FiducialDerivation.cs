using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Gltd.Derivation;

/// <summary>Marker centres a derivation rule produces for one sheet, in raster order (y, then x).</summary>
public sealed record DerivedMarkers(IReadOnlyList<PointDmm> Positions, int Candidates, int Dropped);

/// <summary>Derived markers, or why the rule cannot be applied to this definition (conformance test 16).</summary>
public sealed record DerivationResult(DerivedMarkers? Markers, string? Error);

/// <summary>
/// The fiducial derivation rules of TARGET-SCHEMA.md section 3.7, ported exactly from the validated solvers:
/// <c>grid-boundary-1</c> and <c>grid-boundary-half-1</c> from <c>tools/layout/layout.py</c>, and
/// <c>field-ring-1</c> from <c>tools/layout/zero.py</c>. The drop test is part of each rule.
/// </summary>
public static class FiducialDerivation
{
    /// <summary>A candidate footprint inside this distance of the page edge is dropped.</summary>
    public const int SafeEdge = 60;

    public const int CodeGap = 20;
    public const int RingGap = 10;
    public const int DataBlockGap = 10;
    public const int MarkerSpacing = 20;
    public const int Clearance = 30;

    public static DerivationResult Derive(TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.Fiducials is not { } f)
        {
            return new DerivationResult(null, "The definition has no fiducials block.");
        }

        return f.Scheme switch
        {
            "grid-boundary-1" => GridBoundary(definition, f, halfPitch: false),
            "grid-boundary-half-1" => GridBoundary(definition, f, halfPitch: true),
            "field-ring-1" => FieldRing(definition, f),
            _ => new DerivationResult(null, $"\"{f.Scheme}\" is not a derivation rule this implementation knows."),
        };
    }

    /// <summary>
    /// The definition with <c>fiducials.markers</c> set to what its rule derives for tile 0, with identifiers,
    /// as section 3.7 asks writers to emit. Returned unchanged when the rule cannot be derived.
    /// </summary>
    public static TargetDefinition WithDerivedMarkers(TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.Fiducials is not { } f)
        {
            return definition;
        }

        int size = MarkerIds.DictionarySize(f.Family);
        if (size <= 0 || Derive(definition).Markers is not { } derived)
        {
            return definition;
        }

        var assignment = MarkerIds.Assign(derived.Positions, definition.Tiling, 0, size);
        return definition with { Fiducials = f with { Markers = assignment.Markers } };
    }

    /// <summary>The outer edge of a marker plus its quiet zone on both sides.</summary>
    public static int Footprint(Fiducials f) => f.MarkerSize + (2 * f.QuietZone);

    /// <summary>Code footprints placed by <c>corners-1</c>, which the drop tests avoid.</summary>
    public static IReadOnlyList<Box2> CodeBoxes(TargetDefinition d)
    {
        if (d.Codes is not { Placement: CodePlacement.Corners1 } codes || !Corners1.Supports(codes.Count))
        {
            return [];
        }

        int side = Corners1.FootprintModules * codes.ModuleSize;
        return Corners1.Positions(d.Page.Width, d.Page.Height, d.DataBlock?.Height ?? 0, codes.Count, codes.ModuleSize)
            .Select(p => Box2.Square(p.X, p.Y, side))
            .ToList();
    }

    /// <summary>The bounding box of each bull's outermost disc, with that disc's outline about the bull.</summary>
    public static IReadOnlyList<RingBox> RingBoxes(TargetDefinition d)
    {
        var outer = new Dictionary<string, Disc?>(StringComparer.Ordinal);
        foreach (var set in d.RingSets)
        {
            outer.TryAdd(set.Key, set.Discs.Count == 0 ? null : set.Discs.MaxBy(disc => disc.Diameter));
        }

        return d.Bulls.Select(b => outer.GetValueOrDefault(b.RingSet) is { } disc
            ? new RingBox(Box2.Square(b.X, b.Y, disc.Diameter), 2L * b.X, 2L * b.Y, Outline.Of(disc))
            : new RingBox(Box2.Square(b.X, b.Y, 0), 2L * b.X, 2L * b.Y, new Outline(0))).ToList();
    }

    /// <summary>
    /// A bull's outermost disc for the drop test: its bounding box in doubled coordinates, and its outline. Entry 243 section 4: the box of a
    /// circle is what <c>layout.py</c> tests against, and every circle is still tested so. A square is tested against itself, because the box
    /// round the circle through a diamond's points covers the paper on its diagonals, where a marker has more than 100 dmm to spare.
    /// </summary>
    public readonly record struct RingBox(Box2 Box, long CentreX2, long CentreY2, Outline Edge)
    {
        public bool Clashes(Box2 other, long gap)
        {
            if (Edge.IsCircle)
            {
                return other.Overlaps(Box, gap);
            }

            // Separating axes, in doubled coordinates: the page's two and the square's two. The shapes clash unless one axis parts them
            // by at least the gap.
            double turn = Edge.Rotation * Math.PI / 180, half = 2 * Edge.Inscribed;
            double bx = (other.X0 + other.X1) / 2.0, by = (other.Y0 + other.Y1) / 2.0, hw = (other.X1 - other.X0) / 2.0, hh = (other.Y1 - other.Y0) / 2.0;
            (double X, double Y)[] axes = [(1, 0), (0, 1), (Math.Cos(turn), Math.Sin(turn)), (-Math.Sin(turn), Math.Cos(turn))];
            foreach (var (ax, ay) in axes)
            {
                double apart = Math.Abs(((bx - CentreX2) * ax) + ((by - CentreY2) * ay));
                double square = half * (Math.Abs((ax * Math.Cos(turn)) + (ay * Math.Sin(turn))) + Math.Abs((-ax * Math.Sin(turn)) + (ay * Math.Cos(turn))));
                double box = (hw * Math.Abs(ax)) + (hh * Math.Abs(ay));
                if (apart >= square + box + (2 * gap))
                {
                    return false;
                }
            }

            return true;
        }
    }

    private static DerivationResult GridBoundary(TargetDefinition d, Fiducials f, bool halfPitch)
    {
        if (BullLayout.Recognise(d) is not { } layout)
        {
            return new DerivationResult(null, $"{f.Scheme} derives its lattice from a parametric bull grid, and these bulls do not form one.");
        }

        var g = layout.Grid;
        int multiple = halfPitch ? 4 : 2;
        if (g.PitchX <= 0 || g.PitchY <= 0 || g.PitchX % multiple != 0 || g.PitchY % multiple != 0)
        {
            return new DerivationResult(null,
                $"{f.Scheme} needs a positive pitch divisible by {multiple} dmm, not {g.PitchX} by {g.PitchY} (conformance test 19).");
        }

        int stepX = halfPitch ? g.PitchX / 2 : g.PitchX;
        int stepY = halfPitch ? g.PitchY / 2 : g.PitchY;
        var xs = Enumerable.Range(0, (g.Cols * (g.PitchX / stepX)) + 1).Select(i => g.OriginX - (g.PitchX / 2) + (i * stepX)).ToList();
        var rawYs = Enumerable.Range(0, (g.Rows * (g.PitchY / stepY)) + 1).Select(j => g.OriginY - (g.PitchY / 2) + (j * stepY)).ToList();
        foreach (var row in layout.Sighters)
        {
            rawYs.Add(row.OriginY - (g.PitchY / 2));
            rawYs.Add(row.OriginY + (g.PitchY / 2));
        }

        int footprint = Footprint(f);
        var ys = new List<int>();
        foreach (int y in rawYs.Distinct().Order())
        {
            if (ys.Count == 0 || y - ys[^1] >= footprint + MarkerSpacing)
            {
                ys.Add(y);
            }
        }

        var codes = CodeBoxes(d);
        var rings = RingBoxes(d);
        Box2? dataBlock = d.DataBlock is { } b ? Box2.FromRect(b.X, b.Y, b.Width, b.Height) : null;
        var kept = new List<PointDmm>();
        int dropped = 0;
        foreach (int x in xs)
        {
            foreach (int y in ys)
            {
                var box = Box2.Square(x, y, footprint);
                if (box.X0 < 2 * SafeEdge || box.Y0 < 2 * SafeEdge
                    || box.X1 > 2L * (d.Page.Width - SafeEdge) || box.Y1 > 2L * (d.Page.Height - SafeEdge)
                    || codes.Any(c => box.Overlaps(c, CodeGap))
                    || rings.Any(r => r.Clashes(box, RingGap))
                    || (dataBlock is { } block && box.Overlaps(block, DataBlockGap)))
                {
                    dropped++;
                    continue;
                }

                kept.Add(new PointDmm(x, y));
            }
        }

        return new DerivationResult(new DerivedMarkers(Raster(kept), xs.Count * ys.Count, dropped), null);
    }

    private static DerivationResult FieldRing(TargetDefinition d, Fiducials f)
    {
        if (d.Grids is not { Count: > 0 } grids)
        {
            return new DerivationResult(null, "field-ring-1 places markers around a measurement grid, and the definition declares none.");
        }

        var grid = grids[0];
        if (grid.MajorEvery <= 0 || grid.Divisions <= 0)
        {
            return new DerivationResult(null, "field-ring-1 needs positive divisions and majorEvery.");
        }

        var offsets = MeasurementGridLines.Offsets(grid.Half, grid.Divisions);
        int footprint = Footprint(f);

        // Doubled coordinates: the marker columns sit half a footprint plus a clearance outside the field. A style 2 grid's field has
        // its own half-extents (question 59), and only its major lines inside the field carry markers.
        long cx2 = 2L * grid.CentreX, cy2 = 2L * grid.CentreY;
        long left = (2L * (grid.CentreX - grid.HalfX - Clearance)) - footprint;
        long right = (2L * (grid.CentreX + grid.HalfX + Clearance)) + footprint;
        long top = (2L * (grid.CentreY - grid.HalfY - Clearance)) - footprint;
        long bottom = (2L * (grid.CentreY + grid.HalfY + Clearance)) + footprint;

        var candidates = new List<(long X2, long Y2)>();
        if (grid.StyleOrDefault == GridStyle4.Style)
        {
            // Entries 272 and 273: the printer check page's markers stand where its rulers, crosshairs, card and words leave room.
            candidates.AddRange(GridStyle4.MarkerSpots(d.Page).Select(s => (2L * s.X, 2L * s.Y)));
        }
        else if (grid.StyleOrDefault == GridStyle3.Style)
        {
            // Entry 251: a style 3 grid's numbers take the rows above and below it and the corners, so its markers stand in the two side
            // columns only, between the numbers, at the heights of the ticks halfway between the lines.
            foreach (int tick in GridStyle3.Ticks(grid, grid.HalfY))
            {
                candidates.AddRange([(left, cy2 + (2L * tick)), (right, cy2 + (2L * tick))]);
            }
        }
        else
        {
            // A style 2 line too near the field's edge gives way to the corner marker, which would otherwise sit a footprint beside it.
            int corner = grid.StyleOrDefault == GridStyle2.Style ? footprint + MarkerSpacing : 0;
            var major = Enumerable.Range(0, (grid.Divisions / grid.MajorEvery) + 1).Select(k => offsets[k * grid.MajorEvery]).ToList();
            foreach (int o in major.Where(o => o <= grid.HalfY - corner))
            {
                candidates.AddRange([(left, cy2 - (2L * o)), (right, cy2 - (2L * o)), (left, cy2 + (2L * o)), (right, cy2 + (2L * o))]);
            }

            foreach (int o in major.Where(o => o <= grid.HalfX - corner))
            {
                candidates.AddRange([(cx2 - (2L * o), top), (cx2 - (2L * o), bottom), (cx2 + (2L * o), top), (cx2 + (2L * o), bottom)]);
            }

            candidates.AddRange([(left, top), (right, top), (left, bottom), (right, bottom)]);
        }

        var codes = CodeBoxes(d);
        int dataBlockHeight = d.DataBlock?.Height ?? 0;
        Box2? dataBlock = d.DataBlock is { } b ? Box2.FromRect(b.X, b.Y, b.Width, b.Height) : null;
        var seen = new HashSet<PointDmm>();
        var kept = new List<PointDmm>();
        int unique = 0, dropped = 0;
        foreach (var (x2, y2) in candidates)
        {
            var key = new PointDmm(DerivedRounding.Halve(x2), DerivedRounding.Halve(y2));
            if (!seen.Add(key))
            {
                continue;
            }

            unique++;
            var box = new Box2(x2 - footprint, y2 - footprint, x2 + footprint, y2 + footprint);
            if (box.X0 < 2 * SafeEdge || box.Y0 < 2 * SafeEdge
                || box.X1 > 2L * (d.Page.Width - SafeEdge) || box.Y1 > 2L * (d.Page.Height - SafeEdge - dataBlockHeight)
                || codes.Any(c => box.Overlaps(c, CodeGap))
                || (dataBlock is { } block && box.Overlaps(block, DataBlockGap)))
            {
                dropped++;
                continue;
            }

            kept.Add(key);
        }

        return new DerivationResult(new DerivedMarkers(Raster(kept), unique, dropped), null);
    }

    private static List<PointDmm> Raster(IEnumerable<PointDmm> points) => [.. points.OrderBy(p => p.Y).ThenBy(p => p.X)];
}
