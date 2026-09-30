using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;

namespace GroupLab.Core.Evaluation;

/// <summary>
/// One picture read by the whole pipeline and scored against its truth. <see cref="Truth"/> says what the truth is: <c>holes</c>, every hole's
/// position, so each is found or missed and every mark is matched or false; <c>count</c>, only the number of shots, so <see cref="Found"/>
/// is at most that number and <see cref="FalseMarks"/> at least the excess; <c>count-unknown</c>, nothing, so only the marks are counted.
/// Distances are inches on the sheet. The registration error is the distance at every bull between where the truth puts it and where the
/// registration put it, and is known only where the truth mapping is (the synthetic pictures); on a real photograph it is left empty and
/// <see cref="MarkerResidualInches"/>, the registration's own error between its markers, is given instead.
/// </summary>
public sealed record PictureScore(
    string Condition,
    string Picture,
    string Truth,
    int? Holes,
    int Marks,
    int? Found,
    int? FalseMarks,
    double? MedianCentreInches,
    double? WorstCentreInches,
    double? MedianRegistrationInches,
    double? WorstRegistrationInches,
    double? MarkerResidualInches,
    bool Registered,
    long Milliseconds,
    string? Note = null)
{
    /// <summary>Every matched hole's centre error, inches, kept so a condition's median is taken over its holes, not over its pictures.</summary>
    [JsonIgnore]
    public IReadOnlyList<double> CentreErrors { get; init; } = [];

    /// <summary>Every bull's registration error, inches, where the truth mapping is known.</summary>
    [JsonIgnore]
    public IReadOnlyList<double> RegistrationErrors { get; init; } = [];
}

/// <summary>A condition's pictures together: the scoreboard's line.</summary>
public sealed record ScoreboardRow(
    string Condition,
    string Truth,
    int Pictures,
    int Registered,
    int? Holes,
    int Marks,
    int? Found,
    int? FalseMarks,
    double? MedianCentreInches,
    double? WorstCentreInches,
    double? MedianRegistrationInches,
    double? WorstRegistrationInches,
    long MedianMilliseconds,
    IReadOnlyList<int?> FoundPerPicture)
{
    /// <summary>The share of the holes found, where the holes are known.</summary>
    public double? Recall => Holes is > 0 && Found is { } found ? Math.Round((double)found / Holes.Value, 4) : null;

    /// <summary>
    /// A line the baseline records as failing on purpose, so the failure is on the scoreboard rather than hidden, and a fix shows as an
    /// improvement. A drop is not measured on it.
    /// </summary>
    public bool ExpectedToFail { get; init; }

    /// <summary>What the line is about, in words.</summary>
    public string? Note { get; init; }
}

/// <summary>
/// How far a line may fall against the baseline before the scoreboard fails: holes lost and false marks gained over the whole condition, the
/// median centre and registration error grown, and the worst center error grown. Time is reported and never failed on, because it depends
/// on the machine.
/// </summary>
public sealed record ScoreboardMargin(int Holes = 1, int FalseMarks = 1, double CentreInches = 0.005, double WorstCentreInches = 0.03, double RegistrationInches = 0.005);

/// <summary>What a baseline file holds: the rows, the seeds they came from and the margin they are held to.</summary>
public sealed record ScoreboardBaseline(IReadOnlyList<string> Why, string Measured, IReadOnlyList<int> Seeds, ScoreboardMargin Margin, IReadOnlyList<ScoreboardRow> Rows);

/// <summary>A synthetic condition: what it does to a picture, and where it moves a point of the undisturbed picture, for a warp.</summary>
public sealed record SyntheticCondition(string Name, string Description, Func<GrayImage, Random, Scoreboard.JpegRoundTrip?, GrayImage?> Degrade, Func<double, int, double>? ShiftDown = null)
{
    /// <summary>Where a point of the undisturbed picture is in the degraded one.</summary>
    public PointD Move(PointD p, int width) => ShiftDown is null ? p : new PointD(p.X, p.Y + ShiftDown(p.X, width));
}

