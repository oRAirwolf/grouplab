using System.Diagnostics;
using GroupLab.Cli;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Analysis;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 33 section 1: the first test in the project that measures what GroupLab does rather than a stage of it.
/// A GL-CF25-LTR sheet is rendered at 300 DPI, turned 0.7 degrees, scaled by 1.001 and shifted inside a larger frame, and given one hole per
/// bull at an offset the test chooses. It is written to a PNG, and <c>grouplab analyze</c>'s own <see cref="AnalyzeVerb.Analyze"/> runs the
/// whole path on the file: decode, register, detect inside the sheet, assign, pool. Every placed shot must be recovered within the brief's
/// 0.15 in matching radius (docs/PHASE1-BRIEF.md section 4.4) and assigned to the bull it was placed beside, with nothing else found; the
/// centre errors and the pooled mean radius against the truth's are reported.
/// </summary>
public class EndToEndTests(ITestOutputHelper output)
{
    [Fact]
    public void AnalyzeRecoversEveryShotPlacedOnARenderedSheetAndPoolsThem()
    {
        const double dpi = 300, degrees = 0.7, scale = 1.001, dmmPerInch = 254;
        string definitionPath = Repo.PathTo("targets", "GL-CF25-LTR.gltd.json");
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);

        // Image pixel to page dmm: rotate and scale about the frame's centre, which lies over the page's centre.
        int width = render.Width + 240, height = render.Height + 180;
        double s = dmmPerInch / dpi / scale, angle = degrees * Math.PI / 180, cos = Math.Cos(angle) * s, sin = Math.Sin(angle) * s;
        double cx = width / 2.0, cy = height / 2.0, px = definition.Page.Width / 2.0, py = definition.Page.Height / 2.0;
        var truth = new HomographyMapping(new Homography([cos, -sin, px - ((cos * cx) - (sin * cy)), sin, cos, py - ((sin * cx) + (cos * cy)), 0, 0, 1]));

        var random = new Random(33);
        var placed = definition.Bulls.Select(b =>
        {
            double r = 0.30 * dmmPerInch * Math.Sqrt(random.NextDouble()), t = 2 * Math.PI * random.NextDouble();
            return (Bull: b, X: b.X + (r * Math.Cos(t)), Y: b.Y + (r * Math.Sin(t)));
        }).ToList();
        bool OnInk(double x, double y)
        {
            int rx = (int)(x * dpi / dmmPerInch), ry = (int)(y * dpi / dmmPerInch);
            return render.Pixels[(Math.Clamp(ry, 0, render.Height - 1) * render.Width) + Math.Clamp(rx, 0, render.Width - 1)] < 128;
        }

        var holes = placed.Select(p => OnInk(p.X, p.Y)
            ? new SyntheticHole(p.X, p.Y, 0.10 * dmmPerInch, 0.08 * dmmPerInch, 48, 192, 0.006 * dmmPerInch, [0.15, 0.10, 0.05, 0.05], [0, 1, 2, 3])
            : new SyntheticHole(p.X, p.Y, 0.10 * dmmPerInch, 0.065 * dmmPerInch, 34, 192, 0.006 * dmmPerInch, [0.15, 0.10, 0.05, 0.05], [0, 1, 2, 3])).ToList();
        var observed = SyntheticSheet.Compose(render, dpi, truth, width, height, holes, [], random);

