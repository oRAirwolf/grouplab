namespace GroupLab.Core.Gltd.Model;

/// <summary>Maps each enumeration to the exact strings of TARGET-SCHEMA.md section 9.</summary>
public sealed class NameTable<T>
    where T : struct, Enum
{
    private readonly (string Name, T Value)[] _entries;

    public NameTable(params (string Name, T Value)[] entries) => _entries = entries;

    public IEnumerable<string> Names => _entries.Select(e => e.Name);

    public bool TryParse(string name, out T value)
    {
        foreach (var entry in _entries)
        {
            if (string.Equals(entry.Name, name, StringComparison.Ordinal))
            {
                value = entry.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    public string NameOf(T value)
    {
        foreach (var entry in _entries)
        {
            if (EqualityComparer<T>.Default.Equals(entry.Value, value))
            {
                return entry.Name;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(value), value, $"No GLTD name for {typeof(T).Name}.{value}.");
    }
}

/// <summary>The enumeration strings of the JSON Schema in TARGET-SCHEMA.md section 9, one table per enumeration.</summary>
public static class GltdNames
{
    public static NameTable<PageSize> PageSize { get; } = new(
        ("letter", Model.PageSize.Letter),
        ("legal", Model.PageSize.Legal),
        ("tabloid", Model.PageSize.Tabloid),
        ("a3", Model.PageSize.A3),
        ("a4", Model.PageSize.A4),
        ("a5", Model.PageSize.A5),
        ("roll-24", Model.PageSize.Roll24),
        ("roll-36", Model.PageSize.Roll36),
        ("roll-42", Model.PageSize.Roll42),
        ("custom", Model.PageSize.Custom));

    public static NameTable<Orientation> Orientation { get; } = new(
        ("portrait", Model.Orientation.Portrait),
        ("landscape", Model.Orientation.Landscape));

    public static NameTable<InkRole> InkRole { get; } = new(
        ("artwork", Model.InkRole.Artwork),
        ("fiducial", Model.InkRole.Fiducial),
        ("code", Model.InkRole.Code),
        ("text", Model.InkRole.Text),
        ("paper", Model.InkRole.Paper));

    public static NameTable<CellsMode> CellsMode { get; } = new(
        ("none", Model.CellsMode.None),
        ("grid", Model.CellsMode.Grid),
        ("voronoi", Model.CellsMode.Voronoi),
        ("explicit", Model.CellsMode.Explicit));

    public static NameTable<FiducialFamily> FiducialFamily { get; } = new(
        ("none", Model.FiducialFamily.None),
        ("aruco-4x4-50", Model.FiducialFamily.Aruco4x4With50),
        ("aruco-4x4-100", Model.FiducialFamily.Aruco4x4With100),
        ("aruco-5x5-100", Model.FiducialFamily.Aruco5x5With100),
        ("aruco-6x6-250", Model.FiducialFamily.Aruco6x6With250),
        ("apriltag-16h5", Model.FiducialFamily.AprilTag16h5),
        ("apriltag-25h9", Model.FiducialFamily.AprilTag25h9),
        ("apriltag-36h11", Model.FiducialFamily.AprilTag36h11),
        ("apriltag-circle21h7", Model.FiducialFamily.AprilTagCircle21h7));

    public static NameTable<EcLevel> EcLevel { get; } = new(
        ("L", Model.EcLevel.L),
        ("M", Model.EcLevel.M),
        ("Q", Model.EcLevel.Q),
        ("H", Model.EcLevel.H));

    public static NameTable<CodePlacement> CodePlacement { get; } = new(
        ("corners-1", Model.CodePlacement.Corners1),
        ("explicit", Model.CodePlacement.Explicit));

    public static NameTable<PrintScaling> PrintScaling { get; } = new(
        ("none", Model.PrintScaling.None),
        ("fit", Model.PrintScaling.Fit));

    public static NameTable<ColourMode> ColourMode { get; } = new(
        ("mono", Model.ColourMode.Mono),
        ("greyscale", Model.ColourMode.Greyscale),  // British on purpose: a key in a file GroupLab reads and writes
        ("colour", Model.ColourMode.Colour));  // British on purpose: a key in a file GroupLab reads and writes

    public static NameTable<DataBlockLayout> DataBlockLayout { get; } = new(
        ("fields-3x3-1", Model.DataBlockLayout.Fields3x3),
        ("fields-3x2-1", Model.DataBlockLayout.Fields3x2),
        ("explicit", Model.DataBlockLayout.Explicit));

    public static NameTable<FieldSet> FieldSet { get; } = new(
        ("standard-9", Model.FieldSet.Standard9),
        ("standard-6", Model.FieldSet.Standard6),
        ("explicit", Model.FieldSet.Explicit));

    public static NameTable<GridUnit> GridUnit { get; } = new(
        ("moa", Model.GridUnit.Moa),
        ("mil", Model.GridUnit.Mil),
        ("inch", Model.GridUnit.Inch),
        ("cm", Model.GridUnit.Cm),
        ("custom", Model.GridUnit.Custom));

    public static NameTable<DistanceUnit> DistanceUnit { get; } = new(
        ("yd", Model.DistanceUnit.Yards),
        ("m", Model.DistanceUnit.Metres));
}
