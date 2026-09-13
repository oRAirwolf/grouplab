namespace GroupLab.Core.Gltd.Binary;

/// <summary>
/// A GLTD-B body exactly as TARGET-SCHEMA.md section 5.2 lays it out, block by block, with every length still in
/// quanta. <see cref="ExplicitBulls"/> is set for explicit mode (section 5.5) and replaces the grid and sighters.
/// </summary>
public sealed record BodyModel(
    BodyPage Page,
    IReadOnlyList<Rgb> Inks,
    IReadOnlyList<IReadOnlyList<BodyDisc>> RingSets,
    BodyGrid? Grid,
    IReadOnlyList<BodySighterRow> Sighters,
    IReadOnlyList<BodyBull>? ExplicitBulls,
    BodyFiducials Fiducials,
    BodyCodes Codes,
    BodyDataBlock? DataBlock,
    BodyTiling? Tiling,
    IReadOnlyList<BodyMeasurementGrid>? Grids);

/// <summary>One entry of the ink block, section 5.2: a distinct sRGB colour (section 6).</summary>
public readonly record struct Rgb(byte R, byte G, byte B)
{
    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
}

/// <summary>
/// The page block, section 5.2. <see cref="Width"/> is carried only for a custom page, <see cref="Height"/> for custom
/// and roll pages; <see cref="Quantum"/> selects the length unit of section 5.4.
/// </summary>
public sealed record BodyPage(byte PageCode, ushort Width, ushort Height, byte Orientation, byte Quantum);

/// <summary>A disc of the ring set block, section 5.2. <see cref="InkIndex"/> 15 is the paper knockout (section 3.3).</summary>
public sealed record BodyDisc(ushort Diameter, byte InkIndex);

/// <summary>
/// The grid block, section 5.2. Along an axis holding a single bull the pitch mirrors the other axis, or is 0 for a
/// single bull, because it is otherwise unobservable (section 6).
/// </summary>
public sealed record BodyGrid(byte Cols, byte Rows, ushort OriginX, ushort OriginY, ushort PitchX, ushort PitchY, byte RingSetIndex, byte Order);

/// <summary>One row of the sighter block, section 5.2.</summary>
public sealed record BodySighterRow(byte Count, ushort OriginX, ushort OriginY, ushort PitchX, byte RingSetIndex);

/// <summary>One bull of the explicit-mode bull block, section 5.5: 12-bit coordinates and a 3-bit ring set index.</summary>
public sealed record BodyBull(ushort X, ushort Y, byte RingSetIndex, bool Scoring);

/// <summary>The fiducial block, section 5.2. Derived schemes carry no marker list (section 3.7).</summary>
public sealed record BodyFiducials(byte Scheme, byte Family, ushort MarkerSize, byte QuietZone, byte InkIndex);

/// <summary>The code block, section 5.2. Positions are derived by <c>corners-1</c>, never carried (section 3.8).</summary>
public sealed record BodyCodes(byte Count, byte EcLevel, byte ModuleSize, byte Placement);

/// <summary>The data block, section 5.2, present when flag bit 6 is set.</summary>
public sealed record BodyDataBlock(ushort X, ushort Y, ushort Width, ushort Height, byte Layout, byte FieldSet, ushort Reserve, byte InkIndex);

/// <summary>The tiling block, section 5.2, present when flag bit 7 is set.</summary>
public sealed record BodyTiling(byte Cols, byte Rows, ushort SheetWidth, ushort SheetHeight, ushort Overlap);

/// <summary>
/// One entry of the measurement grid block, section 5.2. <see cref="InkPair"/> holds the minor ink index in bits 0 to
/// 3 and the major in bits 4 to 7.
/// </summary>
public sealed record BodyMeasurementGrid(
    ushort CentreX,
    ushort CentreY,
    ushort Half,
    byte Divisions,
    byte MajorEvery,
    byte Unit,
    ushort Distance,
    byte DistanceUnit,
    byte InkPair,
    byte Style,
    byte LabelStep);
