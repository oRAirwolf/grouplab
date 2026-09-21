using System.Globalization;

namespace GroupLab.Core.Marking;

/// <summary>
/// What GroupLab thinks the calibre was, from the holes it measured, and how much that is worth.
/// </summary>
/// <param name="DiameterInches">The bullet diameter this reading suggests, or null where the holes cannot suggest one.</param>
/// <param name="Nearest">The listed calibre nearest that diameter, which is what the screen offers as a choice.</param>
/// <param name="HolesMeasured">How many holes the reading came from.</param>
/// <param name="MedianHoleInches">The median hole as measured, before the paper allowance is put back.</param>
/// <param name="Why">What the screen says about the guess, including what it cannot know.</param>
public sealed record CalibreGuess(double? DiameterInches, Calibre? Nearest, int HolesMeasured, double? MedianHoleInches, string Why);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 6.3: the analysis page shows GroupLab's best guess at the calibre from the measured hole sizes,
/// and Accept cannot proceed until the person confirms or corrects it.
/// <para>
/// <b>Tonight's re-run of the six range scans is the argument for this.</b> Naming the calibre is the difference between nineteen holes found
/// and twenty-four on scan 4, and between missing and finding the shot cut by the edge of the scan on scan 6. It is the single most valuable
/// thing a person can tell GroupLab about a sheet, and until now nothing ever asked them for it.
/// </para>
/// <para>
/// <b>The guess is offered and never applied.</b> A hole in paper is smaller than the bullet that made it, by an amount that varies with the
/// paper, the backing and the velocity, so a diameter read back from holes carries real uncertainty. Using it silently would be GroupLab
/// deciding a fact the shooter knows for certain, on evidence that is only suggestive, and then measuring everything else against it.
/// </para>
/// </summary>
public static class CalibreConfirmation
{
    /// <summary>
    /// How much smaller than the bullet a hole in paper reads, in inches: docs/SCAN-MEASUREMENTS.md section 3.5 measured 260 holes of known
    /// calibre at a mean of 0.0202 in below nominal. Put back on the median hole, it turns a measurement into a estimate of the bullet.
    /// </summary>
    public const double PaperShrinkInches = 0.0202;

    /// <summary>
    /// The fewest holes worth reading a calibre from. One hole is a hole; three begin to be a measurement, and the median of three is not
    /// thrown by a single merged pair or a torn rim.
    /// </summary>
    public const int FewestHoles = 3;

    /// <summary>How far the guess may sit from a listed calibre and still be offered as that one, in inches.</summary>
    public const double NearEnoughInches = 0.02;

    /// <summary>What GroupLab reads the calibre as from this marking's holes.</summary>
    public static CalibreGuess Guess(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var measured = state.Shots
            .Where(s => s.IsShot && s.DiameterInches is > 0)
            .Select(s => s.DiameterInches!.Value)
            .Order()
            .ToList();

        if (measured.Count < FewestHoles)
        {
            return new CalibreGuess(null, null, measured.Count, null,
                measured.Count == 0
                    ? "No hole on this sheet was measured, so GroupLab has nothing to read a calibre from. Enter the bullet's diameter."
                    : string.Create(CultureInfo.InvariantCulture,
                        $"Only {measured.Count} hole{(measured.Count == 1 ? " was" : "s were")} measured, which is too few to read a calibre from. Enter the bullet's diameter."));
        }

        double median = measured[measured.Count / 2];
        double diameter = median + PaperShrinkInches;
        var nearest = Calibre.Common.MinBy(c => Math.Abs(c.DiameterInches - diameter));
        if (nearest is not null && Math.Abs(nearest.DiameterInches - diameter) > NearEnoughInches)
        {
            nearest = null;
        }

        string why = string.Create(CultureInfo.InvariantCulture,
            $"From {measured.Count} holes measuring {median:0.000} in across the middle, and the {PaperShrinkInches:0.000} in that paper closes behind a bullet, this looks like about {Calibre.Shown(diameter)}. A hole is not the bullet, so check it: the reading moves with the paper, the backing and how fast the bullet was going.");

        return new CalibreGuess(diameter, nearest, measured.Count, median, why);
    }

    /// <summary>
    /// Why Accept cannot go ahead yet, or null where it can. Entry 131 section 6.3 makes the calibre a thing a person has to answer rather
    /// than a thing they may leave blank, because leaving it blank costs holes and nothing on the screen ever said so.
    /// </summary>
    /// <param name="state">The marking.</param>
    /// <param name="confirmed">Whether the person has confirmed or corrected the calibre on this marking.</param>
    public static string? WhyAcceptIsHeld(MarkingState state, bool confirmed)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (confirmed || state.Bulls.Count == 0)
        {
            return null;
        }

        return state.Calibre is null
            ? "Say what you were shooting before this is accepted. GroupLab reads smaller holes when it knows the calibre, and on one of the "
              + "test scans that was five holes it would otherwise have refused."
            : null;
    }

    /// <summary>
    /// The sentence beside the field once a calibre is set, saying what it is being used for, so a person can see it is not decoration.
    /// </summary>
    public static string WhatItIsFor(Calibre calibre)
    {
        ArgumentNullException.ThrowIfNull(calibre);
        return string.Create(CultureInfo.InvariantCulture,
            $"Holes are being read as {calibre.Name} bullets. That sets how small a mark may be and still count as a hole, and how large one may be before GroupLab asks whether it is two.");
    }
}