        string path = Path.Combine(Path.GetTempPath(), $"grouplab-end-to-end-{Guid.NewGuid():N}.png");
        try
        {
            using (var mat = OpenCvSharp.Mat.FromPixelData(observed.Height, observed.Width, OpenCvSharp.MatType.CV_8UC1, observed.Pixels))
            {
                OpenCvSharp.Cv2.ImWrite(path, mat);
            }

            var clock = Stopwatch.StartNew();
            var result = AnalyzeVerb.Analyze(path, definitionPath, out string? loadFailure);
            clock.Stop();
            Assert.Null(loadFailure);
            Assert.NotNull(result);
            foreach (var record in result.Trace)
            {
                output.WriteLine($"{record.Stage,-14} {record.DurationMs,6} ms  {record.Status,-8} {record.Summary}");
            }

            output.WriteLine($"whole analysis {clock.ElapsedMilliseconds} ms");
            Assert.True(result.Failure is null, result.Failure);

            // Entry 35 section 6 item 3: the same file with no definition named, the sheet naming its own through its printed codes.
            var identified = AnalyzeVerb.Analyze(path, null, out string? identifyFailure, [Repo.PathTo("targets")]);
            Assert.True(identifyFailure is null, identifyFailure is null ? null : identifyFailure + CodeDiagnostics.Describe(observed));
            Assert.NotNull(identified);
            Assert.True(identified.Failure is null, identified.Failure);
            Assert.Contains(identified.Trace, r => r.Stage == "S0.identify" && r.Status == GroupLab.Core.Trace.StageStatus.Ok);
            Assert.Equal(result.Shots.Select(s => s.PageInches), identified.Shots.Select(s => s.PageInches));

            var errors = new List<double>();
            var misses = new List<string>();
            var used = new HashSet<int>();
            foreach (var p in placed)
            {
                var nearest = result.Shots.Where(shot => !used.Contains(shot.Id))
                    .Select(shot => (Shot: shot, Distance: Math.Sqrt(Math.Pow((shot.PageInches.X * dmmPerInch) - p.X, 2) + Math.Pow((shot.PageInches.Y * dmmPerInch) - p.Y, 2)) / dmmPerInch))
                    .OrderBy(m => m.Distance).FirstOrDefault();
                if (nearest.Shot is null || nearest.Distance > 0.15)
                {
                    misses.Add($"bull {(p.Bull.Label ?? "unlabelled")}: nothing within 0.15 in");
                    continue;
                }

                used.Add(nearest.Shot.Id);
                errors.Add(nearest.Distance);
                int expectedBull = definition.Bulls.ToList().IndexOf(p.Bull);
                if (nearest.Shot.Bull != expectedBull)
                {
                    misses.Add($"bull {(p.Bull.Label ?? "unlabelled")}: its shot was assigned to bull {nearest.Shot.Bull?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none"}");
                }
            }

            int strays = result.Shots.Count(shot => !used.Contains(shot.Id));
            errors.Sort();
            output.WriteLine($"{errors.Count} of {placed.Count} placed shots recovered, {strays} strays; centre error median {errors[errors.Count / 2]:0.0000} in, worst {errors[^1]:0.0000} in");

            // Only the scoring bulls' shots form the group; the sighters' are reported and left out.
            var scoring = placed.Where(p => p.Bull.Scoring).ToList();
            var truthOffsets = scoring.Select(p => new PointD((p.X - p.Bull.X) / dmmPerInch, (p.Y - p.Bull.Y) / dmmPerInch)).ToList();
            double truthMeanRadius = GroupLab.Core.Statistics.GroupStatistics.Rayleigh(truthOffsets).MeanRadius.Value;
            double recoveredMeanRadius = result.Report!.AllShots!.MeanRadius!.Value;
            output.WriteLine($"pooled mean radius {recoveredMeanRadius:0.0000} in over {result.Report.AllShots.Shots} scoring shots, against the placed shots' {truthMeanRadius:0.0000} in; {result.Report.SighterShots} sighter shots left out");

            Assert.True(misses.Count == 0, string.Join("\n", misses));
            Assert.Equal(0, strays);
            Assert.Equal(scoring.Count, result.Report.AllShots.Shots);
            Assert.Equal(placed.Count - scoring.Count, result.Report.SighterShots);
            Assert.True(placed.Count > scoring.Count, "GL-CF25-LTR has sighter bulls, so this test checks they stay out of the group");
            Assert.Equal(placed.Count - scoring.Count, result.Shots.Count(shot => shot.Sighter));
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(path);
        }
    }
}
