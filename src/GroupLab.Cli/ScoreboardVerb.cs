using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Evaluation;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using OpenCvSharp;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab scoreboard</c>, NOTES-FROM-PLANNING.md entry 291 section 7: the detection scoreboard of docs/DETECTION-LEARNING-STUDY.md
/// section 9. <c>--synthetic</c> reads the synthetic degradations; <c>--corpus</c> a folder of real photographs, each in its own folder with
/// a <c>truth.json</c>; <c>truth</c> makes a truth file from a scan of the same sheet. Real photographs are read where they are and never
/// copied anywhere by this verb: the corpus stays on the machine it is on.
/// </summary>
public static class ScoreboardVerb
{
    private const string Usage =
        "usage: grouplab scoreboard --synthetic [--seeds 291,292] [--only <condition>] [--out <file.json>] [--table <file.md>] [--baseline <file.json>]\n" +
        "       grouplab scoreboard --corpus <folder> [--out <file.json>] [--table <file.md>] [--baseline <file.json>]\n" +
        "       grouplab scoreboard truth --scan <scan> --sheet <file.gltd.json> [--calibre <inches>] --out <truth.json>\n" +
        "  --margin holes=1,false=1,center=0.005,worst=0.03,registration=0.005 overrides the baseline's own margin.\n" +
        "  Exits 1 naming each condition that fell beyond the margin against the baseline, 2 on a usage error.";

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        if (args.Length > 0 && args[0] == "truth")
        {
            return Truth(args[1..], output, error);
        }

        string? Option(string name) => Array.IndexOf(args, name) is int i and >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        bool synthetic = args.Contains("--synthetic");
        string? corpus = Option("--corpus");
        if (synthetic == (corpus is not null))
        {
            error.WriteLine(Usage);
            return 2;
        }

        string? baselinePath = Option("--baseline");
        var baseline = baselinePath is not null ? Scoreboard.FromJson(File.ReadAllText(baselinePath)) : null;
        var margin = Option("--margin") is { } m ? ParseMargin(m) : baseline?.Margin ?? new ScoreboardMargin();
        IReadOnlyList<PictureScore> pictures;
        IReadOnlyList<int> seeds = [];
        string? only = null;
        if (synthetic)
        {
            seeds = Option("--seeds") is { } s ? [.. s.Split(',').Select(v => int.Parse(v, CultureInfo.InvariantCulture))] : baseline?.Seeds ?? Scoreboard.DefaultSeeds;
            var definition = Sheet(Path.Combine("targets", Scoreboard.SyntheticSheetFile));
            only = Option("--only");
            pictures = Scoreboard.RunSynthetic(definition, new OpenCvSharpBackend(), seeds, Jpeg, only is null ? null : [only]);
        }
        else
        {
            pictures = RunCorpus(corpus!, error);
        }

        var rows = Scoreboard.Rows(pictures);
        if (baseline is not null)
        {
            rows = [.. rows.Select(r => baseline.Rows.FirstOrDefault(b => b.Condition == r.Condition) is { } b ? r with { ExpectedToFail = b.ExpectedToFail, Note = b.Note } : r)];
        }

        string table = Scoreboard.Table(rows) + (synthetic ? "" : "\n" + Scoreboard.PictureTable(pictures));
        output.Write(table);
        if (Option("--table") is { } tablePath)
        {
            File.WriteAllText(tablePath, table);
        }

