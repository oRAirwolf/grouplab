using System.Security.Cryptography;
using System.Text.Json;
using GroupLab.Cli;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Analysis;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 143, question 45 item 3: the hole counts on Alan's six range scans, checked.
/// <para>
/// <b>Why this exists.</b> Entry 130 item 2b.6 recorded that scan 6 read ten holes. It reads nine, and question 45 established that it
/// read nine at the commit that recorded ten. Nobody noticed for a day, because nothing compared one run against the next. A hole the
/// software used to find and no longer finds is the most serious kind of regression this project can have, and the cheapest guard against
/// it is a number written down where a change trips over it.
/// </para>
/// <para>
/// <b>The scans are not committed and never will be.</b> So this runs only where the folder is present, on Alan's machine, and skips
/// elsewhere with a message saying why rather than passing silently. A skip that does not say what it skipped is how a test quietly stops
/// being a test.
/// </para>
/// <para>
/// <b>Each count is pinned to its file's SHA-256.</b> A scan replaced or rescanned is a different measurement, and comparing a new file
/// against an old count would report a regression that is really a new photograph.
/// </para>
/// </summary>
public class RangeScanCountsTests
{
    private sealed record Expected(string File, string Sha256, string? Calibre, int? HolesWithCalibre, int? HolesWithoutCalibre, int? ShotsAlanRecorded, string Note);

    private sealed record Sheet(string Why, string Folder, string Measured, string MeasuredAt, IReadOnlyList<Expected> Scans);

    private static Sheet Read() =>
        JsonSerializer.Deserialize<Sheet>(File.ReadAllText(Repo.PathTo("tests", "GroupLab.Core.Tests", "Analysis", "range-scan-counts.json")),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    [Fact]
    public void EveryRangeScanStillReadsTheHolesItRead()
    {
        var sheet = Read();
        if (!Directory.Exists(sheet.Folder))
        {
            // Not an assertion: on any machine but Alan's this material does not exist, and pretending to have checked it would be worse
            // than saying plainly that it was not checked.
            Assert.True(true, $"skipped: {sheet.Folder} is not on this machine, so the range scan counts were not checked");
            return;
        }

        var wrong = new List<string>();
        foreach (var scan in sheet.Scans)
        {
            string path = Path.Combine(sheet.Folder, scan.File);
            if (!File.Exists(path))
            {
                wrong.Add($"{scan.File}: the folder is here and this scan is not");
                continue;
            }

            string sha = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
            if (!string.Equals(sha, scan.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                // A different file is a different measurement, so its counts say nothing about this code.
                wrong.Add($"{scan.File}: the file has changed since the counts were taken, so it was not checked. Re-measure and update range-scan-counts.json, or put the original back");
                continue;
            }

            foreach (bool named in new[] { true, false })
            {
                int? want = named ? scan.HolesWithCalibre : scan.HolesWithoutCalibre;
                var calibre = named && scan.Calibre is { } c ? Calibre.Parse(c, out _) : null;
                if (named && (want is null || calibre is null))
                {
                    continue;
                }

                var result = AnalyzeVerb.Analyze(GroupLab.Tests.Support.Temp.Readable(path), null, out string? failure, [Repo.PathTo("targets")], calibre);
                int? got = failure is not null || result?.Failure is not null ? null : result?.Automatic.Detections.Count;
                if (got != want)
                {
                    string how = named ? $"with the calibre {scan.Calibre}" : "with no calibre";
                    wrong.Add($"{scan.File} {how}: {Say(got)} where {Say(want)} was recorded on {sheet.Measured} at {sheet.MeasuredAt}. {scan.Note}");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            "a range scan no longer reads what it read. A hole that stops being found is the most serious regression this project can "
            + "have, so this fails rather than warns:\n  " + string.Join("\n  ", wrong));
    }

    private static string Say(int? holes) => holes is { } n ? $"{n} holes" : "it could not be read";

    /// <summary>The file has to describe every scan in the folder, or a scan could be dropped from the check by deleting a line.</summary>
    [Fact]
    public void TheExpectationCoversEveryScanInTheFolder()
    {
        var sheet = Read();
        if (!Directory.Exists(sheet.Folder))
        {
            Assert.True(true, $"skipped: {sheet.Folder} is not on this machine");
            return;
        }

        var named = sheet.Scans.Select(s => s.File).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = Directory.EnumerateFiles(sheet.Folder, "*.png")
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(f => !named.Contains(f))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0, "these scans are in the folder and not in range-scan-counts.json: " + string.Join(", ", missing));
    }
}
