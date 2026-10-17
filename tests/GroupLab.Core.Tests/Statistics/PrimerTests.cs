using GroupLab.Core.Statistics;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Statistics;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 438 section 2.5: the statistics primer on grouplab.org draws every figure from
/// docs/figures/primer/primer-data.json and computes nothing itself, so the file has to be exactly what GroupLab's own statistics give
/// on its seed. When a change to the statistics changes a number, this fails until `grouplab primer` is run and the file committed.
/// </summary>
public class PrimerTests
{
    [Fact]
    public void TheCommittedNumbersAreWhatGroupLabComputes()
    {
        string committed = File.ReadAllText(Repo.PathTo("docs", "figures", "primer", "primer-data.json")).Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.True(committed == PrimerNumbers.Json(), "docs/figures/primer/primer-data.json is not what GroupLab computes: run `grouplab primer` and commit it");
    }

    [Fact]
    public void ItsFiguresAgreeWithThePublishedArticles()
    {
        using var file = System.Text.Json.JsonDocument.Parse(PrimerNumbers.Json());
        var root = file.RootElement;
        // How many shots: from five shots the true dispersion is 0.68 to 1.92 times what was measured; 20 percent tighter takes 81 a load.
        var five = root.GetProperty("spread").EnumerateArray().First(s => s.GetProperty("n").GetInt32() == 5);
        Assert.Equal(0.675, five.GetProperty("sigmaLowMultiple").GetDouble(), 3);
        Assert.Equal(1.916, five.GetProperty("sigmaHighMultiple").GetDouble(), 3);
        Assert.Equal(81, root.GetProperty("shotsPerLoad").EnumerateArray().First(s => s.GetProperty("tighterPercent").GetInt32() == 20).GetProperty("shots").GetInt32());
        // CEP explained: CEP 50 and mean radius are 1.18 and 1.25 sigma.
        Assert.Equal(1.177, root.GetProperty("cep50Factor").GetDouble(), 3);
        Assert.Equal(1.253, root.GetProperty("meanRadiusFactor").GetDouble(), 3);
        // Two loads that are the same: the test calls them different about one time in twenty, as a 5 percent test should.
        foreach (var same in root.GetProperty("sameLoads").EnumerateArray())
        {
            Assert.InRange(same.GetProperty("testSaysDifferent").GetDouble(), 0.03, 0.07);
        }
    }
}
