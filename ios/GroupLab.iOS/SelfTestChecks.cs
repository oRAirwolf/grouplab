using System.Globalization;
using System.Text;
using System.Text.Json;
using GroupLab.App;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Trace;
using GroupLab.Mobile;

namespace GroupLab.iOS;

/// <summary>One self-test check: whether it passed, in words, and the numbers it measured, which the desktop measures too.</summary>
internal sealed class SelfTestCheck(string name)
{
    public string Name { get; } = name;

    public bool Passed { get; set; }

    public bool Skipped { get; set; }

    public string Detail { get; set; } = "";

    public SortedDictionary<string, double> Numbers { get; } = new(StringComparer.Ordinal);

    /// <summary>The shots found, by bull, as offsets from the bull in inches at the target.</summary>
    public List<(int Bull, double X, double Y)> Shots { get; } = [];
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 4: the imaging and the pipeline, checked on the device they run on. Nothing here is iOS:
/// the same file runs on the desktop (ios/SelfTestReference), so the simulator's numbers are compared with the desktop's in the same CI job.
/// Only GroupLab's own sheets, rendered here, and the committed sample scan, whose consent is in samples/PROVENANCE.md.
/// </summary>
internal static class SelfTestChecks
{
    /// <summary>The sheet every imaging check renders: the 5 by 5 load development sheet's plain sibling, as the desktop's tests use.</summary>
    internal const string SheetFile = "GL-CF25-LTR.gltd.json";

    /// <summary>The resolution it is rendered at, a flatbed scan's.</summary>
    internal const double Dpi = 300;

    /// <summary>The sample scan's caliber and distance, as its load block says (samples/sample.json): 6.5 Creedmoor at 100 yards.</summary>
    internal const string SampleCalibre = "6.5 Creedmoor";

    internal const double SampleDistanceInches = 3600;

    /// <summary>What the sample holds (samples/sample.json, groundTruth): one shot on each of its 25 bulls.</summary>
    internal const int SampleShots = 25;

    internal static List<SelfTestCheck> Imaging()
    {
        var checks = new List<SelfTestCheck>();
        var library = PhoneAnalysis.Library();
        string sheetPath = Path.Combine(Phone.Platform.FilesFolder, "targets", SheetFile);
        var definition = File.Exists(sheetPath) ? GroupLab.Core.Gltd.Json.GltdJsonReader.ReadFile(sheetPath).Definition : null;

        var sheets = new SelfTestCheck("sheets");
        sheets.Numbers["sheets"] = library.Count;
        sheets.Passed = library.Count > 0 && definition is not null;
        sheets.Detail = $"{library.Count} sheets copied from the bundle; {SheetFile} {(definition is null ? "missing" : "read")}";
        checks.Add(sheets);
        if (definition is null)
        {
            return checks;
        }

        TargetDefinition sheet = definition;
        GrayImage? rendered = null;
        checks.Add(Run("render", check =>
        {
            var image = SceneRasterizer.Rasterize(SceneBuilder.Build(sheet).Pages[0], Dpi);
            rendered = image;
            check.Numbers["width"] = image.Width;
            check.Numbers["height"] = image.Height;
            check.Numbers["mean"] = Math.Round(image.Pixels.Average(p => (double)p), 4);
            check.Passed = image.Width > 2000 && image.Height > 2000;
            check.Detail = $"{sheet.Name} rendered at {Dpi:0} dpi, {image.Width} by {image.Height}";
        }));
        if (rendered is not { } render)
        {
            return checks;
        }

        var backend = new OpenCvSharpBackend();
        checks.Add(Run("markers", check =>
        {
            var registration = PageRegistration.Register(render, sheet, 0, Dpi, backend);
            check.Numbers["expected"] = registration.MarkersExpected;
            check.Numbers["found"] = registration.MarkersFound;
            check.Numbers["inliers"] = registration.Inliers;
            check.Numbers["rmsDmm"] = Math.Round(registration.RmsResidual, 4);
            check.Numbers["maxDmm"] = Math.Round(registration.MaxResidual, 4);
            check.Numbers["scale"] = Math.Round(registration.Scale, 6);
            check.Passed = registration.Failure is null && registration.MarkersFound == registration.MarkersExpected && registration.RmsResidual < 1.0;
            check.Detail = registration.Failure ?? $"{registration.MarkersFound} of {registration.MarkersExpected} AprilTag markers read, registered at {registration.RmsResidual:0.000} dmm";
        }));

        checks.Add(Run("homography", check =>
        {
            var truth = new Homography([1.1, 0.05, 30, -0.03, 0.95, 12, 1e-5, -2e-5, 1]);
            var source = new List<PointD>();
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    source.Add(new PointD(100 + (400 * x), 80 + (500 * y)));
                }
            }

            var destination = source.Select(truth.Apply).ToList();
            var fit = backend.FindHomography(source, destination, 1.0);
            double worst = source.Select((p, i) => Distance(fit.Transform.Apply(p), destination[i])).Max();
            check.Numbers["worstPx"] = Math.Round(worst, 6);
            check.Numbers["inliers"] = fit.Inliers.Count(i => i);
            check.Passed = worst < 1e-3 && fit.Inliers.All(i => i);
            check.Detail = $"a known homography recovered from 16 points to {worst:0.000000} px";
        }));

