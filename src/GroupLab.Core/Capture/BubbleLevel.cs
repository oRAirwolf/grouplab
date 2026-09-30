namespace GroupLab.Core.Capture;

/// <summary>
/// The capture screen's level, NOTES-FROM-PLANNING.md entry 281 section 1.1, Alan's specification: a four-way crosshair and a dot driven by
/// the gravity sensor that sits at the center when the phone is parallel to the table and drifts toward the high side the way a bubble does,
/// turning the ready color within <see cref="ReadyDegrees"/>. Android's gravity sensor reads the direction away from the ground in the
/// phone's own axes, x to the right and y to the top of the screen, so a raised edge gives that axis a positive reading, and the bubble
/// follows it: right for a raised right edge, up the screen for a raised top edge.
/// </summary>
public static class BubbleLevel
{
    /// <summary>Within this tilt from flat the dot turns the ready color.</summary>
    public const double ReadyDegrees = 3;

    /// <summary>The tilt at which the dot reaches the end of an arm; beyond it the dot stays at the end.</summary>
    public const double FullScaleDegrees = 15;

    /// <summary>The tilt from flat, in degrees, from the gravity sensor's reading.</summary>
    public static double Tilt(double x, double y, double z)
    {
        double g = Math.Sqrt((x * x) + (y * y) + (z * z));
        return g > 0 ? Math.Acos(Math.Min(1, Math.Abs(z) / g)) * 180 / Math.PI : 0;
    }

    /// <summary>
    /// Where the dot sits, as a share of an arm's length from the center, on the screen's axes: right and down positive. The distance grows
    /// with the tilt, up to 1 at <see cref="FullScaleDegrees"/>.
    /// </summary>
    public static (double Right, double Down) Dot(double x, double y, double z)
    {
        double across = Math.Sqrt((x * x) + (y * y));
        if (across == 0)
        {
            return (0, 0);
        }

        double reach = Math.Min(1, Tilt(x, y, z) / FullScaleDegrees);
        return (reach * x / across, reach * -y / across);
    }

    public static bool Ready(double x, double y, double z) => Tilt(x, y, z) <= ReadyDegrees;

    /// <summary>
    /// Entry 321 section 1: how steeply the camera looks down at which the level changes between flat on a table and upright at a backer, in
    /// degrees from horizontal. Held a phone halfway between the two, neither ideal is near; either side of it is.
    /// </summary>
    public const double ModeDegrees = 45;

    /// <summary>
    /// How far past <see cref="ModeDegrees"/> the camera must go before the mode changes, either way, so a phone held near the middle never
    /// flickers between the two. Both are 35 degrees from the nearer ideal at the switch, far outside <see cref="ReadyDegrees"/>.
    /// </summary>
    public const double ModeMarginDegrees = 10;

    /// <summary>How steeply the camera looks down, or up, in degrees from horizontal: 90 flat on a table, 0 upright.</summary>
    public static double Steepness(double x, double y, double z) => 90 - Tilt(x, y, z);

    /// <summary>
    /// Entry 321 section 1: which level the phone is held for, from gravity's reading in the screen's axes and the mode it was in, null at
    /// the start. Past <see cref="ModeDegrees"/> and <see cref="ModeMarginDegrees"/> it looks down; short of them it is upright, in portrait
    /// or landscape; in between it stays as it was.
    /// </summary>
    public static LevelMode ModeOf(double x, double y, double z, LevelMode? previous)
    {
        double steep = Steepness(x, y, z);
        return previous switch
        {
            LevelMode.LookingDown => steep < ModeDegrees - ModeMarginDegrees ? LevelMode.Upright : LevelMode.LookingDown,
            LevelMode.Upright => steep > ModeDegrees + ModeMarginDegrees ? LevelMode.LookingDown : LevelMode.Upright,
            _ => steep >= ModeDegrees ? LevelMode.LookingDown : LevelMode.Upright,
        };
    }

    /// <summary>
    /// Which way and how far the phone leans from level in a mode, in degrees along the screen's right and down: the bubble's offset, whose
    /// length is the tilt. Looking down it is <see cref="Dot"/>'s. Upright, the reading is turned as the phone would be tipped forward onto
    /// the table about its lower edge, whichever edge is lowest, portrait or landscape, and read the same way; so a camera pointing below
    /// the horizon moves the bubble down the screen as the phone stands, and a raised edge moves it toward that edge, as flat.
    /// </summary>
    public static (double Right, double Down) Lean(double x, double y, double z, LevelMode mode)
    {
        var (a, b, c) = mode == LevelMode.LookingDown ? (x, y, z)
            : Math.Abs(y) >= Math.Abs(x) ? (y >= 0 ? (x, -z, y) : (x, z, -y))
            : x >= 0 ? (-z, y, x) : (z, y, -x);
        double across = Math.Sqrt((a * a) + (b * b));
        if (across == 0)
        {
            return (0, 0);
        }

        double tilt = Tilt(a, b, c);
        return (tilt * a / across, tilt * -b / across);
    }

    /// <summary>A lean, or any direction, in the analysis frame's axes, turned clockwise by <paramref name="degrees"/> into the screen's.</summary>
    public static (double Right, double Down) Turned((double Right, double Down) lean, int degrees) => (((degrees % 360) + 360) % 360) switch
    {
        90 => (-lean.Down, lean.Right),
        180 => (-lean.Right, -lean.Down),
        270 => (lean.Down, -lean.Right),
        _ => lean,
    };

    /// <summary>The word beside the level for its mode (entry 321 section 1).</summary>
    public static string Word(LevelMode mode) => mode == LevelMode.Upright ? "Upright" : "Looking down";
}
