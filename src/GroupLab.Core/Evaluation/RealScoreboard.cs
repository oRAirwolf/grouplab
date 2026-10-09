using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Evaluation;

/// <summary>
/// What a person did to GroupLab's marks on a sent target, read from the package the application sends (entry 165 section 3 item 3): the
/// shots they kept at the end, in the picture's stored pixels, and how many of GroupLab's marks they kept, moved, added, removed or called
/// not a shot.
/// </summary>
public sealed record PersonsMarks(IReadOnlyList<PointD> Shots, int Detected, int Kept, int Moved, int Added, int Removed, int NotAShot);

/// <summary>
/// One sent target re-read by a build and scored against the person's own marks: numbers and labels only, NOTES-FROM-PLANNING.md entry 394
/// section 1. Never the photograph, never a location, never a name or a file path. <see cref="Public"/> is false for a target sent for
/// testing only, whose row may be used to improve detection and must never appear in anything public.
/// </summary>
public sealed record RealRow(
    string Submission,
    string Month,
    bool Public,
    string Build,
    string Target,
    string Capture,
    double? CalibreInches,
    string? Conditions,
    int? Holes,
    int Marks,
    int? Found,
    int? Missed,
    int? FalseMarks,
    double? MedianCentreInches,
    double? WorstCentreInches,
    double? MarkerResidualInches,
    bool Registered,
    int Kept,
    int Moved,
    int Added,
    int Removed,
    int NotAShot,
    string? Note = null)
{
    /// <summary>The line an issue names: what kind of target and capture it was, and which submission.</summary>
    [JsonIgnore]
    public string Line => $"{Target}, {Capture}{(Conditions is null ? "" : ", " + Conditions)}, submission {Submission}";

    /// <summary>Whether the person's own marks give a truth to score against.</summary>
    [JsonIgnore]
    public bool Scored => Holes is not null && Found is not null;
}

/// <summary>
/// The scoreboard of real sent targets, NOTES-FROM-PLANNING.md entry 394: every target a person sent with their corrections, re-read by the
/// current build and scored the way the synthetic scoreboard is (<see cref="Scoreboard.Match"/>, <see cref="Scoreboard.FoundWithinInches"/>),
/// against what the person kept at the end. It runs with no person and no Claude: in the private archive repository's Actions, one row a
/// submission, and every night over all of them, with the synthetic board beside it.
/// </summary>
public static class RealScoreboard
{
    /// <summary>
    /// How many corrected submissions the real scoreboard must hold before automatic tuning runs, entry 394 section 4, and why.
    /// <para>
    /// Tuning is judged on a held-out share, <see cref="HeldOutShare"/>, which it never sees while searching. The study expects tuning to win
    /// "a few holes where lighting or paper sits at the edge of today's thresholds", about two holes in a hundred. Judged on the same holes
    /// before and after, an exact two-sided sign test on holes found by one setting and not the other needs six such holes all one way to
    /// reach p &lt; 0.05 (0.5^6 x 2 = 0.031); at two in a hundred that is 300 held-out holes. The nine corrected sheets sent by 2026-10-09 held 25 shots
    /// each, but a sheet is not always shot full, so taking about 20: 300 holes are 15 held-out submissions, and at a held-out share of 0.3, 50 in all.
    /// Below that, a gain on the held-out share is as likely to be chance as tuning, so the job waits and says how many it has.
    /// </para>
    /// </summary>
    public const int CorrectedForTuning = 50;

    /// <summary>The share of corrected submissions held out of the search and used only to judge its result.</summary>
    public const double HeldOutShare = 0.3;

