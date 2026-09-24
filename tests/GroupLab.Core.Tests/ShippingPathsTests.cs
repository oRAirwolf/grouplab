using System.Text.Json;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 150: an executable is built only when the application changes.
/// <para>
/// <b>Why it needed an entry.</b> Alan, reading the releases page: "it seems like a lot of builds and releases are extremely minor, like
/// just updating release note pages being brought up to date or research articles were written. Why does this need a new executable? That
/// seems like a total waste of time." Five of the last twenty seven nightlies changed nothing that ships inside the executable, and each one
/// compiled and tested on three operating systems to produce a build identical to the one before it.
/// </para>
/// <para>
/// <c>.github/shipping-paths.json</c> is the one list of which paths ship and which are content, read by the nightly's gate job and by these
/// tests. The failure this guards against is not somebody deleting the gate; it is the list and the workflow drifting apart quietly, so that
/// the gate keeps answering and stops being right.
/// </para>
/// </summary>
public class ShippingPathsTests
{
    private static JsonElement Lists()
    {
        string path = Path.Combine(Repo.PathTo(".github"), "shipping-paths.json");
        Assert.True(File.Exists(path), $"{path} is the one list of what ships and what is content, and it is not here.");
        return JsonDocument.Parse(File.ReadAllText(path)).RootElement;
    }

    private static IEnumerable<string> Names(JsonElement lists, string which) =>
        lists.GetProperty(which).EnumerateObject().Select(p => p.Name);

    /// <summary>
    /// Entry 150 section 6.2. Every top level entry in the repository is in exactly one list. A path in neither is a failure rather than a
    /// default, because a new top level directory should make somebody decide which side it is on rather than silently picking one: default
    /// to shipping and every content change wastes a build, default to content and something untested is published.
    /// </summary>
    [Fact]
    public void EveryTopLevelEntryIsInExactlyOneList()
    {
        var lists = Lists();
        // Entry 168: the top level defaults are checked and content. What ships is generated from the build, file by file.
        var ships = Names(lists, "checked").ToHashSet(StringComparer.Ordinal);
        var content = Names(lists, "content").ToHashSet(StringComparer.Ordinal);

        var here = Tracked();

        var both = ships.Intersect(content).Order().ToList();
        Assert.True(both.Count == 0, $"in both lists, so the gate cannot decide: {string.Join(", ", both)}");

        var neither = here.Except(ships).Except(content).Order().ToList();
        Assert.True(neither.Count == 0,
            $"in neither list in .github/shipping-paths.json: {string.Join(", ", neither)}. "
            + "Decide whether each one ships inside the executable or is content, and add it. The nightly's gate fails on an unlisted path.");
    }

    /// <summary>
    /// The top level entries git tracks, which is the right question: build output, a scratch folder somebody left beside the repository and
    /// anything ignored are not part of what a commit can change, so they say nothing about whether a build is needed.
    /// </summary>
    private static HashSet<string> Tracked()
    {
        using var git = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", "ls-tree --name-only HEAD")
        {
            WorkingDirectory = Repo.Root,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        });

        Assert.NotNull(git);
        string output = git.StandardOutput.ReadToEnd();
        git.WaitForExit();
        Assert.Equal(0, git.ExitCode);

        return [.. output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(n => n.Trim()).Where(n => n.Length > 0)];
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 168 section 2. Nightly 94 was built from a test, a site script and the log splitter, because the
    /// gate counted <c>tests</c>, <c>scripts</c> and <c>.github</c> as shipping by directory. What ships is now generated from what the
    /// build reads, and these are the cases that went wrong, held one by one.
    /// </summary>
    [Fact]
    public void WhatShipsIsWhatTheBuildReads()
    {
        string path = Path.Combine(Repo.PathTo(".github"), "shipping-generated.json");
        Assert.True(File.Exists(path), ".github/shipping-generated.json is the gate's list of what ships, and it is not here.");

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var ships = doc.RootElement.GetProperty("ships").EnumerateArray().Select(e => e.GetString()!).ToList();
        bool Ships(string file) => ships.Any(s => s == file || (s.EndsWith('/') && file.StartsWith(s, StringComparison.Ordinal)));

        // What the executable is made of, and what its packages copy.
        Assert.True(Ships("src/GroupLab.Core/Marking/Calibre.cs"), "the application's own source must ship");
        Assert.True(Ships("scripts/package-windows.ps1"), "the Windows packaging script decides what is in the download");
        Assert.True(Ships("samples/gl-cf25-ltr-d-25-shots-600-dpi.png"), "the sample is copied into the Windows package");

        // What made nightly 94, none of which is in the executable.
        foreach (string not in new[] { "scripts/claims.py", "scripts/counts.py", "scripts/split-logs.py", "scripts/release-notes.py",
                     "scripts/shipping-gate.py", "tests/GroupLab.Core.Tests/StateFileTests.cs", ".github/workflows/ci.yml" })
        {
            Assert.False(Ships(not), $"{not} is not in the executable or its package, and a change to it built nightly 94.");
        }
    }

