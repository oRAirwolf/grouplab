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
/// a <c>truth.json</c>, or a store-bought target's folder with only its scans (entry 325); <c>truth</c> makes a truth file from a scan of the same sheet. Real photographs are read where they are and never
/// copied anywhere by this verb: the corpus stays on the machine it is on.
/// </summary>
public static class ScoreboardVerb
{
    private const string Usage =
        "usage: grouplab scoreboard --synthetic [--seeds 291,292] [--only <condition,...>] [--out <file.json>] [--table <file.md>] [--baseline <file.json>]\n" +
        "       grouplab scoreboard --any-target [--seeds 318,319] [--only <line>] [--out <file.json>] [--table <file.md>] [--baseline <file.json>]\n" +
        "       grouplab scoreboard --corpus <folder> [--out <file.json>] [--table <file.md>] [--baseline <file.json>]\n" +
        "       grouplab scoreboard truth --scan <scan> --sheet <file.gltd.json> [--calibre <inches>] --out <truth.json>\n" +
        "  A corpus folder whose truth.json says \"target\": \"any\" is a store-bought target: picture, blank, count and dpi (entry 308).\n" +
        "  A folder with blank.png, shot.png or both and no truth.json is one too, at 600 dpi with the count unknown (entry 325).\n" +
        "  --margin holes=1,false=1,center=0.005,worst=0.03,registration=0.005 overrides the baseline's own margin.\n" +
        "  Exits 1 naming each condition that fell beyond the margin against the baseline, 2 on a usage error.";

    /// <summary>A store-bought target's blank scan and its shot one, in a folder with no truth file (entry 308's layout, entry 325).</summary>
    public const string BlankScan = "blank.png", ShotScan = "shot.png";

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        if (args.Length > 0 && args[0] == "truth")
        {
            return Truth(args[1..], output, error);
        }

        if (Array.IndexOf(args, "--blanks") is int b and >= 0 && b + 1 < args.Length)
        {
            return Blanks(args[b + 1], output);
        }


