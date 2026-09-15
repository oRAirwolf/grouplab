using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Trace;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab spike stability [orders]</c>: the two spread experiments of NOTES-FROM-PLANNING.md entry 52 section 3, one method for both.
/// <list type="bullet">
/// <item><b>Registration against the order of its inputs.</b> Each flat and gated mounted photograph is detected once, then registered
/// <c>orders</c> times with the same corner correspondences handed to the homography fit in a different seeded order each time. The markers,
/// their corners and the sheet are identical in every run, so any spread in the corners kept, the scoring bulls over the gate or the worst
/// scoring bull is the registration's instability and nothing else.</item>
/// <item><b>The edge fit against each of its points.</b> On the photograph's own registration, every located bull is refitted with each edge
/// point left out in turn, and the largest movement of its centre is reported, with how many points lie within a tenth of the fit's
/// rejection limit, where one crossing flips a point in or out.</item>
/// </list>
/// Nothing is tuned: the experiment measures the shipped pipeline and writes every trial to <c>scans/phase1/measurements/stability.json</c>.
/// </summary>
public static class StabilitySpike
{
    public const int DefaultOrders = 200;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static int Run(string scans, string targets, string phase1Scans, int orders, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var real = new OpenCvSharpBackend();
        var frames = SampleSet.All.Where(s => s.Gated && s.Gate is SampleSet.PhotographGate.Flat or SampleSet.PhotographGate.Mounted)
            .Append(SampleSet.All.Single(s => s.File == "telephoto3.jpg")).ToList();
        var rows = new List<object>();
        var table = new List<string>
        {
            "| Frame | Gate | Orders | Distinct outcomes | Corners kept, min / median / max | Scoring bulls over the gate, min / median / max | Worst scoring bull (in), min / median / max | Largest leave-one-out shift of a bull (in) | Bulls with a point near the rejection limit |",
            "|---|---|---|---|---|---|---|---|---|",
        };
        foreach (var sample in frames)
        {
            var (image, metadata) = ImageLoader.Load(Path.Combine(scans, sample.File));
            var definition = Phase0Spike.Definition(targets, sample.Definition);
            var options = new MeasureOptions();
            var fiducials = SheetMeasurer.DetectFiducials(image, metadata, definition, options, real, new TraceRecorder());
            bool gated = sample.Gated;
            var trials = new List<(int Seed, int Kept, int Total, int Over, double Worst)>();
            int failures = 0;
            for (int k = gated ? 0 : orders; k < orders; k++)
            {
                var fit = SheetMeasurer.Register(image, metadata, fiducials, options, new ShuffledCorrespondences(real, k + 1), new TraceRecorder());
                if (fit is null)
                {
                    failures++;
                    continue;
                }

                var scoring = Scoring(definition, SheetMeasurer.LocateBulls(image, definition, fit.Mapping, options with { Locator = BullLocatorKind.EdgeFit }, new TraceRecorder()));
                trials.Add((k + 1, fit.Corners.Count(c => c.Inlier), fit.Corners.Count, scoring.Count(b => b.Error >= Phase0Spike.PaperGate), scoring.Count == 0 ? double.NaN : scoring.Max(b => b.Error)));
                if ((k + 1) % 50 == 0)
                {
                    Console.Error.WriteLine(string.Create(Inv, $"{sample.File}: {k + 1} of {orders} orders"));
                }
            }

            // The edge fit on the photograph's own registration, the sorted order the pipeline ships with.
            var own = SheetMeasurer.Register(image, metadata, fiducials, options, real, new TraceRecorder());
            var sensitivities = new List<(string Bull, EdgeFitSensitivity Result)>();
            if (own is not null)
            {
                var sets = definition.RingSets.GroupBy(s => s.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
                for (int i = 0; i < definition.Bulls.Count; i++)
                {
                    var bull = definition.Bulls[i];
                    if (bull.Scoring && sets.TryGetValue(bull.RingSet, out var set))
                    {
                        sensitivities.Add((bull.Label ?? i.ToString(Inv), EdgeFitBullLocator.LeaveOneOut(image, own.Mapping, i, bull, RingGeometry.Bands(definition, set))));
                    }
                }
            }

            var located = sensitivities.Where(s => s.Result.LargestShift is not null).ToList();
            var worstShift = located.MaxBy(s => s.Result.LargestShift!.Value);
            string shift = worstShift.Result is null ? "" : string.Create(Inv, $"{worstShift.Result.LargestShift!.Value / 254:0.00000} at {worstShift.Bull}, {worstShift.Result.Used} of {worstShift.Result.Observations} points used");
            int nearLimit = located.Count(s => s.Result.NearLimit > 0);
            string gate = sample.Gate == SampleSet.PhotographGate.Flat ? "flat" : sample.Gated ? "mounted" : "excluded";
            if (trials.Count == 0)
            {
                table.Add(string.Create(Inv, $"| `{sample.File}` | {gate} | not reordered | | | | | {shift} | {nearLimit} of {located.Count} |"));
            }
            else
            {
                int distinct = trials.Select(t => (t.Kept, t.Over, Math.Round(t.Worst, 4))).Distinct().Count();
                table.Add(string.Create(Inv,
                    $"| `{sample.File}` | {gate} | {trials.Count}{(failures > 0 ? $", {failures} failed" : "")} | {distinct} | {Spread(trials.Select(t => (double)t.Kept), "0")} of {trials[0].Total} | {Spread(trials.Select(t => (double)t.Over), "0")} of {definition.Bulls.Count(b => b.Scoring)} | {Spread(trials.Select(t => t.Worst / 254), "0.00000")} | {shift} | {nearLimit} of {located.Count} |"));
            }

            output.WriteLine(table[^1]);
            rows.Add(new
            {
                file = sample.File,
                gate,
                orders = trials.Select(t => new { t.Seed, cornersKept = t.Kept, cornersTotal = t.Total, scoringBullsOverGate = t.Over, worstScoringBull = RawMeasurements.R(t.Worst) }),
                registrationFailures = failures,
                leaveOneOut = sensitivities.Select(s => new
                {
                    bull = s.Bull,
                    observations = s.Result.Observations,
                    used = s.Result.Used,
                    largestShift = s.Result.LargestShift is { } l ? RawMeasurements.R(l) : (double?)null,
                    medianShift = s.Result.MedianShift is { } m ? RawMeasurements.R(m) : (double?)null,
                    nearLimit = s.Result.NearLimit,
                }),
            });
        }

        output.WriteLine();
        foreach (string line in table)
        {
            output.WriteLine(line);
        }

        RawMeasurements.Write(phase1Scans, "stability", rows);
        return 0;
    }

    private static List<BullLocation> Scoring(TargetDefinition definition, IReadOnlyList<BullLocation> bulls) =>
        [.. bulls.Where(b => b.Recovered is not null && definition.Bulls[b.Index].Scoring)];

    private static string Spread(IEnumerable<double> values, string format)
    {
        var sorted = values.Where(double.IsFinite).Order().ToList();
        return sorted.Count == 0
            ? ""
            : string.Create(Inv, $"{sorted[0].ToString(format, Inv)} / {sorted[sorted.Count / 2].ToString(format, Inv)} / {sorted[^1].ToString(format, Inv)}");
    }

    /// <summary>The imaging backend, except that the homography fit receives its correspondences in a seeded random order, and its inlier flags are put back in the caller's order.</summary>
    private sealed class ShuffledCorrespondences(IImagingBackend inner, int seed) : IImagingBackend
    {
        public HomographyFit FindHomography(IReadOnlyList<PointD> source, IReadOnlyList<PointD> destination, double ransacThreshold)
        {
            int[] order = [.. Enumerable.Range(0, source.Count)];
            new Random(seed).Shuffle(order);
            var fit = inner.FindHomography([.. order.Select(i => source[i])], [.. order.Select(i => destination[i])], ransacThreshold);
            var inliers = new bool[order.Length];
            for (int k = 0; k < order.Length; k++)
            {
                inliers[order[k]] = fit.Inliers[k];
            }

            return new HomographyFit(fit.Transform, inliers);
        }

        public MarkerDetection DetectMarkers(GrayImage image, MarkerDetectionOptions options) => inner.DetectMarkers(image, options);

        public GrayImage WarpPerspective(GrayImage image, Homography transform, int width, int height) => inner.WarpPerspective(image, transform, width, height);

        public GrayImage Morphology(GrayImage image, MorphologyOperation operation, int radius) => inner.Morphology(image, operation, radius);

        public IReadOnlyList<ImageBlob> FilledBlobs(GrayImage binary) => inner.FilledBlobs(binary);

        public (PointD Shift, double Response) PhaseCorrelate(GrayImage reference, GrayImage moved) => inner.PhaseCorrelate(reference, moved);

        public IReadOnlyList<byte[]> ReadCodes(GrayImage image, double scale) => inner.ReadCodes(image, scale);
    }
}
