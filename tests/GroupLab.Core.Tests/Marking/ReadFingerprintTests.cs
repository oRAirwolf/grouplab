using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Measurement;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// Alan's queue of 2026-11-24, part 3: the result fingerprint the phone's log carries, so a change made for speed is shown to change no
/// result. The same reading always gives the same sixteen digits, and a hole moved by the smallest step a double can take changes them.
/// </summary>
public class ReadFingerprintTests
{
    private static AutomaticResult Reading(double x) => new(null!, null,
        [new BullAim(0, "1", new PointD(100, 200))],
        [new DetectedShot(new PointD(x, 250.25), new AssignedShot(0, 0, 1.5, 0, 1.5, 3, false), 0.308)],
        [], "", null);

    [Fact]
    public void TheSameReadingGivesTheSameFingerprintAndAnyMoveChangesIt()
    {
        string first = ReadFingerprint.Of(Reading(120.5));
        Assert.Matches("^[0-9a-f]{16}$", first);
        Assert.Equal(first, ReadFingerprint.Of(Reading(120.5)));
        Assert.NotEqual(first, ReadFingerprint.Of(Reading(Math.BitIncrement(120.5))));
        Assert.Equal("none", ReadFingerprint.Of(new AutomaticResult(null!, null, [], [], [], "", null)));
    }
}
