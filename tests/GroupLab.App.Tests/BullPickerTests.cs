using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 5.3.3: a bull picker on each shots-list row, the third route to "this shot belongs to that bull".
/// <para>
/// <b>Why a third route.</b> The other two, clicking the bull on the image and typing its label, both need the person to have found the hole
/// on the sheet first. Somebody working down the shots list has the shot's name in front of them and not its position, and on a 25 bull sheet
/// at 1280 by 720 that hunt is the slow part of the job.
/// </para>
/// <para>
/// <b>What must be true of it.</b> Choosing a bull here marks it chosen, exactly as clicking the bull does, so a later re-assignment leaves it
/// alone. That is the whole difference between a person's answer and the detector's guess.
/// </para>
/// </summary>
public class BullPickerTests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static IReadOnlyList<ComboBox> Pickers(MainWindow window) =>
        [.. window.ShotList.GetLogicalDescendants().OfType<ComboBox>()];

    /// <summary>
    /// One shot's picker, found by the name it carries for a screen reader. The list is not in the state's order, so counting rows would
    /// find the wrong one and the test would pass or fail for a reason that has nothing to do with the picker.
    /// </summary>
    private static ComboBox PickerFor(MainWindow window, int id) =>
        Pickers(window).First(c => Avalonia.Automation.AutomationProperties.GetName(c) == $"Bull for shot {window.ShotLabelFor(id)}");

    [AvaloniaFact]
    public void ChoosingABullOnTheRowMovesTheShotToItAndMarksItChosen()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            Settle();
            var state = window.Session.State;
            Assert.NotEmpty(state.Bulls);

            var shot = state.Shots.First(s => s.IsShot && s.Bull is not null);
            var pickers = Pickers(window);
            Assert.Equal(state.Shots.Count(s => s.IsShot), pickers.Count);

            // Every picker starts on the bull the shot is already on, so nothing reads as unassigned that is not.
            var bulls = state.Bulls.OrderBy(b => b.Index).ToList();
            var picker = PickerFor(window, shot.Id);
            Assert.Equal(bulls.First(b => b.Index == shot.Bull!.Value).Label, picker.SelectedItem);

            // Move it to a different bull.
            var moveTo = bulls.First(b => b.Index != shot.Bull!.Value);
            picker.SelectedItem = moveTo.Label;
            Settle();

            var after = window.Session.State.Find(shot.Id)!;
            Assert.Equal(moveTo.Index, after.Bull);
            Assert.True(after.BullChosen, "the person's answer was not marked as chosen, so a re-assignment could undo it");
            Assert.Equal(shot.Image, after.Image);
            Assert.Equal(shot.Id, window.Canvas.Selected);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Choosing "no bull" takes the shot off every bull, which is a real answer and not a blank.</summary>
    [AvaloniaFact]
    public void ChoosingNoBullTakesTheShotOffEveryBull()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            Settle();
            var shot = window.Session.State.Shots.First(s => s.IsShot && s.Bull is not null);
            var picker = PickerFor(window, shot.Id);

            picker.SelectedItem = MainWindow.NoBull;
            Settle();

            var after = window.Session.State.Find(shot.Id)!;
            Assert.Null(after.Bull);
            Assert.True(after.BullChosen);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Undo puts it back, because a picked bull is an edit like any other.</summary>
    [AvaloniaFact]
    public void TheChoiceIsUndone()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            Settle();
            var shot = window.Session.State.Shots.First(s => s.IsShot && s.Bull is not null);
            int was = shot.Bull!.Value;
            var bulls = window.Session.State.Bulls.OrderBy(b => b.Index).ToList();
            var picker = PickerFor(window, shot.Id);

            picker.SelectedItem = bulls.First(b => b.Index != was).Label;
            Settle();
            Assert.NotEqual(was, window.Session.State.Find(shot.Id)!.Bull);

            window.Session.Undo();
            Settle();
            Assert.Equal(was, window.Session.State.Find(shot.Id)!.Bull);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
