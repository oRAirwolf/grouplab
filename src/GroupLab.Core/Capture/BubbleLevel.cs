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
}
