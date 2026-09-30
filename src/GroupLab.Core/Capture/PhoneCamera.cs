using GroupLab.Core.Imaging;

namespace GroupLab.Core.Capture;

/// <summary>
/// The camera's arithmetic that does not depend on the phone's own camera library, NOTES-FROM-PLANNING.md entry 290 section 2 item 5: the
/// iPhone and iPad camera makes the same choices the Android one makes through CameraX, and these are the parts of them that can be tested
/// on any machine. Which of the camera's modes feeds the live analysis, how much larger the picture is measured than the analysis frame,
/// and how the level reads the device's gravity when the screen is turned.
/// </summary>
public static class PhoneCamera
{
    /// <summary>
    /// The live analysis frame's size, as on Android (entry 281): at 640 by 480 a sheet's square codes were about a pixel a module, and at
    /// 1920 by 1440 a Letter sheet across half the frame gives its markers about 20 pixels a side.
    /// </summary>
    public const int AnalysisWidth = 1920;

    public const int AnalysisHeight = 1440;

    /// <summary>Whether a size has the sensor's own 4:3 shape, either way round.</summary>
    public static bool IsFourByThree(int width, int height) =>
        width > 0 && height > 0 && ((long)width * 3 == (long)height * 4 || (long)height * 3 == (long)width * 4);

    /// <summary>
    /// The camera mode the live analysis and the picture both come from, as an index into <paramref name="modes"/>, or -1 where none is
    /// 4:3. Entry 281 section 1.6: preview, analysis and picture are all 4:3, so what the shooter framed is what is saved. Of the 4:3 modes
    /// it takes the stream size nearest <see cref="AnalysisWidth"/> by <see cref="AnalysisHeight"/>, the smallest at least that large
    /// first and the largest smaller one where there is none, the rule CameraX follows on Android (closest higher, then lower); among
    /// modes of that size, the one whose largest picture is the largest, then full range luminance, then the first listed.
    /// </summary>
    public static int ChooseMode(IReadOnlyList<CameraMode> modes)
    {
        ArgumentNullException.ThrowIfNull(modes);
        int best = -1;
        for (int i = 0; i < modes.Count; i++)
        {
            var mode = modes[i];
            if (!IsFourByThree(mode.StreamWidth, mode.StreamHeight))
            {
                continue;
            }

            if (best < 0 || Better(mode, modes[best]))
            {
                best = i;
            }
        }

        return best;
    }

    private static bool Better(CameraMode a, CameraMode b)
    {
        int size = CompareStream(a, b);
        if (size != 0)
        {
            return size < 0;
        }

        long pictureA = (long)a.PictureWidth * a.PictureHeight, pictureB = (long)b.PictureWidth * b.PictureHeight;
        if (pictureA != pictureB)
        {
            return pictureA > pictureB;
        }

        return a.FullRange && !b.FullRange;
    }

    /// <summary>Negative where <paramref name="a"/>'s stream size is the better, positive where <paramref name="b"/>'s is, 0 where they are the same.</summary>
    private static int CompareStream(CameraMode a, CameraMode b)
    {
        long wanted = (long)AnalysisWidth * AnalysisHeight;
        long areaA = (long)a.StreamWidth * a.StreamHeight, areaB = (long)b.StreamWidth * b.StreamHeight;
        if (areaA == areaB)
        {
            return 0;
        }

        bool highA = Math.Max(a.StreamWidth, a.StreamHeight) >= AnalysisWidth && Math.Min(a.StreamWidth, a.StreamHeight) >= AnalysisHeight && areaA >= wanted;
        bool highB = Math.Max(b.StreamWidth, b.StreamHeight) >= AnalysisWidth && Math.Min(b.StreamWidth, b.StreamHeight) >= AnalysisHeight && areaB >= wanted;
        if (highA != highB)
        {
            return highA ? -1 : 1;
        }

        // Both at least the size wanted: the smaller is nearer. Both smaller: the larger is nearer.
        return highA ? areaA.CompareTo(areaB) : areaB.CompareTo(areaA);
    }

