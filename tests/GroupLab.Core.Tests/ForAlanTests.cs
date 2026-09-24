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
}
