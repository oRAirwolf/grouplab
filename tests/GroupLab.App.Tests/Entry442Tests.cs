using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 442, Quack's request on the Discord from nightly 198 on macOS: in the pooled analysis, Space on a row of the
/// shot table chose the shot and dropped the focus back to the start of the window; the arrow keys did nothing anywhere; and the caliber
/// line read ".568.96 cm (0.57 cm)".
/// </summary>
public class Entry442Tests
{
    private static void Press(Window window, Key key, PhysicalKey physical, string? text = null)
    {
        window.KeyPress(key, RawInputModifiers.None, physical, text);
        window.KeyRelease(key, RawInputModifiers.None, physical, text);
        Dispatcher.UIThread.RunJobs();
    }

    private static object? Focused(Window window) => window.FocusManager?.GetFocusedElement();

    /// <summary>Tabs from the start of the window, as a person would, until the focus is on a row of the shot table.</summary>
    private static Button TabIntoTheShotTable(MainWindow window)
    {
        window.Focus();
        for (int i = 0; i < 400; i++)
        {
            Press(window, Key.Tab, PhysicalKey.Tab);
            if (Focused(window) is Button row && window.OffsetRows.Contains(row))
            {
                return row;
            }
        }

        throw new Xunit.Sdk.XunitException("Tab never reached the shot table");
    }

    private static int ShotOf(object? focused) => Assert.IsType<int>(Assert.IsType<Button>(focused).Tag);

    /// <summary>
    /// Section 1 on one sheet: Space and Enter on a row choose its shot and leave the focus on that row, drawn for the keyboard, so the next
    /// Tab goes on to the row after it rather than starting again from the top.
    /// </summary>
    [AvaloniaFact]
    public void SpaceOnAShotRowKeepsTheFocusThere()
    {
        var (window, path) = AnalysisStateTests.Marked();
        try
        {
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            var row = TabIntoTheShotTable(window);
            int shot = ShotOf(row);

            Press(window, Key.Space, PhysicalKey.Space, " ");
            Assert.Equal([shot], window.Plot.Selected);
            var after = Assert.IsType<Button>(Focused(window));
            Assert.Contains(after, window.OffsetRows);
            Assert.Equal(shot, after.Tag);
            Assert.Contains(":focus-visible", after.Classes);

            var ids = window.OffsetRows.Select(r => (int)r.Tag!).ToList();
            Press(window, Key.Tab, PhysicalKey.Tab);
            Assert.Equal(ids[ids.IndexOf(shot) + 1], ShotOf(Focused(window)));

            Press(window, Key.Enter, PhysicalKey.Enter);
            Assert.Equal([ids[ids.IndexOf(shot) + 1]], window.Plot.Selected);
            Assert.Equal(ids[ids.IndexOf(shot) + 1], ShotOf(Focused(window)));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(path);
        }
    }

