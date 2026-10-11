using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.App.Tests;

/// <summary>NOTES-FROM-PLANNING.md entry 407, the sections that are not a screen of their own: the Selected shot area and the bottom bar.</summary>
public class Entry407Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    /// <summary>
    /// Section 4 (concept 3A): the Selected shot area asks no reason for leaving a shot out, a reason a session already carries is still shown,
    /// and Show in folder has left the bottom bar for the ··· menu as "Open the session's folder".
    /// </summary>
    [AvaloniaFact]
    public void NoReasonIsAskedAStoredOneIsShownAndTheFolderIsInTheMenu()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            var shot = window.Session.State.Shots.First(s => s.IsShot);
            window.Canvas.Selected = shot.Id;
            window.RefreshForTests();
            Settle();
            var reasons = Enum.GetValues<ExclusionReason>().Select(r => r.Words()).ToHashSet();
            Assert.DoesNotContain(window.GetLogicalDescendants().OfType<ComboBox>(), c => c.ItemsSource is IEnumerable<string> items && items.Any(reasons.Contains));

            window.Session.SetExclusion(shot.Id, ExclusionReason.CalledFlyer);
            Settle();
            Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == "Left out as called flyer.");

            Assert.Contains(MainWindow.OpenSessionFolder, window.MenuItems);
            Assert.DoesNotContain(window.GetLogicalDescendants().OfType<Button>(), b => b.Content as string == "Show in folder");
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// Section 6 (concept 6A): paper and backing are in a fold at the bottom of the setup, closed at first, and carried to the next sheet while
    /// "Remember these for my next sheet" is on, which it is at first; turned off, the next sheet starts with neither.
    /// </summary>
    [AvaloniaFact]
    public void PaperAndBackingAreFoldedAwayAndCarriedToTheNextSheetUnlessTurnedOff()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            Assert.False(window.MaterialFoldOpen);
            var (paper, backing, remember) = window.MaterialControls;
            Assert.True(remember.IsChecked);
            window.MaterialFoldOpen = true;
            paper.SelectedItem = "card stock";
            backing.SelectedItem = "foam board";
            Settle();
            Assert.Equal(("card stock", "foam board"), (window.Session.State.Paper, window.Session.State.Backing));

            window.OpenImage(path);
            Settle();
            Assert.Equal(("card stock", "foam board"), (window.Session.State.Paper, window.Session.State.Backing));

            remember.IsChecked = false;
            window.OpenImage(path);
            Settle();
            Assert.Null(window.Session.State.Paper);
            Assert.Null(paper.SelectedItem);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// Section 7 (concept 7A, question 88): with no readings, the speeds are typed on the analysis itself, counted against the shots as they go,
    /// and Match to shots pairs and keeps them, so "Velocity and the vertical" leaves its empty state without a trip to Ballistics.
    /// </summary>
    [AvaloniaFact]
    public void ReadingsTypedOnTheAnalysisAreCountedMatchedAndKept()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Book = RecordBook.Empty.With(new Load("H4350", null) { BallisticCoefficient = 0.3, DragModel = GroupLab.Core.Ballistics.DragModel.G7, BulletWeightGrains = 140 });
            window.Session.SetShotDistance(3600);
            window.Session.SetEquipment(null, null, "H4350");
            window.CalibreAnswered();
            window.Analyse();
            Settle();
            Assert.Equal(VelocityBlockState.NoReadings, window.Velocity!.State);

            int shots = window.Session.State.Shots.Count(s => s.IsShot && s.Exclusion is null);
            window.TypeReadingsHere("2705 2711");
            Assert.Equal(VelocityBlock.Entered(2, shots), window.ReadingsCountText);
            window.TypeReadingsHere(string.Join("\n", Enumerable.Range(0, shots).Select(i => 2700 + ((i * 7) % 23) - 11)));
            Assert.Equal(VelocityBlock.Entered(shots, shots), window.ReadingsCountText);
            window.MatchReadingsHere();
            Settle();
            Assert.NotEqual(VelocityBlockState.NoReadings, window.Velocity!.State);
            Assert.False(window.ShowingBallistics);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