/// <summary>
/// The detection scoreboard, NOTES-FROM-PLANNING.md entry 261 option (a), built by entry 291 section 7: every build re-reads the same
/// pictures, synthetic degradations of a rendered sheet and, locally, a corpus of real photographs with their truth, and records by
/// condition how many holes were found, how many false marks were made, how far off the found ones were, how far off the registration was,
/// and how long each picture took. A drop beyond a set margin against a committed baseline fails, naming the condition and the numbers.
/// docs/DETECTION-LEARNING-STUDY.md section 1 describes the conditions and section 9 how to run it.
/// </summary>
public static class Scoreboard
{
    /// <summary>A picture encoded as JPEG at a quality and decoded again; Core has no encoder, so the command line supplies one.</summary>
    public delegate GrayImage JpegRoundTrip(GrayImage image, int quality);

    /// <summary>A hole counts as found when a mark lies within this distance of it, inches (docs/DETECTION-LEARNING-STUDY.md section 1).</summary>
    public const double FoundWithinInches = 0.1;

    /// <summary>The synthetic sheet: GL-CF25-LTR at 300 dpi with one hole on each of its 25 bulls, as the study measured it.</summary>
    public const string SyntheticSheetFile = "GL-CF25-LTR.gltd.json";

    public const double SyntheticDpi = 300;

    /// <summary>The seeds the committed baseline was measured with: two, as the study used.</summary>
    public static IReadOnlyList<int> DefaultSeeds { get; } = [291, 292];

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// The conditions of docs/DETECTION-LEARNING-STUDY.md section 1, each alone. Shading multiplies the picture, as light falling on the paper
    /// does; glare lifts it toward white; the warps move every column down by the curl or wave at that column, as a sheet bowed across its
    /// width is seen from above; blur, noise and JPEG act on the whole picture.
    /// </summary>
    public static IReadOnlyList<SyntheticCondition> Conditions { get; } =
    [
        new("clean", "no degradation", (image, _, _) => image),
        new("hard shadow", "the lower third at 55 percent", (image, _, _) => Shade(image, (x, y, w, h) => y > h * 2 / 3.0 ? 0.55 : 1)),
        new("soft shadow", "a ramp from full light at the top to 55 percent at the bottom", (image, _, _) => Shade(image, (x, y, w, h) => 1 - (0.45 * y / h))),
        new("hand shadow", "a tilted ellipse at 50 percent with a soft edge", (image, _, _) => Shade(image, HandShadow)),
        new("dim", "the whole sheet at 45 percent", (image, _, _) => Shade(image, (_, _, _, _) => 0.45)),
        new("uneven", "a diagonal from full light to 65 percent", (image, _, _) => Shade(image, (x, y, w, h) => 1 - (0.35 * ((x / w) + (y / h)) / 2))),
        new("glare", "a bright ellipse, clipped to white at its center", (image, _, _) => Glare(image)),
        new("curl 15 px", "every column moved down by 15 px times the sine of its place across the sheet", (image, _, _) => Warp(image, Curl), Curl),
        new("wave 8 px", "two waves across, 8 px", (image, _, _) => Warp(image, Wave), Wave),
        new("blur 1.5 px", "a Gaussian blur, sigma 1.5 px", (image, _, _) => Blur(image, 1.5)),
        new("blur 3 px", "a Gaussian blur, sigma 3 px", (image, _, _) => Blur(image, 3)),
        new("noise 12", "gray noise, sd 12 levels", (image, random, _) => Noise(image, random, 12)),
        new("jpeg 40", "JPEG at quality 40", (image, _, jpeg) => jpeg?.Invoke(image, 40)),
    ];

    /// <summary>The synthetic picture for a seed, undisturbed: GL-CF25-LTR at 300 dpi with one hole on each bull, and each hole's centre in pixels.</summary>
    public static (GrayImage Image, IReadOnlyList<PointD> Holes, IPageMapping Truth) SyntheticPicture(TargetDefinition definition, int seed)
    {
        var (grey, _, holes, truth) = SyntheticPicture(definition, seed, BullColour.Black);
        return (grey, holes, truth);
    }

