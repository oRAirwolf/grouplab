namespace GroupLab.Core.Gltd.Binary;

/// <summary>
/// A GLTD-B body exactly as section 5.2 lays it out, block by block, with every length still in quanta.
/// <see cref="ExplicitBulls"/> is set for explicit mode (section 5.5) and replaces the grid and sighters.
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

public readonly record struct Rgb(byte R, byte G, byte B)
{
    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
}

/// <summary><see cref="Width"/> is carried only for a custom page, <see cref="Height"/> for custom and roll pages.</summary>
public sealed record BodyPage(byte PageCode, ushort Width, ushort Height, byte Orientation, byte Quantum);

/// <summary><see cref="InkIndex"/> 15 is the paper knockout.</summary>
public sealed record BodyDisc(ushort Diameter, byte InkIndex);

public sealed record BodyGrid(byte Cols, byte Rows, ushort OriginX, ushort OriginY, ushort PitchX, ushort PitchY, byte RingSetIndex, byte Order);

public sealed record BodySighterRow(byte Count, ushort OriginX, ushort OriginY, ushort PitchX, byte RingSetIndex);

public sealed record BodyBull(ushort X, ushort Y, byte RingSetIndex, bool Scoring);

public sealed record BodyFiducials(byte Scheme, byte Family, ushort MarkerSize, byte QuietZone, byte InkIndex);

public sealed record BodyCodes(byte Count, byte EcLevel, byte ModuleSize, byte Placement);

public sealed record BodyDataBlock(ushort X, ushort Y, ushort Width, ushort Height, byte Layout, byte FieldSet, ushort Reserve, byte InkIndex);

public sealed record BodyTiling(byte Cols, byte Rows, ushort SheetWidth, ushort SheetHeight, ushort Overlap);

/// <summary><see cref="InkPair"/> holds the minor ink index in bits 0 to 3 and the major in bits 4 to 7.</summary>
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
