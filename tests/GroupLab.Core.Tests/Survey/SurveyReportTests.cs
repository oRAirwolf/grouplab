using System.Security.Cryptography;
using System.Text.Json.Nodes;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Survey;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Survey;

/// <summary>
/// docs/SURVEY.md sections 2 and 3, NOTES-FROM-PLANNING.md entries 207 and 208: the survey sends what the question lists and nothing more,
/// and the benchmark is the same work on every machine.
/// </summary>
public sealed class SurveyReportTests
{
    private static readonly MachineFacts Machine = new("Windows 11", "X64", "A processor", 8, 16384, "2560 by 1440", 1.5, null, null);

    private static IEnumerable<string> Names(JsonNode? node) => node switch
    {
        JsonObject o => o.SelectMany(p => Names(p.Value).Prepend(p.Key)),
        JsonArray a => a.SelectMany(Names),
        _ => [],
    };

    [Fact]
    public void AReportHoldsOnlyTheNamesTheReceiverTakes()
    {
        var bench = new BenchmarkResult(Benchmark.Workload, 2550, 3300, 1900, [new StageTime("registration", 400)], 700, 25, 25);
        var analyses = new[] { new AnalysisFacts(4000, 3000, 3266, 2449, [new StageTime("detection", 900)], 800) };
        var report = JsonNode.Parse(SurveyReport.Build(SurveyReport.NewInstallation(), "0.2.0", Machine, bench, analyses));
        Assert.All(Names(report), n => Assert.Contains(n, SurveyReport.Keys));
        Assert.Equal(SurveyReport.Schema, (string?)report!["schema"]);
    }

    [Fact]
    public void NothingAboutThePersonOrTheirFilesIsInIt()
    {
        string report = SurveyReport.Build(SurveyReport.NewInstallation(), "0.2.0", SurveyReport.ThisMachine(), null, []);
        foreach (string personal in new[] { Environment.UserName, Environment.MachineName, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) }
            .Where(s => s.Length > 3))
        {
            Assert.DoesNotContain(personal, report, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void AnInstallationIsANewRandomNumberEachTime()
    {
        string one = SurveyReport.NewInstallation(), two = SurveyReport.NewInstallation();
        Assert.Matches("^[0-9a-f]{32}$", one);
        Assert.NotEqual(one, two);
    }

    [Fact]
    public void OnlyTheNewestAnalysesGo()
    {
        var analyses = Enumerable.Range(1, SurveyReport.MostAnalyses + 7).Select(i => new AnalysisFacts(i, i, i, i, [], 1)).ToList();
        var sent = JsonNode.Parse(SurveyReport.Build("0", "0", Machine, null, analyses))!["analyses"]!.AsArray();
        Assert.Equal(SurveyReport.MostAnalyses, sent.Count);
        Assert.Equal(8, (int)sent[0]!["width"]!);
    }

    [Fact]
    public void TheBenchmarkIsTheSamePixelsEveryTime()
    {
        var definition = BuiltIns.Load(Benchmark.SheetFile);
        var (one, holes) = Benchmark.Sheet(definition);
        var (two, _) = Benchmark.Sheet(definition);
        Assert.Equal(25, holes);
        Assert.Equal(SHA256.HashData(one.Pixels), SHA256.HashData(two.Pixels));
    }

    [Fact]
    public void TheBenchmarkFindsEveryHoleItPlacedAndTimesEachStep()
    {
        var result = Benchmark.Run(BuiltIns.Load(Benchmark.SheetFile), new OpenCvSharpBackend());
        Assert.Equal(result.HolesPlaced, result.HolesFound);
        Assert.NotEmpty(result.Stages);
        Assert.True(result.TotalMilliseconds > 0);
        Assert.True(result.PeakMegabytes > 0);
    }
}
