using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 278: section 2, the desktop's CSV import starting from GroupLab's guesses, the phone's CSV B, and section 5c
/// with entry 279 section 3, a shot left out is left out of the figures on the screen.
/// </summary>
public class Entry278Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static MainWindow Window()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        return window;
    }

    /// <summary>Unholy's report: excluding a shot changed no figure. The mean radius shown is now the group's without it.</summary>
    [AvaloniaFact]
    public void LeavingAShotOutChangesTheFiguresShown()
    {
        var window = Window();
        var table = ShotCsv.Read("x (in),y (in)\n0.1,0\n-0.1,0.1\n0,-0.1\n0.15,0.1\n-0.05,-0.05\n3,0\n");
        Assert.Null(window.ImportShots(table, 0, 1, CoordinateUnit.Inch, upIsPositive: true, null)); // no distance, so the value is a length
        Settle();
        string before = window.KeptFigures.Single(k => k.Name == "Mean radius").Value;
        window.Session.SetExclusion(window.Session.State.Shots[^1].Id, ExclusionReason.CalledFlyer);
        Settle();
        var after = window.KeptFigures.Single(k => k.Name == "Mean radius");
        Assert.NotEqual(before, after.Value);
        var five = ShotCsv.Marking([new(0.1, 0), new(-0.1, -0.1), new(0, 0.1), new(0.15, -0.1), new(-0.05, 0.05)], 3600);
        Assert.Equal(UnitSettings.Imperial.Length(GroupAnalysis.Analyse(five).AllShots!.MeanRadius!.Value), after.Value);
        Assert.Contains("with every shot: " + before, after.Tip ?? "", StringComparison.Ordinal);
        Assert.Contains(window.StatisticsText, t => t.Contains("5 counted, 1 left out", StringComparison.Ordinal));
        window.Close();
    }

    /// <summary>Numbers measured from the group's center import with no point of aim, so no offset from aim is invented.</summary>
    [AvaloniaFact]
    public void NumbersFromTheGroupsCenterImportWithNoAim()
    {
        var window = Window();
        var table = ShotCsv.Read("x from center (mm),y from center (mm)\n-5,3\n4,-1\n1,6\n-2,-4\n2,-4\n");
        var guess = CsvGuess.For(table, "group.csv");
        Assert.True(guess.Complete);
        Assert.True(guess.FromGroupCentre.Value);
        Assert.Null(window.ImportShots(table, guess.Across.Value!.Value, guess.UpDown.Value!.Value, guess.Unit.Value!.Value, guess.UpIsPositive.Value!.Value, 3600, fromGroupCentre: true));
        Settle();
        Assert.Null(window.Session.State.PointOfAim);
        Assert.Contains(window.KeptFigures, k => k.Name == "Mean radius");
        window.Close();
    }
}
