namespace GroupLab.Core.Capture;

/// <summary>How the phone is held for a picture (entry 321 section 1): flat over a sheet on a table, or upright at a sheet on a backer.</summary>
public enum LevelMode
{
    LookingDown,
    Upright,
}

/// <summary>What decided the level: gravity, or the sheet's own angle once its markers were read (entry 321 section 2).</summary>
public enum LevelSource
{
    Gravity,
    Sheet,
}

/// <summary>
/// The capture screen's level at a moment: the mode, null where there is no gravity to say, what decided it, and the lean in degrees along the
/// screen's right and down, whose length is the tilt.
/// </summary>
public sealed record LevelReading(LevelMode? Mode, LevelSource Source, double Right, double Down)
{
    public double Tilt => Math.Sqrt((Right * Right) + (Down * Down));

    /// <summary>Within <see cref="BubbleLevel.ReadyDegrees"/>: the crosshair turns green.</summary>
    public bool Ready => Tilt <= BubbleLevel.ReadyDegrees;

    /// <summary>Where the dot sits, as <see cref="BubbleLevel.Dot"/> places it: a share of an arm's length, reaching 1 at the full scale.</summary>
    public (double Right, double Down) Dot
    {
        get
        {
            double tilt = Tilt;
            double reach = Math.Min(1, tilt / BubbleLevel.FullScaleDegrees);
            return tilt == 0 ? (0, 0) : (reach * Right / tilt, reach * Down / tilt);
        }
    }

    /// <summary>The word beside the level; empty where the mode is not known.</summary>
    public string Word => Mode is { } mode ? BubbleLevel.Word(mode) : "";

    /// <summary>The source as the log writes it.</summary>
    public string SourceName => Source == LevelSource.Sheet ? "sheet" : "gravity";

    /// <summary>The mode as the log writes it.</summary>
    public string ModeName => Mode switch { LevelMode.Upright => "upright", LevelMode.LookingDown => "looking down", _ => "unknown" };
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 321: the capture screen's level, the same on Android and iOS. Alan: "people may be taking pictures of the
/// target that are still on the target backer at the range ... the crosshair will turn green and a photo will be taken if it is in an
/// upright position as well as a looking down position".
/// <para>
/// Gravity, thirty times a second, chooses the mode (<see cref="BubbleLevel.ModeOf"/>, which never flickers) and moves the bubble. Once a
/// frame has read the sheet's markers, the sheet's own angle decides instead (<see cref="OffAxis.Right"/>): a backer leaning back, or a
/// table that is not level, is square to the camera when the sheet is, whatever gravity says. A frame takes most of a second to judge, so
/// the sheet is not waited for between frames: what it said is kept as its difference from gravity at the frame, and gravity's movements
/// since are added, so the bubble follows the hand at once and is set right by each frame. The sheet decides for
/// <see cref="SheetFreshMs"/> after the last frame that read it, and gravity alone after that, before the sheet is found, and for a target
/// GroupLab did not print. With no gravity at all, as in a clip recorded without it, the sheet's own lean is shown as it is.
/// </para>
/// <para>
/// A roll about the camera's own axis tilts gravity but leaves the camera just as square to the sheet, so between frames it moves the
/// bubble and the next frame takes it back out; turning the phone about the camera's axis is not what the level asks for.
/// </para>
/// </summary>
public sealed class CameraLevel
{
    /// <summary>How long after the last frame that read the sheet its angle still decides, in milliseconds.</summary>
    public const long SheetFreshMs = 2500;

    private readonly Lock gate = new();
    private LevelMode? mode;
    private (double X, double Y, double Z)? gravity;
    private (double Right, double Down)? offset;
    private (double Right, double Down)? sheetOnly;
    private long? sheetMs;

    /// <summary>The last gravity reading, in the screen's axes, null before the first.</summary>
    public (double X, double Y, double Z)? Gravity
    {
        get
        {
            lock (gate)
            {
                return gravity;
            }
        }
    }

    /// <summary>The mode now, null before gravity has said.</summary>
    public LevelMode? Mode
    {
        get
        {
            lock (gate)
            {
                return mode;
            }
        }
    }

    /// <summary>Everything forgotten, as when the camera starts again.</summary>
    public void Reset()
    {
        lock (gate)
        {
            (mode, gravity, offset, sheetOnly, sheetMs) = (null, null, null, null, null);
        }
    }

    /// <summary>A gravity reading in the screen's axes, as <see cref="BubbleLevel"/> takes it; the level then.</summary>
    public LevelReading Felt(double x, double y, double z, long nowMs)
    {
        lock (gate)
        {
            gravity = (x, y, z);
            var now = BubbleLevel.ModeOf(x, y, z, mode);
            if (now != mode)
            {
                // What the sheet said was measured against the other mode's level, so it no longer applies.
                offset = null;
            }

            mode = now;
            return Reading(nowMs)!;
        }
    }

    /// <summary>
    /// A judged frame: the sheet's lean in the screen's axes where its markers were read, null where they were not, and gravity's reading
    /// when the frame was taken; the level then, null with neither gravity nor sheet.
    /// </summary>
    public LevelReading? Seen((double Right, double Down)? lean, (double X, double Y, double Z)? atFrame, long nowMs)
    {
        lock (gate)
        {
            if (lean is { } sheet)
            {
                if ((atFrame ?? gravity) is { } g)
                {
                    var felt = BubbleLevel.Lean(g.X, g.Y, g.Z, mode ??= BubbleLevel.ModeOf(g.X, g.Y, g.Z, null));
                    offset = (sheet.Right - felt.Right, sheet.Down - felt.Down);
                    sheetOnly = null;
                }
                else
                {
                    offset = null;
                    sheetOnly = sheet;
                }

                sheetMs = nowMs;
            }

            return Reading(nowMs);
        }
    }

    /// <summary>The level now, from what has been felt and seen; null with neither.</summary>
    public LevelReading? Now(long nowMs)
    {
        lock (gate)
        {
            return Reading(nowMs);
        }
    }

    private LevelReading? Reading(long nowMs)
    {
        bool fresh = sheetMs is { } seen && nowMs - seen <= SheetFreshMs;
        if (gravity is { } g && mode is { } m)
        {
            var felt = BubbleLevel.Lean(g.X, g.Y, g.Z, m);
            return fresh && offset is { } o
                ? new LevelReading(m, LevelSource.Sheet, felt.Right + o.Right, felt.Down + o.Down)
                : new LevelReading(m, LevelSource.Gravity, felt.Right, felt.Down);
        }

        return fresh && sheetOnly is { } s ? new LevelReading(null, LevelSource.Sheet, s.Right, s.Down) : null;
    }
}
