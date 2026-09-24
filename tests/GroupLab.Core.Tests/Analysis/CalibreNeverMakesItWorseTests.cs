using System.Security.Cryptography;
using System.Text.Json;
using GroupLab.Cli;
using GroupLab.Core.Detection;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Analysis;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 161: naming the calibre made the reading worse.
/// <para>
/// A friend shot ten 6.5 Creedmoor rounds, one per bull on bulls 1 to 10, and GroupLab detected all ten perfectly. Told nothing, it raised
/// one review item. Told the truth, 0.264 in, it raised six, five of them calling a good hole possibly two. <b>Software that gets worse when
/// it is told the truth has the wrong model in it.</b> The model was a constant: a named calibre replaced the sheet's own measured hole with
/// 0.945 times the bullet, and on that paper a hole measures 1.14 times the bullet.
/// </para>
/// <para>
/// These run on scans that are not in the repository, where they are present, and skip saying so where they are not. That is weaker than a
/// test that always runs, and the always-running half is <see cref="Detection.CalibreSplitTests"/>, which holds the rule itself on synthetic
/// marks. This half is the one that proves the rule on the paper that exposed it.
/// </para>
/// </summary>
public class CalibreNeverMakesItWorseTests(ITestOutputHelper output)
{
    private sealed record Scan(string File, string Sha256, string? Calibre);

    private sealed record RangeSheet(string Folder, IReadOnlyList<Scan> Scans);

    private sealed record Friend(string Folder, string File, string Sha256, string PublishedSha256, string Calibre, int Shots);

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private static string Fixture(string name) => Repo.PathTo("tests", "GroupLab.Core.Tests", "Analysis", name);

    /// <summary>Every local scan with a known calibre, where it is on this machine and is the file the counts were taken from.</summary>
    private static List<(string Name, string Path, Calibre Calibre)> Present(List<string> skipped)
    {
        var found = new List<(string, string, Calibre)>();

        var friend = JsonSerializer.Deserialize<Friend>(File.ReadAllText(Fixture("friend-scan-2026-09-23.json")), Json)!;
        Add(TestData.Folder(friend.File, friend.Folder), friend.File, TestData.Matches(TestData.Path(friend.File, friend.Folder), friend.PublishedSha256) ? friend.PublishedSha256 : friend.Sha256, friend.Calibre);

        var range = JsonSerializer.Deserialize<RangeSheet>(File.ReadAllText(Fixture("range-scan-counts.json")), Json)!;
        foreach (var scan in range.Scans.Where(s => s.Calibre is not null))
        {
            Add(range.Folder, scan.File, scan.Sha256, scan.Calibre!);
        }

        return found;

        void Add(string folder, string file, string sha, string calibre)
        {
            string path = Path.Combine(folder, file);
            if (!File.Exists(path))
            {
                skipped.Add($"{file}: not on this machine");
                return;
            }

            if (!string.Equals(Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path))), sha, StringComparison.OrdinalIgnoreCase))
            {
                skipped.Add($"{file}: a different file from the one measured, so it says nothing about this code");
                return;
            }

            found.Add((file, path, Calibre.Parse(calibre, out _)!));
        }
    }

    private static (int Open, int Oversized, int Holes)? Reading(string path, Calibre? calibre)
    {
        var result = AnalyzeVerb.Analyze(GroupLab.Tests.Support.Temp.Readable(path), null, out string? failure, [Repo.PathTo("targets")], calibre);
        if (failure is not null || result?.Marking is not { } marking)
        {
            return null;
        }

        var items = ReviewQueue.For(marking);
        return (ReviewQueue.Open(items), items.Count(i => i.Kind == ReviewKind.Oversized && !i.Resolved), result.Automatic.Detections.Count);
    }

    /// <summary>
    /// Entry 161 section 8.2, the regression this entry exists to prevent: ten perfectly detected holes, the correct calibre named, and not
    /// one of them called possibly two.
    /// </summary>
    [Fact]
    public void TheFriendsScanRaisesNoDoublesWithTheCorrectCalibreNamed()
    {
        var friend = JsonSerializer.Deserialize<Friend>(File.ReadAllText(Fixture("friend-scan-2026-09-23.json")), Json)!;
        string path = TestData.Path(friend.File, friend.Folder);
        if (!File.Exists(path))
        {
            Assert.True(true, $"skipped: {path} is not on this machine, so entry 161's scan was not checked");
            return;
        }

        var reading = Reading(path, Calibre.Parse(friend.Calibre, out _));
        Assert.NotNull(reading);
        Assert.Equal(friend.Shots, reading.Value.Holes);
        Assert.True(reading.Value.Oversized == 0,
            $"with {friend.Calibre} named, {reading.Value.Oversized} of the {friend.Shots} holes on {friend.File} are called possibly two. "
            + "They are single holes measuring 1.14 of the bullet, and the reference has to be the sheet's own marks, not the calibre.");
    }

    /// <summary>
    /// Entry 161 section 8.3, the general form of the fault, held forever: naming the correct calibre never increases the number of open
    /// review items on any fixture. Section 8.4's table is written to the test output on every run, so the counts are there to read.
    /// </summary>
    [Fact]
    public void NamingTheCorrectCalibreNeverAddsReviewItems()
    {
        var skipped = new List<string>();
        var scans = Present(skipped);
        foreach (string line in skipped)
        {
            output.WriteLine("skipped " + line);
        }

        if (scans.Count == 0)
        {
            Assert.True(true, "skipped: none of the local scans is on this machine");
            return;
        }

        output.WriteLine("scan | calibre | holes without | holes with | open without | open with | doubles without | doubles with");
        var worse = new List<string>();
        foreach (var (name, path, calibre) in scans)
        {
            var without = Reading(path, null);
            var with = Reading(path, calibre);
            if (without is not { } a || with is not { } b)
            {
                worse.Add($"{name}: did not read both ways");
                continue;
            }

            output.WriteLine($"{name} | {calibre.DiameterInches:0.000} | {a.Holes} | {b.Holes} | {a.Open} | {b.Open} | {a.Oversized} | {b.Oversized}");
            if (b.Open > a.Open)
            {
                worse.Add($"{name}: {a.Open} open review items with no calibre and {b.Open} with {calibre.Name} named");
            }
        }

        Assert.True(worse.Count == 0, "naming the correct calibre made these worse, which is entry 161's defect:\n" + string.Join("\n", worse));
    }
}
