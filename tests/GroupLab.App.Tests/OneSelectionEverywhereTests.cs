using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 5.3.1: one shot selected, shown on the image, in the shots list and in the review queue at once.
/// <para>
/// <b>Why the third place is the one that was missing.</b> The image and the shots list already agreed. The review queue did not: it marked
/// the item it was working through and nothing else, so a person who clicked a hole because they wanted to know why it was queried had no
/// way to see which of the items was about it. Selecting the hole is exactly the moment that question is being asked.
/// </para>
/// </summary>
public class OneSelectionEverywhereTests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    [AvaloniaFact]
    public void SelectingAShotMarksItInTheShotsListAndInEveryReviewItemAboutIt()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            Settle();
            var queried = window.ReviewItems.FirstOrDefault(i => i.ShotId is not null);
            Assert.NotNull(queried);
            int id = queried.ShotId!.Value;

            window.Canvas.Selected = id;
            window.RefreshForTests();
            Settle();

            // The shots list marks exactly the selected shot, and the review queue marks every item that is about it.
            Assert.Single(window.ShotRowsSelected);
            var marked = window.ReviewRowsAboutTheSelectedShot;
            Assert.NotEmpty(marked);
            Assert.All(marked, text => Assert.Contains($", shot {window.ShotLabelFor(id)}", text, StringComparison.Ordinal));

            int expected = window.ReviewItems.Count(i => i.ShotId == id);
            Assert.Equal(expected, marked.Count);

            // And selecting another shot moves the mark rather than adding to it.
            var other = window.ReviewItems.FirstOrDefault(i => i.ShotId is { } o && o != id);
            if (other is not null)
            {
                window.Canvas.Selected = other.ShotId!.Value;
                window.RefreshForTests();
                Settle();
                Assert.All(window.ReviewRowsAboutTheSelectedShot,
                    text => Assert.Contains($", shot {window.ShotLabelFor(other.ShotId!.Value)}", text, StringComparison.Ordinal));
            }
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// Clearing the selection does not leave the three places disagreeing. Entry 97 section 5 is why: while an item still needs a person, the
    /// editor selects that item's shot rather than leaving nothing selected, because a bull typed with nothing selected goes nowhere. So the
    /// mark never disappears from one place and stays in another; it moves to the item's shot in all three.
    /// </summary>
    [AvaloniaFact]
    public void ClearingTheSelectionLeavesTheThreePlacesAgreeing()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            Settle();
            Assert.Contains(window.ReviewItems, i => !i.Resolved && i.ShotId is not null);

            window.Canvas.Selected = null;
            window.RefreshForTests();
            Settle();

            // Something still needs a person, so the editor has put the selection on that item's shot.
            int on = Assert.IsType<int>(window.Canvas.Selected);
            Assert.Single(window.ShotRowsSelected);
            Assert.All(window.ReviewRowsAboutTheSelectedShot,
                text => Assert.Contains($", shot {window.ShotLabelFor(on)}", text, StringComparison.Ordinal));
            Assert.Equal(window.ReviewItems.Count(i => i.ShotId == on), window.ReviewRowsAboutTheSelectedShot.Count);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
