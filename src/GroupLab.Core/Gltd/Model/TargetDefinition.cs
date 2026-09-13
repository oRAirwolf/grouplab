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

/// <summary>TARGET-SCHEMA.md section 3.2: dimensions are always explicit, even for a named size (rule R2).</summary>
public sealed record Page(PageSize Size, int Width, int Height, Orientation? Orientation);

/// <summary>An indexed ink, TARGET-SCHEMA.md section 3.3. <see cref="Srgb"/> is held as <c>#RRGGBB</c> in upper case.</summary>
public sealed record Ink(string Key, string Srgb, InkRole Role);

/// <summary>A reusable bull design, TARGET-SCHEMA.md section 3.4: concentric filled discs, outermost first, never strokes.</summary>
public sealed record RingSet(string Key, IReadOnlyList<Disc> Discs);

/// <summary>One filled disc of a ring set, TARGET-SCHEMA.md section 3.4.</summary>
public sealed record Disc(int Diameter, string Ink);

/// <summary>
/// An aiming point, TARGET-SCHEMA.md section 3.5. Its identity is its index in the bull array; labels are for
/// humans and need not be unique.
/// </summary>
public sealed record Bull(int X, int Y, string RingSet, string? Label, bool Scoring, Offset? LabelOffset);

/// <summary>A signed displacement in dmm, used for <see cref="Bull.LabelOffset"/>.</summary>
public sealed record Offset(int X, int Y);

/// <summary>A page position in dmm, measured from the page top-left (TARGET-SCHEMA.md section 3.2).</summary>
public sealed record PointDmm(int X, int Y);

/// <summary>Cells, TARGET-SCHEMA.md section 3.6.</summary>
public sealed record Cells(
    CellsMode Mode,
    bool? Drawn,
    int? SighterGap,
    string? Ink,
    int? Stroke,
    CellGrid? Grid,
    IReadOnlyList<IReadOnlyList<PointDmm>>? Polygons);

/// <summary>
/// A stored cell lattice, TARGET-SCHEMA.md section 3.6. Optional on a parametric layout, where it must equal the
/// lattice derived from the bull grid (test 24a).
/// </summary>
public sealed record CellGrid(int OriginX, int OriginY, int PitchX, int PitchY, int Cols, int Rows);

/// <summary>
/// Fiducials, TARGET-SCHEMA.md section 3.7. <see cref="Scheme"/> stays a string: the schema admits any versioned
/// rule name, and a reader that does not implement a rule must still be able to load and report it.
/// </summary>
public sealed record Fiducials(
    string Scheme,
    FiducialFamily Family,
    int MarkerSize,
    int QuietZone,
    string Ink,
    IReadOnlyList<Marker>? Markers);

/// <summary>A marker centre and its family code value, TARGET-SCHEMA.md section 3.7.</summary>
public sealed record Marker(int Id, int X, int Y);

/// <summary>QR codes carrying the definition frame, TARGET-SCHEMA.md section 3.8.</summary>
public sealed record Codes(
    int Count,
    int? Version,
    EcLevel EcLevel,
    int ModuleSize,
    int? QuietZone,
    CodePlacement Placement,
    bool? HumanReadableId,
    IReadOnlyList<PointDmm> Positions);

/// <summary>Print intent, TARGET-SCHEMA.md section 3.9. Carried in GLTD-J only; section 6 excludes it from the body.</summary>
public sealed record PrintSettings(
    PrintScaling? Scaling,
    int? MinimumDpi,
    ColourMode? ColourMode,
    bool? Duplex,
    string? Generator,
    string? Notes);

/// <summary>The load-data band, TARGET-SCHEMA.md section 3.10. A declared detection exclusion zone (section 7).</summary>
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

/// <summary>One field of an explicit data block, TARGET-SCHEMA.md section 3.10.</summary>
public sealed record DataField(string Key, string Label, int? X, int? Y, int? Width, int? Height);

/// <summary>Load data for one printing, TARGET-SCHEMA.md section 3.11. Excluded from the identifier and from GLTD-B.</summary>
public sealed record Instance(string? Serial, string? Printed, IReadOnlyList<KeyValuePair<string, string>>? Values);

/// <summary>
/// Assembly shape of a tiled sheet, TARGET-SCHEMA.md section 3.12. The tile index is in the frame header, not here,
/// so every tile shares one body and one identifier.
/// </summary>
public sealed record Tiling(int Cols, int Rows, int SheetWidth, int SheetHeight, int Overlap);

/// <summary>A printed angular measurement grid, TARGET-SCHEMA.md section 3.13. Its lines are derived, never stored.</summary>
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
