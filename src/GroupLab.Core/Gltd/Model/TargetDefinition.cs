using System.Text.Json;

namespace GroupLab.Core.Gltd.Model;

/// <summary>
/// A GLTD-J document, TARGET-SCHEMA.md section 3. Every length is an integer number of dmm
/// (rule R1). Top-level members this revision does not know are kept in
/// <see cref="UnknownFields"/>, in the order read, so a save preserves them (rule R4).
/// </summary>
public sealed record TargetDefinition(
    int Gltd,
    int Revision,
    string? Id,
    string Name,
    string? Description,
    string? Author,
    string? Licence,
    string? Created,
    string Units,
    Page Page,
    IReadOnlyList<Ink> Inks,
    IReadOnlyList<RingSet> RingSets,
    IReadOnlyList<Bull> Bulls,
    Cells? Cells,
    Fiducials? Fiducials,
    Codes? Codes,
    PrintSettings? Print,
    DataBlock? DataBlock,
    Instance? Instance,
    Tiling? Tiling,
    IReadOnlyList<MeasurementGrid>? Grids,
    IReadOnlyList<KeyValuePair<string, JsonElement>> UnknownFields);

public sealed record Page(PageSize Size, int Width, int Height, Orientation? Orientation);

/// <summary><see cref="Srgb"/> is held as <c>#RRGGBB</c> in upper case.</summary>
public sealed record Ink(string Key, string Srgb, InkRole Role);

public sealed record RingSet(string Key, IReadOnlyList<Disc> Discs);

public sealed record Disc(int Diameter, string Ink);

public sealed record Bull(int X, int Y, string RingSet, string? Label, bool Scoring, Offset? LabelOffset);

public sealed record Offset(int X, int Y);

public sealed record PointDmm(int X, int Y);

public sealed record Cells(
    CellsMode Mode,
    bool? Drawn,
    int? SighterGap,
    string? Ink,
    int? Stroke,
    CellGrid? Grid,
    IReadOnlyList<IReadOnlyList<PointDmm>>? Polygons);

public sealed record CellGrid(int OriginX, int OriginY, int PitchX, int PitchY, int Cols, int Rows);

/// <summary>
/// <see cref="Scheme"/> stays a string: the schema admits any versioned rule name, and a reader
/// that does not implement a rule must still be able to load and report it.
/// </summary>
public sealed record Fiducials(
    string Scheme,
    FiducialFamily Family,
    int MarkerSize,
    int QuietZone,
    string Ink,
    IReadOnlyList<Marker>? Markers);

public sealed record Marker(int Id, int X, int Y);

public sealed record Codes(
    int Count,
    int? Version,
    EcLevel EcLevel,
    int ModuleSize,
    int? QuietZone,
    CodePlacement Placement,
    bool? HumanReadableId,
    IReadOnlyList<PointDmm> Positions);

public sealed record PrintSettings(
    PrintScaling? Scaling,
    int? MinimumDpi,
    ColourMode? ColourMode,
    bool? Duplex,
    string? Generator,
    string? Notes);

public sealed record DataBlock(
    int X,
    int Y,
    int Width,
    int Height,
    DataBlockLayout Layout,
    FieldSet FieldSet,
    int Reserve,
    string Ink,
    string? LabelInk,
    int? Border,
    IReadOnlyList<DataField>? Fields);

public sealed record DataField(string Key, string Label, int? X, int? Y, int? Width, int? Height);

/// <summary>Excluded from the definition identifier and from GLTD-B, section 3.11.</summary>
public sealed record Instance(string? Serial, string? Printed, IReadOnlyList<KeyValuePair<string, string>>? Values);

public sealed record Tiling(int Cols, int Rows, int SheetWidth, int SheetHeight, int Overlap);

public sealed record MeasurementGrid(
    string Key,
    int CentreX,
    int CentreY,
    int Half,
    int Divisions,
    int MajorEvery,
    GridUnit Unit,
    int Distance,
    DistanceUnit DistanceUnit,
    string? MinorInk,
    string? MajorInk,
    string? AxisInk,
    int? MinorStroke,
    int? MajorStroke,
    int? AxisStroke,
    int? LabelStep,
    string? LabelInk);