        if (Option("--out") is { } outPath)
        {
            var file = new ScoreboardBaseline(
                ["NOTES-FROM-PLANNING.md entry 291 section 7: the detection scoreboard, written by grouplab scoreboard."],
                DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), seeds, margin, rows);
            File.WriteAllText(outPath, Scoreboard.ToJson(file));
        }

        if (baseline is null)
        {
            return 0;
        }

        // A run of one condition is held only to that condition's line.
        var held = only is null ? baseline.Rows : [.. baseline.Rows.Where(r => string.Equals(r.Condition, only, StringComparison.OrdinalIgnoreCase))];
        foreach (string better in Scoreboard.Improvements(held, rows, margin))
        {
            output.WriteLine($"better than the baseline: {better}");
        }

        var drops = Scoreboard.Drops(held, rows, margin);
        foreach (string drop in drops)
        {
            error.WriteLine($"DROP {drop}");
        }

        return drops.Count > 0 ? 1 : 0;
    }

    /// <summary>A picture through JPEG at a quality and back, with OpenCV's encoder, for the synthetic JPEG condition.</summary>
    public static GrayImage Jpeg(GrayImage image, int quality)
    {
        ArgumentNullException.ThrowIfNull(image);
        using var mat = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
        Cv2.ImEncode(".jpg", mat, out byte[] bytes, new ImageEncodingParam(ImwriteFlags.JpegQuality, quality));
        using var decoded = Cv2.ImDecode(bytes, ImreadModes.Grayscale);
        return OpenCvSharpBackend.Copy(decoded);
    }

    /// <summary>
    /// Every folder under <paramref name="folder"/> holding a <c>truth.json</c>, read as the phone read it (grey and the brightest channel, the
    /// focal length the phone recorded, the calibre), and scored in the sheet's own inches against the truth.
    /// </summary>
    public static IReadOnlyList<PictureScore> RunCorpus(string folder, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(error);
        var scores = new List<PictureScore>();
        var backend = new OpenCvSharpBackend();
        foreach (string truthPath in Directory.EnumerateFiles(folder, "truth.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            string dir = Path.GetDirectoryName(truthPath)!;
            var truth = JsonNode.Parse(File.ReadAllText(truthPath))!;
            string picture = Path.Combine(dir, (string)truth["picture"]!);
            string sheetFile = (string)truth["sheet"]!;
            var definition = Sheet(File.Exists(sheetFile) ? sheetFile : Path.Combine("targets", sheetFile));
            string kind = (string?)truth["truth"] ?? "count-unknown";
            string condition = (string?)truth["condition"] ?? "real";
            string name = Path.GetRelativePath(folder, dir).Replace('\\', '/');
            var calibre = truth["calibre"] is { } c ? Calibre.Of((double)c) : null;
            var (grey, metadata) = ImageLoader.Load(picture);
            var (value, _) = ImageLoader.LoadMaxChannel(picture);
            if (truth["focalLengthMm"] is { } focal && metadata.FocalLengthMm is null)
            {
                metadata = metadata with { FocalLengthMm = (double)focal, FocalLength35mm = (int?)truth["focalLength35mm"] };
            }

            var clock = Stopwatch.StartNew();
            var result = AutomaticMarking.Run(grey, value, metadata, definition, backend, calibre: calibre);
            clock.Stop();
            bool registered = result.Failure is null && result.Scale is not null;
            var marks = registered ? result.Detections.Select(d => result.Scale!.Mapping.ToPage(d.Image)).Select(p => new PointD(p.X / 254, p.Y / 254)).ToList() : [];
            int? holes = kind switch
            {
                "holes" => truth["shots"]!.AsArray().Count,
                "count" => (int?)truth["count"],
                _ => null,
            };
            int? found = null, falseMarks = null;
            IReadOnlyList<double> errors = [];
            if (kind == "holes")
            {
                var expected = truth["shots"]!.AsArray().Select(p => new PointD((double)p![0]!, (double)p[1]!)).ToList();
                (int f, int fm, errors) = Scoreboard.Match(expected, marks, Scoreboard.FoundWithinInches);
                (found, falseMarks) = (f, fm);
            }
            else if (kind == "count" && holes is { } count)
            {
                (found, falseMarks) = (Math.Min(count, marks.Count), Math.Max(0, marks.Count - count));
            }

            var sorted = errors.Order().ToList();
            scores.Add(new PictureScore(condition, name, kind, holes, marks.Count, found, falseMarks,
                sorted.Count > 0 ? Math.Round(sorted[sorted.Count / 2], 4) : null, sorted.Count > 0 ? Math.Round(sorted[^1], 4) : null,
                null, null, Scoreboard.Residual(result), registered, clock.ElapsedMilliseconds, result.Failure)
            {
                CentreErrors = errors,
            });
            error.WriteLine($"{name}: {marks.Count} marks, {(found is { } ff ? ff.ToString(CultureInfo.InvariantCulture) : "?")} found, {clock.ElapsedMilliseconds} ms");
        }

        return scores;
    }

    /// <summary>
    /// A truth file from a scan of the same sheet: every hole GroupLab reads on the scan, in the sheet's own inches (the printed artwork's,
    /// not the paper's, so a photograph that cannot know the print scale is compared like with like). Check it by hand before trusting it.
    /// </summary>
    private static int Truth(string[] args, TextWriter output, TextWriter error)
    {
        string? Option(string name) => Array.IndexOf(args, name) is int i and >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        if (Option("--scan") is not { } scan || Option("--sheet") is not { } sheet || Option("--out") is not { } outPath)
        {
            error.WriteLine(Usage);
            return 2;
        }

        var definition = Sheet(File.Exists(sheet) ? sheet : Path.Combine("targets", sheet));
        var calibre = Option("--calibre") is { } c ? Calibre.Of(double.Parse(c, CultureInfo.InvariantCulture)) : null;
        var (grey, metadata) = ImageLoader.Load(scan);
        var (value, _) = ImageLoader.LoadMaxChannel(scan);
        var result = AutomaticMarking.Run(grey, value, metadata, definition, new OpenCvSharpBackend(), calibre: calibre);
        if (result.Failure is not null || result.Scale is null)
        {
            error.WriteLine($"the scan did not read: {result.Failure}");
            return 1;
        }

        var shots = new JsonArray([.. result.Detections.Select(d => result.Scale.Mapping.ToPage(d.Image))
            .OrderBy(p => p.Y).Select(p => (JsonNode)new JsonArray(Math.Round(p.X / 254, 4), Math.Round(p.Y / 254, 4)))]);
        var file = new JsonObject
        {
            ["why"] = "Every hole GroupLab read on a scan of this sheet, in the sheet's own inches from its top left; checked by hand before use.",
            ["sheet"] = Path.GetFileName(sheet),
            ["truth"] = "holes",
            ["scanPrintScale"] = result.Scale.PrintScale,
            ["shots"] = shots,
        };
        if (calibre is not null)
        {
            file["calibre"] = calibre.DiameterInches;
        }

        File.WriteAllText(outPath, file.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
        output.WriteLine($"{shots.Count} holes written to {outPath}");
        return 0;
    }

    private static TargetDefinition Sheet(string path) =>
        GltdJsonReader.ReadFile(path).Definition ?? throw new InvalidDataException($"{path} did not read as a target definition; run this from the repository's root.");

    private static ScoreboardMargin ParseMargin(string text)
    {
        var margin = new ScoreboardMargin();
        foreach (var part in text.Split(',').Select(p => p.Split('=')))
        {
            double v = double.Parse(part[1], CultureInfo.InvariantCulture);
            margin = part[0] switch
            {
                "holes" => margin with { Holes = (int)v },
                "false" => margin with { FalseMarks = (int)v },
                "center" => margin with { CentreInches = v },
                "worst" => margin with { WorstCentreInches = v },
                "registration" => margin with { RegistrationInches = v },
                _ => throw new ArgumentException($"unknown margin {part[0]}: holes, false, center, worst or registration"),
            };
        }

        return margin;
    }
}
