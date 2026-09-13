using System.Globalization;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Registration;

/// <summary>One page of conformance test 43: its registration and every bull it recovered.</summary>
public sealed record SyntheticScanReport(int TileIndex, double Dpi, RegistrationResult Registration, IReadOnlyList<BullRecovery> Bulls)
{
    public BullRecovery? WorstBull => Bulls.MaxBy(b => b.Error);

    /// <summary>
    /// Registered, with the residual over the inlier corners within the gate, and every bull within the gate. The
    /// residual is gated as a root mean square because that is how DETECTION-PIPELINE.md stage S3 measures and quotes
    /// registration residual; the largest single corner is reported beside it.
    /// </summary>
    public bool Passed =>
        Registration.ImageToPage is not null
        && Registration.RmsResidual < SyntheticScanCheck.Gate
        && WorstBull is { } worst && worst.Error < SyntheticScanCheck.Gate;

    public string Summary()
    {
        var r = Registration;
        var inv = CultureInfo.InvariantCulture;
        string worst = WorstBull is { } w
            ? string.Create(inv, $"{w.Error / 254:0.00000} in at {w.Label ?? w.Index.ToString(inv)}")
            : "none";
        string failure = r.Failure is null ? "" : "  " + r.Failure;
        return string.Create(inv,
            $"tile {TileIndex}  {Dpi:0} dpi  markers {r.MarkersFound}/{r.MarkersExpected}  inlier corners {r.Inliers}/{4 * r.MarkersFound}  " +
            $"residual rms {r.RmsResidual / 254:0.00000} in, max {r.MaxResidual / 254:0.00000} in  worst bull {worst}  " +
            $"scale {r.Scale:0.0000} (x {r.ScaleX:0.0000}, y {r.ScaleY:0.0000}){failure}");
    }
}

/// <summary>
/// Conformance test 43 of TARGET-SCHEMA.md section 10, the Phase 0a gate of PHASE0-BRIEF.md section 3: take a clean
/// render, distort it as a scanner would, then find the markers, register, and recover every bull centre at its declared
/// coordinate within the Phase 0 residual gate.
/// </summary>
public static class SyntheticScanCheck
{
    /// <summary>The Phase 0 residual gate of DESIGN.md, one thousandth of an inch, in dmm.</summary>
    public const double Gate = 0.254;

    public static SyntheticScanReport Run(GrayImage render, TargetDefinition definition, int tileIndex, double dpi, Perturbation perturbation, IImagingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(render);
        ArgumentNullException.ThrowIfNull(perturbation);
        ArgumentNullException.ThrowIfNull(backend);
        var (transform, width, height) = perturbation.For(render.Width, render.Height, dpi);
        var scan = backend.WarpPerspective(render, transform, width, height);
        var registration = PageRegistration.Register(scan, definition, tileIndex, dpi, backend);
        var bulls = registration.ImageToPage is { } h ? BullLocator.Locate(scan, definition, h) : [];
        return new SyntheticScanReport(tileIndex, dpi, registration, bulls);
    }
}