        checks.Add(Run("warp", check =>
        {
            var shift = new Homography([1, 0, 5, 0, 1, 3, 0, 0, 1]);
            var warped = backend.WarpPerspective(render, shift, render.Width, render.Height);
            double one = Difference(render, warped, 5, 3), other = Difference(render, warped, -5, -3);
            double best = Math.Min(one, other);
            check.Numbers["meanDifference"] = Math.Round(best, 4);
            check.Passed = best < 2;
            check.Detail = $"a 5, 3 pixel shift warped, {best:0.00} grey levels from the shifted render";
        }));

        checks.Add(Run("morphology", check =>
        {
            const int Side = 64;
            var pixels = new byte[Side * Side];
            for (int y = 10; y < 30; y++)
            {
                for (int x = 10; x < 30; x++)
                {
                    pixels[(y * Side) + x] = 255;
                }
            }

            pixels[(20 * Side) + 20] = 0;
            pixels[(50 * Side) + 50] = 255;
            var image = new GrayImage(Side, Side, pixels);
            var opened = backend.Morphology(image, MorphologyOperation.Open, 1);
            var closed = backend.Morphology(image, MorphologyOperation.Close, 1);
            bool speckGone = opened.Pixels[(50 * Side) + 50] == 0;
            bool squareKept = opened.Pixels[(15 * Side) + 15] == 255;
            bool holeFilled = closed.Pixels[(20 * Side) + 20] == 255;
            check.Numbers["openedOn"] = opened.Pixels.Count(p => p == 255);
            check.Numbers["closedOn"] = closed.Pixels.Count(p => p == 255);
            check.Passed = speckGone && squareKept && holeFilled;
            check.Detail = $"opening removed the speck: {speckGone}, kept the square: {squareKept}; closing filled the hole: {holeFilled}";
        }));

        checks.Add(Run("blobs", check =>
        {
            const int Width = 120, Height = 60;
            var pixels = new byte[Width * Height];
            (int X, int Y, int R)[] discs = [(20, 30, 8), (60, 30, 5), (95, 30, 3)];
            foreach (var (cx, cy, r) in discs)
            {
                for (int y = cy - r; y <= cy + r; y++)
                {
                    for (int x = cx - r; x <= cx + r; x++)
                    {
                        if (((x - cx) * (x - cx)) + ((y - cy) * (y - cy)) <= r * r)
                        {
                            pixels[(y * Width) + x] = 255;
                        }
                    }
                }
            }

            var blobs = backend.FilledBlobs(new GrayImage(Width, Height, pixels)).OrderByDescending(b => b.Area).ToList();
            check.Numbers["count"] = blobs.Count;
            for (int i = 0; i < blobs.Count && i < 3; i++)
            {
                check.Numbers[$"area{i + 1}"] = blobs[i].Area;
            }

            check.Passed = blobs.Count == 3 && blobs.Zip(discs).All(p => Math.Abs(p.First.Area - (Math.PI * p.Second.R * p.Second.R)) < 0.3 * Math.PI * p.Second.R * p.Second.R);
            check.Detail = $"{blobs.Count} filled blobs, areas {string.Join(", ", blobs.Select(b => b.Area))}";
        }));

        checks.Add(Run("phase correlation", check =>
        {
            const int Side = 256, Dx = 7, Dy = -4;
            int left = (render.Width / 2) - (Side / 2), top = (render.Height / 3) - (Side / 2);
            var reference = new byte[Side * Side];
            var moved = new byte[Side * Side];
            for (int y = 0; y < Side; y++)
            {
                for (int x = 0; x < Side; x++)
                {
                    reference[(y * Side) + x] = render.Pixels[((top + y) * render.Width) + left + x];
                    moved[(y * Side) + x] = render.Pixels[((top + y - Dy) * render.Width) + left + x - Dx];
                }
            }

            var (found, response) = backend.PhaseCorrelate(new GrayImage(Side, Side, reference), new GrayImage(Side, Side, moved));
            check.Numbers["shiftX"] = Math.Round(found.X, 4);
            check.Numbers["shiftY"] = Math.Round(found.Y, 4);
            check.Numbers["response"] = Math.Round(response, 4);
            check.Passed = Math.Abs(Math.Abs(found.X) - Math.Abs(Dx)) < 0.25 && Math.Abs(Math.Abs(found.Y) - Math.Abs(Dy)) < 0.25 && Math.Sign(found.X) == -Math.Sign(found.Y);
            check.Detail = $"a {Dx}, {Dy} pixel shift found as {found.X:0.00}, {found.Y:0.00}, response {response:0.00}";
        }));

