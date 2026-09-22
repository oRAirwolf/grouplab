using System.Diagnostics;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 138 section 6: a generated history with published and skipped nightlies, checking that a version's notes
/// start at the previous published version.
/// <para>
/// <b>What went wrong without this.</b> Every build's notes opened with a hand written catch-up list covering everything since nightly 18,
/// so each one looked like it held months of work. And the range started at the rolling <c>nightly</c> tag, which moves: a run reading it
/// could see itself or an older build depending on when it looked. Both are the kind of fault that is invisible in the code and obvious the
/// moment a person reads a release page.
/// </para>
/// <para>
/// The history here is built in a temporary repository, so the test does not depend on this project's own tags and cannot be broken by
/// publishing another build.
/// </para>
/// </summary>
public class ReleaseNoteRangeTests
{
    /// <summary>
    /// Removes a temporary git repository. Git marks the files in its object store read only, and Windows refuses to delete those, so the
    /// attribute is cleared first. A temporary folder is never worth failing a test over, so what is left behind is left behind.
    /// </summary>
    private static void Remove(string folder)
    {
        try
        {
            foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(folder, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static string Run(string folder, string exe, string arguments)
    {
        var process = Process.Start(new ProcessStartInfo(exe, arguments)
        {
            WorkingDirectory = folder,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit(60000);
        return process.ExitCode == 0 ? output : output + error;
    }

    private static void Git(string folder, string arguments) => Run(folder, "git", arguments);

    /// <summary>One commit carrying a release note, and optionally a published build's tag afterwards.</summary>
    private static void Commit(string folder, string note, string? tag = null)
    {
        File.AppendAllText(Path.Combine(folder, "work.txt"), note + "\n");
        Git(folder, "add -A");
        Git(folder, $"commit -q -m \"work\" -m \"Release-note: {note}\" -m \"Release-note-kind: changed\"");
        if (tag is not null)
        {
            Git(folder, $"tag {tag}");
        }
    }

    [NeedsPythonFact]
    public void AVersionsNotesStartAtThePreviousPublishedVersion()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-notes-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            Git(folder, "init -q");
            Git(folder, "config user.email a@b.c");
            Git(folder, "config user.name Test");

            // Two published builds, then two nightlies that were cancelled before publishing and so never got a tag, then a published one.
            Commit(folder, "the first thing a person would notice in the whole history", "v0.2.0-nightly.10");
            Commit(folder, "something that shipped in the twelfth build and nothing later", "v0.2.0-nightly.12");
            Commit(folder, "a change from a run that was cancelled before it published");
            Commit(folder, "another change from a run that never published either");
            Commit(folder, "the change the twentieth build was made for", "v0.2.0-nightly.20");

            string notes = Run(folder, SiteSyncTests.PythonOnThisMachine!, $"\"{Repo.PathTo("scripts/release-notes.py")}\" 0.2.0-nightly.20 HEAD");

            // Everything since nightly 12, which is the previous published build: the two from the cancelled runs roll into this one.
            Assert.Contains("a change from a run that was cancelled before it published", notes, StringComparison.Ordinal);
            Assert.Contains("another change from a run that never published either", notes, StringComparison.Ordinal);
            Assert.Contains("the change the twentieth build was made for", notes, StringComparison.Ordinal);

            // And nothing from before it.
            Assert.DoesNotContain("something that shipped in the twelfth build", notes, StringComparison.Ordinal);
            Assert.DoesNotContain("the first thing a person would notice in the whole history", notes, StringComparison.Ordinal);
        }
        finally
        {
            Remove(folder);
        }
    }

    /// <summary>
    /// The first build on a train has no previous published version, so it carries everything. It must not be empty, and it must not fail.
    /// </summary>
    [NeedsPythonFact]
    public void TheFirstBuildCarriesEverything()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-notes-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            Git(folder, "init -q");
            Git(folder, "config user.email a@b.c");
            Git(folder, "config user.name Test");
            Commit(folder, "the only change there has ever been in this history", "v0.2.0-nightly.1");

            string notes = Run(folder, SiteSyncTests.PythonOnThisMachine!, $"\"{Repo.PathTo("scripts/release-notes.py")}\" 0.2.0-nightly.1 HEAD");

            Assert.Contains("the only change there has ever been in this history", notes, StringComparison.Ordinal);
        }
        finally
        {
            Remove(folder);
        }
    }

    /// <summary>The catch-up list is gone from the generator for good, and lives once in the file instead.</summary>
    [Fact]
    public void TheCatchUpListIsNotInTheGeneratorAnyMore()
    {
        string script = File.ReadAllText(Repo.PathTo("scripts/release-notes.py"));

        Assert.DoesNotContain("SINCE_EIGHTEEN", script, StringComparison.Ordinal);
        Assert.DoesNotContain("since-eighteen", script, StringComparison.Ordinal);

        // And the workflow no longer starts from the rolling tag, which moves.
        string nightly = File.ReadAllText(Repo.PathTo(".github/workflows/nightly.yml"));
        Assert.DoesNotContain("previous-nightly", nightly, StringComparison.Ordinal);
    }
}
