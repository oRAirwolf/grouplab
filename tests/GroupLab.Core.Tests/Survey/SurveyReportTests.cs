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
        var report = JsonNode.Parse(SurveyReport.Build(SurveyReport.NewInstallation(), "0.2.0", Machine, [new BenchmarkRun("0.2.0", bench)], analyses));
        Assert.All(Names(report), n => Assert.Contains(n, SurveyReport.Keys));
        Assert.Equal(SurveyReport.Schema, (string?)report!["schema"]);
    }

    [Fact]
    public void NothingAboutThePersonOrTheirFilesIsInIt()
    {
        string report = SurveyReport.Build(SurveyReport.NewInstallation(), "0.2.0", SurveyReport.ThisMachine(), [], []);
        foreach (string personal in new[] { Environment.UserName, Environment.MachineName, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) }
            .Where(s => s.Length > 3))
        {
            Assert.DoesNotContain(personal, report, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 241 section 1: every run goes, each with the version that ran it, so the server can count a machine once
    /// a version by the median of its runs; and never more than a report's share at once.
    /// </summary>
    [Fact]
    public void EveryRunGoesWithTheVersionThatRanIt()
    {
        var runs = Enumerable.Range(1, SurveyReport.MostBenchmarks + 3)
            .Select(i => new BenchmarkRun(i <= 2 ? "0.2.0-nightly.111" : "0.2.0-nightly.112", new BenchmarkResult(Benchmark.Workload, 2550, 3300, 1000 + i, [], 400, 25, 25)))
            .ToList();
        var sent = JsonNode.Parse(SurveyReport.Build(SurveyReport.NewInstallation(), "0.2.0-nightly.112", Machine, runs, []))!["benchmarks"]!.AsArray();
        Assert.Equal(SurveyReport.MostBenchmarks, sent.Count);
        Assert.Equal("0.2.0-nightly.111", (string?)sent[0]!["version"]);
        Assert.Equal("0.2.0-nightly.112", (string?)sent[^1]!["version"]);
        Assert.Equal(1001, (long)sent[0]!["totalMilliseconds"]!);
    }

    /// <summary>Entry 241 section 2.4: a delete request is the number and its schema, and nothing about the machine.</summary>
    [Fact]
    public void ADeleteRequestHoldsTheNumberAndNothingElse()
    {
        string installation = SurveyReport.NewInstallation();
        var request = JsonNode.Parse(SurveyReport.Delete(installation))!.AsObject();
        Assert.Equal(["schema", "installation"], request.Select(p => p.Key));
        Assert.Equal(SurveyReport.DeleteSchema, (string?)request["schema"]);
        Assert.Equal(installation, (string?)request["installation"]);
    }

    /// <summary>Entry 241 section 2.5: the question says what the number is and what can be done with it, before anyone sends it.</summary>
    [Fact]
    public void TheQuestionSaysWhatTheNumberIs()
    {
        string said = string.Join(" ", SurveyReport.WhatIsSent);
        Assert.Contains("not tied to your device, account or network", said, StringComparison.Ordinal);
        Assert.Contains("reset it or delete your reports in Settings", said, StringComparison.Ordinal);
        Assert.Contains("version of GroupLab that ran it", said, StringComparison.Ordinal);
        Assert.True(SurveyReport.WordingVersion >= 2);
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
        var sent = JsonNode.Parse(SurveyReport.Build("0", "0", Machine, [], analyses))!["analyses"]!.AsArray();
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