    /// <summary>
    /// Sections 1 and 2 in Quack's case, three sheets of a set pooled: Space keeps the focus; Up and Down choose the shot before and after;
    /// Right and Left the same bull on the next and previous sheet; Home and End the first and last; Esc clears the choice; the focus follows.
    /// </summary>
    [AvaloniaFact]
    public async Task ThePooledShotTableWorksFromTheKeyboard()
    {
        var (window, folder) = Entry423Tests.Window(detect: true);
        try
        {
            var set = Entry423Tests.TwoMoaSet();
            window.OpenDropped([.. new[] { 0, 1, 2 }.Select(k => Entry423Tests.Sheet(set, folder, k))]);
            await Entry423Tests.Settle(window);
            Assert.IsType<SetGroup>(window.CurrentSet);
            var state = window.Session.State;
            int Bull(int id) => state.Find(id)!.Bull!.Value;

            var row = TabIntoTheShotTable(window);
            Press(window, Key.Space, PhysicalKey.Space, " ");
            int shot = ShotOf(Focused(window));
            Assert.Equal(ShotOf(row), shot);
            Assert.Equal([shot], window.Plot.Selected);

            var ids = window.OffsetRows.Select(r => (int)r.Tag!).ToList();
            Press(window, Key.Down, PhysicalKey.ArrowDown);
            int next = ids[ids.IndexOf(shot) + 1];
            Assert.Equal([next], window.Plot.Selected);
            Assert.Equal(next, ShotOf(Focused(window)));
            Press(window, Key.Up, PhysicalKey.ArrowUp);
            Assert.Equal([shot], window.Plot.Selected);

            // A bull of the first sheet that holds a shot on the second: Right goes to it, Left comes back.
            Press(window, Key.Home, PhysicalKey.Home);
            int first = ids[0];
            Assert.Equal([first], window.Plot.Selected);
            Assert.Equal(0, SetPool.SheetOf(Bull(first)));
            Press(window, Key.Right, PhysicalKey.ArrowRight);
            int beside = ShotOf(Focused(window));
            Assert.Equal([beside], window.Plot.Selected);
            Assert.Equal(1, SetPool.SheetOf(Bull(beside)));
            Assert.Equal(SetPool.BullOnSheet(Bull(first)), SetPool.BullOnSheet(Bull(beside)));
            Press(window, Key.Left, PhysicalKey.ArrowLeft);
            Assert.Equal([first], window.Plot.Selected);
            Press(window, Key.Left, PhysicalKey.ArrowLeft);
            Assert.Equal([first], window.Plot.Selected);

            Press(window, Key.End, PhysicalKey.End);
            Assert.Equal([ids[^1]], window.Plot.Selected);
            Assert.Equal(ids[^1], ShotOf(Focused(window)));
            Press(window, Key.Escape, PhysicalKey.Escape);
            Assert.Empty(window.Plot.Selected);
            Assert.Equal(ids[^1], ShotOf(Focused(window)));
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }

    /// <summary>
    /// Section 2 elsewhere: Up and Down move between the rail's buttons, Left and Right between the buttons of the top bar and the tab strip,
    /// and a text box keeps its own arrows. Moving never presses anything.
    /// </summary>
    [AvaloniaFact]
    public void ArrowsMoveBetweenButtonsAndLeaveTextBoxesAlone()
    {
        var (window, path) = AnalysisStateTests.Marked();
        try
        {
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            Button Named(string name) => window.GetVisualDescendants().OfType<Button>()
                .First(b => b.IsEffectivelyVisible && (Avalonia.Automation.AutomationProperties.GetName(b) == name || b.Content as string == name));

            Assert.True(Named("Analyze").Focus(NavigationMethod.Tab));
            Press(window, Key.Down, PhysicalKey.ArrowDown);
            Assert.Same(Named("Targets"), Focused(window));
            Press(window, Key.Down, PhysicalKey.ArrowDown);
            Assert.Same(Named("Session records"), Focused(window));
            Press(window, Key.Up, PhysicalKey.ArrowUp);
            Assert.Same(Named("Targets"), Focused(window));
            Assert.True(window.Plot.IsEffectivelyVisible, "moving along the rail opened a screen");

            Assert.True(Named("Report").Focus(NavigationMethod.Tab));
            Press(window, Key.Left, PhysicalKey.ArrowLeft);
            Assert.Same(Named("Own window"), Focused(window));
            Press(window, Key.Right, PhysicalKey.ArrowRight);
            Press(window, Key.Right, PhysicalKey.ArrowRight);
            Assert.Same(Named("Export"), Focused(window));

            var (tabs, close) = window.TabStripParts.Tabs[0];
            Assert.True(tabs.Focus(NavigationMethod.Tab));
            Press(window, Key.Right, PhysicalKey.ArrowRight);
            Assert.Same(close, Focused(window));
            Press(window, Key.Left, PhysicalKey.ArrowLeft);
            Assert.Same(tabs, Focused(window));

            var box = window.GetVisualDescendants().OfType<TextBox>().First(t => t.IsEffectivelyVisible);
            Assert.True(box.Focus(NavigationMethod.Tab));
            Press(window, Key.Left, PhysicalKey.ArrowLeft);
            Press(window, Key.Down, PhysicalKey.ArrowDown);
            Assert.Same(box, Focused(window));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(path);
        }
    }

    /// <summary>
    /// Section 3: the caliber line is written in both units and is not a number a tap switches, so a size remembered in centimeters leaves it
    /// as it is; and the switch itself reads a number written without its leading zero whole.
    /// </summary>
    [AvaloniaFact]
    public void TheCaliberLineReadsInBothUnitsWhateverWasTapped()
    {
        var (window, path) = AnalysisStateTests.Marked();
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.224));
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            var line = window.GetVisualDescendants().OfType<TextBlock>().Single(t => t.IsEffectivelyVisible && t.Text == ".224 in (5.69 mm)");
            Assert.DoesNotContain(UnitTap.Value, line.Classes);
            Assert.Equal("0.57 cm (0.57 cm)", UnitSwitch.Convert(".224 in (5.69 mm)", UnitKind.Length, "cm"));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(path);
        }
    }
}
