using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Capture;

/// <summary>What a camera frame showed of a sheet before GroupLab knew which sheet it is.</summary>
/// <param name="Definition">The sheet, from its codes or from its markers' layout; null while neither names one.</param>
/// <param name="FromCodes">Whether the codes named it, which is certain; the markers' layout is only good enough to guide the camera.</param>
/// <param name="MarkersFound">Markers decoded in the frame.</param>
/// <param name="MedianSidePixels">Their median side, in frame pixels; 0 with none.</param>
/// <param name="CodesRead">Square codes read in the frame.</param>
public sealed record LiveSearch(TargetDefinition? Definition, bool FromCodes, int MarkersFound, double MedianSidePixels, int CodesRead);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 260: which sheet the camera is looking at, from one frame of the analysis stream. The codes are tried first,
/// since a code that passes its check names exactly one definition (<see cref="SheetIdentification"/>). But a code's modules are about a
/// pixel each at a camera's analysis resolution unless the sheet fills the frame, so on 2026-09-28 the Fold 7 never read one, never knew the
/// sheet, and the capture screen said "Move back" to a sheet lying in plain view. So the markers are tried as well: each library sheet whose
/// markers carry the ids found is fitted by a homography from its marker centres to the found ones, and the sheet fitting the most markers
/// most closely is taken. Sheets that share ids share their layout, so the one chosen is the right shape to guide the camera by; the picture
/// itself is identified from its codes at full resolution afterwards.
/// </summary>
public static class LiveSheet
{
    /// <summary>The fewest markers a layout is fitted from.</summary>
    public const int LeastMarkers = 6;

    /// <summary>A layout fits when its markers land, on average, within this share of a marker's side of where they were found.</summary>
    public const double FitWithinSides = 0.5;

    /// <summary>
    /// The marker side, in pixels, below which a sheet's codes are too small to read. A built-in sheet's marker is 40 dmm and its code's
    /// module 4 dmm, so a module is a tenth of a marker's side, and at 20 pixels a side a module gets 2 pixels, about the least a QR
    /// reader decodes from.
    /// </summary>
    public const double ReadableSidePixels = 20;

    /// <summary>The longest page among the built-in sheets, in dmm, Letter's 11 in, for the first guess at a marker's size.</summary>
    private const double LongestPageDmm = 2794;

    public static LiveSearch Find(GrayImage frame, IReadOnlyList<TargetDefinition> candidates, IImagingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(backend);

        // The first guess sizes the detector for a Letter sheet across half the frame, as the measurer does for a photograph.
        double guess = 0.5 * Math.Max(frame.Width, frame.Height) / LongestPageDmm * 40;
        var found = backend.DetectMarkers(frame, new MarkerDetectionOptions(MarkerFamily.AprilTag36h11, guess)).Markers;
        double median = found.Count == 0 ? 0 : found.Select(Side).Order().ElementAt(found.Count / 2);

        var codes = backend.ReadCodes(frame, 1.0);
        var named = codes.Select(p => GltdBinary.Decode([p]).DefinitionId).OfType<string>().Distinct(StringComparer.Ordinal).ToList();
        if (named.Count == 1 && candidates.FirstOrDefault(c => GltdBinary.Encode(c).Encoding?.DefinitionId == named[0]) is { } fromCodes)
        {
            return new LiveSearch(fromCodes, true, found.Count, median, codes.Count);
        }

        return new LiveSearch(found.Count >= LeastMarkers ? ByLayout(found, median, candidates) : null, false, found.Count, median, codes.Count);
    }

    /// <summary>The candidate whose marker layout fits the found markers best: most markers fitted, then the closest fit.</summary>
    public static TargetDefinition? ByLayout(IReadOnlyList<DetectedMarker> found, double sidePixels, IReadOnlyList<TargetDefinition> candidates)
    {
        ArgumentNullException.ThrowIfNull(found);
        ArgumentNullException.ThrowIfNull(candidates);
        TargetDefinition? best = null;
        int bestCount = 0;
        double bestResidual = double.MaxValue;
        foreach (var candidate in candidates)
        {
            if (candidate.Fiducials?.Markers is not { Count: > 0 } markers)
            {
                continue;
            }

            var byId = markers.GroupBy(m => m.Id).Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.First());
            var page = new List<PointD>();
            var image = new List<PointD>();
            foreach (var marker in found)
            {
                if (byId.TryGetValue(marker.Id, out var at))
                {
                    page.Add(new PointD(at.X, at.Y));
                    image.Add(Centre(marker));
                }
            }

            if (page.Count < LeastMarkers || HomographyEstimate.Fit(page, image) is not { } fit)
            {
                continue;
            }

            double residual = Math.Sqrt(page.Select((p, i) => Squared(fit.Apply(p), image[i])).Average()) / Math.Max(1, sidePixels);
            if (residual <= FitWithinSides && (page.Count > bestCount || (page.Count == bestCount && residual < bestResidual)))
            {
                (best, bestCount, bestResidual) = (candidate, page.Count, residual);
            }
        }

        return best;
    }

    private static double Squared(PointD a, PointD b) => ((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y));

    private static PointD Centre(DetectedMarker marker) =>
        new(marker.Corners.Average(c => c.X), marker.Corners.Average(c => c.Y));

    private static double Side(DetectedMarker marker) =>
        Enumerable.Range(0, marker.Corners.Count).Average(i => Math.Sqrt(Squared(marker.Corners[i], marker.Corners[(i + 1) % marker.Corners.Count])));
}
