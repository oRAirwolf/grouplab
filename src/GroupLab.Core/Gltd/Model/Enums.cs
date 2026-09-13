namespace GroupLab.Core.Gltd.Model;

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

public enum Orientation
{
    Portrait,
    Landscape,
}

public enum InkRole
{
    Artwork,
    Fiducial,
    Code,
    Text,
    Paper,
}

public enum CellsMode
{
    None,
    Grid,
    Voronoi,
    Explicit,
}

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

public enum EcLevel
{
    L,
    M,
    Q,
    H,
}

public enum CodePlacement
{
    Corners1,
    Explicit,
}

public enum PrintScaling
{
    None,
    Fit,
}

public enum ColourMode
{
    Mono,
    Greyscale,
    Colour,
}

public enum DataBlockLayout
{
    Fields3x3,
    Fields3x2,
    Explicit,
}

public enum FieldSet
{
    Standard9,
    Standard6,
    Explicit,
}

public enum GridUnit
{
    Moa,
    Mil,
    Inch,
    Cm,
    Custom,
}

public enum DistanceUnit
{
    Yards,
    Metres,
}
