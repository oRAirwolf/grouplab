using System.Globalization;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;

namespace GroupLab.Core.Marking;

/// <summary>
/// What to say when a sheet is not perfect, NOTES-FROM-PLANNING.md entry 115 section 4. Each sentence says what to do next rather than what
/// failed, because a person holding a scan of their own sheet can act on "the scan is 96 pixels to the inch; scan it again at 300 or more"
/// and cannot act on "registration failed". The print scale is said plainly as well, because a sheet printed at 97 percent is read in its
/// own inches and every size on it reads 3 percent large (entry 161, which corrected the comment that said it was measured correctly).
/// </summary>
public static class DetectionAdvice
{
    /// <summary>
    /// Below this, at the page, a scan has too few pixels to be worth trying again with. Measured on a synthetic GL-CF25-LTR: 300 and 150 dpi
    /// find every marker and every hole, 96 dpi finds 31 of 38 markers and 24 of 25 holes, 75 dpi finds 15 markers and the same 24 holes, and
    /// at 60 dpi nothing is found at all. So the detector copes further down than one would guess, and this is the figure below which it stops.
    /// </summary>
    public const double LowestUsefulDpi = 120;

    /// <summary>How many markers of another sheet may be seen before the image is probably not the sheet that was chosen.</summary>
    public const int StrayMarkers = 2;

    /// <summary>The share of a sheet's bulls that must be found where it puts them.</summary>
    public const double BullsFound = 0.8;

    /// <summary>
    /// How far the markers may sit from where the sheet says they are, in dmm, before the fit says this is another sheet. A sheet that matches
    /// fits to a few hundredths; a 5x5 read as a 5x6 fits to 1.63.
    /// </summary>
    public const double FitWorthDoubting = 1.0;

    /// <summary>A print scale further from 1 than this is worth saying out loud: a quarter of one percent.</summary>
    public const double ScaleWorthSaying = 0.0025;

    /// <summary>
    /// Why a measurement did not register, in words, with what to do next. Null where it registered.
    /// </summary>
    public static string? Failure(SheetMeasurement measurement, ImageMetadata metadata, TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(measurement);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(definition);
        if (measurement.Failure is null)
        {
            return null;
        }

        var inv = CultureInfo.InvariantCulture;
        var fiducials = measurement.Fiducials;
        int found = fiducials?.Matches.Count ?? 0, expected = fiducials?.Expected ?? 0;

        // The resolution the markers imply, or the file's own where nothing was found to measure with.
        double? dpi = fiducials is { PixelsPerDmm: > 0 } f ? f.PixelsPerDmm * 254 : Stated(metadata);
        if (dpi is { } resolution && resolution < LowestUsefulDpi)
        {
            return string.Create(inv,
                $"This image is about {resolution:0} pixels to the inch at the sheet, and the detector needs {LowestUsefulDpi:0}. Scan it at 300 dpi, or photograph it closer so the sheet fills the frame.");
        }

        if (found == 0 && expected > 0)
        {
            return string.Create(inv,
                $"None of this sheet's {expected} markers were found. Check it is the sheet you chose, {definition.Name}, that the whole sheet is in the picture with its corner squares, and that it is not too dark or blurred.");
        }

        if (found < 4)
        {
            return string.Create(inv,
                $"{found} of the sheet's {expected} markers were found, and registering needs 4. Take the picture again with the whole sheet in frame and square on, or scan it flat.");
        }

        if (fiducials is { Unexpected: > 0 } odd && odd.Unexpected >= found)
        {
            return string.Create(inv,
                $"{odd.Unexpected} markers here belong to another sheet, and {found} to {definition.Name}. Choose the sheet this image really is.");
        }

        return $"The sheet's markers were found but its page could not be fitted to them ({measurement.Failure}). Scan it flat, or photograph it square on with the whole sheet in frame.";
    }

    /// <summary>
    /// What the holes and the calibre disagree about, where they disagree enough to matter, or null. NOTES-FROM-PLANNING.md entry 161
    /// section 3.2: a friend's scan of ten 6.5 Creedmoor shots had holes 0.301 in across where the calibre predicted 0.249, and GroupLab
    /// quietly replaced its own measurement with the prediction and flagged five good holes. A disagreement that large is information the
    /// shooter wants, and saying it out loud is how that defect would have been caught.
    /// </summary>
    public static string? CalibreDisagrees(AutomaticResult result, Calibre? calibre)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (calibre is null
            || result.Difference?.HoleSize is not { MarksMedianInches: { } marks, CalibreHoleInches: { } expected } size
            || size.Source is HoleSizeSource.Calibre or HoleSizeSource.Bound
            || Math.Abs((marks / expected) - 1) <= RenderDifferenceHoleDetector.CalibreDisagrees)
        {
            return null;
        }

