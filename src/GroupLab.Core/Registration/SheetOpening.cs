using GroupLab.Core.Imaging;

namespace GroupLab.Core.Registration;

/// <summary>
/// What opening a picture came to, NOTES-FROM-PLANNING.md entry 356 section 5, in its order: a GroupLab sheet whose codes read; a store-bought
/// target its fingerprint recognizes; a picture that looks like a GroupLab sheet but whose codes would not read, which is an error and says
/// so; and anything else, which is not an error at all but a target GroupLab has not been told about.
/// </summary>
public enum OpeningOutcome
{
    /// <summary>A GroupLab sheet whose codes named it: carry on.</summary>
    Sheet,

    /// <summary>A store-bought target the fingerprints recognize: carry on, with the scale note (entries 340 and 341).</summary>
    StoreTarget,

    /// <summary>It has a GroupLab sheet's markers or codes, and its codes would not name it: the problem dialog.</summary>
    LooksLikeGroupLab,

    /// <summary>Anything else: "Which target is this?", calm, with no warning and no amber.</summary>
    NotGroupLab,
}

/// <summary>
/// What of a GroupLab sheet a picture shows when its codes do not name it: how many of its square markers decode, and where QR codes sit,
/// read or not, in image pixels.
/// <para>
/// Entry 356 section 5 named a third sign, a printed "GL-" name seen. GroupLab has no reader of printed words, so it is not used here; the
/// markers and the codes are what a sheet carries that nothing else does. An AprilTag decodes only against its own family's code book, so a
/// store-bought target, which prints none, cannot show one by chance; a QR code alone can be a maker's, on its package or its corner, so it
/// counts only where it cannot be read as anything, or beside a marker.
/// </para>
/// </summary>
public sealed record SheetLook(int Markers, IReadOnlyList<IReadOnlyList<PointD>> Codes, int CodesRead)
{
    /// <summary>The fewest decoded markers that make a picture a GroupLab sheet on their own: no store-bought target prints any.</summary>
    public const int LeastMarkers = 2;

    /// <summary>The fewest QR codes, located and read as nothing, that make a picture look like a GroupLab sheet with no marker seen.</summary>
    public const int LeastUnreadCodes = 2;

    /// <summary>
    /// Whether the picture looks like a GroupLab sheet: two markers or more; or one marker and a located code; or two codes located that
    /// read as nothing.
    /// </summary>
    public bool LooksLikeGroupLab =>
        Markers >= LeastMarkers
        || (Markers >= 1 && Codes.Count >= 1)
        || Codes.Count - CodesRead >= LeastUnreadCodes;

    /// <summary>
    /// Looks at a picture for a GroupLab sheet's marks: its markers, sized for a sheet across half the picture, double that and the far
    /// guess, and its codes, located with or without decoding; <paramref name="codesRead"/> is how many codes the identification read as
    /// anything at all.
    /// </summary>
    public static SheetLook Of(GrayImage image, IImagingBackend backend, int codesRead = 0)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(backend);
        int markers = 0;
        double guess = 0.5 * Math.Max(image.Width, image.Height) / 2794.0 * 40;
        foreach (double side in new[] { guess, 2 * guess, guess * Measurement.SheetMeasurer.FarGuess })
        {
            var found = backend.DetectMarkers(image, new MarkerDetectionOptions(MarkerFamily.AprilTag36h11, side));
            // Entry 365: scale markers beside a target are not a GroupLab sheet's; their codes are kept apart for that.
            markers = Math.Max(markers, found.Markers.Select(m => m.Id).Concat(found.Rejected.Select(r => r.Id)).Where(id => !ScaleMarkers.ScaleMarkerLayout.IsMarker(id)).Distinct().Count());
            if (markers >= LeastMarkers)
            {
                break;
            }
        }

        return new SheetLook(markers, backend.LocateCodes(image), codesRead);
    }
}

/// <summary>The rule of entry 356 section 5, applied to what the opening found.</summary>
public static class SheetOpening
{
    /// <param name="identity">What the codes said.</param>
    /// <param name="storeTargetRecognized">Whether a store-bought target's fingerprint was recognized.</param>
    /// <param name="look">What of a GroupLab sheet the picture shows; asked for only where the first two did not settle it.</param>
    public static OpeningOutcome Decide(SheetIdentity identity, bool storeTargetRecognized, Func<SheetLook> look)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(look);
        if (identity.Definition is not null)
        {
            return OpeningOutcome.Sheet;
        }

        if (storeTargetRecognized)
        {
            return OpeningOutcome.StoreTarget;
        }

        // Codes that read as a GroupLab frame are a GroupLab sheet, whatever else the picture shows.
        return identity.DefinitionId is not null || look().LooksLikeGroupLab ? OpeningOutcome.LooksLikeGroupLab : OpeningOutcome.NotGroupLab;
    }
}
