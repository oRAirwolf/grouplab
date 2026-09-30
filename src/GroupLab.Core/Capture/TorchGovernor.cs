namespace GroupLab.Core.Capture;

/// <summary>A change of the torch on Auto: the new level (0 is off), and the reason, for the log.</summary>
public sealed record TorchChange(int Level, string Reason);

/// <summary>
/// The torch on Auto, NOTES-FROM-PLANNING.md entry 302. Until then the torch came on at full strength when the paper was dim or the light
/// uneven, and stayed on for the rest of the session so it would not flicker: it never dimmed and never went off, even with glare from the
/// lens's own light on the paper. Now each frame's judgment moves it a step at a time:
/// <list type="bullet">
/// <item>It starts at the lowest level the phone offers and steps up only while the paper is still too dim.</item>
/// <item>It steps down, or goes off, on glare (more of the paper blown out than <see cref="CaptureQualities.FineClipped"/>), on a hotspot
/// (the light across the bulls clearly less even than it was without the torch), or when the paper is bright enough that the torch is not
/// needed (<see cref="BrightPaper"/> at the median).</item>
/// <item>It never flickers: a change needs <see cref="AgreeingFrames"/> frames in a row that want it and <see cref="SettleMs"/> since the
/// last change, which is also the time the exposure takes to settle; a step down for glare or a hotspot lowers the ceiling for the rest of
/// the session, so that level is never tried again; and after <see cref="MostReversals"/> changes of direction it stays where it is.</item>
/// </list>
/// A phone without strength levels has one level: on and off only. The same logic serves the iPhone's torch levels.
/// </summary>
public sealed class TorchGovernor
{
    /// <summary>The paper's median level, 0 to 255, at which the torch is not needed: about the brightest paper not yet blown out.</summary>
    public const double BrightPaper = 235;

    /// <summary>How much less even the bulls' light must be with the torch than without it before the torch is blamed for a hotspot.</summary>
    public const double HotspotMargin = 0.05;

    /// <summary>The frames in a row that must want the same change before it is made.</summary>
    public const int AgreeingFrames = 4;

    /// <summary>The least time between two changes, in milliseconds, which also lets the exposure settle after each.</summary>
    public const long SettleMs = 1500;

    /// <summary>The changes of direction allowed in a session, after which the torch stays where it is.</summary>
    public const int MostReversals = 2;

    private readonly int maxLevel;
    private int ceiling;
    private int wanted;
    private int wantedFrames;
    private string wantedReason = "";
    private long lastChangeMs = long.MinValue / 2;
    private int lastDirection;
    private int reversals;
    private double? evennessWithout;

    /// <param name="maxLevel">The phone's highest torch level, 1 where only on and off are offered, 0 where there is no torch.</param>
    /// <param name="level">The level the torch is at now.</param>
    public TorchGovernor(int maxLevel, int level = 0)
    {
        this.maxLevel = Math.Max(0, maxLevel);
        ceiling = this.maxLevel;
        Level = Math.Clamp(level, 0, this.maxLevel);
    }

    /// <summary>The torch's level now, 0 for off.</summary>
    public int Level { get; private set; }

    /// <summary>
    /// One frame judged at <paramref name="nowMs"/>. Returns the change to make, or null to leave the torch as it is. A frame with no
    /// quality (no sheet registered yet) changes nothing and breaks a run of agreeing frames.
    /// </summary>
    public TorchChange? Next(CaptureQuality? quality, double? evenness, long nowMs)
    {
        if (maxLevel == 0 || quality is null)
        {
            wantedFrames = 0;
            return null;
        }

        if (Level == 0 && evenness is { } e)
        {
            evennessWithout = e;
        }

        (int target, string reason) = Want(quality, evenness);
        if (target == Level)
        {
            wantedFrames = 0;
            return null;
        }

        if (target == wanted && reason == wantedReason)
        {
            wantedFrames++;
        }
        else
        {
            (wanted, wantedReason, wantedFrames) = (target, reason, 1);
        }

        if (wantedFrames < AgreeingFrames || nowMs - lastChangeMs < SettleMs || reversals >= MostReversals)
        {
            return null;
        }

        int direction = Math.Sign(target - Level);
        if (lastDirection != 0 && direction != lastDirection)
        {
            reversals++;
        }

        if (reason is "glare" or "hotspot")
        {
            ceiling = Math.Min(ceiling, target);
        }

        (Level, lastDirection, lastChangeMs, wantedFrames) = (target, direction, nowMs, 0);
        return new TorchChange(target, reason);
    }

    private (int Level, string Reason) Want(CaptureQuality quality, double? evenness)
    {
        bool glare = quality.ClippedShare is > CaptureQualities.FineClipped;
        bool hotspot = Level > 0 && evenness is { } even && even < PictureCheck.EvenLight && evennessWithout is { } without && even < without - HotspotMargin;
        bool bright = quality.PaperLevel is >= BrightPaper;
        bool dim = !glare && (quality.ExposurePart is < CaptureGuidance.Holds || (Level == 0 && evenness is < PictureCheck.EvenLight));
        if (Level > 0 && (glare || hotspot))
        {
            return (Level - 1, glare ? "glare" : "hotspot");
        }

        if (Level > 0 && bright)
        {
            return (Level - 1, "bright");
        }

        if (dim && Level < ceiling && !bright)
        {
            return (Level + 1, Level == 0 && quality.ExposurePart is >= CaptureGuidance.Holds ? "uneven" : "dim");
        }

        return (Level, "");
    }
}
