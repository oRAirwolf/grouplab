using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 9: every change says so in a small line at the bottom right that goes away by itself, with Undo
/// where the change can be undone, and never a dialog.
/// <para>
/// A dialog for a confirmation is a claim on somebody's attention: it stops what they are doing and demands a click before they can carry
/// on. That is right for "this will delete twenty sessions" and wrong for "the shot moved", which is most of what happens here. A toast also
/// puts Undo in the honest place: after the change, where the person can see what it did, rather than before it in a dialog asking whether
/// they are sure about something they cannot yet see.
/// </para>
/// </summary>
public class ToastTests
{
    private static Toaster Quiet()
    {
        // A control that removes itself on a timer cannot be asserted about reliably, and what matters here is what was said.
        var toaster = new Toaster { FadesByItself = false };
        return toaster;
    }

    [AvaloniaFact]
    public void ItSaysWhatHappened()
    {
        var toaster = Quiet();
        toaster.Say("Shot 3 deleted.");

        Assert.Equal(["Shot 3 deleted."], toaster.Showing);
    }

    [AvaloniaFact]
    public void AChangeThatCanBeUndoneOffersUndo()
    {
        var toaster = Quiet();
        bool undone = false;
        toaster.Show(new Confirmation("Shot 3 deleted.", () => undone = true));

        var undo = toaster.Layer.GetLogicalDescendants().OfType<Button>().Single(b => Equals(b.Content, "Undo"));
        undo.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.True(undone);
    }

    /// <summary>
    /// Pressing Undo says so too. Without that line a person cannot tell whether the button worked, and the toast they pressed it on has
    /// just disappeared, so there is nothing else on the screen that changed.
    /// </summary>
    [AvaloniaFact]
    public void UndoingSaysSoAsWell()
    {
        var toaster = Quiet();
        toaster.Show(new Confirmation("Shot 3 deleted.", () => { }));

        var undo = toaster.Layer.GetLogicalDescendants().OfType<Button>().Single(b => Equals(b.Content, "Undo"));
        undo.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["Put back."], toaster.Showing);
    }

    /// <summary>A change that cannot be undone says so without offering a button that would do nothing.</summary>
    [AvaloniaFact]
    public void AChangeThatCannotBeUndoneOffersNothing()
    {
        var toaster = Quiet();
        toaster.Say("Units saved.");

        Assert.DoesNotContain(toaster.Layer.GetLogicalDescendants().OfType<Button>(), b => Equals(b.Content, "Undo"));
    }

    /// <summary>A column of toasts climbing the window is worse than the one thing each of them says.</summary>
    [AvaloniaFact]
    public void OnlyAFewAreShownAtOnce()
    {
        var toaster = Quiet();
        for (int i = 0; i < 8; i++)
        {
            toaster.Say($"Thing {i}.");
        }

        Assert.Equal(3, toaster.Showing.Count);

        // And it is the newest that are kept, because the oldest is the one the person has already read.
        Assert.Equal("Thing 7.", toaster.Showing[^1]);
    }

    /// <summary>
    /// The window uses one toaster for everything, so a person learns one place to look. Entry 131 section 9 asks for one component used
    /// everywhere, and a second one appearing somewhere else would defeat the point of it.
    /// </summary>
    [AvaloniaFact]
    public void TheWindowHasOneAndItIsOverTheContent()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.Confirmations.FadesByItself = false;
        window.Confirmations.Say("Something happened.");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["Something happened."], window.Confirmations.Showing);

        // It is in the window's tree, over the content rather than inside a panel that would clip it.
        Assert.Contains(window.GetLogicalDescendants(), c => ReferenceEquals(c, window.Confirmations.Layer));
        window.Close();
    }
}