    private static readonly JsonSerializerOptions Json = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    /// <summary>The person's marks from an application package's <c>corrected</c> part, or null where there is none (an upload from the page).</summary>
    public static PersonsMarks? Read(JsonNode? app)
    {
        if (app?["corrected"]?["marks"] is not JsonArray marks)
        {
            return null;
        }

        int detected = app["detected"]?["marks"] is JsonArray d ? d.Count : 0;
        int removed = app["corrected"]?["removed"] is JsonArray r ? r.Count : 0;
        var shots = new List<PointD>();
        int kept = 0, moved = 0, added = 0, notAShot = 0;
        foreach (var mark in marks)
        {
            if (mark is null)
            {
                continue;
            }

            string change = (string?)mark["change"] ?? "";
            kept += change == "kept" ? 1 : 0;
            moved += change.Contains("moved", StringComparison.Ordinal) ? 1 : 0;
            added += change.Contains("added", StringComparison.Ordinal) ? 1 : 0;
            if ((bool?)mark["notAShot"] == true)
            {
                notAShot++;
                continue;
            }

            // A shot left out of the group for a reason (a flyer, a bad round) is still a hole in the paper, so it stays in the truth.
            shots.Add(new PointD((double)mark["x"]!, (double)mark["y"]!));
        }

        return new PersonsMarks(shots, detected, kept, moved, added, removed, notAShot);
    }

    /// <summary>
    /// The scored row: the person's shots and the build's marks, both in the picture's pixels, taken to the sheet's inches through the
    /// registration the build made, and matched within <see cref="Scoreboard.FoundWithinInches"/>.
    /// </summary>
    public static RealRow Score(string submission, string month, bool isPublic, string build, string target, string capture, double? calibreInches, string? conditions,
        PersonsMarks person, AutomaticResult result, Func<PointD, PointD>? toPageInches)
    {
        ArgumentNullException.ThrowIfNull(person);
        ArgumentNullException.ThrowIfNull(result);
        if (toPageInches is null)
        {
            return new RealRow(submission, month, isPublic, build, target, capture, calibreInches, conditions, person.Shots.Count, result.Detections.Count, 0, person.Shots.Count,
                result.Detections.Count, null, null, Scoreboard.Residual(result), false, person.Kept, person.Moved, person.Added, person.Removed, person.NotAShot,
                result.Failure ?? "not registered");
        }

        var truth = person.Shots.Select(toPageInches).ToList();
        var marks = result.Detections.Select(d => toPageInches(d.Image)).ToList();
        var (found, falseMarks, errors) = Scoreboard.Match(truth, marks, Scoreboard.FoundWithinInches);
        var sorted = errors.Order().ToList();
        return new RealRow(submission, month, isPublic, build, target, capture, calibreInches, conditions, truth.Count, marks.Count, found, truth.Count - found, falseMarks,
            sorted.Count > 0 ? Math.Round(sorted[sorted.Count / 2], 4) : null, sorted.Count > 0 ? Math.Round(sorted[^1], 4) : null, Scoreboard.Residual(result), true,
            person.Kept, person.Moved, person.Added, person.Removed, person.NotAShot);
    }

    /// <summary>One row as a line of JSON, the scoreboard file's format.</summary>
    public static string ToLine(RealRow row) => JsonSerializer.Serialize(row, Json);

