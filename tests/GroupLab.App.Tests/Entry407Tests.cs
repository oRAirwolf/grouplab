using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;

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
}
