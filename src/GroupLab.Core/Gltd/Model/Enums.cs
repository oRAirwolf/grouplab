namespace GroupLab.Core.Gltd.Model;

/// <summary>Named page sizes, TARGET-SCHEMA.md section 3.2. Standard dimensions are in <see cref="PageSizes"/>.</summary>
public enum PageSize
{
    Letter,
    Legal,
    Tabloid,
    A3,
    A4,
    A5,
    Roll24,
    Roll36,
    Roll42,
    Custom,
}

/// <summary>A hint to the print dialog only, TARGET-SCHEMA.md section 3.2: page width and height are already final.</summary>
public enum Orientation
{
    Portrait,
    Landscape,
}

/// <summary>
/// Ink roles, TARGET-SCHEMA.md section 3.3. <see cref="Paper"/> is a knockout that lays no ink at all, which is
/// why the renderer never draws it, not even in white.
/// </summary>
public enum InkRole
{
    Artwork,
    Fiducial,
    Code,
    Text,
    Paper,
}

/// <summary>Cell modes, TARGET-SCHEMA.md section 3.6. Cells are a default assignment for a human to correct, never authoritative.</summary>
public enum CellsMode
{
    None,
    Grid,
    Voronoi,
    Explicit,
}

/// <summary>Marker families, TARGET-SCHEMA.md section 3.7, in the order of the family byte of section 5.5.</summary>
public enum FiducialFamily
{
    None,
    Aruco4x4With50,
    Aruco4x4With100,
    Aruco5x5With100,
    Aruco6x6With250,
    AprilTag16h5,
    AprilTag25h9,
    AprilTag36h11,
    AprilTagCircle21h7,
}

/// <summary>QR error correction levels, TARGET-SCHEMA.md section 3.8, in the order of the ecLevel byte of section 5.2.</summary>
public enum EcLevel
{
    L,
    M,
    Q,
    H,
}

/// <summary>Code placement, TARGET-SCHEMA.md section 3.8. <see cref="Explicit"/> has no byte layout yet (section 11, question 13).</summary>
public enum CodePlacement
{
    Corners1,
    Explicit,
}

/// <summary>Print scaling intent, TARGET-SCHEMA.md section 3.9. Advisory: print scale is detected afterwards, not trusted.</summary>
public enum PrintScaling
{
    None,
    Fit,
}

/// <summary>TARGET-SCHEMA.md section 3.9.</summary>
public enum ColourMode
{
    Mono,
    Greyscale,
    Colour,
}

/// <summary>Data block layouts, TARGET-SCHEMA.md section 3.10, in the order of the layout byte of section 5.2.</summary>
public enum DataBlockLayout
{
    Fields3x3,
    Fields3x2,
    Explicit,
}

/// <summary>Data block field sets, TARGET-SCHEMA.md section 3.10, in the order of the fieldSet byte of section 5.2.</summary>
public enum FieldSet
{
    Standard9,
    Standard6,
    Explicit,
}

/// <summary>Measurement grid units, TARGET-SCHEMA.md section 3.13. The wire codes differ from this order; see WireCodes.</summary>
public enum GridUnit
{
    Moa,
    Mil,
    Inch,
    Cm,
    Custom,
}

/// <summary>TARGET-SCHEMA.md section 3.13.</summary>
public enum DistanceUnit
{
    Yards,
    Metres,
}
