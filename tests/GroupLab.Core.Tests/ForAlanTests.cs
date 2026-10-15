using System.Globalization;
using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 180 section 2: docs/notes/for-alan.md is what the planning session reads to tell Alan what needs him, so
/// its first line says how many requests are open, and that count is the requests whose status does not say they are answered. A finished
/// request still listed as open makes Alan do the work twice.
/// </summary>
public class ForAlanTests
{
    [Fact]
    public void TheOpenCountIsTheRequestsNotAnswered()
    {
        string text = File.ReadAllText(Repo.PathTo("docs", "notes", "for-alan.md")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var said = Regex.Match(text, @"\*\*Open: (\d+)\.\*\*");
        Assert.True(said.Success, "for-alan.md does not start by saying how many requests are open");

        var open = new List<string>();
        foreach (Match request in Regex.Matches(text, @"(?m)^## (\d+)\. .*\n\n(\*\*.*)$"))
        {
            string status = request.Groups[2].Value;
            if (!status.Contains("Answered", StringComparison.OrdinalIgnoreCase) && !status.Contains("Being applied", StringComparison.Ordinal))
            {
                open.Add(request.Groups[1].Value);
            }
        }

        // "Being applied" is open: Alan is doing it and it closes when he confirms.
        int applying = Regex.Matches(text, @"(?m)^\*\*Opened [^\n]*Being applied").Count;
        Assert.Equal(int.Parse(said.Groups[1].Value, CultureInfo.InvariantCulture), open.Count + applying);
    }

    /// <summary>
    /// Entry 406 section 3: <c>grouplab-change-backup.py</c> makes its folder readable by root only, so request 93's plain
    /// <c>test -f "$B/files/..."</c> failed without a word and its chain stopped after the backup with nothing installed. Every command
    /// in a paste that reads or copies from a backup folder runs under sudo.
    /// </summary>
    [Fact]
    public void NoPasteReadsABackupFolderWithoutSudo()
    {
        var plain = new Regex(@"(?<![\w-])(?<!sudo )(test|\[|ls|cat|stat|head|sha256sum|cmp|diff|cp|install) [^;&|\n`]*(\$B\b|\$\{B\}|grouplab-server/backups/|<backup>)");
        var found = new List<string>();
        foreach (string file in new[] { "notes/for-alan.md", "notes/for-alan-archive.md", "RESTORE.md" })
        {
            string[] lines = File.ReadAllLines(Repo.PathTo(["docs", .. file.Split('/')]));
            for (int i = 0; i < lines.Length; i++)
            {
                if (plain.Match(lines[i]) is { Success: true } m)
                {
                    found.Add($"docs/{file}:{i + 1}: {m.Value}");
                }
            }
        }

        Assert.True(found.Count == 0, "These read a backup folder without sudo, which fails silently because the folder is root's only: "
            + string.Join("; ", found));
    }

    /// <summary>
    /// Entry 411 section 1.4: the three sheets shot on 4 October were thrown away because nothing said they were still needed. An open
    /// request that asks for a sheet to be shot and scanned has a <c>**Keep:**</c> line saying which sheets to keep and until when.
    /// </summary>
    [Fact]
    public void ARequestThatScansAShotSheetSaysToKeepIt()
    {
        string text = File.ReadAllText(Repo.PathTo("docs", "notes", "for-alan.md")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var missing = new List<string>();
        foreach (string request in Regex.Split(text, @"\n(?=## \d+\. )").Skip(1))
        {
            string status = Regex.Match(request, @"\n\n(\*\*.*)").Groups[1].Value;
            bool answered = status.Contains("Answered", StringComparison.OrdinalIgnoreCase);
            if (!answered && Regex.IsMatch(request, @"\b(shoot|shot)\b", RegexOptions.IgnoreCase) && Regex.IsMatch(request, @"\bscan", RegexOptions.IgnoreCase)
                && !request.Contains("**Keep:**", StringComparison.Ordinal))
            {
                missing.Add(request[..request.IndexOf('\n', StringComparison.Ordinal)]);
            }
        }

        Assert.True(missing.Count == 0, "These ask for a shot sheet to be scanned and do not say to keep it: " + string.Join("; ", missing));
    }

    /// <summary>
    /// Entry 426: request 98's paste fetched the server files from commit e959956a, which had been rebased away, so GitHub answered 404
    /// and the paste would have stopped at its first fetch. Every commit a paste fetches from is on the history of what is checked out,
    /// which on CI is main. CI's test job fetches main's history for this; a shallow checkout there fails rather than checking nothing.
    /// </summary>
    [Fact]
    public void EveryPasteFetchesFromACommitOnMain()
    {
        if (Git("rev-parse --is-shallow-repository").Output == "true")
        {
            Assert.True(Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true",
                "The checkout is shallow, so whether a paste's commit is on main cannot be told; ci.yml fetches main's history before the tests.");
            return;
        }

        var stranded = new List<string>();
        foreach (string file in new[] { "for-alan.md", "for-alan-archive.md" })
        {
            string text = File.ReadAllText(Repo.PathTo("docs", "notes", file));
            foreach (string commit in Regex.Matches(text, @"raw\.githubusercontent\.com/oRAirwolf/grouplab/([0-9a-f]{7,40})/").Select(m => m.Groups[1].Value).Distinct())
            {
                if (Git($"merge-base --is-ancestor {commit} HEAD").Code != 0)
                {
                    stranded.Add($"{file}: {commit}");
                }
            }
        }

        Assert.True(stranded.Count == 0, "These pastes fetch from a commit that is not on main, which GitHub answers with 404: " + string.Join("; ", stranded));
    }

    private static (int Code, string Output) Git(string arguments)
    {
        using var git = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = Repo.PathTo(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;
        string output = git.StandardOutput.ReadToEnd().Trim();
        git.StandardError.ReadToEnd();
        git.WaitForExit();
        return (git.ExitCode, output);
    }
}
