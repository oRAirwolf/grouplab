using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;

namespace GroupLab.Cli.Spike;

/// <summary>
/// docs/PHASE1-BRIEF.md section 4.1: the neutral-darkness hole detector, ported into Core, revalidated on the survey's
/// corpus before anything is built to beat it. Every scan in <c>scans/</c> the survey measured is run with the survey's own
/// settings, and compared with docs/SCAN-MEASUREMENTS.md section 3.7 per file and section 3.2 pooled. The messaging-app copy
/// is run at its derived 93 DPI with the same threshold, which the survey reports in section 5.3, and outside the pool, as
/// the survey kept it. On <c>300_nm_hand_load.jpg</c> the two holes the survey's detector missed and a person located, in
/// section 8, are checked by position.
/// </summary>
public static class HolesBaseline
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly object ProgressLock = new();

    private sealed record Survey(string File, string Label, int Holes, double Diameter, double DiameterSd, double CoreV, double AnnulusMinimumV);

    /// <summary>docs/SCAN-MEASUREMENTS.md section 3.7: accepted holes, hull diameter mean and sd in inches, core mean V, annulus minimum V.</summary>
    private static readonly Survey[] PerFile =
    [
        new("28_6_5_cm_153_5_lrht_40_3_h4350_starline_srp_cci_450_magnus_s.jpg", "28_6_5 cci_450", 16, 0.2201, 0.0319, 195.4, 24.8),
        new("28_6_5_cm_153_5_lrht_40_3_h4350_starline_srp_gm205mar_magnus_s.jpg", "28_6_5 40_3 gm205mar", 25, 0.2388, 0.0349, 194.1, 31.5),
        new("28_6_5_cm_153_5_lrht_40_3_h4350_starline_srp_rem_7_5_br_magnus_s.jpg", "28_6_5 rem_7_5_br", 24, 0.2188, 0.0279, 190.0, 36.3),
        new("28_6_5_cm_153_5_lrht_42_4_h4350_starline_srp_gm205mar_magnus_s.jpg", "28_6_5 42_4 gm205mar", 20, 0.2410, 0.0715, 194.8, 48.0),
        new("300_nm_factory.jpg", "300_nm_factory", 17, 0.2649, 0.0408, 205.3, 16.4),
        new("300_nm_hand_load.jpg", "300_nm_hand_load", 25, 0.2958, 0.0376, 198.0, 18.9),
        new("338lmao.jpg", "338lmao", 28, 0.3236, 0.0768, 187.8, 68.0),
        new("6_5retumbo.jpg", "6_5retumbo", 19, 0.2256, 0.0572, 193.6, 54.6),
        new("IMG_20250530_0001.jpg", "IMG_20250530_0001 (300 DPI)", 7, 0.1988, 0.0761, 169.9, 21.7),
        new("n568-gm210m.jpg", "n568-gm210m", 28, 0.2366, 0.0259, 197.8, 41.7),
        new("n568-ruag.jpg", "n568-ruag", 28, 0.2435, 0.0267, 199.3, 48.1),
        new("n568.jpg", "n568", 29, 0.2961, 0.0241, 194.4, 41.2),
        new("retumbo.jpg", "retumbo.jpg", 25, 0.3033, 0.0213, 186.6, 21.6),
        new("retumbo.png", "retumbo.png", 25, 0.3110, 0.0421, 178.6, 18.9),
        new("retumbo_0001.jpg", "retumbo_0001", 27, 0.2773, 0.0559, 191.0, 60.4),
    ];

    /// <summary>The messaging-app copy, docs/SCAN-MEASUREMENTS.md section 5: no DPI tag, 93 DPI derived, 12 detections at the survey's threshold.</summary>
    private const string MessagingCopy = "1748713494260-cf6994ff-96c5-4eda-8647-424356f59979_1.jpg";

    /// <summary>docs/SCAN-MEASUREMENTS.md section 8: the two holes on <c>300_nm_hand_load.jpg</c> the survey's detector missed, inches, and the hit tolerance.</summary>
    private static readonly (double X, double Y, string What)[] KnownMisses = [(8.02, 4.42, "the isolated hole outside bull 15"), (7.98, 5.88, "the second lobe of the overlapping pair at bull 20")];

    private const double HitTolerance = 0.15;

    /// <summary>
    /// The size band <c>tools/scan_analysis/s12_summarise_holes.py</c> keeps before summarising, inches. The detector accepts
    /// holes up to 0.60 in and the roll-up keeps them only to 0.55 in, so the survey's tables count a hole between the two as
    /// detected and not summarised; the comparison applies the same band, and reports both counts.
    /// </summary>
    private const double SummaryMinimum = 0.15, SummaryMaximum = 0.55;

    public static int Run(string scans, string phase1Scans, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        void Progress(string line)
        {
            lock (ProgressLock)
            {
                output.WriteLine(string.Create(Inv, $"[{clock.Elapsed.TotalSeconds,6:0}s] {line}"));
                output.Flush();
            }
        }

        var jobs = PerFile.Select(s => (s.File, Survey: (Survey?)s, DpiOverride: (double?)null)).Append((File: MessagingCopy, Survey: null, DpiOverride: 93.0)).ToList();
        var results = new (HoleDetection Detection, double Dpi)[jobs.Count];
        Parallel.For(0, jobs.Count, new ParallelOptions { MaxDegreeOfParallelism = 4 }, i =>
        {
            var (file, _, dpiOverride) = jobs[i];
            var (image, metadata) = ImageLoader.LoadMaxChannel(Path.Combine(scans, file));
            double dpi = dpiOverride ?? metadata.DpiX ?? 600;
            results[i] = (NeutralDarknessHoleDetector.Detect(image, dpi, new OpenCvSharpBackend()), dpi);
            Progress(string.Create(Inv, $"{file}: {results[i].Detection.Holes.Count} holes at {dpi:0} DPI, {results[i].Detection.Rejected.Count} blobs rejected"));
        });

        output.WriteLine();
        output.WriteLine("Per file, the survey (docs/SCAN-MEASUREMENTS.md section 3.7) against the port, both over the roll-up's 0.15 to 0.55 in band, and the port's detections before the band. Hull diameter in inches with the sample sd, V grey levels.");
        output.WriteLine();
        output.WriteLine("| File | DPI | Holes, survey / port | Hull diameter mean ± sd, survey | Port | Core mean V, survey / port | Annulus minimum V, survey / port |");
        output.WriteLine("|---|---|---|---|---|---|---|");
        var raw = new List<object>();
        var pooled = new List<DetectedHole>();
        for (int i = 0; i < jobs.Count; i++)
        {
            var (file, survey, _) = jobs[i];
            var (detection, dpi) = results[i];
            var holes = survey is null ? detection.Holes : [.. detection.Holes.Where(h => h.DiameterInches >= SummaryMinimum && h.DiameterInches <= SummaryMaximum)];
            string label = survey?.Label ?? "messaging copy (93 DPI, outside the pool)";
            string portDiameter = holes.Count == 0 ? "" : string.Create(Inv, $"{holes.Average(h => h.DiameterInches):0.0000} ± {Sd(holes.Select(h => h.DiameterInches)):0.0000}");
            string portCore = holes.Count == 0 ? "" : string.Create(Inv, $"{holes.Average(h => h.CoreMeanV):0.0}");
            string portAnnulus = holes.Count == 0 ? "" : string.Create(Inv, $"{holes.Average(h => h.AnnulusMinimumV):0.0}");
            output.WriteLine(survey is { } s
                ? string.Create(Inv, $"| {label} | {dpi:0} | {s.Holes} / {holes.Count} ({detection.Holes.Count} detected) | {s.Diameter:0.0000} ± {s.DiameterSd:0.0000} | {portDiameter} | {s.CoreV:0.0} / {portCore} | {s.AnnulusMinimumV:0.0} / {portAnnulus} |")
                : string.Create(Inv, $"| {label} | {dpi:0} | 12 / {holes.Count} | | {portDiameter} | / {portCore} | / {portAnnulus} |"));
            if (survey is not null)
            {
                pooled.AddRange(holes);
            }

            raw.Add(new
            {
                file,
                dpi,
                paperLevel = detection.PaperLevel,
                surveyHoles = survey?.Holes ?? 12,
                holes = holes.Select(h => new { xIn = RawMeasurements.R(h.X / dpi), yIn = RawMeasurements.R(h.Y / dpi), diameterIn = RawMeasurements.R(h.DiameterInches), solidity = RawMeasurements.R(h.Solidity), paperV = RawMeasurements.R(h.PaperV), coreMeanV = RawMeasurements.R(h.CoreMeanV), annulusMinimumV = RawMeasurements.R(h.AnnulusMinimumV) }).ToArray(),
                rejected = detection.Rejected.Select(r => new { xIn = RawMeasurements.R(r.X / dpi), yIn = RawMeasurements.R(r.Y / dpi), diameterIn = RawMeasurements.R(r.DiameterInches), reason = r.Reason }).ToArray(),
            });
        }

        output.WriteLine();
        output.WriteLine("Pooled over the fifteen files, the survey (section 3.2) against the port, mean ± sd.");
        output.WriteLine();
        output.WriteLine("| Quantity | Survey | Port |");
        output.WriteLine("|---|---|---|");
        output.WriteLine(string.Create(Inv, $"| holes | 343 | {pooled.Count} |"));
        output.WriteLine(string.Create(Inv, $"| hull diameter (in) | 0.2655 ± 0.0570 | {pooled.Average(h => h.DiameterInches):0.0000} ± {Sd(pooled.Select(h => h.DiameterInches)):0.0000} |"));
        output.WriteLine(string.Create(Inv, $"| paper V | 245.65 ± 9.93 | {pooled.Average(h => h.PaperV):0.00} ± {Sd(pooled.Select(h => h.PaperV)):0.00} |"));
        output.WriteLine(string.Create(Inv, $"| core mean V | 192.55 ± 26.70 | {pooled.Average(h => h.CoreMeanV):0.00} ± {Sd(pooled.Select(h => h.CoreMeanV)):0.00} |"));
        output.WriteLine(string.Create(Inv, $"| annulus minimum V | 38.53 ± 35.59 | {pooled.Average(h => h.AnnulusMinimumV):0.00} ± {Sd(pooled.Select(h => h.AnnulusMinimumV)):0.00} |"));

        int reference = jobs.FindIndex(j => j.File == "300_nm_hand_load.jpg");
        var (referenceDetection, referenceDpi) = results[reference];
        output.WriteLine();
        output.WriteLine(string.Create(Inv, $"300_nm_hand_load.jpg: {referenceDetection.Holes.Count} detections against the survey's 25 verified true positives. The two holes the survey missed, located by a person (section 8), within {HitTolerance} in of a detection:"));
        output.WriteLine();
        foreach (var (x, y, what) in KnownMisses)
        {
            var nearest = referenceDetection.Holes.Select(h => Math.Sqrt(Math.Pow((h.X / referenceDpi) - x, 2) + Math.Pow((h.Y / referenceDpi) - y, 2))).DefaultIfEmpty(double.PositiveInfinity).Min();
            output.WriteLine(string.Create(Inv, $"- ({x:0.00}, {y:0.00}) in, {what}: nearest detection {nearest:0.000} in, {(nearest <= HitTolerance ? "found" : "missed, as the survey missed it")}"));
        }

        RawMeasurements.Write(phase1Scans, "holes-baseline", raw);
        output.WriteLine();
        output.WriteLine(string.Create(Inv, $"done in {clock.Elapsed.TotalMinutes:0.0} minutes"));
        return 0;
    }

    private static double Sd(IEnumerable<double> values)
    {
        var v = values.ToList();
        if (v.Count < 2)
        {
            return double.NaN;
        }

        double mean = v.Average();
        return Math.Sqrt(v.Sum(x => (x - mean) * (x - mean)) / (v.Count - 1));
    }
}
