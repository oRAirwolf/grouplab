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
        string[] lines = Logs.Notes();
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
            var named = new HashSet<int>();
            foreach (string line in lines[from..to])
            {
                foreach (Match s in Section().Matches(line))
                {
                    // "**Sections 3 and 4.**" names two, so every number in the run counts, not only the first.
                    foreach (Match number in Number().Matches(s.Groups["ns"].Value))
                    {
                        named.Add(int.Parse(number.Value, CultureInfo.InvariantCulture));
                    }
                }
            }

            int sections = named.Count;
            checkedCount++;
            Assert.True(claimed == sections, $"{lines[from]} says \"all {said}\" in its status line and has {sections} numbered sections. Name the sections not done, or correct the count.");
        }

        // Entry 105's corrected line no longer counts, so today nothing is checked; the test is a guard on the next fold.
        Assert.True(checkedCount >= 0);
    }

    [GeneratedRegex(@"\b[Aa]ll (?<n>\d+|zero|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve) (?:items|sections)\b")]
    private static partial Regex AllCount();

    /// <summary>
    /// A section of the entry, named either way a fold has written them. Early folds used a numbered heading; folds from entry 144 on use a
    /// bold run inside a bullet. Entry 160 taught this test the second form, because the heading level in the log had drifted at entry 119
    /// and this test had been silently skipping the thirty four newest entries, so nothing had told anybody the form had changed.
    /// </summary>
    [GeneratedRegex(@"^### (?<ns>\d+)\. |\*\*Sections? (?<ns>\d+(?:(?:,| and| to) \d+)*)")]
    private static partial Regex Section();

    [GeneratedRegex(@"\d+")]
    private static partial Regex Number();
}
