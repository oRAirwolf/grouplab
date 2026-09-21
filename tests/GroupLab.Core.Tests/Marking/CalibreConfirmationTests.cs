using System.Collections.Immutable;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 6.3: GroupLab's best guess at the calibre from the measured hole sizes, and Accept held until the
/// person confirms or corrects it.
/// <para>
/// <b>Tonight's re-run of the six range scans is why this is worth a gate rather than a field.</b> Naming the calibre is the difference
/// between nineteen holes found and twenty-four on scan 4, and between finding and missing the shot cut by the edge of the scan on scan 6.
/// It is the most valuable single thing a person can tell GroupLab about a sheet, and nothing ever asked them for it.
/// </para>
/// </summary>
public class CalibreConfirmationTests
{
    private static MarkingState WithHoles(params double[] diameters)
    {
        var shots = diameters.Select((d, i) => new MarkedShot(i + 1, new PointD(100 + (i * 50), 100), ShotProvenance.Automatic, Bull: i + 1, MeasuredDiameterInches: d));
        return MarkingState.Empty with
        {
            Scale = new LengthReference(new PointD(0, 0), new PointD(100, 0), 1),
            Bulls = [.. diameters.Select((_, i) => new BullAim(i + 1, (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), new PointD(100 + (i * 50), 100)))],
            Shots = [.. shots],
            NextId = diameters.Length + 1,
        };
    }

    /// <summary>
    /// A sheet of .22 holes reads as a .22. The holes measure well under the bullet, which is the whole difficulty, so the paper's own
    /// closing is put back before the reading is offered.
    /// </summary>
    [Fact]
    public void ASheetOfSmallHolesReadsAsASmallCalibre()
    {
        // .224 bullets leave holes around 0.20 in on this paper.
        var guess = CalibreConfirmation.Guess(WithHoles(0.198, 0.204, 0.201, 0.207, 0.199));

        Assert.NotNull(guess.DiameterInches);
        Assert.InRange(guess.DiameterInches!.Value, 0.21, 0.24);
        Assert.Equal(5, guess.HolesMeasured);
        Assert.Equal(0.201, guess.MedianHoleInches!.Value, 6);
    }

    [Fact]
    public void ASheetOfLargeHolesReadsAsALargeCalibre()
    {
        var guess = CalibreConfirmation.Guess(WithHoles(0.285, 0.292, 0.288, 0.295, 0.290));

        Assert.NotNull(guess.DiameterInches);
        Assert.InRange(guess.DiameterInches!.Value, 0.29, 0.33);
    }

    /// <summary>
    /// The reading never states a calibre as fact. A hole in paper is not the bullet that made it, and the sentence says so and says what
    /// moves it, so a person has something to judge rather than a number to accept.
    /// </summary>
    [Fact]
    public void TheGuessSaysWhatItCannotKnow()
    {
        string why = CalibreConfirmation.Guess(WithHoles(0.198, 0.204, 0.201, 0.207, 0.199)).Why;

        Assert.Contains("A hole is not the bullet", why, StringComparison.Ordinal);
        Assert.Contains("check it", why, StringComparison.Ordinal);
        Assert.Contains("the paper, the backing", why, StringComparison.Ordinal);
    }

    /// <summary>Too few holes is said as too few, not answered with a number read off one or two marks.</summary>
    [Fact]
    public void TooFewHolesGiveNoGuess()
    {
        var two = CalibreConfirmation.Guess(WithHoles(0.198, 0.204));
        Assert.Null(two.DiameterInches);
        Assert.Contains("too few", two.Why, StringComparison.Ordinal);

        var none = CalibreConfirmation.Guess(MarkingState.Empty);
        Assert.Null(none.DiameterInches);
        Assert.Contains("nothing to read a calibre from", none.Why, StringComparison.Ordinal);
    }

    /// <summary>A hole size a person set by hand counts as a measurement, because they measured it.</summary>
    [Fact]
    public void AHoleSizeSetByHandCountsTowardsTheGuess()
    {
        var session = new MarkingSession(WithHoles(0.198, 0.204, 0.201));
        session.SetHoleDiameter(1, 0.260);

        var guess = CalibreConfirmation.Guess(session.State);

        Assert.Equal(3, guess.HolesMeasured);
        Assert.Equal(0.204, guess.MedianHoleInches!.Value, 6);
    }

    /// <summary>A mark somebody said is not a shot is not a hole, so it is not evidence about the calibre either.</summary>
    [Fact]
    public void AMarkThatIsNotAShotIsNotEvidence()
    {
        var session = new MarkingSession(WithHoles(0.198, 0.204, 0.201, 0.900));
        session.SetNotAShot(4, true);

        Assert.Equal(3, CalibreConfirmation.Guess(session.State).HolesMeasured);
    }

    /// <summary>
    /// The gate itself: on a sheet of bulls with no calibre named, Accept is held, and the reason says what it costs rather than telling
    /// somebody to fill a field.
    /// </summary>
    [Fact]
    public void AcceptIsHeldUntilTheCalibreIsAnswered()
    {
        var state = WithHoles(0.198, 0.204, 0.201);

        string? held = CalibreConfirmation.WhyAcceptIsHeld(state, confirmed: false);

        Assert.NotNull(held);
        Assert.Contains("five holes", held, StringComparison.Ordinal);
    }

    /// <summary>Answering it lets Accept through, whether the answer was the guess or a correction of it.</summary>
    [Fact]
    public void AnsweringItLetsAcceptThrough()
    {
        var state = WithHoles(0.198, 0.204, 0.201);

        Assert.Null(CalibreConfirmation.WhyAcceptIsHeld(state with { Calibre = Calibre.Of(0.224) }, confirmed: false));
        Assert.Null(CalibreConfirmation.WhyAcceptIsHeld(state, confirmed: true));
    }

    /// <summary>
    /// A plain group, marked by hand on a photograph with no sheet, is not held. There are no bulls, no size gate reading the calibre and
    /// nothing for the answer to change, so asking would be a question with no purpose.
    /// </summary>
    [Fact]
    public void APlainGroupIsNotHeld()
    {
        var state = MarkingState.Empty with { Scale = new LengthReference(new PointD(0, 0), new PointD(100, 0), 1) };

        Assert.Null(CalibreConfirmation.WhyAcceptIsHeld(state, confirmed: false));
    }

    /// <summary>Once a calibre is set the screen says what it is being used for, so it does not read as a field nobody needs to fill.</summary>
    [Fact]
    public void TheCalibreSaysWhatItIsFor()
    {
        string said = CalibreConfirmation.WhatItIsFor(Calibre.Of(0.224));

        Assert.Contains("how small a mark may be", said, StringComparison.Ordinal);
        Assert.Contains("whether it is two", said, StringComparison.Ordinal);
    }
}
