using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Analysis;
using GroupLab.Core.Detection;
using GroupLab.Core.Evaluation;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;

namespace GroupLab.Cli;

/// <summary>
/// The learning loop's command line, NOTES-FROM-PLANNING.md entry 394: it scores sent targets against the person's own corrections, holds the
/// real scoreboard to its baseline every night, and searches the detection constants once there is enough to judge them on. It runs in the
/// private archive repository's Actions (<c>scripts/learning/</c>), with no person and no Claude in the loop; a better set of constants
/// reaches people only as a pull request, through the tests and the nightly.
/// <para>
/// <b>What it reads and writes.</b> A submission folder as the archive holds it: <c>meta.json</c> with the application's package, and the
/// rebuilt picture beside it. What it writes is numbers and labels only (<see cref="RealRow"/>): never the picture, never a location, never a
/// name or a file path. The submission's own id is the row's key.
/// </para>
/// </summary>
public static class LearnVerb
{
    public const string Usage =
        "usage: grouplab learn score <submission folder>... [--build <name>] [--library <directory>] [--out <rows.jsonl>]\n" +
        "       grouplab learn check --rows <rows.jsonl> --baseline <baseline.jsonl> [--write-baseline] [--build <name>] [--summary <file.md>]\n" +
        "                            [--drops <file.md>] [--synthetic <verdict.txt>]\n" +
        "       grouplab learn tune <folder of submission folders> [--rounds <n>] [--out <result.json>] [--library <directory>]\n" +
        "  score appends one row a submission; check exits 1 naming each line worse than its baseline; tune waits, saying so, until the\n" +
        "  real scoreboard holds " + "50 corrected submissions, and otherwise writes the constants it found and whether they passed.";

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        string? Option(string name) => Array.IndexOf(args, name) is int i and >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        var positional = new List<string>();
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                i += args[i] == "--write-baseline" ? 0 : 1;
                continue;
            }

            positional.Add(args[i]);
        }

        string build = Option("--build") ?? "this build";
        string library = Option("--library") ?? AnalyzeVerb.DefaultLibrary;
        switch (args.Length > 0 ? args[0] : "")
        {
            case "score" when positional.Count > 0:
            {
                var rows = positional.Select(folder => ScoreFolder(folder, build, library, null, error)).ToList();
                string text = string.Concat(rows.Select(r => RealScoreboard.ToLine(r) + "\n"));
                if (Option("--out") is { } outPath)
                {
                    File.AppendAllText(outPath, text);
                }
                else
                {
                    output.Write(text);
                }

                return 0;
            }

            case "check" when Option("--rows") is { } rowsPath && Option("--baseline") is { } baselinePath:
                return Check(rowsPath, baselinePath, args.Contains("--write-baseline"), build, Option("--summary"), Option("--drops"), Option("--synthetic"), output, error);

            case "tune" when positional.Count == 1:
                return Tune(positional[0], Option("--rounds") is { } r ? int.Parse(r, CultureInfo.InvariantCulture) : 1, Option("--out"), library, output, error);

            default:
                error.WriteLine(Usage);
                return 2;
        }
    }

    /// <summary>
    /// One submission read by this build, as the application would read it, and scored against the person's corrections. An upload from the
    /// page has none, and a target that is not a GroupLab sheet is not scored yet; each still gets its row, saying so.
    /// </summary>
    public static RealRow ScoreFolder(string folder, string build, string library, RenderDifferenceOptions? detection, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(error);
        var meta = JsonNode.Parse(File.ReadAllText(Path.Combine(folder, "meta.json")))!;
        // The archive names each folder by its day and the receiver's random id, 2026-09-25_2eeac6a3: a key that says nothing about anyone.
        string submission = Path.GetFileName(Path.GetFullPath(folder).TrimEnd('\\', '/'));
        string month = submission.Length >= 7 ? submission[..7] : "";
        bool isPublic = (string?)meta["consent"]?["level"] == "publishable" && meta["exclude_from_public_dataset"]?.GetValue<bool>() != true;
        var app = meta["app"];
        var person = RealScoreboard.Read(app);
        double? calibre = app?["told"]?["calibreInches"] is JsonValue c && c.TryGetValue(out double inches) ? inches : null;
        RealRow Unscored(string target, string capture, string note) =>
            new(submission, month, isPublic, build, target, capture, calibre, null, null, 0, null, null, null, null, null, null, false,
                person?.Kept ?? 0, person?.Moved ?? 0, person?.Added ?? 0, person?.Removed ?? 0, person?.NotAShot ?? 0, note);

        if (person is null)
        {
            return Unscored("unknown", "unknown", (string?)meta["source"] == "app" ? "no corrections in the package" : "sent from the upload page, no corrections to score against");
        }

        string? picture = meta["files"] is JsonArray files && files.Count > 0 ? (string?)files[0]?["stored"] : null;
        if (picture is null || !File.Exists(Path.Combine(folder, picture)))
        {
            return Unscored("unknown", "unknown", "the picture is not in the folder");
        }

        string path = Path.Combine(folder, picture);
        var (grey, metadata) = ImageLoader.Load(path);
        var (value, _) = ImageLoader.LoadMaxChannel(path);
        var backend = new OpenCvSharpBackend();
        var identity = SheetIdentification.Identify(grey, SheetIdentification.Candidates([library]), backend, new GroupLab.Core.Trace.TraceRecorder());
        if (identity.Definition is not { } definition)
        {
            return Unscored("not a GroupLab sheet", "unknown", "store-bought targets are not scored yet");
        }

        var result = AutomaticMarking.Run(grey, value, metadata, definition, backend, calibre: calibre is { } d ? Calibre.Of(d) : null, detectionOptions: detection);
        bool registered = result.Failure is null && result.Scale is not null;
        string capture = result.Scale is { } scale && SheetReference.Correction(result.Measurement.Scale) is not null ? "scan" : "photo";
        string? conditions = result.Capture is { } cap
            ? string.Create(CultureInfo.InvariantCulture, $"{cap.OffAxisDegrees:0} degrees off square, quality {cap.Quality.Score}")
            : null;
        Func<PointD, PointD>? toPage = registered ? p => { var q = result.Scale!.Mapping.ToPage(p); return new PointD(q.X / 254, q.Y / 254); } : null;
        var row = RealScoreboard.Score(submission, month, isPublic, build, definition.Name ?? "GroupLab sheet", capture, calibre, conditions, person, result, toPage);
        error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{submission}: {row.Marks} marks, {row.Found?.ToString(CultureInfo.InvariantCulture) ?? "?"} of {row.Holes} found, {row.FalseMarks} false"));
        return row;
    }

    private static int Check(string rowsPath, string baselinePath, bool writeBaseline, string build, string? summaryPath, string? dropsPath, string? syntheticPath,
        TextWriter output, TextWriter error)
    {
        var rows = RealScoreboard.FromLines(File.ReadAllLines(rowsPath));
        var baseline = File.Exists(baselinePath) ? RealScoreboard.FromLines(File.ReadAllLines(baselinePath)) : [];
        var drops = RealScoreboard.Drops(baseline, rows, new ScoreboardMargin());
        foreach (string drop in drops)
        {
            error.WriteLine("DROP " + drop);
        }

        string? synthetic = syntheticPath is not null && File.Exists(syntheticPath) ? File.ReadAllText(syntheticPath).Trim() : null;
        string date = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string summary = RealScoreboard.Summary(rows, drops, build, date, synthetic);
        output.Write(summary);
        if (summaryPath is not null)
        {
            File.WriteAllText(summaryPath, summary);
        }

        if (dropsPath is not null)
        {
            File.WriteAllText(dropsPath, drops.Count == 0 ? "" : string.Concat(drops.Select(d => "- " + d + "\n")));
        }

        if (writeBaseline)
        {
            File.WriteAllText(baselinePath, string.Concat(RealScoreboard.NextBaseline(baseline, rows).Select(r => RealScoreboard.ToLine(r) + "\n")));
        }

        return drops.Count > 0 ? 1 : 0;
    }

    /// <summary>A detection constant the search may move: its name, how to read it and how to set it.</summary>
    private sealed record Knob(string Name, Func<RenderDifferenceOptions, double> Get, Func<RenderDifferenceOptions, double, RenderDifferenceOptions> Set);

    /// <summary>
    /// The classical thresholds the study's option (b) searches (docs/DETECTION-LEARNING-STUDY.md section 3): each a length or a fraction the
    /// detector compares a mark with, none of them a model.
    /// </summary>
    private static readonly Knob[] Knobs =
    [
        new("OpenRadiusInches", o => o.OpenRadiusInches, (o, v) => o with { OpenRadiusInches = v }),
        new("CloseRadiusInches", o => o.CloseRadiusInches, (o, v) => o with { CloseRadiusInches = v }),
        new("ResidualFraction", o => o.ResidualFraction, (o, v) => o with { ResidualFraction = v }),
        new("MinimumSolidity", o => o.MinimumSolidity, (o, v) => o with { MinimumSolidity = v }),
        new("MaximumAspect", o => o.MaximumAspect, (o, v) => o with { MaximumAspect = v }),
        new("PaperBlockInches", o => o.PaperBlockInches, (o, v) => o with { PaperBlockInches = v }),
    ];

    /// <summary>G3 of docs/DETECTION-PIPELINE.md: recall stays above this when any threshold moves by <see cref="StabilityStep"/> either way.</summary>
    private const double StableRecall = 0.9, StabilityStep = 0.3;

    /// <summary>
    /// The study's option (b), entry 394 section 4. It waits until the real scoreboard holds <see cref="RealScoreboard.CorrectedForTuning"/>
    /// corrected submissions. Then a coordinate search moves each constant by 15 percent either way, keeping a move that finds more holes for
    /// fewer false marks on the synthetic board and the submissions not held out, and drops no synthetic line past the scoreboard's margin.
    /// The result passes only when, on the held-out submissions it never saw, it is better in total and worse on none, and when G3 holds
    /// around it. The output is the constants, the before and after table and the verdict; a pull request is made from it by
    /// <c>scripts/learning/learn.py</c>, never a change to anything shipped.
    /// </summary>
    private static int Tune(string root, int rounds, string? outPath, string library, TextWriter output, TextWriter error)
    {
        var folders = Directory.EnumerateDirectories(root).Where(d => File.Exists(Path.Combine(d, "meta.json"))).Order(StringComparer.Ordinal).ToList();
        var corrected = folders.Where(d => RealScoreboard.Read(JsonNode.Parse(File.ReadAllText(Path.Combine(d, "meta.json")))?["app"]) is not null).ToList();
        var result = new JsonObject { ["corrected"] = corrected.Count, ["needed"] = RealScoreboard.CorrectedForTuning };
        if (corrected.Count < RealScoreboard.CorrectedForTuning)
        {
            result["waiting"] = true;
            result["reason"] = $"{corrected.Count} corrected submissions of the {RealScoreboard.CorrectedForTuning} a held-out check needs (RealScoreboard.CorrectedForTuning)";
            return Finish(result, outPath, output);
        }

        var held = corrected.Where(d => RealScoreboard.HeldOut(Path.GetFileName(d))).ToList();
        var training = corrected.Except(held).ToList();
        var definition = GltdJsonReader.ReadFile(Path.Combine(library, Scoreboard.SyntheticSheetFile)).Definition!;
        var backend = new OpenCvSharpBackend();
        IReadOnlyList<ScoreboardRow> Synthetic(RenderDifferenceOptions o) => Scoreboard.Rows(Scoreboard.RunSynthetic(definition, backend, Scoreboard.DefaultSeeds, ScoreboardVerb.Jpeg, null, o));
        IReadOnlyList<RealRow> Real(IEnumerable<string> set, RenderDifferenceOptions o) => [.. set.Select(d => ScoreFolder(d, "tuning", library, o, TextWriter.Null)).Where(r => r.Scored)];
        static int Net(IEnumerable<ScoreboardRow> s, IEnumerable<RealRow> r) =>
            s.Sum(x => (x.Found ?? 0) - (x.FalseMarks ?? 0)) + r.Sum(x => (x.Found ?? 0) - (x.FalseMarks ?? 0));

        var start = new RenderDifferenceOptions();
        var startSynthetic = Synthetic(start);
        var current = start;
        var currentSynthetic = startSynthetic;
        int currentNet = Net(startSynthetic, Real(training, start));
        var margin = new ScoreboardMargin();
        for (int round = 0; round < rounds; round++)
        {
            foreach (var knob in Knobs)
            {
                foreach (double factor in (double[])[0.85, 1.15])
                {
                    var trial = knob.Set(current, knob.Get(current) * factor);
                    var trialSynthetic = Synthetic(trial);
                    if (Scoreboard.Drops(startSynthetic, trialSynthetic, margin).Count > 0)
                    {
                        continue;
                    }

                    int net = Net(trialSynthetic, Real(training, trial));
                    error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"round {round + 1}, {knob.Name} x{factor}: {net} against {currentNet}"));
                    if (net > currentNet)
                    {
                        (current, currentSynthetic, currentNet) = (trial, trialSynthetic, net);
                    }
                }
            }
        }

        // The judgement, on what the search never saw.
        var before = Real(held, start);
        var after = Real(held, current);
        bool worseOnNone = after.All(a => before.FirstOrDefault(b => b.Submission == a.Submission) is not { } b || (a.Found >= b.Found && a.FalseMarks <= b.FalseMarks));
        int heldBefore = Net([], before), heldAfter = Net([], after);
        bool stable = Knobs.All(k => ((double[])[1 - StabilityStep, 1 + StabilityStep]).All(f =>
        {
            var rows = Synthetic(k.Set(current, k.Get(current) * f));
            int holes = rows.Sum(r => r.Holes ?? 0), found = rows.Sum(r => r.Found ?? 0);
            return holes > 0 && (double)found / holes > StableRecall;
        }));
        bool changed = Knobs.Any(k => k.Get(current) != k.Get(start));
        bool accepted = changed && heldAfter > heldBefore && worseOnNone && stable;

        result["waiting"] = false;
        result["accepted"] = accepted;
        result["reason"] = !changed ? "no move found that does better" : !(heldAfter > heldBefore) ? "not better on the held-out submissions"
            : !worseOnNone ? "worse on at least one held-out submission" : !stable ? "G3 fails around it: recall falls to 90 percent or below at 30 percent off" : "better on the held-out share, worse on nothing, G3 holds";
        result["constants"] = new JsonObject(Knobs.Select(k => KeyValuePair.Create(k.Name, (JsonNode?)Math.Round(k.Get(current), 4))));
        result["was"] = new JsonObject(Knobs.Select(k => KeyValuePair.Create(k.Name, (JsonNode?)k.Get(start))));
        result["heldOut"] = new JsonObject
        {
            ["submissions"] = after.Count,
            ["netBefore"] = heldBefore,
            ["netAfter"] = heldAfter,
            // Entry 394 section 1: a testing-only target's numbers stay out of anything public, and a pull request is public. So the table
            // gives totals over the held-out targets that may be published, and only a count of the others.
            ["publicFound"] = $"{after.Where(r => r.Public).Sum(r => r.Found)} of {after.Where(r => r.Public).Sum(r => r.Holes)}, was {before.Where(r => r.Public).Sum(r => r.Found)}",
            ["publicFalseMarks"] = $"{after.Where(r => r.Public).Sum(r => r.FalseMarks)}, was {before.Where(r => r.Public).Sum(r => r.FalseMarks)}",
            ["testingOnlyCount"] = after.Count(r => !r.Public),
        };
        result["table"] = SyntheticTable(startSynthetic, currentSynthetic);
        return Finish(result, outPath, output);
    }

    private static string SyntheticTable(IReadOnlyList<ScoreboardRow> before, IReadOnlyList<ScoreboardRow> after)
    {
        var text = new StringBuilder("| Synthetic condition | Found before | Found after | False marks before | False marks after |\n|---|---|---|---|---|\n");
        foreach (var b in before)
        {
            var a = after.FirstOrDefault(r => r.Condition == b.Condition);
            text.Append(CultureInfo.InvariantCulture, $"| {b.Condition} | {b.Found} of {b.Holes} | {a?.Found} | {b.FalseMarks} | {a?.FalseMarks} |\n");
        }

        return text.ToString();
    }

    private static int Finish(JsonObject result, string? outPath, TextWriter output)
    {
        string json = result.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        if (outPath is not null)
        {
            File.WriteAllText(outPath, json + "\n");
        }

        output.WriteLine(json);
        return 0;
    }
}