    /// <summary>
    /// Entry 297: the same picture with its bulls printed in <paramref name="colour"/>, as the two images a photograph gives, grey by luminance
    /// for the markers and codes and value, max(R, G, B), for the holes. The holes and the noise are the same as the black picture's.
    /// </summary>
    public static (GrayImage Grey, GrayImage Value, IReadOnlyList<PointD> Holes, IPageMapping Truth) SyntheticPicture(TargetDefinition definition, int seed, BullColour colour)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var black = SceneBuilder.Build(definition).Pages[0];
        var render = SceneRasterizer.Rasterize(black, SyntheticDpi);
        var random = new Random(seed);
        bool OnInk(double x, double y) => render[(int)(x * SyntheticDpi / 254), (int)(y * SyntheticDpi / 254)] < 128;
        var holes = definition.Bulls.Where(b => b.Scoring)
            .Select(b => (X: b.X + (double)random.Next(-40, 41), Y: b.Y + (double)random.Next(-40, 41)))
            .Select(p => GroupLab.Core.Detection.SyntheticSheet.SampleHole(random, p.X, p.Y, OnInk(p.X, p.Y), HoleBacking.ScannerLid, 0.871))
            .ToList();
        double s = 254 / SyntheticDpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var centres = holes.Select(h => truth.ToImage(new PointD(h.X, h.Y))).ToList();
        if (colour == BullColour.Black)
        {
            var image = GroupLab.Core.Detection.SyntheticSheet.Compose(render, SyntheticDpi, truth, render.Width, render.Height, holes, [], random);
            return (image, image, centres, truth);
        }

