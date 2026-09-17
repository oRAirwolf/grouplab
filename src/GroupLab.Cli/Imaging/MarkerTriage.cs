using GroupLab.Core.Imaging;

namespace GroupLab.Cli.Imaging;

/// <summary>
/// How many GroupLab markers a photograph shows, for intake's triage, NOTES-FROM-PLANNING.md entry 27 section 1 as entry 76 section 4
/// corrected it. Triage used to try three guesses of a marker's side, a 20th, 40th and 80th of the image's long side, and hold a frame on
/// what they found. A sheet photographed from where a person stands puts its markers at a 110th to a 175th of the long side, so four of six
/// frames of submission 3a493942 were held with 0 to 3 markers although every one registers with 33 to 38. The guesses now reach a 320th,
/// and wherever a guess decodes anything, detection runs again at the median side of what it found, which is the bootstrap the measurement
/// itself uses (DETECTION-PIPELINE.md stage S2). The count is the most distinct markers any pass decoded.
/// </summary>
public static class MarkerTriage
{
    /// <summary>First guesses of a marker's side, as fractions of the image's long side.</summary>
    public static IReadOnlyList<double> Fractions { get; } = [20, 40, 80, 160, 320];

    public static int Count(GrayImage image, IImagingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(backend);
        int best = 0;
        foreach (double fraction in Fractions)
        {
            var first = backend.DetectMarkers(image, Options(Math.Max(image.Width, image.Height) / fraction));
            best = Math.Max(best, Distinct(first));
            if (first.Markers.Count > 0)
            {
                double[] sides = [.. first.Markers.Select(MeanSide).Order()];
                best = Math.Max(best, Distinct(backend.DetectMarkers(image, Options(sides[sides.Length / 2]))));
            }
        }

        return best;
    }

    private static MarkerDetectionOptions Options(double side) => new(MarkerFamily.AprilTag36h11, side);

    private static int Distinct(MarkerDetection detection) => detection.Markers.Select(m => m.Id).Distinct().Count();

    private static double MeanSide(DetectedMarker marker) =>
        Enumerable.Range(0, 4).Average(k => Math.Sqrt(Math.Pow(marker.Corners[k].X - marker.Corners[(k + 1) % 4].X, 2) + Math.Pow(marker.Corners[k].Y - marker.Corners[(k + 1) % 4].Y, 2)));
}