        string? Option(string name) => Array.IndexOf(args, name) is int i and >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        bool synthetic = args.Contains("--synthetic");
        bool anyTarget = args.Contains("--any-target");
        string? corpus = Option("--corpus");
        if ((synthetic ? 1 : 0) + (anyTarget ? 1 : 0) + (corpus is not null ? 1 : 0) != 1)
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
            pictures = Scoreboard.RunSynthetic(definition, new OpenCvSharpBackend(), seeds, Jpeg, only?.Split(','));
        }
        else if (anyTarget)
        {
            // Entry 318 section 2: targets GroupLab did not print, drawn in code, read by the finder the marking screens offer for them.
            seeds = Option("--seeds") is { } s ? [.. s.Split(',').Select(v => int.Parse(v, CultureInfo.InvariantCulture))] : baseline?.Seeds ?? Scoreboard.AnyTargetSeeds;
            only = Option("--only");
            pictures = Scoreboard.RunAnyTarget(new OpenCvSharpBackend(), seeds, only is null ? null : [only]);
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

        string table = Scoreboard.Table(rows) + (synthetic || anyTarget ? "" : "\n" + Scoreboard.PictureTable(pictures));
        output.Write(table);
        if (Option("--table") is { } tablePath)
        {
            File.WriteAllText(tablePath, table);
        }

        if (Option("--out") is { } outPath)
        {
            var file = new ScoreboardBaseline(
                anyTarget
                    ? [
                        "NOTES-FROM-PLANNING.md entry 318 section 2: the scoreboard's any-target class, docs/DETECTION-LEARNING-STUDY.md section 7.",
                        "Four targets GroupLab did not print, drawn in code (black bulls, fluorescent, diamonds, grid), 16 holes of a .308 each, at 200 dpi,",
                        "read by the finder for such targets under four conditions on two seeds. AnyTargetScoreboardTests holds every build to these lines.",
                        "Written by grouplab scoreboard --any-target --out; when a change makes a line better, move it in the same commit and say why.",
                    ]
                    : ["NOTES-FROM-PLANNING.md entry 291 section 7: the detection scoreboard, written by grouplab scoreboard."],
                DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), seeds, margin, rows);
            File.WriteAllText(outPath, Scoreboard.ToJson(file));
        }

        if (baseline is null)
        {
            return 0;
        }

        // A run of some conditions, named with commas, is held only to their lines.
        var held = only is null ? baseline.Rows : [.. baseline.Rows.Where(r => only.Split(',').Contains(r.Condition, StringComparer.OrdinalIgnoreCase))];
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

            // Entry 308: a target GroupLab did not print, a store-bought one, read by the detector that needs no printed artwork.
            if ((string?)truth["target"] == "any")
            {
                scores.AddRange(AnyTarget(dir, truth, Path.GetRelativePath(folder, dir).Replace('\\', '/'), backend, error));
                continue;
            }

            string picture = Path.Combine(dir, (string)truth["picture"]!);
            string sheetFile = (string)truth["sheet"]!;
            // Entry 354: a sheet the generator made is in no library, so its definition sits beside the picture in the corpus folder.
            var definition = Sheet(File.Exists(Path.Combine(dir, sheetFile)) ? Path.Combine(dir, sheetFile) : File.Exists(sheetFile) ? sheetFile : Path.Combine("targets", sheetFile));
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

        // Entries 325 and 327: a store-bought target's folder as it is filled, before anybody writes a truth file for it: blank.png, scanned
        // before it is shot, and shot.png once it is, both at 600 dpi. It is read as an "any target" case; the shot scan's count stays unknown
        // until a truth file names it. Either scan may be a crop of the target, so nothing here looks for the sheet's outline or its corners.
        foreach (string dir in Directory.EnumerateDirectories(folder, "*", SearchOption.AllDirectories).Prepend(folder).Order(StringComparer.Ordinal))
        {
            bool blank = File.Exists(Path.Combine(dir, BlankScan)), shot = File.Exists(Path.Combine(dir, ShotScan));
            if (File.Exists(Path.Combine(dir, "truth.json")) || !(blank || shot))
            {
                continue;
            }

            var truth = new JsonObject { ["target"] = "any" };
            if (shot)
            {
                truth["picture"] = ShotScan;
            }

            if (blank)
            {
                truth["blank"] = BlankScan;
            }

            string name = dir == folder ? Path.GetFileName(Path.GetFullPath(folder).TrimEnd('\\', '/')) : Path.GetRelativePath(folder, dir).Replace('\\', '/');
            scores.AddRange(AnyTarget(dir, truth, name, backend, error));
        }

        return scores;
    }


    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 308 section 4: a store-bought target, scanned blank before it was shot and again after, as an "any target"
    /// case. The truth file says <c>"target": "any"</c>, the shot scan (<c>picture</c>), the blank scan (<c>blank</c>, optional), how many shots
    /// were fired (<c>count</c>) and the scans' resolution (<c>dpi</c>, 600 where it is not given). The detector for a target GroupLab did not
    /// print, the neutral darkness detector of docs/DETECTION-LEARNING-STUDY.md section 7, reads both: on the shot scan its marks are held to
    /// the count, and on the blank one every mark is a false one, which is what makes a store-bought target's printing a hard test. The pictures
    /// stay in C:\Dev\grouplab-local\commercial-targets and are never committed or shown. Entry 318 section 2: they are read by the finder the
    /// marking screens offer for such a target, that detector with the paper's level taken locally and two more looks, a light hole in dark
    /// print and a dark centre in a bright ring; <c>calibre</c> in the truth file names the bullet, where it is known.
    /// </summary>
    public static IReadOnlyList<PictureScore> AnyTarget(string dir, JsonNode truth, string name, GroupLab.Core.Imaging.IImagingBackend backend, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(dir);
        ArgumentNullException.ThrowIfNull(truth);
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(error);
        double dpi = (double?)truth["dpi"] ?? 600;
        int? count = (int?)truth["count"];
        var scores = new List<PictureScore>();
        foreach (var (key, condition, expected) in new[] { ("picture", "any target, shot", count), ("blank", "any target, blank", (int?)0) })
        {
            if ((string?)truth[key] is not { } file)
            {
                continue;
            }

            var (value, _) = ImageLoader.LoadMaxChannel(Path.Combine(dir, file));
            var clock = Stopwatch.StartNew();
            var found = GroupLab.Core.Detection.AnyTargetHoleFinder.Find(value, dpi, backend, (double?)truth["calibre"]);
            clock.Stop();
            int marks = found.Holes.Count;
            int? hits = expected is { } n ? Math.Min(n, marks) : null;
            int? falseMarks = expected is { } m ? Math.Max(0, marks - m) : null;
            scores.Add(new PictureScore(condition, name + "/" + file, expected is null ? "count-unknown" : "count", expected, marks, hits, falseMarks,
                null, null, null, null, null, true, clock.ElapsedMilliseconds));
            error.WriteLine($"{name}/{file}: {marks} marks{(expected is { } e ? $" against {e}" : "")}, {clock.ElapsedMilliseconds} ms");

            // Entry 331 section 1: where each mark is, so a false one on a blank can be looked at and given its cause.
            foreach (var hole in found.Holes)
            {
                error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  mark at {hole.Image.X:0},{hole.Image.Y:0} px, {hole.DiameterInches:0.000} in, {hole.Look}{(hole.Doubt is { } d ? ", " + d : "")}"));
            }
        }

        return scores;
    }

    /// <summary>
    /// Entry 331 section 1.2: the store-bought blanks of request 58 with synthetic holes rendered in, scored by the any-target finder. Each
    /// subfolder's blank.png is a 600 dpi colour scan, read at 200 dpi as the scoreboard's any-target pictures are; what lies under each pixel
    /// comes from its colour. A subfolder whose name says Shoot-N-C is reactive. Nothing is written: the blanks and the scores stay local.
    /// </summary>
    private static int Blanks(string folder, TextWriter output)
    {
        const double Scanned = 600, Read = 200;
        var backend = new OpenCvSharpBackend();
        output.WriteLine("| Blank | Calibre | Seed | Found | False marks | Paper | Ink | Ring lines | Red | Touching |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|---|");
        foreach (string dir in Directory.EnumerateDirectories(folder).Order(StringComparer.Ordinal))
        {
            string file = Path.Combine(dir, "blank.png");
            if (!File.Exists(file))
            {
                continue;
            }

            using var colour = Cv2.ImRead(file, ImreadModes.Color);
            using var small = new Mat();
            Cv2.Resize(colour, small, new OpenCvSharp.Size(0, 0), Read / Scanned, Read / Scanned, InterpolationFlags.Area);
            int w = small.Width, h = small.Height;
            var value = new byte[w * h];
            var places = new BlankPlace[w * h];
            var paper = new byte[w * h];
            var ink = new byte[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var p = small.At<Vec3b>(y, x);
                    byte bl = p.Item0, g = p.Item1, r = p.Item2;
                    int k = (y * w) + x;
                    value[k] = Math.Max(r, Math.Max(g, bl));
                    places[k] = r > 140 && g < 110 && bl < 110 ? BlankPlace.Red
                        : value[k] < 90 ? BlankPlace.Ink
                        : Math.Min(r, Math.Min(g, bl)) > 190 ? BlankPlace.Paper
                        : BlankPlace.Other;
                    paper[k] = places[k] == BlankPlace.Paper ? (byte)255 : (byte)0;
                    ink[k] = places[k] == BlankPlace.Ink ? (byte)255 : (byte)0;
                }
            }

            // A ring line is ink within 0.02 in of paper, or paper within 0.02 in of ink: where a printed line and the paper meet.
            int reach = (int)Math.Ceiling(0.02 * Read);
            using var paperMat = Mat.FromPixelData(h, w, MatType.CV_8UC1, paper);
            using var inkMat = Mat.FromPixelData(h, w, MatType.CV_8UC1, ink);
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size((2 * reach) + 1, (2 * reach) + 1));
            using var nearPaper = new Mat();
            using var nearInk = new Mat();
            Cv2.Dilate(paperMat, nearPaper, kernel);
            Cv2.Dilate(inkMat, nearInk, kernel);
            for (int k = 0; k < places.Length; k++)
            {
                int y = k / w, x = k % w;
                if ((places[k] == BlankPlace.Ink && nearPaper.At<byte>(y, x) > 0) || (places[k] == BlankPlace.Paper && nearInk.At<byte>(y, x) > 0))
                {
                    places[k] = BlankPlace.RingLine;
                }
            }

            string name = Path.GetFileName(dir);
            bool reactive = name.Contains("shoot-n-c", StringComparison.OrdinalIgnoreCase);
            foreach (var score in Scoreboard.ScoreBlank(name, new GrayImage(w, h, value), places, Read, reactive, backend))
            {
                string Of(string key) => score.ByPlace.TryGetValue(key, out var c) ? $"{c.Found}/{c.Total}" : "";
                output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"| {name} | {score.CalibreInches:0.000} | {score.Seed} | {score.Found}/{score.Holes} | {score.FalseMarks} | {Of("paper")} | {Of("ink")} | {Of("ringline")} | {Of("red")} | {Of("touching")} |"));
            }
        }

        return 0;
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
