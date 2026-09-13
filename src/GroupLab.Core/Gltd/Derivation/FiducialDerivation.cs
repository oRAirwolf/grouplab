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

    /// <summary>The bounding box of each bull's outermost disc.</summary>
    public static IReadOnlyList<Box2> RingBoxes(TargetDefinition d)
    {
        var outer = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var set in d.RingSets)
        {
            outer.TryAdd(set.Key, set.Discs.Count == 0 ? 0 : set.Discs.Max(disc => disc.Diameter));
        }

        return d.Bulls.Select(b => Box2.Square(b.X, b.Y, outer.GetValueOrDefault(b.RingSet))).ToList();
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
                    || rings.Any(r => box.Overlaps(r, RingGap))
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

        // Doubled coordinates: the marker columns sit half a footprint plus a clearance outside the field.
        long cx2 = 2L * grid.CentreX, cy2 = 2L * grid.CentreY;
        long left = (2L * (grid.CentreX - grid.Half - Clearance)) - footprint;
        long right = (2L * (grid.CentreX + grid.Half + Clearance)) + footprint;
        long top = (2L * (grid.CentreY - grid.Half - Clearance)) - footprint;
        long bottom = (2L * (grid.CentreY + grid.Half + Clearance)) + footprint;

        var major = Enumerable.Range(0, (grid.Divisions / grid.MajorEvery) + 1).Select(k => 2L * offsets[k * grid.MajorEvery]).ToList();
        var candidates = new List<(long X2, long Y2)>();
        foreach (long o in major)
        {
            candidates.AddRange([(left, cy2 - o), (right, cy2 - o), (left, cy2 + o), (right, cy2 + o)]);
        }

        foreach (long o in major)
        {
            candidates.AddRange([(cx2 - o, top), (cx2 - o, bottom), (cx2 + o, top), (cx2 + o, bottom)]);
        }

        candidates.AddRange([(left, top), (right, top), (left, bottom), (right, bottom)]);

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
