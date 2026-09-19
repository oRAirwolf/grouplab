using System.Globalization;
using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 106 section 1: entry 105 was folded as "All eight items" while it had nine sections, and the ninth was a
/// defect still in the build. So an actioned status line that says "all N items" or "all N sections" must match the entry's numbered
/// sections. Status lines are otherwise free prose, so only that claim is checked; a line that names what was done in other words is not.
/// </summary>
public partial class NotesStatusTests
{
    private static readonly string[] Words = ["zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve"];

    [Fact]
    public void AnActionedStatusThatCountsTheSectionsCountsThemAll()
    {
        string[] lines = File.ReadAllLines(Repo.PathTo("docs", "NOTES-FROM-PLANNING.md"));
        var entries = lines.Select((l, i) => (l, i)).Where(x => x.l.StartsWith("## ", StringComparison.Ordinal)).Select(x => x.i).ToList();
        int checkedCount = 0;
        for (int e = 0; e < entries.Count; e++)
        {
            int from = entries[e], to = e + 1 < entries.Count ? entries[e + 1] : lines.Length;
            string? status = lines[from..to].FirstOrDefault(l => l.StartsWith("**Status: actioned", StringComparison.Ordinal));
            if (status is null || AllCount().Match(status) is not { Success: true } claim)
            {
                continue;
            }

            string said = claim.Groups["n"].Value.ToLowerInvariant();
            int claimed = int.TryParse(said, NumberStyles.None, CultureInfo.InvariantCulture, out int n) ? n : Array.IndexOf(Words, said);
            int sections = lines[from..to].Count(l => Section().IsMatch(l));
            checkedCount++;
            Assert.True(claimed == sections, $"{lines[from]} says \"all {said}\" in its status line and has {sections} numbered sections. Name the sections not done, or correct the count.");
        }

        // Entry 105's corrected line no longer counts, so today nothing is checked; the test is a guard on the next fold.
        Assert.True(checkedCount >= 0);
    }

    [GeneratedRegex(@"\b[Aa]ll (?<n>\d+|zero|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve) (?:items|sections)\b")]
    private static partial Regex AllCount();

    [GeneratedRegex(@"^### \d+\. ")]
    private static partial Regex Section();
}
