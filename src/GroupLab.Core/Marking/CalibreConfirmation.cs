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
/// <param name="Neighbours">
/// The listed diameters this sheet's holes cannot be told apart from <see cref="Nearest"/>, nearest first. They go beside the preselected one
/// in the confirmation step, because a calibre stated as measured when the evidence cannot separate it from its neighbour is a false claim.
/// </param>
/// <param name="Rough">
/// Whether this reading is too rough to preselect anything from. A photograph is always rough: question 38 measured the same holes at 0.90 to
/// 1.45 times the bullet depending on the light they were photographed in, so no absolute diameter can be read from one.
/// </param>
/// <param name="FromTheLoad">Whether the diameter came from the load record rather than from the holes, in which case nothing was guessed.</param>
public sealed record CalibreGuess(
    double? DiameterInches,
    Calibre? Nearest,
    int HolesMeasured,
    double? MedianHoleInches,
    string Why,
    IReadOnlyList<Calibre>? Neighbours = null,
    bool Rough = false,
    bool FromTheLoad = false)
{
    /// <summary>The preselected calibre and the ones beside it, in the order the confirmation step offers them.</summary>
    public IReadOnlyList<Calibre> Offered => Nearest is null ? Neighbours ?? [] : [Nearest, .. Neighbours ?? []];
}

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

    /// <summary>
    /// How many standard errors of the median two listed diameters must be apart before this sheet can be said to tell them apart. Two is
    /// the usual reading of "the evidence separates these", and nothing here is a fixed distance in inches: the window comes from the spread
    /// of this sheet's own single holes, so a sheet that measures consistently earns a narrow one and a ragged sheet does not.
    /// </summary>
    public const double SeparatingErrors = 2.0;

    /// <summary>
    /// The standard error of a median, as a multiple of the standard error of a mean, for a normal population: the median of n values is
    /// about 1.25 times as noisy as their mean, which is the price of a statistic that a single merged pair cannot throw.
    /// </summary>
    public const double MedianPenalty = 1.2533;

    /// <summary>
    /// How far this estimator lands from the truth on a sheet whose holes are all alike, in inches, whatever the sheet's own spread says.
    /// <para>
    /// <b>The sheet's own scatter cannot see this and it is the larger term.</b> Putting back a fixed allowance for the paper assumes this
    /// paper, this backing and this velocity close a hole like the ones the allowance was measured on. On the four range scans of known
    /// calibre, the estimate landed 0.0016, 0.0035 and 0.0068 in high on the three centrefire sheets, and 0.034 in low on the rimfire one.
    /// Seven thousandths is the centrefire spread of that, and it is why .451 and .458 are offered together however tidily a sheet measures.
    /// </para>
    /// <para>
    /// The rimfire miss is far larger and is not covered here: question 38 records that the hole-to-bullet ratio is not calibre-independent,
    /// which needs its own measurement across more small-calibre sheets than the one there was.
    /// </para>
    /// </summary>
    public const double EstimatorErrorInches = 0.007;

    /// <summary>What GroupLab reads the calibre as from this marking's holes.</summary>
    /// <param name="state">The marking.</param>
    /// <param name="firearm">Which list to guess from, taken from the rifle chosen on the Equipment screen.</param>
    /// <param name="loadDiameterInches">
    /// The bullet diameter the chosen load records, where it has one. It wins outright: a figure the shooter wrote down beats anything read
    /// off paper, and there is then nothing to guess.
    /// </param>
    /// <param name="fromPhotograph">
    /// Whether the marks were read from a photograph. Question 38 measured photographs of sheets of known calibre at 0.90 to 1.45 times the
    /// bullet, sheet by sheet, against 0.92 to 0.95 on scans, and showed the difference is the light rather than the medium. So a photograph
    /// supports no absolute diameter at all, and this preselects nothing from one.
    /// </param>
    public static CalibreGuess Guess(MarkingState state, FirearmType firearm = FirearmType.Rifle, double? loadDiameterInches = null, bool fromPhotograph = false)
    {
        ArgumentNullException.ThrowIfNull(state);

        // Entry, 2026-09-22 requirement 2: a diameter on the load record is not a guess, and nothing is read from the holes to second-guess it.
        if (loadDiameterInches is > 0)
        {
            var told = Calibre.Of(loadDiameterInches.Value);
            return new CalibreGuess(loadDiameterInches, told, 0, null,
                "The load you chose records a bullet diameter of " + told.Name + ", so GroupLab is using it rather than reading one off the holes.",
                [], false, true);
        }

        // Only holes nobody has flagged as possibly two: a merged pair measures like nothing on the sheet and would widen the spread that
        // decides how sure this can be.
        var measured = state.Shots
            .Where(s => s.IsShot && s.Oversize is null && s.DiameterInches is > 0)
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

        // How far apart two diameters must be before this sheet can tell them apart, from this sheet's own holes and nothing else.
        double mean = measured.Average();
        double spread = measured.Count > 1
            ? Math.Sqrt(measured.Sum(v => Math.Pow(v - mean, 2)) / (measured.Count - 1))
            : 0;
        double sampling = MedianPenalty * spread / Math.Sqrt(measured.Count);
        double window = SeparatingErrors * Math.Sqrt((sampling * sampling) + (EstimatorErrorInches * EstimatorErrorInches));

        // A photograph supports no absolute diameter, so it preselects nothing and says why rather than offering a number to be trusted.
        if (fromPhotograph)
        {
            return new CalibreGuess(diameter, null, measured.Count, median,
                string.Create(CultureInfo.InvariantCulture,
                    $"These {measured.Count} holes measure {median:0.000} in across the middle, but this is a photograph, and a hole photographed in low light reads far wider than the same hole scanned: on sheets of known calibre the reading ran from 0.9 to 1.45 times the bullet depending on the light. So GroupLab will not guess a calibre from it. Say what you were shooting."),
                [.. CalibreGuessList.For(firearm).Select(CalibreGuessList.Of)], true);
        }

        var nearest = CalibreGuessList.Nearest(diameter, firearm);
        var neighbours = CalibreGuessList.Either
            .Where(d => Math.Abs(d - nearest.DiameterInches) > 1e-9 && Math.Abs(d - diameter) <= window)
            .OrderBy(d => Math.Abs(d - diameter))
            .Select(CalibreGuessList.Of)
            .ToList();

        string why = string.Create(CultureInfo.InvariantCulture,
            $"From {measured.Count} holes measuring {median:0.000} in across the middle, and the {PaperShrinkInches:0.000} in that paper closes behind a bullet, this looks most like {nearest.Name}. A hole is not the bullet, so check it: the reading moves with the paper, the backing and how fast the bullet was going.")
            + (neighbours.Count > 0
                ? " These holes vary too much to tell it from " + Listed(neighbours) + ", so those are beside it: pick the one you fired."
                : "");

        return new CalibreGuess(diameter, nearest, measured.Count, median, why, neighbours, neighbours.Count > 0);
    }

    /// <summary>A list of calibres read as a sentence would say them.</summary>
    private static string Listed(IReadOnlyList<Calibre> calibres) => calibres.Count switch
    {
        0 => "",
        1 => calibres[0].Name,
        _ => string.Join(", ", calibres.Take(calibres.Count - 1).Select(c => c.Name)) + " or " + calibres[^1].Name,
    };


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
