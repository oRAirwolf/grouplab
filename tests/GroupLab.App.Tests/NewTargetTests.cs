using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 140 sections 1.4 and 2: nothing is lost on the way out of a sheet, and a sheet can be cleared on purpose.
/// <para>
/// Section 1 made opening an image throw the last sheet away. That is right, and it is also how ten minutes of correcting marks disappears
/// without anybody being asked. Every way out of a sheet now goes through the same question, and it is a row in the panel rather than a
/// dialog, so cancel leaves the sheet exactly as it was.
/// </para>
/// </summary>
public class NewTargetTests
{
    private static MainWindow NewWindow()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        return new MainWindow(store) { Width = 1400, Height = 900 };
    }

    private static void Mark(MainWindow window, int shots = 5)
    {
        var session = window.Session;
        session.Open(@"C:\a-target.png");
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        session.SetPointOfAim(new PointD(0, 0));
        // A scatter rather than a line. A perfectly collinear group is degenerate for the shape tests, and the resampling behind them then
        // takes two minutes a test where an ordinary group takes a second.
        for (int k = 0; k < shots; k++)
        {
            double angle = 2 * Math.PI * k / shots;
            session.AddShot(new PointD(30 * Math.Cos(angle), 30 * Math.Sin(angle)));
        }

        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void NewTargetClearsTheSheetAndTheToastBringsItBack()
    {
        var window = NewWindow();
        window.Show();
        window.Confirmations.FadesByItself = false;
        Mark(window);
        window.Session.SetCalibre(Calibre.Of(0.264));
        window.Session.SetExpectedShots(5);
        Dispatcher.UIThread.RunJobs();

        // Unsaved work asks before it goes, and answering discards it.
        window.NewTarget();
        Assert.True(window.AskingAboutUnsavedWork);
        Assert.Equal(5, window.Session.State.Shots.Count);
        window.AnswerDiscard();
        Dispatcher.UIThread.RunJobs();

        Assert.False(window.AskingAboutUnsavedWork);
        Assert.Empty(window.Session.State.Shots);
        Assert.Null(window.Session.State.ImagePath);
        Assert.Null(window.Session.State.Calibre);
        Assert.Null(window.Session.State.ExpectedShots);

        // Entry 131 section 9: the change says so, and it can be taken back.
        Assert.Equal(["New target: the sheet is cleared."], window.Confirmations.Showing);
        var undo = window.Confirmations.Layer.GetLogicalDescendants().OfType<Button>().Single(b => Equals(b.Content, "Undo"));
        undo.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(5, window.Session.State.Shots.Count);
        Assert.Equal(0.264, window.Session.State.Calibre!.DiameterInches, 6);
        window.Close();
    }

    /// <summary>Cancel is the answer that changes nothing: the sheet, its marks and the question itself are all where they were.</summary>
    [AvaloniaFact]
    public void CancelLeavesTheSheetExactlyAsItWas()
    {
        var window = NewWindow();
        window.Show();
        Mark(window);

        window.NewTarget();
        Assert.True(window.AskingAboutUnsavedWork);
        window.AnswerCancel();
        Dispatcher.UIThread.RunJobs();

        Assert.False(window.AskingAboutUnsavedWork);
        Assert.Equal(5, window.Session.State.Shots.Count);
        Assert.Equal(@"C:\a-target.png", window.Session.State.ImagePath);
        window.Close();
    }

    /// <summary>A sheet with nothing on it has nothing to lose, so it clears without a question.</summary>
    [AvaloniaFact]
    public void AnEmptySheetIsClearedWithoutAsking()
    {
        var window = NewWindow();
        window.Show();

        Assert.False(window.HasUnsavedWork);
        window.NewTarget();
        Dispatcher.UIThread.RunJobs();

        Assert.False(window.AskingAboutUnsavedWork);
        Assert.Empty(window.Session.State.Shots);
        window.Close();
    }

    /// <summary>Entry 140 section 2: Ctrl+N is the same action as the menu item.</summary>
    [AvaloniaFact]
    public void ControlNStartsANewTarget()
    {
        var window = NewWindow();
        window.Show();
        Mark(window);

        window.KeyPressQwerty(PhysicalKey.N, (RawInputModifiers)CommandKey.Modifier);
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.AskingAboutUnsavedWork);
        window.Close();
    }

    /// <summary>
    /// Saving answers the question too, and the sheet is then no longer unsaved work: opening the next image asks nothing until it is edited
    /// again.
    /// </summary>
    [AvaloniaFact]
    public void SavingSettlesItAndTheSheetIsNoLongerUnsavedWork()
    {
        var window = NewWindow();
        window.Show();
        Mark(window);
        Assert.True(window.HasUnsavedWork);

        window.Analyse();
        Dispatcher.UIThread.RunJobs();

        Assert.False(window.HasUnsavedWork);
        window.NewTarget();
        Assert.False(window.AskingAboutUnsavedWork);
        window.Close();
    }
}