    /// <summary>The rows of a scoreboard file, one JSON object a line; blank lines are skipped.</summary>
    public static IReadOnlyList<RealRow> FromLines(IEnumerable<string> lines) =>
        [.. lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => JsonSerializer.Deserialize<RealRow>(l, Json)!)];

    /// <summary>
    /// The lines worse than their baseline by the scoreboard's margins (<see cref="ScoreboardMargin"/>, the synthetic board's): more than
    /// <see cref="ScoreboardMargin.Holes"/> holes lost, more than <see cref="ScoreboardMargin.FalseMarks"/> false marks gained, the median
    /// centre error grown by more than <see cref="ScoreboardMargin.CentreInches"/> or the worst by more than
    /// <see cref="ScoreboardMargin.WorstCentreInches"/>, or a row that registered no longer registering. Each names the line and both numbers.
    /// </summary>
    public static IReadOnlyList<string> Drops(IReadOnlyList<RealRow> baseline, IReadOnlyList<RealRow> rows, ScoreboardMargin margin)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(margin);
        var inv = CultureInfo.InvariantCulture;
        var drops = new List<string>();
        foreach (var now in rows.Where(r => r.Scored))
        {
            if (baseline.FirstOrDefault(b => b.Submission == now.Submission) is not { Scored: true } then)
            {
                continue;
            }

            if (then.Registered && !now.Registered)
            {
                drops.Add($"{now.Line}: registered in the baseline and not now ({now.Note})");
            }

            if (then.Found!.Value - now.Found!.Value > margin.Holes)
            {
                drops.Add(string.Create(inv, $"{now.Line}: found {now.Found} of {now.Holes}, the baseline {then.Found}"));
            }

            if (now.FalseMarks!.Value - then.FalseMarks!.Value > margin.FalseMarks)
            {
                drops.Add(string.Create(inv, $"{now.Line}: {now.FalseMarks} false marks, the baseline {then.FalseMarks}"));
            }

            if (now.MedianCentreInches - then.MedianCentreInches > margin.CentreInches)
            {
                drops.Add(string.Create(inv, $"{now.Line}: median center error {now.MedianCentreInches:0.0000} in, the baseline {then.MedianCentreInches:0.0000} in"));
            }

            if (now.WorstCentreInches - then.WorstCentreInches > margin.WorstCentreInches)
            {
                drops.Add(string.Create(inv, $"{now.Line}: worst center error {now.WorstCentreInches:0.0000} in, the baseline {then.WorstCentreInches:0.0000} in"));
            }
        }

        return drops;
    }

    /// <summary>
    /// The baseline after tonight: a new row is added as it is, a row that found more holes with no more false marks replaces its baseline,
    /// and a row that fell keeps its baseline, so a regression stays visible until it is fixed.
    /// </summary>
    public static IReadOnlyList<RealRow> NextBaseline(IReadOnlyList<RealRow> baseline, IReadOnlyList<RealRow> rows)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(rows);
        var next = baseline.ToDictionary(b => b.Submission);
        foreach (var now in rows)
        {
            if (!next.TryGetValue(now.Submission, out var then) || !then.Scored
                || (now.Scored && now.Found >= then.Found && now.FalseMarks <= then.FalseMarks && (now.Found > then.Found || now.FalseMarks < then.FalseMarks)))
            {
                next[now.Submission] = now;
            }
        }

        return [.. next.Values.OrderBy(r => r.Submission, StringComparer.Ordinal)];
    }

    /// <summary>
    /// A few lines a reader can take in at once, written every night: how many submissions, how many scored, the totals, the drops, and the
    /// synthetic board's verdict beside it. Totals only, so nothing about one testing-only target is in it.
    /// </summary>
    public static string Summary(IReadOnlyList<RealRow> rows, IReadOnlyList<string> drops, string build, string date, string? synthetic)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(drops);
        var inv = CultureInfo.InvariantCulture;
        var scored = rows.Where(r => r.Scored).ToList();
        int holes = scored.Sum(r => r.Holes ?? 0), found = scored.Sum(r => r.Found ?? 0), falseMarks = scored.Sum(r => r.FalseMarks ?? 0);
        var centres = scored.Where(r => r.MedianCentreInches is not null).Select(r => r.MedianCentreInches!.Value).Order().ToList();
        var text = new StringBuilder();
        text.AppendLine(string.Create(inv, $"# The real scoreboard, {date}, {build}"));
        text.AppendLine();
        text.AppendLine(string.Create(inv, $"- {rows.Count} submissions read, {scored.Count} with a person's corrections to score against, {CorrectedForTuning} needed before tuning runs."));
        text.AppendLine(scored.Count == 0
            ? "- Nothing scored yet."
            : string.Create(inv, $"- Found {found} of {holes} holes ({100.0 * found / Math.Max(1, holes):0.0} percent), {falseMarks} false marks, median center error {(centres.Count > 0 ? centres[centres.Count / 2] : 0):0.0000} in over the submissions."));
        text.AppendLine(drops.Count == 0 ? "- No line worse than its baseline." : string.Create(inv, $"- {drops.Count} line{(drops.Count == 1 ? "" : "s")} worse than the baseline; the issue in the error-report repository names each."));
        text.AppendLine(synthetic is null ? "- The synthetic board was not run." : "- The synthetic board: " + synthetic);
        return text.ToString();
    }

    /// <summary>
    /// Whether a submission is held out of tuning: a fixed choice by its name, so the same submission is always on the same side and the search
    /// never sees what it is judged on.
    /// </summary>
    public static bool HeldOut(string submission)
    {
        ArgumentNullException.ThrowIfNull(submission);
        uint hash = 2166136261;
        foreach (char c in submission)
        {
            hash = (hash ^ c) * 16777619;
        }

        return hash % 1000 < HeldOutShare * 1000;
    }
}
