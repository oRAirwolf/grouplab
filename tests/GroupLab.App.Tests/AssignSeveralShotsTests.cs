using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 5.3.3: "select several shots and assign them together".
/// <para>
/// <b>The thing that has to be right is the undo.</b> A person who ticks eight shots and puts them on bull 3 did one thing. If that is eight
/// undo steps, then pressing Ctrl+Z once leaves seven of them moved and one back, which is a state nobody asked for and nobody can see is
/// wrong by looking at the sheet.
/// </para>
/// </summary>
public class AssignSeveralShotsTests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    /// <summary>
    /// Ticks a shot through the tick box itself rather than through the window's state. That matters here: ticking the second shot rebuilds
    /// the list, which throws away the very box being clicked, and only driving the real control proves that is safe.
    /// </summary>
    private static void Tick(MainWindow window, int id, bool ticked)
    {
        var box = window.ShotList.GetLogicalDescendants().OfType<CheckBox>()
            .First(c => Avalonia.Automation.AutomationProperties.GetName(c) == $"Choose shot {window.ShotLabelFor(id)}");
        box.IsChecked = ticked;
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void TickingSeveralShotsAndAssigningThemIsOneEditAndOneUndo()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            Settle();
            var state = window.Session.State;
            var shots = state.Shots.Where(s => s.IsShot && s.Bull is not null).Take(4).ToList();
            Assert.Equal(4, shots.Count);

            // Nothing ticked: no bar at all, so the list never offers an action that would do nothing.
            Assert.Empty(Bar(window));

            Tick(window, shots[0].Id, true);
            Assert.Empty(Bar(window));

            foreach (var shot in shots.Skip(1))
            {
                Tick(window, shot.Id, true);
            }

            Settle();
            var bar = Assert.Single(Bar(window));
            var was = shots.ToDictionary(s => s.Id, s => s.Bull);

            var bulls = state.Bulls.OrderBy(b => b.Index).ToList();
            var moveTo = bulls.First(b => shots.All(s => s.Bull != b.Index));
            bar.SelectedItem = moveTo.Label;
            Named(window, $"Assign the {shots.Count} chosen shots").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();

            foreach (var shot in shots)
            {
                var after = window.Session.State.Find(shot.Id)!;
                Assert.Equal(moveTo.Index, after.Bull);
                Assert.True(after.BullChosen);
                Assert.Equal(shot.Image, after.Image);
            }

            Assert.Empty(window.TickedShots);
            Assert.Contains("4 shots assigned to bull", window.StatusText, StringComparison.Ordinal);

            // One undo, and all four are back where they were.
            window.Session.Undo();
            Settle();
            foreach (var shot in shots)
            {
                Assert.Equal(was[shot.Id], window.Session.State.Find(shot.Id)!.Bull);
            }
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Clearing the choice takes the bar away and changes nothing about the shots.</summary>
    [AvaloniaFact]
    public void ClearingTheChoiceChangesNothing()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            Settle();
            var shots = window.Session.State.Shots.Where(s => s.IsShot).Take(3).ToList();
            var before = window.Session.State;
            foreach (var shot in shots)
            {
                Tick(window, shot.Id, true);
            }

            Settle();
            Assert.Single(Bar(window));

            Named(window, "Clear the choice").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();

            Assert.Empty(window.TickedShots);
            Assert.Empty(Bar(window));
            Assert.Equal(before, window.Session.State);

            // And unticking back down to one takes the bar away again, which is the same rebuild in the other direction.
            Tick(window, shots[0].Id, true);
            Tick(window, shots[1].Id, true);
            Assert.Single(Bar(window));
            Tick(window, shots[1].Id, false);
            Assert.Empty(Bar(window));
            Assert.Equal(before, window.Session.State);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// The bar's own picker, which is the one above the list rather than one of the rows'. The bar is the list's only WrapPanel; every shot
    /// row is a Grid, so the two cannot be confused.
    /// </summary>
    private static IReadOnlyList<ComboBox> Bar(MainWindow window) =>
        [.. window.ShotList.Children.OfType<WrapPanel>().SelectMany(p => p.GetLogicalDescendants().OfType<ComboBox>())];

    private static Button Named(MainWindow window, string content) =>
        window.ShotList.Children.OfType<WrapPanel>().SelectMany(p => p.GetLogicalDescendants().OfType<Button>()).First(b => Equals(b.Content, content));
}
