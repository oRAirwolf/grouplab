using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 8: the ballistics page's imperial and metric toggle.
/// <para>
/// <b>The thing that has to be right is not the labels.</b> A toggle that renames "ft/s" to "m/s" and leaves 2850 in the box has quietly
/// turned a rifle into something travelling at Mach 8, and the solver will answer in perfect detail about it. So the numbers are rewritten
/// when the toggle moves, everything stored stays imperial, and a value typed in one system and read back in the other comes back as what
/// was meant.
/// </para>
/// </summary>
public class BallisticUnitsTests
{
    private static MainWindow NewWindow()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        return new MainWindow(store) { Width = 1400, Height = 900 };
    }

    /// <summary>The box that follows a label in its row. Several fields share a row, so the first box in the row is not always the right one.</summary>
    private static TextBox Box(MainWindow window, string label)
    {
        foreach (var row in window.GetLogicalDescendants().OfType<Avalonia.Controls.WrapPanel>())
        {
            var children = row.Children.ToList();
            int at = children.FindIndex(c => c is TextBlock text && (text.Text ?? "").StartsWith(label, StringComparison.Ordinal));
            if (at >= 0 && children.Skip(at + 1).OfType<TextBox>().FirstOrDefault() is { } box)
            {
                return box;
            }
        }

        throw new InvalidOperationException("no box beside a label starting " + label);
    }

    private static string Labels(MainWindow window) =>
        string.Join(" | ", window.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? ""));

    [AvaloniaFact]
    public void TheToggleRewritesWhatIsTypedRatherThanOnlyItsLabel()
    {
        var window = NewWindow();
        window.Show();
        window.ShowBallistics();
        Dispatcher.UIThread.RunJobs();

        var velocity = Box(window, "Muzzle velocity");
        var sight = Box(window, "Sight height");
        velocity.Text = "2850";
        sight.Text = "1.5";

        window.UseUnits(UnitSettings.Metric);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(UnitSettings.Metric, window.Units);
        Assert.Equal("868.68", velocity.Text);
        Assert.Equal("38.1", sight.Text);
        Assert.Contains("Muzzle velocity, m/s", Labels(window), StringComparison.Ordinal);
        Assert.Contains("Sight height, mm", Labels(window), StringComparison.Ordinal);

        // And back again, to what was typed.
        window.UseUnits(UnitSettings.Imperial);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("2850", velocity.Text);
        Assert.Equal("1.5", sight.Text);
        Assert.Contains("Muzzle velocity, ft/s", Labels(window), StringComparison.Ordinal);
        window.Close();
    }

    /// <summary>
    /// The record keeps imperial whichever side the toggle is on, because the solver and every other screen read it. A metric person typing
    /// 869 m/s and an imperial one typing 2850 ft/s are saying the same thing, and the file says it once.
    /// </summary>
    [AvaloniaFact]
    public void WhatIsStoredIsImperialWhicheverSideTheToggleIsOn()
    {
        var window = NewWindow();
        window.Show();
        window.ShowBallistics();
        window.UseUnits(UnitSettings.Metric);
        Dispatcher.UIThread.RunJobs();

        window.Book = window.Book.With(new Load("6.5 test", null));
        window.ChooseBallisticLoad("6.5 test");
        Dispatcher.UIThread.RunJobs();
        Box(window, "Muzzle velocity").Text = "869";
        Box(window, "Its standard deviation").Text = "4";
        window.KeepBallistics();
        Dispatcher.UIThread.RunJobs();

        var kept = window.Book.FindLoad("6.5 test")!;
        Assert.Equal(2851.05, kept.MuzzleVelocityFps!.Value, 1);
        Assert.Equal(13.12, kept.MuzzleVelocitySdFps!.Value, 1);
        window.Close();
    }

    /// <summary>The air follows the toggle too, in the line that says what the trajectory was worked out in.</summary>
    [AvaloniaFact]
    public void TheAirIsStatedInTheUnitsInForce()
    {
        var window = NewWindow();
        window.Show();
        window.ShowBallistics();
        Dispatcher.UIThread.RunJobs();
        Assert.Contains("Temperature, °F", Labels(window), StringComparison.Ordinal);
        Assert.Contains("Station pressure, inHg", Labels(window), StringComparison.Ordinal);

        window.UseUnits(UnitSettings.Metric);
        Dispatcher.UIThread.RunJobs();

        Assert.Contains("Temperature, °C", Labels(window), StringComparison.Ordinal);
        Assert.Contains("Station pressure, hPa", Labels(window), StringComparison.Ordinal);
        Assert.Contains("Altitude, m", Labels(window), StringComparison.Ordinal);
        Assert.Contains("Crosswind uncertainty, km/h", Labels(window), StringComparison.Ordinal);

        // 59 degrees Fahrenheit is the standard atmosphere's temperature and reads as 15 on the other side.
        Assert.Equal("15", Box(window, "Temperature").Text);
        window.Close();
    }

    /// <summary>Grains are grains on both sides: a reloader weighs in them whatever else they measure in.</summary>
    [AvaloniaFact]
    public void BulletWeightStaysInGrains()
    {
        var window = NewWindow();
        window.Show();
        window.ShowBallistics();
        window.UseUnits(UnitSettings.Metric);
        Dispatcher.UIThread.RunJobs();

        Assert.Contains("Bullet weight, gr", Labels(window), StringComparison.Ordinal);
        window.Close();
    }
}
