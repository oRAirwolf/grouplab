using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Statistics;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 5.2.4 on the screen it lives on: the sessions of one load over time, on Session records.
/// <para>
/// <b>What is held here is when it refuses to draw.</b> "Is this load getting better or worse" cannot be asked of a list that mixes two
/// loads, and a chart drawn over such a list would answer a question nobody asked while looking exactly like one that did.
/// </para>
/// </summary>
public class SessionsScreenTrendTests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    /// <summary>With every load showing there is no chart, only the sentence saying what to do to get one.</summary>
    [AvaloniaFact]
    public void WithSeveralLoadsShowingItDrawsNothingAndSaysWhy()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            var shape = Shape(window);
            Save(window, shape, "H4350 41.5", "2026-03-01", 0.52);
            Save(window, shape, "Varget 41.0", "2026-03-08", 0.41);

            window.ShowSessions();
            Settle();

            Assert.Contains("Choose one load above", window.SessionTrendDescription, StringComparison.Ordinal);
            Assert.Empty(window.GetLogicalDescendants().OfType<SessionsOverTime>());
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// With one load chosen the chart appears, and its caption says both what the sessions measured and whether they can separate them.
    /// Six sessions that wander is the ordinary case, and the one the caption has to get right.
    /// </summary>
    [AvaloniaFact]
    public void WithOneLoadChosenTheChartAppearsAndTheCaptionSaysWhetherTheSessionsCanTell()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            var shape = Shape(window);
            double[] radii = [0.44, 0.51, 0.41, 0.55, 0.39, 0.48];
            for (int i = 0; i < radii.Length; i++)
            {
                Save(window, shape, "H4350 41.5", $"2026-03-{i + 1:00}", radii[i]);
            }

            window.ShowSessions();
            window.FilterSessions(null, "H4350 41.5");
            Settle();

            var chart = Assert.Single(window.GetLogicalDescendants().OfType<SessionsOverTime>());
            Assert.Equal(6, chart.Points.Count);

            // Oldest first, whatever order the records list them in, because the chart's whole meaning is the order.
            Assert.Equal([.. chart.Points.Select(p => p.When).Order()], [.. chart.Points.Select(p => p.When)]);

            string said = window.SessionTrendDescription;
            Assert.Contains("Is this load getting better or worse?", said, StringComparison.Ordinal);
            Assert.Contains("6 sessions of H4350 41.5", said, StringComparison.Ordinal);
            Assert.DoesNotContain("enough to say so", said, StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// One real analysed session, kept only for its shape, and removed from the store again. Every session the tests then save is a copy of
    /// it with its own date, load and mean radius, so the records are as real as a saved session and the figures are the test's own.
    /// </summary>
    private static SessionRecord Shape(MainWindow window)
    {
        window.Session.SetShotDistance(3600);
        window.Session.SetEquipment(new Rifle("Tikka T3x", 0.25, AngularUnit.Moa), null, "H4350 41.5");
        window.CalibreAnswered();
        window.Analyse();
        long id = window.CurrentSession!.Value;
        var shape = window.Sessions!.Get(id)!;
        window.BackToEditor();
        window.Sessions.Delete(id);
        return shape;
    }

    /// <summary>Saves one more session of the given load, shot on the given date, measuring the given mean radius.</summary>
    private static void Save(MainWindow window, SessionRecord shape, string load, string shotDate, double meanRadius) =>
        window.Sessions!.Save(shape with
        {
            Id = 0,
            CreatedUtc = shotDate + "T00:00:00Z",
            ShotDate = shotDate,
            Load = load,
            MeanRadiusInches = meanRadius,
            MeanRadiusLowerInches = meanRadius - 0.06,
            MeanRadiusUpperInches = meanRadius + 0.06,
        });
}
