using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Statistics;

namespace GroupLab.App.Tests;

/// <summary>
/// docs/PROOF-CHECKLIST.md rows 15 and 23, each met so far only on generated sessions, against their proposed gates on Alan's own two 6 ARC
/// scans of 2026-09-26 (dominus-k and magnus-m, 25 shots each, from the test-data release, under entry 171's standing consent): a session
/// saved, reopened, exported and imported gives the same figures, and the Compare screen's verdicts on the two sessions are the comparison's
/// own. Each scan is opened, detected, analysed and saved through the window as a person
/// would, by <see cref="Entry109Tests.AnalyzeScan"/>. Without the test data the tests say so and pass, as every test on these scans does.
/// </summary>
public class RealSessionProofTests(ITestOutputHelper output)
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    /// <summary>Everything the analysis screen says about the group: its figures, the judgement cards and the zero.</summary>
    private static List<string> Screen(MainWindow window) =>
        [.. window.StatisticsText.Concat(window.JudgementCards.SelectMany(c => c)).Concat(window.ZeroText).Where(t => t.Length > 0)];

    private static MainWindow AnotherComputer()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        Settle();
        return window;
    }

    /// <summary>
    /// Row 15: Alan's dominus-k session, saved by Accept and analyse, reopened after the window has moved to another sheet, exported with
    /// Export all my data and imported on another computer, shows the same figures, cards and zero, line for line, each time.
    /// </summary>
    [AvaloniaFact]
    public void AlansOwnSessionKeepsItsFiguresThroughSaveReopenExportAndImport()
    {
        if (Entry109Tests.SuppressorScans() is not { } scans)
        {
            Assert.True(true, "skipped: the 2026-09-26 6 ARC scans are not in GROUPLAB_TEST_DATA");
            return;
        }

        var (window, path, synthetic) = Entry109Tests.Sheet();
        MainWindow? other = null;
        string file = Path.Combine(Path.GetTempPath(), $"grouplab-data-{Guid.NewGuid():N}{DataExport.Extension}");
        try
        {
            long id = Entry109Tests.AnalyzeScan(window, scans.Dominus, "6 ARC, Dominus K");
            Settle();
            var shown = Screen(window);
            Assert.Contains(shown, t => t.Contains("Mean radius", StringComparison.Ordinal));
            var saved = window.Sessions!.Get(id)!;
            Assert.Equal(25, saved.ShotCount);

            // Reopened after the window has gone on to another sheet.
            window.BackToEditor();
            window.OpenImage(path);
            window.ApplyDetection(synthetic);
            Settle();
            window.OpenSession(id);
            Settle();
            Assert.True(window.Analysing);
            Assert.Equal(shown, Screen(window));

            // Exported, and imported on another computer, where it opens with no image and says the same.
            Assert.True(window.ExportAll(file));
            other = AnotherComputer();
            Assert.Single(other.PlanImport(file)!.NewSessions);
            Assert.Equal(1, other.ApplyImport());
            var imported = Assert.Single(other.Sessions!.List());
            Assert.Equal(saved.MeanRadiusInches, imported.MeanRadiusInches);
            other.OpenSession(imported.Id);
            Settle();
            Assert.True(other.Analysing);
            Assert.Equal(shown, Screen(other));
        }
        finally
        {
            window.Close();
            other?.Close();
            GroupLab.Tests.Support.Temp.DeleteFile(file);
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// Row 23: the two suppressor sessions chosen in Session records and compared. The screen's groups, tests, verdicts, powers and headline are
    /// the comparison of the shots as they were marked, worked out here from the two markings without the screen; and they are the figures
    /// docs/PROOF-CHECKLIST.md recorded on 2026-10-01: sigma 0.347 and 0.341 in, the dispersion test p = 0.907 and the centres p = 0.049,
    /// within the drift detection has had since (0.906 for the dispersion on 2026-10-08).
    /// </summary>
    [AvaloniaFact]
    public void AlansTwoSuppressorSessionsCompareOnScreenAsTheComparisonSays()
    {
        if (Entry109Tests.SuppressorScans() is not { } scans)
        {
            Assert.True(true, "skipped: the 2026-09-26 6 ARC scans are not in GROUPLAB_TEST_DATA");
            return;
        }

        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            var groups = new List<(string Name, IReadOnlyList<PointD> Offsets)>();
            long Analyse(string scan, string load)
            {
                long id = Entry109Tests.AnalyzeScan(window, scan, load);
                var state = window.Session.State;
                var sighters = state.Bulls.Where(b => !b.Scoring).Select(b => b.Index).ToHashSet();
                var kept = state.Shots.Where(s => s.IsShot && s.Exclusion is null && !(s.Bull is { } b && sighters.Contains(b))).ToList();
                groups.Add((load, GroupAnalysis.CompositeOffsets(state, kept)));
                return id;
            }

            long dominus = Analyse(scans.Dominus, "6 ARC, Dominus K");
            long magnus = Analyse(scans.Magnus, "6 ARC, Magnus S");
            var expected = LoadComparison.Compare(groups, UnitSettings.Imperial.Length);

            window.ShowSessions();
            Settle();
            window.ChooseSession(dominus, true);
            window.ChooseSession(magnus, true);
            window.CompareChosen();
            Settle();
            Assert.True(window.ShowingCompare);
            var report = window.Comparison!;

            Assert.Equal(expected.Groups.Count, report.Groups.Count);
            for (int g = 0; g < expected.Groups.Count; g++)
            {
                Assert.StartsWith(expected.Groups[g].Name, report.Groups[g].Name, StringComparison.Ordinal);
                Assert.Equal(expected.Groups[g].Rayleigh.Sigma.Value, report.Groups[g].Rayleigh.Sigma.Value, 12);
                Assert.Equal(expected.Groups[g].Offsets.Count, report.Groups[g].Offsets.Count);
            }

            Assert.Equal(expected.Tests.Select(t => (t.Name, t.Verdict, t.Power)), report.Tests.Select(t => (t.Name, t.Verdict, t.Power)));
            Assert.Equal(expected.Tests.Select(t => t.PValue), report.Tests.Select(t => t.PValue), (a, b) => Math.Abs(a - b) <= 1e-12);
            Assert.Equal(expected.Headline, report.Headline);
            Assert.Equal(expected.Resolve, report.Resolve);

            // What docs/PROOF-CHECKLIST.md row 23 recorded on 2026-10-01 through the same comparison.
            Assert.Equal(0.347, report.Groups[0].Rayleigh.Sigma.Value, 3);
            Assert.Equal(0.341, report.Groups[1].Rayleigh.Sigma.Value, 3);
            Assert.StartsWith("These two loads group alike as far as these shots can tell, and put their groups in different places", report.Headline, StringComparison.Ordinal);
            double dispersion = report.Tests.Single(t => t.Name.StartsWith("Sigma ratio", StringComparison.Ordinal)).PValue;
            double centres = report.Tests.Single(t => t.Name.StartsWith("Group center", StringComparison.Ordinal)).PValue;
            output.WriteLine(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"sigma {report.Groups[0].Rayleigh.Sigma.Value:0.0000} and {report.Groups[1].Rayleigh.Sigma.Value:0.0000} in, dispersion p = {dispersion:0.0000}, centres p = {centres:0.0000}; {report.Headline}"));
            Assert.InRange(dispersion, 0.89, 0.92);
            Assert.InRange(centres, 0.04, 0.06);

            // And the screen says it: the headline, every test's verdict and what it could have detected.
            var text = window.CompareText.ToList();
            Assert.Contains(report.Headline, text);
            Assert.All(report.Tests, t => Assert.Contains(text, s => s.Contains(t.Verdict, StringComparison.Ordinal)));
            Assert.All(report.Tests, t => Assert.Contains(t.Power, text));
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