    /// <summary>
    /// The picture's pixels, as the phone measures it, per pixel of the analysis frame: the picture's longer side, cut to the phone's
    /// working copy (<see cref="WorkingSize.PhoneMegapixels"/>), over the frame's longer side. Entry 281 section 1.4: the guidance judges
    /// resolution at the size the picture is measured at, not at the stream's. 1 where either size is not known.
    /// </summary>
    public static double MeasuredScale(int pictureWidth, int pictureHeight, int frameWidth, int frameHeight)
    {
        if (pictureWidth <= 0 || pictureHeight <= 0 || frameWidth <= 0 || frameHeight <= 0)
        {
            return 1;
        }

        double working = Math.Max(pictureWidth, pictureHeight) * Math.Min(1, WorkingSize.Scale(pictureWidth, pictureHeight, WorkingSize.PhoneMegapixels));
        return working / Math.Max(frameWidth, frameHeight);
    }

    /// <summary>
    /// Entry 321: Android's gravity sensor, which reads in the phone's natural axes, turned into the screen's, as the level takes it. The
    /// screen follows the phone into landscape, and <paramref name="displayDegrees"/> is how far it is turned (the display's rotation, 0, 90,
    /// 180 or 270): at 90 the phone is turned counterclockwise, so the screen's right is the phone's bottom and its top the phone's right.
    /// </summary>
    public static (double X, double Y, double Z) LevelFromAndroid(double x, double y, double z, int displayDegrees) => (((displayDegrees % 360) + 360) % 360) switch
    {
        90 => (-y, x, z),
        180 => (-x, -y, z),
        270 => (y, -x, z),
        _ => (x, y, z),
    };

    /// <summary>
    /// The level's reading from iOS's gravity (Core Motion's device motion), in the axes <see cref="BubbleLevel"/> takes: Android's, the
    /// direction away from the ground, x to the right of the screen and y to its top. Core Motion gives the pull toward the ground in the
    /// device's own axes, x to the right and y to the top of the device held upright, so the reading is turned round, and then turned with
    /// the screen, since an iPad's screen may be the other way up or on its side while the device's axes stay where they are.
    /// </summary>
    public static (double X, double Y, double Z) LevelFromGravity(double x, double y, double z, ScreenTurn screen) => screen switch
    {
        // The device turned clockwise: the screen's right is the device's top, and the screen's top is the device's left.
        ScreenTurn.LandscapeLeft => (-y, x, -z),

        // The device turned counterclockwise: the screen's right is the device's bottom, and the screen's top is the device's right.
        ScreenTurn.LandscapeRight => (y, -x, -z),
        ScreenTurn.UpsideDown => (x, y, -z),
        _ => (-x, -y, -z),
    };

    /// <summary>
    /// The angle, in degrees, a picture or preview from the back camera is turned by so it stands the way the screen does; the sensor's
    /// own frame is the device on its side, turned counterclockwise. iOS asks for it on each output's connection.
    /// </summary>
    public static double RotationDegrees(ScreenTurn screen) => screen switch
    {
        ScreenTurn.LandscapeRight => 0,
        ScreenTurn.LandscapeLeft => 180,
        ScreenTurn.UpsideDown => 270,
        _ => 90,
    };
}

/// <summary>One of the camera's modes: the size of its stream and of the largest picture it can take, and whether its luminance is full range.</summary>
public readonly record struct CameraMode(int StreamWidth, int StreamHeight, int PictureWidth, int PictureHeight, bool FullRange);

/// <summary>
/// Which way up the screen is, named as iOS names its interface orientations: landscape left has the device turned clockwise from
/// upright, landscape right counterclockwise.
/// </summary>
public enum ScreenTurn
{
    Upright,
    UpsideDown,
    LandscapeLeft,
    LandscapeRight,
}