        var coloured = BullColours.Apply(black, colour);
        int noise = random.Next();
        GrayImage Seen(Func<Gltd.Binary.Rgb, double> level) =>
            GroupLab.Core.Detection.SyntheticSheet.Compose(SceneRasterizer.Rasterize(coloured, SyntheticDpi, level: level), SyntheticDpi, truth, render.Width, render.Height, holes, [], new Random(noise));
        return (Seen(SceneRasterizer.Luminance), Seen(SceneRasterizer.Value), centres, truth);
    }

    /// <summary>
    /// Entry 297 section 4: the conditions each bull color is read under, shadow, glare and poor light, beside the clean picture. A color is
    /// held to the black sheet's line for the same condition (<c>ScoreboardTests</c>), and a color that fails is not offered.
    /// </summary>
    public static IReadOnlyList<string> ColourConditions { get; } = ["clean", "hard shadow", "glare", "dim"];

    /// <summary>A colored line's name: the color, then the condition, as "red glare".</summary>
    public static string ColourLine(BullColour colour, string condition) => $"{BullColours.Name(colour)} {condition}";

    /// <summary>
    /// Every condition on every seed, read as a photograph (no stated resolution) by <see cref="AutomaticMarking.Run"/>. A condition whose
    /// degradation cannot be made here, JPEG without an encoder, is left out, and the baseline comparison then names it as missing.
    /// </summary>
    public static IReadOnlyList<PictureScore> RunSynthetic(TargetDefinition definition, IImagingBackend backend, IReadOnlyList<int> seeds, JpegRoundTrip? jpeg, IEnumerable<string>? only = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(seeds);
        var chosen = only is null ? Conditions : [.. Conditions.Where(c => only.Contains(c.Name, StringComparer.OrdinalIgnoreCase))];
        var scores = new List<PictureScore>();
        foreach (int seed in seeds)
        {
            // Entry 297: the colored sheets under their conditions, each read from its two images.
            foreach (var colour in new[] { BullColour.Blue, BullColour.Red })
            {
                var lines = Conditions.Where(c => ColourConditions.Contains(c.Name) && (only is null || only.Contains(ColourLine(colour, c.Name), StringComparer.OrdinalIgnoreCase))).ToList();
                if (lines.Count == 0)
                {
                    continue;
                }

                var (grey, value, colourHoles, colourTruth) = SyntheticPicture(definition, seed, colour);
                foreach (var condition in lines)
                {
                    var seenGrey = condition.Degrade(grey, new Random(seed * 31), jpeg);
                    var seenValue = condition.Degrade(value, new Random(seed * 31), jpeg);
                    if (seenGrey is null || seenValue is null)
                    {
                        continue;
                    }

                    scores.Add(Score(ColourLine(colour, condition.Name), seed, condition, definition, backend, seenGrey, seenValue, colourHoles, colourTruth));
                }
            }

            var (flat, holes, truth) = SyntheticPicture(definition, seed);
            foreach (var condition in chosen)
            {
                var image = condition.Degrade(flat, new Random(seed * 31), jpeg);
                if (image is null)
                {
                    continue;
                }

                scores.Add(Score(condition.Name, seed, condition, definition, backend, image, image, holes, truth));
            }
        }

        return scores;
    }

    /// <summary>One synthetic picture read and scored: its grey and value images, the holes' centres before the condition and the truth mapping.</summary>
    private static PictureScore Score(string line, int seed, SyntheticCondition condition, TargetDefinition definition, IImagingBackend backend, GrayImage grey, GrayImage value,
        IReadOnlyList<PointD> holes, IPageMapping truth)
    {
        var metadata = new ImageMetadata("JPEG", grey.Width, grey.Height, null, null, null, null, null, null, null);
        var clock = Stopwatch.StartNew();
        var result = AutomaticMarking.Run(grey, value, metadata, definition, backend);
        clock.Stop();
        var moved = holes.Select(h => condition.Move(h, grey.Width)).ToList();
        var marks = result.Detections.Select(d => d.Image).ToList();
        var (found, falseMarks, errors) = Match(moved, marks, FoundWithinInches * SyntheticDpi);
        bool registered = result.Failure is null && result.Scale is not null;
        var registration = registered
            ? definition.Bulls.Select(b =>
            {
                var page = new PointD(b.X, b.Y);
                var expected = condition.Move(truth.ToImage(page), grey.Width);
                var measured = result.Scale!.Mapping.ToImage(page);
                return Distance(expected, measured) / SyntheticDpi;
            }).ToList()
            : [];
        var inches = errors.Select(e => e / SyntheticDpi).ToList();
        return new PictureScore(line, $"seed {seed}", "holes", holes.Count, marks.Count, found, falseMarks,
            Median(inches), Worst(inches), Median(registration), Worst(registration), Residual(result), registered, clock.ElapsedMilliseconds,
            result.Failure)
        {
            CentreErrors = inches,
            RegistrationErrors = registration,
        };
    }

    /// <summary>
    /// Scores a picture's marks against its truth hole by hole: each hole is matched to the nearest unmatched mark within
    /// <paramref name="within"/>, nearest pairs first, so one mark never counts for two holes. Any unit, the same for both.
    /// </summary>
    public static (int Found, int FalseMarks, IReadOnlyList<double> Errors) Match(IReadOnlyList<PointD> truth, IReadOnlyList<PointD> marks, double within)
    {
        ArgumentNullException.ThrowIfNull(truth);
        ArgumentNullException.ThrowIfNull(marks);
        var pairs = truth.SelectMany((t, i) => marks.Select((m, k) => (i, k, d: Distance(t, m)))).Where(p => p.d <= within).OrderBy(p => p.d).ToList();
        var holesTaken = new HashSet<int>();
        var marksTaken = new HashSet<int>();
        var errors = new List<double>();
        foreach (var (i, k, d) in pairs)
        {
            if (holesTaken.Contains(i) || marksTaken.Contains(k))
            {
                continue;
            }

            holesTaken.Add(i);
            marksTaken.Add(k);
            errors.Add(d);
        }

        return (holesTaken.Count, marks.Count - marksTaken.Count, errors);
    }

    /// <summary>The registration's own error between its markers, inches, from what it measured: every photograph has this, truth or not.</summary>
    public static double? Residual(AutomaticResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Measurement.Registration is { } fit ? Math.Round(fit.RmsResidual / 254, 4) : null;
    }

    /// <summary>The pictures grouped into the scoreboard's lines, in the order their conditions first appear.</summary>
    public static IReadOnlyList<ScoreboardRow> Rows(IEnumerable<PictureScore> pictures)
    {
        ArgumentNullException.ThrowIfNull(pictures);
        return [.. pictures.GroupBy(p => p.Condition).Select(g =>
        {
            var list = g.ToList();
            string truth = list.Select(p => p.Truth).Distinct().Count() == 1 ? list[0].Truth : "mixed";
            var centre = list.SelectMany(p => p.CentreErrors).ToList();
            var registration = list.SelectMany(p => p.RegistrationErrors).ToList();
            int? Sum(Func<PictureScore, int?> of) => list.All(p => of(p) is not null) ? list.Sum(p => of(p)!.Value) : null;
            var times = list.Select(p => p.Milliseconds).Order().ToList();
            return new ScoreboardRow(g.Key, truth, list.Count, list.Count(p => p.Registered), Sum(p => p.Holes), list.Sum(p => p.Marks), Sum(p => p.Found), Sum(p => p.FalseMarks),
                Median(centre), Worst(centre), Median(registration), Worst(registration), times[times.Count / 2], [.. list.Select(p => p.Found)]);
        })];
    }

    /// <summary>
    /// Every way the current rows fall short of the baseline's by more than the margin, each naming the condition and both numbers. A line
    /// the baseline expects to fail is not held to it, and a line the baseline has that the run does not is a drop.
    /// </summary>
    public static IReadOnlyList<string> Drops(IReadOnlyList<ScoreboardRow> baseline, IReadOnlyList<ScoreboardRow> current, ScoreboardMargin margin)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(margin);
        var drops = new List<string>();
        foreach (var was in baseline.Where(b => !b.ExpectedToFail))
        {
            var now = current.FirstOrDefault(c => c.Condition == was.Condition);
            if (now is null)
            {
                drops.Add($"{was.Condition}: not run, and the baseline has it");
                continue;
            }

            if (now.Registered < was.Registered)
            {
                drops.Add($"{was.Condition}: {now.Registered} of {now.Pictures} pictures registered, the baseline {was.Registered} of {was.Pictures}");
            }

            if (was.Found is { } f0 && now.Found is { } f1 && f1 < f0 - margin.Holes)
            {
                drops.Add($"{was.Condition}: {f1} of {now.Holes} holes found, the baseline {f0} of {was.Holes} (margin {margin.Holes})");
            }

            if (was.FalseMarks is { } m0 && now.FalseMarks is { } m1 && m1 > m0 + margin.FalseMarks)
            {
                drops.Add($"{was.Condition}: {m1} false marks, the baseline {m0} (margin {margin.FalseMarks})");
            }

            Grew("median center error", was.MedianCentreInches, now.MedianCentreInches, margin.CentreInches);
            Grew("worst center error", was.WorstCentreInches, now.WorstCentreInches, margin.WorstCentreInches);
            Grew("median registration error", was.MedianRegistrationInches, now.MedianRegistrationInches, margin.RegistrationInches);

            void Grew(string what, double? before, double? after, double allowed)
            {
                if (before is { } b && after is { } a && a > b + allowed)
                {
                    drops.Add(string.Create(CultureInfo.InvariantCulture, $"{was.Condition}: {what} {a:0.0000} in, the baseline {b:0.0000} in (margin {allowed:0.000} in)"));
                }
            }
        }

        return drops;
    }

    /// <summary>Lines better than the baseline by more than the margin, the expected failures above all, so a fix is seen and the baseline moved.</summary>
    public static IReadOnlyList<string> Improvements(IReadOnlyList<ScoreboardRow> baseline, IReadOnlyList<ScoreboardRow> current, ScoreboardMargin margin)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(margin);
        var better = new List<string>();
        foreach (var was in baseline)
        {
            var now = current.FirstOrDefault(c => c.Condition == was.Condition);
            if (now is null)
            {
                continue;
            }

            if (was.Found is { } f0 && now.Found is { } f1 && f1 > f0 + (was.ExpectedToFail ? 0 : margin.Holes))
            {
                better.Add($"{was.Condition}: {f1} of {now.Holes} holes found, the baseline {f0}{(was.ExpectedToFail ? ", a line expected to fail" : "")}");
            }

            if (now.Registered > was.Registered)
            {
                better.Add($"{was.Condition}: {now.Registered} of {now.Pictures} registered, the baseline {was.Registered}");
            }
        }

        return better;
    }

    public static string ToJson(ScoreboardBaseline baseline) => JsonSerializer.Serialize(baseline, Json) + "\n";

    public static ScoreboardBaseline FromJson(string json) =>
        JsonSerializer.Deserialize<ScoreboardBaseline>(json, Json) ?? throw new InvalidDataException("The scoreboard file is empty.");

    /// <summary>The rows as a plain Markdown table, numbers only.</summary>
    public static string Table(IReadOnlyList<ScoreboardRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var text = new StringBuilder();
        text.AppendLine("| Condition | Truth | Pictures | Registered | Found | False marks | Median center error | Worst center error | Median registration error | Worst registration error | Median time |");
        text.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var row in rows)
        {
            string found = row.Found is { } f ? (row.Holes is { } h ? $"{f} of {h}" : f.ToString(CultureInfo.InvariantCulture)) : $"{row.Marks} marks";
            if (row.FoundPerPicture.Count > 1 && row.FoundPerPicture.All(p => p is not null))
            {
                found += $" ({string.Join(", ", row.FoundPerPicture)})";
            }

            string name = row.ExpectedToFail ? row.Condition + " (expected to fail)" : row.Condition;
            text.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {name} | {row.Truth} | {row.Pictures} | {row.Registered} | {found} | {(row.FalseMarks is { } m ? m.ToString(CultureInfo.InvariantCulture) : "")} | {Inches(row.MedianCentreInches)} | {Inches(row.WorstCentreInches)} | {Inches(row.MedianRegistrationInches)} | {Inches(row.WorstRegistrationInches)} | {row.MedianMilliseconds} ms |"));
        }

        return text.ToString();
    }

    /// <summary>One line per picture, for a corpus whose pictures differ: numbers only.</summary>
    public static string PictureTable(IReadOnlyList<PictureScore> pictures)
    {
        ArgumentNullException.ThrowIfNull(pictures);
        var text = new StringBuilder();
        text.AppendLine("| Picture | Condition | Truth | Holes | Marks | Found | False marks | Median center error | Worst center error | Marker residual | Time |");
        text.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var p in pictures)
        {
            text.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {p.Picture} | {p.Condition} | {p.Truth} | {p.Holes?.ToString(CultureInfo.InvariantCulture) ?? ""} | {p.Marks} | {p.Found?.ToString(CultureInfo.InvariantCulture) ?? ""} | {p.FalseMarks?.ToString(CultureInfo.InvariantCulture) ?? ""} | {Inches(p.MedianCentreInches)} | {Inches(p.WorstCentreInches)} | {Inches(p.MarkerResidualInches)} | {p.Milliseconds} ms |"));
        }

        return text.ToString();
    }

    private static string Inches(double? value) => value is { } v ? v.ToString("0.0000", CultureInfo.InvariantCulture) + " in" : "";

    private static double? Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        var sorted = values.Order().ToList();
        double m = sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : (sorted[(sorted.Count / 2) - 1] + sorted[sorted.Count / 2]) / 2;
        return Math.Round(m, 4);
    }

    private static double? Worst(IReadOnlyList<double> values) => values.Count == 0 ? null : Math.Round(values.Max(), 4);

    private static double Distance(PointD a, PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    private static double Curl(double x, int width) => 15 * Math.Sin(2 * Math.PI * x / width);

    private static double Wave(double x, int width) => 8 * Math.Sin(4 * Math.PI * x / width);

    private static double HandShadow(double x, double y, double w, double h)
    {
        // An ellipse over the lower left, tilted 30 degrees, as a hand holding the phone throws it; its edge softens over about 0.1 in.
        double cx = 0.35 * w, cy = 0.68 * h, a = 0.18 * w, b = 0.26 * h, t = Math.PI / 6;
        double dx = x - cx, dy = y - cy;
        double u = ((dx * Math.Cos(t)) + (dy * Math.Sin(t))) / a, v = ((-dx * Math.Sin(t)) + (dy * Math.Cos(t))) / b;
        double r = Math.Sqrt((u * u) + (v * v));
        double edge = 1 / (1 + Math.Exp((r - 1) * Math.Min(a, b) / 15));
        return 1 - (0.5 * edge);
    }

    private static GrayImage Shade(GrayImage image, Func<double, double, double, double, double> factor)
    {
        var pixels = new byte[image.Pixels.Length];
        int w = image.Width, h = image.Height;
        Parallel.For(0, h, y =>
        {
            for (int x = 0; x < w; x++)
            {
                pixels[(y * w) + x] = (byte)Math.Clamp(Math.Round(image.Pixels[(y * w) + x] * factor(x, y, w, h)), 0, 255);
            }
        });
        return new GrayImage(w, h, pixels);
    }

    private static GrayImage Glare(GrayImage image)
    {
        // A lamp's hot spot on glossy paper: white at its centre, where the camera clips, falling off as a Gaussian ellipse.
        int w = image.Width, h = image.Height;
        double cx = 0.55 * w, cy = 0.4 * h, ax = 0.14 * w, ay = 0.09 * h;
        var pixels = new byte[image.Pixels.Length];
        Parallel.For(0, h, y =>
        {
            for (int x = 0; x < w; x++)
            {
                double g = Math.Min(1, 1.25 * Math.Exp(-((((x - cx) / ax) * ((x - cx) / ax)) + (((y - cy) / ay) * ((y - cy) / ay)))));
                double v = image.Pixels[(y * w) + x];
                pixels[(y * w) + x] = (byte)Math.Clamp(Math.Round(v + ((255 - v) * g)), 0, 255);
            }
        });
        return new GrayImage(w, h, pixels);
    }

    private static GrayImage Warp(GrayImage image, Func<double, int, double> shiftDown)
    {
        int w = image.Width, h = image.Height;
        var pixels = new byte[image.Pixels.Length];
        Parallel.For(0, w, x =>
        {
            double shift = shiftDown(x, w);
            for (int y = 0; y < h; y++)
            {
                double from = y - shift;
                int y0 = (int)Math.Floor(from);
                double f = from - y0;
                int a = Math.Clamp(y0, 0, h - 1), b = Math.Clamp(y0 + 1, 0, h - 1);
                pixels[(y * w) + x] = (byte)Math.Round((image.Pixels[(a * w) + x] * (1 - f)) + (image.Pixels[(b * w) + x] * f));
            }
        });
        return new GrayImage(w, h, pixels);
    }

    private static GrayImage Blur(GrayImage image, double sigma)
    {
        var blurred = GroupLab.Core.Detection.SyntheticSheet.Blur([.. image.Pixels.Select(p => (float)p)], image.Width, image.Height, sigma);
        return new GrayImage(image.Width, image.Height, [.. blurred.Select(v => (byte)Math.Clamp(Math.Round(v), 0, 255))]);
    }

    private static GrayImage Noise(GrayImage image, Random random, double sd) =>
        new(image.Width, image.Height, [.. image.Pixels.Select(p => (byte)Math.Clamp(Math.Round(p + (sd * SyntheticSurface.Gaussian(random))), 0, 255))]);
}
