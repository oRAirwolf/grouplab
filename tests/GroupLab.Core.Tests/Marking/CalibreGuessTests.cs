using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// Alan's requirement of 2026-09-22: the calibre GroupLab guesses at snaps to something somebody actually shoots, it says which list it came
/// from, and it never states a diameter as measured when this sheet's holes cannot tell it from its neighbour.
/// <para>
/// <b>Why a list at all.</b> Reading 0.2371 in off a sheet and offering "0.237 in" is arithmetic dressed as knowledge: nobody loads a .237.
/// Offering .243 with .224 beside it is a question a shooter can answer in a second, and the answer is worth five holes on one of the range
/// scans. The list limits the guess and not the person: any diameter at all can still be typed.
/// </para>
/// <para>
/// <b>Why "cannot tell apart" is not a number in this file.</b> The window comes from the spread of the sheet's own single holes, so a sheet
/// that measures consistently earns a narrow one and a ragged sheet is told it is ragged. A fixed tolerance would say the same thing about a
/// tidy .308 sheet and a wind-blown .22 sheet whose holes range over a tenth of an inch.
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

    [Theory]
    [InlineData(0.172)]
    [InlineData(0.510)]
    [InlineData(0.243)]
    [InlineData(0.308)]
    public void ASheetOfOneRifleCalibreSnapsToThatCalibre(double bullet)
    {
        var guess = CalibreConfirmation.Guess(Tight(bullet));

        Assert.Equal(bullet, guess.Nearest!.DiameterInches, 6);
        Assert.Contains(CalibreGuessList.Rifle, d => Math.Abs(d - guess.Nearest.DiameterInches) < 1e-9);
    }

    [Theory]
    [InlineData(0.312)]
    [InlineData(0.500)]
    [InlineData(0.430)]
    public void APistolSheetSnapsToThePistolList(double bullet)
    {
        var guess = CalibreConfirmation.Guess(Tight(bullet), FirearmType.Pistol);

        Assert.Equal(bullet, guess.Nearest!.DiameterInches, 6);
        Assert.Contains(CalibreGuessList.Pistol, d => Math.Abs(d - guess.Nearest.DiameterInches) < 1e-9);
    }

    /// <summary>
    /// The same holes, guessed from the two lists, give different answers, which is the whole reason the lists are separate: .312 is not on
    /// the rifle list and .308 is not on the pistol one, and a sheet shot with a revolver should not be told it was a .308.
    /// </summary>
    [Fact]
    public void TheSameHolesReadDifferentlyForARifleAndAPistol()
    {
        var holes = Tight(0.310);

        Assert.Equal(0.308, CalibreConfirmation.Guess(holes, FirearmType.Rifle).Nearest!.DiameterInches, 6);
        Assert.Equal(0.312, CalibreConfirmation.Guess(holes, FirearmType.Pistol).Nearest!.DiameterInches, 6);
    }

    /// <summary>
    /// The .22 rimfire is on the list by the name a shooter uses, because "0.222 in" is not what anybody calls a box of it, and its
    /// millimetre value is there like every other.
    /// </summary>
    [Fact]
    public void TheRimfireIsNamedTheWayAShooterNamesIt()
    {
        var guess = CalibreConfirmation.Guess(Tight(0.222));

        Assert.Equal(0.222, guess.Nearest!.DiameterInches, 6);
        Assert.StartsWith("22LR, ", guess.Nearest.Name, StringComparison.Ordinal);
        Assert.Contains("5.64 mm", guess.Nearest.Name, StringComparison.Ordinal);
        Assert.All(CalibreGuessList.Rifle.Select(CalibreGuessList.Of), c => Assert.Contains("mm", c.Name, StringComparison.Ordinal));
    }

    /// <summary>
    /// The close pairs. Holes never measure to a thousandth in practice, so .222 against .224 and .308 against .312 are offered together,
    /// with the likelier one first. Stating either as measured would be a claim the sheet cannot support.
    /// </summary>
    [Theory]
    [InlineData(0.224, 0.222, FirearmType.Rifle)]
    [InlineData(0.451, 0.458, FirearmType.Pistol)]
    [InlineData(0.308, 0.312, FirearmType.Rifle)]
    [InlineData(0.500, 0.510, FirearmType.Pistol)]
    [InlineData(0.400, 0.410, FirearmType.Pistol)]
    public void ANeighbourTooCloseToSeparateIsOfferedBesideIt(double bullet, double neighbour, FirearmType firearm)
    {
        // Five holes with an ordinary spread, about a hundredth of an inch, which is tighter than any real sheet measured here.
        var holes = Sheet([.. new[] { -0.006, 0.006, 0.0, -0.004, 0.004 }.Select(off => bullet - CalibreConfirmation.PaperShrinkInches + off)]);

        var guess = CalibreConfirmation.Guess(holes, firearm);

        Assert.Equal(bullet, guess.Nearest!.DiameterInches, 6);
        Assert.Contains(guess.Neighbours!, c => Math.Abs(c.DiameterInches - neighbour) < 1e-9);
        Assert.True(guess.Rough, "a guess that cannot separate its neighbours is not a firm one");
        Assert.Equal(guess.Nearest, guess.Offered[0]);
        Assert.Contains("pick the one you fired", guess.Why, StringComparison.Ordinal);
    }

    /// <summary>
    /// The window moves with the sheet: the same calibre measured tidily offers fewer neighbours than measured raggedly, which is the
    /// difference between a scan of a flat sheet and a photograph of a windy day's work.
    /// <para>
    /// A tidy sheet does not offer none. The estimator's own error is about seven thousandths whatever the holes do, because putting back a
    /// fixed allowance for the paper assumes this paper closes like the paper it was measured on, and .257 is only thirteen thousandths from
    /// .264. A sheet cannot measure that error, so it never earns its way below it.
    /// </para>
    /// </summary>
    [Fact]
    public void ATidySheetOffersFewerNeighboursThanARaggedOne()
    {
        double bullet = 0.264 - CalibreConfirmation.PaperShrinkInches;
        var tidy = Sheet([.. new[] { -0.0005, 0.0005, 0.0, -0.0004, 0.0004 }.Select(off => bullet + off)]);
        var ragged = Sheet([.. new[] { -0.03, 0.03, 0.0, -0.02, 0.02 }.Select(off => bullet + off)]);

        var few = CalibreConfirmation.Guess(tidy);
        var many = CalibreConfirmation.Guess(ragged);

        Assert.True(few.Neighbours!.Count < many.Neighbours!.Count, $"tidy offered {few.Neighbours.Count}, ragged offered {many.Neighbours.Count}");
        Assert.Equal(0.264, few.Nearest!.DiameterInches, 6);
        Assert.Equal(0.264, many.Nearest!.DiameterInches, 6);

        // Even at its tidiest it does not claim to tell .264 from .257, and it never claims to tell it from something a tenth away.
        Assert.Contains(few.Neighbours, c => Math.Abs(c.DiameterInches - 0.257) < 1e-9);
        Assert.DoesNotContain(few.Neighbours, c => Math.Abs(c.DiameterInches - 0.172) < 1e-9);
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
    /// depending on the light they were taken in, so there is no diameter in one to read. It says so and offers the list.
    /// </summary>
    [Fact]
    public void APhotographGuessesNothingAndSaysWhy()
    {
        var guess = CalibreConfirmation.Guess(Tight(0.264), FirearmType.Rifle, fromPhotograph: true);

        Assert.Null(guess.Nearest);
        Assert.True(guess.Rough);
        Assert.Contains("this is a photograph", guess.Why, StringComparison.Ordinal);
        Assert.Contains("will not guess a calibre from it", guess.Why, StringComparison.Ordinal);
        Assert.Equal(CalibreGuessList.Rifle.Count, guess.Offered.Count);
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
