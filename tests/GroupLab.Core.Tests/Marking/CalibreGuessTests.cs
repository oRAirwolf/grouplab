using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// What GroupLab says about the calibre when it has not been told one.
/// <para>
/// <b>It used to guess, and entry 161 stopped it.</b> Alan's requirement of 2026-09-22 was that the guess snap to something somebody
/// shoots and offer its neighbours where the holes could not tell them apart. It did that faithfully, and on a friend's ten 6.5 Creedmoor
/// holes it offered .308 and .312: both wrong, by 0.044 in. The guess inverted a hole-to-bullet relationship that the scans now measure at
/// 0.765 to 1.127, which is not a constant and is not even on one side of 1, so no list and no window can rescue it.
/// </para>
/// <para>
/// So it gives the measurement and asks. A fact the shooter knows does not become a guess the software makes, which is question 37's rule.
/// </para>
/// </summary>
public class CalibreGuessTests
{
    /// <summary>A sheet whose holes measure these diameters, which is all the guess reads.</summary>
    private static MarkingState Sheet(params double[] diameters) => MarkingState.Empty with
    {
        Scale = new LengthReference(new PointD(0, 0), new PointD(100, 0), 1),
        Bulls = [.. diameters.Select((_, i) => new BullAim(i + 1, (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), new PointD(100 + (i * 50), 100)))],
        Shots = [.. diameters.Select((d, i) => new MarkedShot(i + 1, new PointD(100 + (i * 50), 100), ShotProvenance.Automatic, Bull: i + 1, MeasuredDiameterInches: d))],
        NextId = diameters.Length + 1,
    };

    /// <summary>
    /// A sheet of holes that measure a bullet of this diameter, tightly enough that the sheet can speak for itself: five holes within a
    /// thousandth, which is about what the scans of known calibre actually do.
    /// </summary>
    private static MarkingState Tight(double bullet) =>
        Sheet([.. new[] { -0.0005, 0.0005, 0.0, -0.0004, 0.0004 }.Select(off => bullet - CalibreConfirmation.PaperShrinkInches + off)]);

    /// <summary>
    /// Entry 161 section 4: whatever the bullet, the holes are measured and nothing is named. The two cases that matter are here: holes
    /// smaller than the bullet, as the earlier scans made, and holes larger than it, as the friend's did.
    /// </summary>
    [Theory]
    [InlineData(0.224, FirearmType.Rifle)]
    [InlineData(0.264, FirearmType.Rifle)]
    [InlineData(0.308, FirearmType.Rifle)]
    [InlineData(0.451, FirearmType.Pistol)]
    public void NoCartridgeIsNamedFromTheHoles(double bullet, FirearmType firearm)
    {
        var guess = CalibreConfirmation.Guess(Tight(bullet), firearm);

        Assert.Null(guess.Nearest);
        Assert.Null(guess.DiameterInches);
        Assert.Empty(guess.Offered);
        Assert.Contains("Name what you fired", guess.Why, StringComparison.Ordinal);
    }

    /// <summary>The friend's sheet itself, in numbers: ten holes about 0.30 in across. It must not be called .308, or anything else.</summary>
    [Fact]
    public void TheFriendsSheetIsNotCalledThreeOhEight()
    {
        var guess = CalibreConfirmation.Guess(Sheet(0.285, 0.29, 0.295, 0.298, 0.30, 0.301, 0.302, 0.304, 0.306, 0.308));

        Assert.Null(guess.Nearest);
        Assert.Contains("0.301 in across the middle", guess.Why, StringComparison.Ordinal);
        Assert.DoesNotContain(".308", guess.Why, StringComparison.Ordinal);
        Assert.DoesNotContain(".312", guess.Why, StringComparison.Ordinal);
    }

    /// <summary>
    /// A diameter on the load record is not a guess. The shooter wrote it down; nothing read off paper beats that, and GroupLab does not
    /// offer a second opinion on it.
    /// </summary>
    [Fact]
    public void ADiameterOnTheLoadWinsAndNothingIsGuessed()
    {
        var guess = CalibreConfirmation.Guess(Tight(0.224), FirearmType.Rifle, loadDiameterInches: 0.264);

        Assert.True(guess.FromTheLoad);
        Assert.Equal(0.264, guess.DiameterInches!.Value, 6);
        Assert.Equal(0.264, guess.Nearest!.DiameterInches, 6);
        Assert.Empty(guess.Neighbours!);
        Assert.False(guess.Rough);
        Assert.Contains("records a bullet diameter", guess.Why, StringComparison.Ordinal);
    }

    /// <summary>
    /// A photograph preselects nothing, question 38: photographs of sheets of known calibre read from 0.90 to 1.45 times the bullet
    /// depending on the light they were taken in, so there is no diameter in one to read. It says why, which is wider than a scan's reason.
    /// </summary>
    [Fact]
    public void APhotographGuessesNothingAndSaysWhy()
    {
        var guess = CalibreConfirmation.Guess(Tight(0.264), FirearmType.Rifle, fromPhotograph: true);

        Assert.Null(guess.Nearest);
        Assert.True(guess.Rough);
        Assert.Contains("this is a photograph", guess.Why, StringComparison.Ordinal);
        Assert.Contains("will not guess a calibre from it", guess.Why, StringComparison.Ordinal);
        Assert.Empty(guess.Offered);
    }

    /// <summary>
    /// A mark flagged as possibly two holes is left out of the reading. It measures like nothing on the sheet, and its width would widen the
    /// window that decides how sure the guess can be.
    /// </summary>
    [Fact]
    public void AMarkThatMayBeTwoHolesIsNotMeasured()
    {
        var sheet = Tight(0.264);
        var withADouble = sheet with
        {
            Shots = sheet.Shots.Add(new MarkedShot(99, new PointD(900, 100), ShotProvenance.Automatic, Bull: 99, MeasuredDiameterInches: 0.42)
            {
                Oversize = new GroupLab.Core.Marking.DetectedOversize(2.1, false),
            }),
        };

        Assert.Equal(CalibreConfirmation.Guess(sheet).MedianHoleInches, CalibreConfirmation.Guess(withADouble).MedianHoleInches);
        Assert.Equal(5, CalibreConfirmation.Guess(withADouble).HolesMeasured);
    }

    /// <summary>Whatever the list says, a diameter that is not on it can still be typed, and the refusals for designations still stand.</summary>
    [Fact]
    public void AnythingCanStillBeTypedAndTheDesignationsAreStillRefused()
    {
        Assert.Equal(0.2235, Calibre.Parse("0.2235", out _)!.DiameterInches, 6);
        Assert.Null(Calibre.Parse("6.5", out string? why));
        Assert.Contains("Enter the bullet diameter", why!, StringComparison.Ordinal);
        Assert.Null(Calibre.Parse("6.5 mm", out string? metric));
        Assert.Contains("not the bullet's diameter", metric!, StringComparison.Ordinal);
        Assert.Null(Calibre.Parse("7.62 mm", out _));
    }
}
