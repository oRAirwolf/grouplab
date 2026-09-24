using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 169: the analysis screen cut down to what a shooter reads, after the first outside user's "all of this is just
/// way too dense".
/// </summary>
public class Entry169Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    /// <summary>Section 1: six things in view, in his order, each a value with the rest in its tooltip; everything else in Advanced, closed.</summary>
    [AvaloniaFact]
    public void TheFiguresAShooterReadsAreInViewAndTheRestIsInAdvanced()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetShotDistance(3600);
            window.Analyse();
            Settle();
            var kept = window.KeptFigures.Select(k => k.Name).ToList();
            Assert.Equal(["Center from aim", "Extreme spread", "Group width × height", "Mean radius", "CEP 50", "CEP 90"], kept);
            Assert.All(window.KeptFigures.Where(k => k.Name != "Center from aim"), k => Assert.False(string.IsNullOrWhiteSpace(k.Tip), k.Name + " has no tooltip"));
            Assert.Contains("interval", window.KeptFigures.Single(k => k.Name == "Mean radius").Tip, StringComparison.Ordinal);
            Assert.DoesNotContain("Sigma", kept);

            Assert.Equal("Advanced", window.AdvancedPanel.Header);
            Assert.False(window.AdvancedPanel.IsExpanded);
            var inside = window.AdvancedPanel.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
            Assert.Contains("Sigma", inside);

            // Opened once, it stays open for the next sheet and the next run.
            window.AdvancedPanel.IsExpanded = true;
            Settle();
            Assert.True(window.SettingsStore.LoadWhyOpen("analysis-advanced"));
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Section 2: MOA and mil side by side whatever the setting, the distance stated, and nothing below the block but its "why".</summary>
    [AvaloniaFact]
    public void TheZeroBlockGivesMoaAndMilAndTheDistanceItIsFor()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetShotDistance(3600);
            window.Analyse();
            Settle();
            var zero = window.ZeroBlockText.ToList();
            Assert.Contains("MOA", zero);
            Assert.Contains("mil", zero);
            Assert.Contains("For a zero at 100 yd.", zero);
            Assert.Contains(zero, t => t.StartsWith("Windage, ", StringComparison.Ordinal));
            Assert.Contains(zero, t => t.StartsWith("Elevation, ", StringComparison.Ordinal));
            Assert.DoesNotContain(zero, t => t.StartsWith("Carried to another distance", StringComparison.Ordinal));
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Sections 5 and 6: the badge says the scale was checked, and Back returns to the marking as it was left.</summary>
    [AvaloniaFact]
    public void TheBadgeIsPlainAndBackReturnsToMarking()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            int shots = window.Session.State.Shots.Count;
            window.Analyse();
            Settle();
            var texts = window.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
            Assert.Contains("✓ Scale checked", texts);
            Assert.DoesNotContain(texts, t => t.StartsWith("registered, residual", StringComparison.Ordinal));
            Assert.Contains(texts, t => t.StartsWith("Scale: from ", StringComparison.Ordinal));

            var back = window.GetLogicalDescendants().OfType<Button>().Single(b => b.Content as string == "‹ Back");
            back.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.False(window.Analysing);
            Assert.Equal(shots, window.Session.State.Shots.Count);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Section 7: the right button drags the sheet in any tool, and Alt shows every button's shortcut until it is let go.</summary>
    [AvaloniaFact]
    public void RightDragPansAndAltShowsTheShortcuts()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            var canvas = window.Canvas;
            var from = new Point(700, 450);
            var before = canvas.ToImage(from);
            window.MouseDown(from, MouseButton.Right);
            window.MouseMove(from + new Vector(60, 40));
            window.MouseUp(from + new Vector(60, 40), MouseButton.Right);
            Settle();
            var after = canvas.ToImage(from);
            Assert.NotEqual(before, after);

            Assert.Empty(window.ShortcutsShown);
            window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
            Settle();
            Assert.Contains("V", window.ShortcutsShown);
            Assert.Contains(CommandKey.Label("Z"), window.ShortcutsShown);
            window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
            Settle();
            Assert.Empty(window.ShortcutsShown);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Section 8: another program's CSV comes in through the mapping, and GroupLab's own CSV goes out with its units in the header.</summary>
    [AvaloniaFact]
    public void ACsvOfShotsIsImportedAndAnalysed()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        var table = ShotCsv.Read("Shot;Horizontal (mm);Vertical (mm)\n1;-5.1;3.0\n2;4.2;-1.5\n3;0.8;6.2\n4;-2.4;-4.4\n5;3.3;2.1\nTotal;;\n");
        Assert.Null(window.ImportShots(table, 1, 2, CoordinateUnit.Millimetre, upIsPositive: true, 3600));
        Settle();
        Assert.True(window.Analysing);
        Assert.Equal(5, window.Session.State.Shots.Count);
        Assert.Contains(window.KeptFigures, k => k.Name == "Mean radius");

        string csv = ShotCsv.Write(window.Session.State);
        Assert.StartsWith("shot,bull,x right (in),y up (in),x right (MOA at 100 yd)", csv, StringComparison.Ordinal);
        Assert.Equal(6, csv.TrimEnd('\n').Split('\n').Length);
        window.Close();
    }

    /// <summary>Section 4.2: the figures in their own window, and back in the panel when it closes.</summary>
    [AvaloniaFact]
    public void TheFiguresOpenInTheirOwnWindowAndComeBack()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Analyse();
            Settle();
            window.PopOutFigures();
            Settle();
            Assert.True(window.FiguresPoppedOut);
            window.CloseFiguresWindow();
            Settle();
            Assert.False(window.FiguresPoppedOut);
            Assert.NotEmpty(window.KeptFigures);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
