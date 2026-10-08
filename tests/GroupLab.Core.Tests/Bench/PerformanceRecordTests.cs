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

        foreach (string heading in new[] { "## The baseline gate, entry 388 section 4", "## The method", "## How the record is made", "## What was found while measuring, and not changed", "## The record", "## The interface, control by control" })
        {
            Assert.Contains(heading, record, StringComparison.Ordinal);
        }

        // A table with no machine beside it cannot be compared with the next one.
        Assert.Contains("Measured ", record, StringComparison.Ordinal);
        Assert.Contains("processors", record, StringComparison.Ordinal);
        Assert.Contains("commit ", record, StringComparison.Ordinal);
        Assert.Contains("Release build", record, StringComparison.Ordinal);

        // The record says what has been optimised and that no figure moved, which is the point of it.
        Assert.Contains("has changed no figure", record, StringComparison.Ordinal);

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

    /// <summary>
    /// Entry 388 section 4, Phase 9's baseline gate: a figure fails when it is more than a quarter and 20 ms slower than its baseline, a
    /// figure the baseline names and the run did not measure fails, and every figure the committed baseline holds is one the gate names.
    /// </summary>
    [Fact]
    public void TheBaselineGateFailsASlowerFigureAndAMissingOne()
    {
        static BenchMeasurement M(string area, string name, double ms) => new(area, name, "", 5, ms, ms, ms, null, null);
        var baseline = System.Text.Json.Nodes.JsonNode.Parse("""{ "bench": { "images / load a 600 dpi Letter scan": 600, "statistics / the CEP table": 10 } }""")!.AsObject();

        Assert.True(BenchGate.Check([M("images", "load a 600 dpi Letter scan", 740), M("statistics", "the CEP table", 29)], baseline).Passed);
        var (lines, passed) = BenchGate.Check([M("images", "load a 600 dpi Letter scan", 760), M("statistics", "the CEP table", 29)], baseline);
        Assert.False(passed);
        Assert.StartsWith("FAILED  images / load a 600 dpi Letter scan: 760 ms against 600 ms (+27 percent)", lines[0], StringComparison.Ordinal);
        Assert.False(BenchGate.Check([M("images", "load a 600 dpi Letter scan", 600)], baseline).Passed);

        var committed = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Repo.PathTo("docs", "performance-baseline.json")))!.AsObject();
        Assert.All(committed["bench"]!.AsObject(), f => Assert.Contains(f.Key, BenchGate.Gated));
        Assert.Contains("grouplab bench --gate docs/performance-baseline.json", committed["about"]!.GetValue<string>(), StringComparison.Ordinal);
    }
}