    /// <summary>
    /// Entry 168 section 3.2: the release notes generator reads a trailer that wraps, and refuses a note saying the application did not
    /// change. Nightly 94's notes were cut at the first line break, and the generator's own documented example wraps. The cases live in
    /// the script's <c>--self-test</c>, and this runs it, because a check that exists and is never run is not a check.
    /// </summary>
    [Fact]
    public void TheReleaseNotesGeneratorPassesItsOwnSelfTest()
    {
        foreach (string python in new[] { "python3", "python" })
        {
            System.Diagnostics.Process? run;
            try
            {
                run = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(python, "scripts/release-notes.py --self-test")
                {
                    WorkingDirectory = Repo.Root,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                });
            }
            catch (System.ComponentModel.Win32Exception)
            {
                continue;
            }

            Assert.NotNull(run);
            string said = run.StandardOutput.ReadToEnd() + run.StandardError.ReadToEnd();
            run.WaitForExit();
            if (run.ExitCode == 9009)
            {
                continue;   // the Windows store stub that stands in for a missing python
            }

            Assert.True(run.ExitCode == 0, "scripts/release-notes.py --self-test failed:\n" + said);
            return;
        }

        Assert.True(Environment.GetEnvironmentVariable("CI") is null,
            "no python on this CI runner, so the release notes generator's self-test did not run, and it must");
    }

    /// <summary>
    /// Entry 150 section 6.1. The gate exists and every job that builds, packages or releases depends on it. Written against the text of the
    /// workflow because there is no other way to assert the shape of a workflow from here, and a gate no job depends on is a gate that
    /// answers into the air.
    /// </summary>
    [Fact]
    public void TheNightlyHasAGateAndEveryBuildingJobDependsOnIt()
    {
        // Line endings normalised first. A fresh checkout on a Windows runner has CRLF here, and a pattern anchored on "\n  package:\n"
        // passed on this machine and failed there. That has already cost two red pushes in this repository.
        string yaml = File.ReadAllText(Path.Combine(Repo.PathTo(".github"), "workflows", "nightly.yml")).Replace("\r\n", "\n");

        Assert.Contains("application-changed: ${{ steps.gate.outputs.application-changed }}", yaml, StringComparison.Ordinal);
        Assert.Contains("scripts/shipping-gate.py --decide", yaml, StringComparison.Ordinal);

        foreach (string job in new[] { "package", "publish" })
        {
            string header = $"\n  {job}:\n";
            int at = yaml.IndexOf(header, StringComparison.Ordinal);
            Assert.True(at >= 0, $"the nightly has no {job} job any more, so this test no longer describes it.");

            // The job's own block, up to the next job at the same indentation.
            int next = yaml.IndexOf("\n  publish:\n", at + header.Length, StringComparison.Ordinal);
            string block = next > 0 ? yaml[at..next] : yaml[at..];

            Assert.True(block.Contains("needs.name-it.outputs.application-changed == 'yes'", StringComparison.Ordinal),
                $"the nightly's {job} job does not depend on the gate, so content alone would still produce an executable. "
                + "NOTES-FROM-PLANNING.md entry 150 section 2.");
        }
    }

    /// <summary>
    /// The gate's own script is here and says what it is for. A workflow calling a script that does not exist fails at two in the morning
    /// with nobody reading the log.
    /// </summary>
    [Fact]
    public void TheGateScriptIsHere()
    {
        string path = Path.Combine(Repo.PathTo("scripts"), "shipping-gate.py");
        Assert.True(File.Exists(path), "scripts/shipping-gate.py is what the nightly's gate runs, and it is not here.");

        string text = File.ReadAllText(path);
        foreach (string mode in new[] { "--decide", "--lists", "--history" })
        {
            Assert.Contains(mode, text, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Entry 150 section 4: the releases page says plainly that a gap in the numbers means nothing shipped that night, so a night with no
    /// build does not look like a page nobody updated. Same principle as entry 145: say what happened, including when it was nothing.
    /// </summary>
    [Fact]
    public void TheReleasesPageSaysWhyTheNumbersSkip()
    {
        string build = File.ReadAllText(Path.Combine(Repo.PathTo("website"), "build.py"));
        Assert.Contains("a gap in the numbers means nothing shipped that night", build, StringComparison.Ordinal);
    }
}
