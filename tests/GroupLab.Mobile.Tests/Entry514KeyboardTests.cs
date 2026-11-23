using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 514 section 2, the iOS real-tap flake on Targets' keyboard Next (run 44891904639): seven seconds after the
/// first keyboard came up, with the distance field still focused and no tap from anybody, the keyboard room put the keyboard down and took
/// its bar away, and nothing raised it again, so every Next after it found no bar. A pane that says Open with no rectangle is not a closed
/// keyboard, and while a field keeps the focus a bar taken away comes back once the system says the keyboard is up.
/// </summary>
public class Entry514KeyboardTests
{
    private const double Height = 874;
    private const double Top = Height - 336;

    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
    }

    private static (Shell Shell, Window Window, TextBox Field) OnTargets()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
        var shell = new Shell();
        var window = new Window { Width = 402, Height = Height, Content = shell };
        window.Show();
        Settle();
        shell.Show(Shell.Place.Targets);
        Settle();
        var field = shell.GetVisualDescendants().OfType<TextBox>().First(t => t.IsEffectivelyVisible && t.IsEffectivelyEnabled);
        return (shell, window, field);
    }

    private static bool BarShown(Shell shell) => shell.Keyboard.Bar.GetVisualParent() is not null;

    [AvaloniaFact]
    public void APaneThatSaysOpenWithNoRectangleLeavesTheBarWhereItIs()
    {
        var (shell, window, field) = OnTargets();
        try
        {
            field.Focus();
            shell.Keyboard.PaneForTests = (InputPaneState.Open, new Rect(0, Top, 402, 336));
            shell.Keyboard.PaneSaid(InputPaneState.Open, new Rect(0, Top, 402, 336));
            Settle();
            Assert.True(BarShown(shell));

            shell.Keyboard.PaneForTests = (InputPaneState.Open, default);
            shell.Keyboard.PaneSaid(InputPaneState.Open, default);
            Settle();
            Assert.True(BarShown(shell), "an Open with no rectangle took the bar away");
            Assert.Equal(Top, shell.Keyboard.KeyboardTop);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AnOpenWithNoRectangleTakesThePanesOwn()
    {
        var (shell, window, field) = OnTargets();
        try
        {
            field.Focus();
            shell.Keyboard.PaneForTests = (InputPaneState.Open, new Rect(0, Top, 402, 336));
            shell.Keyboard.PaneSaid(InputPaneState.Open, default);
            Settle();
            Assert.True(BarShown(shell));
            Assert.Equal(Top, shell.Keyboard.KeyboardTop);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ABarTakenAwayWhileTheFieldKeepsTheFocusComesBackWhenTheKeyboardIsUp()
    {
        var (shell, window, field) = OnTargets();
        try
        {
            field.Focus();
            shell.Keyboard.PaneForTests = (InputPaneState.Open, new Rect(0, Top, 402, 336));
            shell.Keyboard.Opened(Top);
            Settle();

            // The system says it closed, the field keeps the focus: the bar goes, as before.
            shell.Keyboard.PaneForTests = (InputPaneState.Closed, default);
            shell.Keyboard.PaneSaid(InputPaneState.Closed, default);
            Settle();
            Assert.False(BarShown(shell));
            Assert.True(field.IsFocused);

            // While it stays closed, the watch raises nothing.
            shell.Keyboard.Check();
            Settle();
            Assert.False(BarShown(shell));

            // The system says the keyboard is up again with no event: the watch brings the bar back.
            shell.Keyboard.PaneForTests = (InputPaneState.Open, new Rect(0, Top, 402, 336));
            shell.Keyboard.Check();
            Settle();
            Assert.True(BarShown(shell), "the bar did not come back with the field focused and the keyboard up");
            Assert.Equal("Next", shell.Keyboard.BarWords);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void WithNoFieldFocusedTheWatchRaisesNothing()
    {
        var (shell, window, field) = OnTargets();
        try
        {
            shell.Keyboard.PaneForTests = (InputPaneState.Open, new Rect(0, Top, 402, 336));
            shell.Keyboard.Check();
            Settle();
            Assert.False(BarShown(shell));
            Assert.Null(shell.Keyboard.KeyboardTop);
        }
        finally
        {
            window.Close();
        }
    }
}
