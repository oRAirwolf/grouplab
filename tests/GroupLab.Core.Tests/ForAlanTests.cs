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
}