        checks.Add(Run("QR codes", check =>
        {
            var codes = backend.ReadCodes(render, 1.0);
            var identity = SheetIdentification.Identify(render, library, backend, new TraceRecorder());
            check.Numbers["codes"] = codes.Count;
            check.Numbers["codesRead"] = identity.CodesRead;
            check.Passed = codes.Count > 0 && identity.Definition?.Id == sheet.Id;
            check.Detail = $"{codes.Count} codes read; the sheet named itself {identity.Definition?.Name ?? identity.Failure ?? "nothing"}";
        }));

        checks.Add(Run("synthetic shots", check =>
        {
            // As AutomaticMarkingTests.ARenderedSheetIsPrefilledWithOneAssignedShotPerBull makes it: a hole beside every bull, heavier where it
            // falls on ink, the render resampled at the page's own scale, seed 5; the desktop holds one assigned shot per bull on it.
            double s = 254 / Dpi;
            var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
            bool OnInk(double x, double y)
            {
                var p = truth.ToImage(new PointD(x, y));
                return render.Pixels[((int)p.Y * render.Width) + (int)p.X] < 128;
            }

            var holes = sheet.Bulls.Select(b =>
            {
                double x = b.X + 30, y = b.Y - 20;
                bool ink = OnInk(x, y);
                return new SyntheticHole(x, y, 0.10 * 254, (ink ? 0.08 : 0.065) * 254, ink ? 48 : 34, 192, 0.006 * 254, [0.15, 0.10, 0.05, 0.05], [0, 1, 2, 3]);
            }).ToList();
            var observed = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], new Random(5));
            var result = AutomaticMarking.Run(observed, observed, ImageMetadata.ForScan(render.Width, render.Height, Dpi), sheet, backend);
            var expected = holes.Select(h => truth.ToImage(new PointD(h.X, h.Y))).ToList();
            double worst = result.Detections.Count == 0 ? double.NaN
                : result.Detections.Max(d => expected.Min(e => Distance(d.Image, e)));
            check.Numbers["holes"] = holes.Count;
            check.Numbers["found"] = result.Detections.Count;
            check.Numbers["worstPx"] = Math.Round(worst, 3);
            check.Passed = result.Failure is null && result.Detections.Count == holes.Count && worst < 3;
            check.Detail = result.Failure ?? $"{result.Detections.Count} of {holes.Count} synthetic holes found, the farthest {worst:0.00} px from where it was punched";
        }));

        return checks;
    }

    /// <summary>
    /// The whole pipeline on the committed sample scan, through <see cref="PhoneAnalysis.Run"/>, the code the files picker feeds: the
    /// reduced decode, the working copy, the sheet named by its codes, registration, the holes, their bulls, and the saved session.
    /// </summary>
    internal static SelfTestCheck Pipeline(string sample)
    {
        return Run("sample pipeline", check =>
        {
            if (!File.Exists(sample))
            {
                check.Skipped = true;
                check.Detail = "the sample scan was not put in the application's files";
                return;
            }

            string copy = Path.Combine(Phone.Platform.CacheFolder, "self-test-sample" + Path.GetExtension(sample));
            File.Copy(sample, copy, overwrite: true);
            var setup = new ShotSetup(Calibre.Parse(SampleCalibre, out _), SampleDistanceInches);
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var result = PhoneAnalysis.Run(copy, setup, UnitSettings.Imperial, null, CancellationToken.None);
            File.Delete(copy);
            check.Numbers["seconds"] = Math.Round(clock.Elapsed.TotalSeconds, 1);
            if (result.Failure is not null || result.State.Scale is null)
            {
                check.Detail = result.Failure ?? "no scale";
                return;
            }

            Describe(check, result.State);
            check.Numbers["workingWidth"] = result.Image?.Metadata.Width ?? 0;
            check.Numbers["workingHeight"] = result.Image?.Metadata.Height ?? 0;
            var perBull = check.Shots.GroupBy(s => s.Bull).ToList();
            check.Passed = check.Shots.Count == SampleShots && perBull.Count == SampleShots && perBull.All(g => g.Count() == 1);
            check.Detail = $"{result.Definition?.Name}: {check.Shots.Count} shots on {perBull.Count} bulls, mean radius {check.Numbers.GetValueOrDefault("meanRadius"):0.000} in, extreme spread {check.Numbers.GetValueOrDefault("extremeSpread"):0.000} in";
            if (result.SessionId is { } id)
            {
                PhoneAnalysis.Store().Delete(id);
            }

            PhoneAnalysis.Discard(result.Image);
        });
    }

    /// <summary>A marking's shots and its pooled figures, in inches, as the result screen shows them.</summary>
    internal static void Describe(SelfTestCheck check, MarkingState state)
    {
        var shots = state.Shots.Where(s => s.IsShot).ToList();
        var offsets = GroupAnalysis.CompositeOffsets(state, shots);
        for (int i = 0; i < shots.Count; i++)
        {
            check.Shots.Add((shots[i].Bull ?? -1, Math.Round(offsets[i].X, 4), Math.Round(offsets[i].Y, 4)));
        }

        check.Shots.Sort();
        if (GroupAnalysis.Analyse(state).Counted is { } figures)
        {
            check.Numbers["shots"] = figures.Shots;
            Put(check, "meanRadius", figures.MeanRadius?.Value);
            Put(check, "sigma", figures.Sigma?.Value);
            Put(check, "extremeSpread", figures.ExtremeSpread?.Value);
            Put(check, "cep50", figures.Cep50?.Value);
            Put(check, "width", figures.Width);
            Put(check, "height", figures.Height);
            Put(check, "centreX", figures.CentreFromAim?.X);
            Put(check, "centreY", figures.CentreFromAim?.Y);
        }
    }

    private static void Put(SelfTestCheck check, string name, double? value)
    {
        if (value is { } v && double.IsFinite(v))
        {
            check.Numbers[name] = Math.Round(v, 5);
        }
    }

    /// <summary>One check, with anything it throws as its failure: a missing native library says so rather than stopping the rest.</summary>
    internal static SelfTestCheck Run(string name, Action<SelfTestCheck> body)
    {
        var check = new SelfTestCheck(name);
        try
        {
            body(check);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            check.Passed = false;
            check.Detail = Describe(e);
        }

        return check;
    }

    /// <summary>An exception and every one inside it, type and message, so a failure on a phone says what actually failed.</summary>
    internal static string Describe(Exception e)
    {
        var parts = new List<string>();
        for (Exception? at = e; at is not null && parts.Count < 5; at = at.InnerException)
        {
            parts.Add(at.GetType().Name + ": " + at.Message);
        }

        return string.Join(" <- ", parts);
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    /// <summary>The mean grey difference between <paramref name="warped"/> and the render moved by dx, dy, over the part both cover.</summary>
    private static double Difference(GrayImage render, GrayImage warped, int dx, int dy)
    {
        double sum = 0;
        long count = 0;
        for (int y = 20; y < render.Height - 20; y += 3)
        {
            for (int x = 20; x < render.Width - 20; x += 3)
            {
                sum += Math.Abs(warped.Pixels[((y + dy) * render.Width) + x + dx] - render.Pixels[(y * render.Width) + x]);
                count++;
            }
        }

        return sum / count;
    }

    /// <summary>The checks as JSON, written by hand so nothing depends on reflection, which a trimmed iOS build leaves out.</summary>
    internal static string Json(string platform, bool openCv, double budgetMegabytes, IEnumerable<SelfTestCheck> checks)
    {
        using var stream = new MemoryStream();
        using (var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteString("platform", platform);
            json.WriteBoolean("opencv", openCv);
            json.WriteNumber("budgetMb", Math.Round(budgetMegabytes, 1));
            json.WriteStartArray("checks");
            foreach (var check in checks)
            {
                json.WriteStartObject();
                json.WriteString("name", check.Name);
                json.WriteBoolean("passed", check.Passed);
                json.WriteBoolean("skipped", check.Skipped);
                json.WriteString("detail", check.Detail);
                json.WriteStartObject("numbers");
                foreach (var (key, value) in check.Numbers)
                {
                    if (double.IsFinite(value))
                    {
                        json.WriteNumber(key, value);
                    }
                }

                json.WriteEndObject();
                json.WriteStartArray("shots");
                foreach (var (bull, x, y) in check.Shots)
                {
                    json.WriteStartArray();
                    json.WriteNumberValue(bull);
                    json.WriteNumberValue(x);
                    json.WriteNumberValue(y);
                    json.WriteEndArray();
                }

                json.WriteEndArray();
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>One line a check, for the log.</summary>
    internal static string Line(SelfTestCheck check) =>
        string.Create(CultureInfo.InvariantCulture, $"{(check.Skipped ? "SKIP" : check.Passed ? "PASS" : "FAIL")} {check.Name}: {check.Detail}");
}
