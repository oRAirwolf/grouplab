using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 465 item 7B: on Compare loads, two loads keep their pictures at their size and the rest of "Each load's
/// group" says what the difference means: both loads' main figures side by side and the comparison's own sentences on whether these shots
/// tell them apart and how many would. Three or more loads leave the card as it was.
/// </summary>
public sealed class Entry465CompareTests
{
    private static List<string> Texts(Control control) => [.. control.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "")];

    [AvaloniaFact]
    public void TwoLoadsSayWhatTheDifferenceMeansBesideThePicturesAndThreeAreAsBefore()
    {
        var (window, path, _) = Entry109Tests.Sheet(1280, 720);
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.Session.SetShotDistance(3600);
            window.Session.SetEquipment(new Rifle("Tikka T3x", 0.25, AngularUnit.Moa), null, "H4350 41.5");
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            long first = window.CurrentSession!.Value;
            long second = window.Sessions!.Save(window.Sessions.Get(first)! with { Id = 0, CreatedUtc = "2099-01-01T00:00:00Z", ShotDate = "2099-01-01", Load = "Factory 150" });
            window.ChooseSession(first, true);
            window.ChooseSession(second, true);
            window.CompareChosen();
            Dispatcher.UIThread.RunJobs();
            Dispatcher.UIThread.RunJobs();

            var report = window.Comparison!;
            var card = window.CompareGroups!;
            Assert.NotNull(report.Apart);
            Assert.All(card.Plots, p => Assert.Equal(200, p.Width));
            var meaning = Assert.IsType<StackPanel>(card.Meaning);
            Assert.True(card.MeaningBeside, "at 1280 by 720 the card has room beside its two pictures, and the meaning went under them");
            var said = Texts(meaning);
            Assert.Contains(LoadGroups.MeaningHeading, said);
            Assert.Contains(said, t => t == "H4350 41.5");
            Assert.Contains(said, t => t == "Factory 150");
            foreach (string label in new[] { "Shots", "Mean radius", "Extreme spread", "CEP 90" })
            {
                Assert.Contains(label, said);
            }

            // The figures in the unit the card writes, an angle at a known distance, for each load.
            Assert.True(said.Count(t => t.EndsWith(" MOA", StringComparison.Ordinal)) >= 6, string.Join(" | ", said));
            Assert.Equal(2, said.Count(t => t == report.Groups[0].Shots.ToString(System.Globalization.CultureInfo.CurrentCulture)));
            Assert.Contains(report.Headline, said);
            Assert.Contains(report.Apart!, said);
            Assert.Contains(report.ShotsNeeded, said);
            Assert.StartsWith("To tell a 10 percent difference in spread apart would take about", report.ShotsNeeded, StringComparison.Ordinal);

            // Three loads: no meaning, and the card says what the card always said.
            long third = window.Sessions.Save(window.Sessions.Get(first)! with { Id = 0, CreatedUtc = "2099-02-01T00:00:00Z", ShotDate = "2099-02-01", Load = "Hand load 3" });
            window.ChooseSession(third, true);
            window.CompareChosen();
            Dispatcher.UIThread.RunJobs();
            var three = window.CompareGroups!;
            Assert.Null(three.Meaning);
            Assert.Null(window.Comparison!.Apart);
            Assert.DoesNotContain(LoadGroups.MeaningHeading, Texts(three));
            var before = new LoadGroups([.. window.Comparison.Groups.Select(g => new LoadGroup(g.Name, g.Offsets, g.Centre, g.MeanRadius.Value))], inches => "", cell: 200);
            Assert.Equal(Texts(before).Count, Texts(three).Count);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Under the pictures where the card is too narrow for it beside them, and in the unit the person reads.</summary>
    [AvaloniaFact]
    public void ANarrowCardPutsTheMeaningUnderThePicturesInTheUnitChosen()
    {
        var a = Enumerable.Range(0, 10).Select(i => new GroupLab.Core.Imaging.PointD(Math.Cos(i) * 0.3, Math.Sin(i * 1.7) * 0.3)).ToList();
        var b = Enumerable.Range(0, 10).Select(i => new GroupLab.Core.Imaging.PointD(Math.Cos(i * 1.3) * 0.5, Math.Sin(i * 0.7) * 0.5)).ToList();
        var report = LoadComparison.Compare([("Load A", a), ("Load B", b)], UnitSettings.Metric.Length);
        var card = new LoadGroups([.. report.Groups.Select(g => new LoadGroup(g.Name, g.Offsets, g.Centre, g.MeanRadius.Value))], UnitSettings.Metric.Length, cell: 150, report: report);
        var window = new Window { Width = 400, Height = 900, Content = card };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            Assert.NotNull(card.Meaning);
            Assert.False(card.MeaningBeside);
            var said = Texts(card.Meaning!);
            Assert.Equal(6, said.Count(t => t.EndsWith(" cm", StringComparison.Ordinal)));
            Assert.Contains(report.ShotsNeeded, said);
        }
        finally
        {
            window.Close();
        }
    }
}