        // The detector's figures are the sheet's own inches; on a scan they are said in real ones, like every other size. Entry 171.
        double k = result.Scale?.PrintScale ?? 1;
        return string.Create(CultureInfo.InvariantCulture,
            $"These holes measure {marks * k:0.000} in across, and a {calibre.Name} bullet would be expected to make about {expected * k:0.000} in. GroupLab is judging one hole from two against the sheet's own marks, not the calibre.");
    }

    /// <summary>
    /// The line a photograph's figures carry, NOTES-FROM-PLANNING.md entry 171 section 1.2, word for word. A photograph has no absolute ruler,
    /// so it cannot measure the print scale, and its figures stay in the sheet's own inches.
    /// </summary>
    public const string SheetInches = "Measured in the sheet's own inches; if the sheet was not printed at actual size, the figures are off by the same percentage.";

    /// <summary>
    /// Why to print at actual size, entry 171 section 1.3: the one source the print screen, <c>docs/WHAT-CAN-BE-MEASURED.md</c> and the tour
    /// take it from, and a test holds all three to it.
    /// </summary>
    public const string WhyActualSize = "It matters for photographs, because a photograph cannot measure the print scale; a scan can and corrects for it.";

    /// <summary>
    /// What the print scale means for this sheet's figures, or null where there is nothing to say.
    /// <para>
    /// <b>On a scan the figures are real inches.</b> NOTES-FROM-PLANNING.md entry 171 section 1 answered question 49: a scan measures the print
    /// scale and every distance is multiplied by it, so the sentence names the scale and says the sizes are corrected, once it is more than a
    /// quarter of a percent from full size. On a photograph, or a scan whose stated resolution puts the sheet somewhere no printer would, the
    /// figures stay in the sheet's own inches and <see cref="SheetInches"/> says so. Until entry 171 nothing was corrected on either, and
    /// entry 161 section 6 had made this sentence say so.
    /// </para>
    /// </summary>
    public static string? PrintScale(SheetMeasurement measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement);
        if (measurement.Scale?.Scale is not { } scale)
        {
            return SheetInches;
        }

        if (SheetReference.Correction(measurement.Scale) is null)
        {
            return string.Create(CultureInfo.InvariantCulture,
                $"The file's stated resolution puts this sheet at {scale * 100:0.0} percent of its intended size, which is more likely a wrong resolution than a real print, so nothing is corrected. ") + SheetInches;
        }

        return Math.Abs(scale - 1) <= ScaleWorthSaying
            ? null
            : string.Create(CultureInfo.InvariantCulture,
                $"This sheet was printed at {scale * 100:0.0} percent of its intended size. The scan measured that, so every size here is corrected to real inches.");
    }

    /// <summary>
    /// What to say when the sheet's codes name one definition and a person chose another: the codes are the sheet's own word for what it is.
    /// </summary>
    public static string WrongSheet(string codesSay, TargetDefinition chosen)
    {
        ArgumentNullException.ThrowIfNull(chosen);
        return $"This sheet's own codes say it is {codesSay}, and {chosen.Name} was chosen. Analysing it as {chosen.Name} would measure the wrong bulls, so nothing was analysed. Choose {codesSay}, or open the sheet this image really is.";
    }

    /// <summary>
    /// A sheet that registered, but whose evidence says it may not be the sheet that was chosen, NOTES-FROM-PLANNING.md entry 115 section 4.
    /// Reading a sheet as another definition used to give numbers with nothing said: a 5x5 read as a 5x6 fitted its markers to 1.63 dmm where
    /// its own sheet fits to 0.14, a 5x5 read as an A4 5x5 found 17 of 28 bulls and 15 holes, and a 5x5 read as a zeroing grid saw 14 markers
    /// that grid does not have. Each of those is said here. It is a warning and not a refusal, because a damaged or marked-up sheet can look
    /// like this too, and the person can see the sheet and GroupLab cannot.
    /// </summary>
    public static string? Suspect(SheetMeasurement measurement, TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(measurement);
        ArgumentNullException.ThrowIfNull(definition);
        if (measurement.Failure is not null)
        {
            return null;
        }

        var inv = CultureInfo.InvariantCulture;
        var reasons = new List<string>();
        if (measurement.Fiducials is { Unexpected: var stray } && stray >= StrayMarkers)
        {
            reasons.Add(string.Create(inv, $"{stray} markers here are not this sheet's"));
        }

        int found = measurement.Bulls.Count(b => b.Recovered is not null), bulls = measurement.Bulls.Count;
        if (bulls > 0 && found < BullsFound * bulls)
        {
            reasons.Add(string.Create(inv, $"only {found} of its {bulls} bulls were found where it puts them"));
        }

        if (measurement.Registration is { RmsResidual: var residual } && residual > FitWorthDoubting)
        {
            reasons.Add(string.Create(inv, $"its markers fit the page to {residual / 254:0.000} in, where a sheet that matches fits to a few thousandths"));
        }

        return reasons.Count == 0
            ? null
            : $"This may not be {definition.Name}: " + string.Join(", and ", reasons) + ". Check the sheet, and choose the one this image really is; the figures mean nothing if it is another sheet.";
    }

    /// <summary>The resolution the image file states, where it states one worth trusting.</summary>
    private static double? Stated(ImageMetadata metadata) =>
        metadata.IsCamera || metadata.DpiX is not { } dpi || dpi <= 0 ? null : dpi;
}
