using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Publication;
using GroupLab.Core.Survey;
using GroupLab.Core.Updates;

namespace GroupLab.App;

/// <summary>
/// The hardware survey's side of the application, docs/SURVEY.md and NOTES-FROM-PLANNING.md entries 207 and 208, shared by the desktop and
/// the phone. Only when the person has said yes: each analysis's sizes, times and memory are kept in a small file beside the settings,
/// and at most once a week, or when a benchmark is waiting to go, they go in one report and the file is emptied. Said no, or not asked
/// yet, nothing is kept and nothing is sent. Nothing here draws anything.
/// </summary>
public sealed class SurveyQueue(AppSettingsStore settings, Func<MachineFacts> machine)
{
    /// <summary>The most often a report goes, so one machine is one report a week and never a stream.</summary>
    public static readonly TimeSpan Every = TimeSpan.FromDays(7);

    private readonly object gate = new();

    private string AnalysesPath => Path.Combine(Path.GetDirectoryName(settings.Path) ?? ".", "survey-analyses.json");

    /// <summary>Keeps one analysis's facts for the next report, when the person has said yes; otherwise nothing.</summary>
    public void Record(AnalysisFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (settings.LoadSurveyChoice() != SurveyChoice.Yes)
        {
            return;
        }

        lock (gate)
        {
            var kept = Waiting().ToList();
            kept.Add(facts);
            Write(kept.Skip(Math.Max(0, kept.Count - SurveyReport.MostAnalyses)).ToList());
        }
    }

    /// <summary>The analyses kept since the last report.</summary>
    public IReadOnlyList<AnalysisFacts> Waiting()
    {
        try
        {
            if (!File.Exists(AnalysesPath) || JsonNode.Parse(File.ReadAllText(AnalysesPath)) is not JsonArray all)
            {
                return [];
            }

            return [.. all.OfType<JsonObject>().Select(a => new AnalysisFacts(
                (int?)a["width"] ?? 0, (int?)a["height"] ?? 0, (int?)a["workingWidth"] ?? 0, (int?)a["workingHeight"] ?? 0,
                [.. (a["stages"] as JsonArray ?? []).OfType<JsonObject>().Select(s => new StageTime((string?)s["stage"] ?? "", (long?)s["milliseconds"] ?? 0))],
                (long?)a["peakMegabytes"] ?? 0))];
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }

    /// <summary>Whether a report is due: yes said, and a week since the last, or a benchmark run not yet sent.</summary>
    public bool Due(DateTimeOffset now) =>
        settings.LoadSurveyChoice() == SurveyChoice.Yes
        && (settings.LoadSurveySent() is not { } last || now - last >= Every || Unsent().Count > 0);

    /// <summary>
    /// The runs waiting to go, oldest first, NOTES-FROM-PLANNING.md entry 241 section 1: every run made while the survey was on, until a
    /// report has taken it. The receiver takes three reports a day from one installation; a run it held back goes with the next.
    /// </summary>
    public IReadOnlyList<BenchmarkRunRecord> Unsent() => [.. settings.LoadBenchmarkRuns().Where(r => r.ToSend && !r.Sent)];

    /// <summary>
    /// Sends the report when one is due and the receiver is open. A report the receiver took empties the file and marks the benchmark sent;
    /// one that met no answer or a refusal waits for the next time. Never a dialog. Returns whether a report went.
    /// </summary>
    public async Task<bool> SendDueAsync(bool open, DateTimeOffset now, CancellationToken token)
    {
        if (!open || !Due(now))
        {
            return false;
        }

        var runs = Unsent().Take(SurveyReport.MostBenchmarks).ToList();
        var analyses = Waiting();
        string report = SurveyReport.Build(settings.LoadInstallation(), AppInfo.Version, machine(),
            [.. runs.Select(r => new BenchmarkRun(r.Version ?? "earlier", r.Result))], analyses);
        var answer = await TheOutsideWorld.Current.PostSurveyAsync(ReceiverTerms.Current.SurveyReceiver, report, token).ConfigureAwait(true);
        if (answer is not { Status: >= 200 and < 300 })
        {
            DiagnosticLog.Info("survey.kept", ("status", answer?.Status));
            return false;
        }

        lock (gate)
        {
            // Only what went is removed: an analysis recorded while the report was on its way waits for the next one.
            Write(Waiting().Skip(analyses.Count).ToList());
        }

        settings.SaveSurveySent(now);
        settings.MarkBenchmarkRunsSent([.. runs.Where(r => r.RanAt is not null).Select(r => r.RanAt!.Value)]);
        if (runs.Count > 0 && settings.LoadBenchmark() is { Sent: false } last)
        {
            settings.SaveBenchmark(last.Result, sent: true);
        }

        DiagnosticLog.Info("survey.sent", ("analyses", analyses.Count), ("benchmarks", runs.Count));
        return true;
    }

    /// <summary>
    /// Asks the server to delete everything it keeps under this installation's number, NOTES-FROM-PLANNING.md entry 241 section 2.4. The
    /// number is kept, so the person can see it worked and carry on; resetting it is a separate choice. Returns whether the receiver took it.
    /// </summary>
    public async Task<bool> DeleteAsync(bool open, CancellationToken token)
    {
        if (!open)
        {
            return false;
        }

        var answer = await TheOutsideWorld.Current.PostSurveyAsync(ReceiverTerms.Current.SurveyReceiver, SurveyReport.Delete(settings.LoadInstallation()), token).ConfigureAwait(true);
        bool taken = answer is { Status: >= 200 and < 300 };
        DiagnosticLog.Info("survey.delete", ("taken", taken), ("status", answer?.Status));
        return taken;
    }

    /// <summary>Forgets every kept analysis, as saying no does.</summary>
    public void Forget()
    {
        lock (gate)
        {
            Write([]);
        }
    }

    private void Write(IReadOnlyList<AnalysisFacts> analyses)
    {
        try
        {
            if (analyses.Count == 0)
            {
                File.Delete(AnalysesPath);
                return;
            }

            var all = new JsonArray([.. analyses.Select(a => (JsonNode)new JsonObject
            {
                ["width"] = a.Width,
                ["height"] = a.Height,
                ["workingWidth"] = a.WorkingWidth,
                ["workingHeight"] = a.WorkingHeight,
                ["stages"] = new JsonArray([.. a.Stages.Select(s => (JsonNode)new JsonObject { ["stage"] = s.Stage, ["milliseconds"] = s.Milliseconds })]),
                ["peakMegabytes"] = a.PeakMegabytes,
            })]);
            Directory.CreateDirectory(Path.GetDirectoryName(AnalysesPath) ?? ".");
            File.WriteAllText(AnalysesPath, all.ToJsonString());
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Info("survey.unwritten", ("reason", e.GetType().Name));
        }
    }
}
