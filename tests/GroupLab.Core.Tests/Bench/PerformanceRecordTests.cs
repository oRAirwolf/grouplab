using GroupLab.Cli;
using GroupLab.Cli.Bench;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Bench;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 117 section 4: the record is committed, with the date and the machine it was measured on, so a change's
/// effect is a difference between two committed tables rather than an opinion. A record with no table in it, or with a table nobody can say
/// where came from, is not that, and this test says so rather than letting the document drift into prose.
/// </summary>
public class PerformanceRecordTests
{
    [Fact]
    public void ThePerformanceRecordCarriesBothTablesAndSaysWhereTheyCameFrom()
    {
        string path = Repo.PathTo("docs", "PERFORMANCE.md");
        Assert.True(File.Exists(path), "docs/PERFORMANCE.md is missing: run `grouplab bench --record docs/PERFORMANCE.md`.");
        string record = File.ReadAllText(path);

        foreach (string heading in new[] { "## The gate, which is not written yet", "## The method", "## How the record is made", "## What was found while measuring, and not changed", "## The record", "## The interface, control by control" })
        {
            Assert.Contains(heading, record, StringComparison.Ordinal);
        }

        // A table with no machine beside it cannot be compared with the next one.
        Assert.Contains("Measured ", record, StringComparison.Ordinal);
        Assert.Contains("processors", record, StringComparison.Ordinal);
        Assert.Contains("commit ", record, StringComparison.Ordinal);
        Assert.Contains("Release build", record, StringComparison.Ordinal);

        // The record is the baseline for a phase that has not started, and saying so is the point of it.
        Assert.Contains("has been optimised", record, StringComparison.Ordinal);

        // Every area the benchmark has is a section of the record, so a new area cannot land in a table nobody prints.
        foreach (string area in BenchSuite.Areas)
        {
            Assert.Contains("### " + area, record, StringComparison.Ordinal);
        }

        // Every exclusion is in the record by name, which is entry 117 section 3b's rule and the reason the document is trustworthy.
        foreach (var exclusion in BenchSuite.Exclusions)
        {
            Assert.Contains(exclusion.Name, record, StringComparison.Ordinal);
        }

        Assert.DoesNotContain('—', record);
    }

    /// <summary>The record names the commands that make it, because a table nobody can regenerate is a screenshot.</summary>
    [Fact]
    public void TheRecordNamesTheCommandsThatMakeIt()
    {
        string record = File.ReadAllText(Repo.PathTo("docs", "PERFORMANCE.md"));
        Assert.Contains("grouplab bench --runs 5 --record docs/PERFORMANCE.md", record, StringComparison.Ordinal);
        Assert.Contains("GROUPLAB_BENCH_TO_DOCS=1", record, StringComparison.Ordinal);
        Assert.Contains(BenchVerb.Usage, File.ReadAllText(Repo.PathTo("src", "GroupLab.Cli", "Program.cs")), StringComparison.Ordinal);
    }
}
