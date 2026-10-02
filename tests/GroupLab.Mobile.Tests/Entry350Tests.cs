using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 350, a friend's TestFlight reports of build 150 on an iPhone's Targets screen: "The Done button and bar ended
/// up floating over the app and clicking done did nothing", and "Keyboard doesn't go away making the targets". The keyboard's bar must
/// never outlive the field that called it, Done must always put everything back, and a tap outside a field must close the keyboard even when
/// the system never said it had opened.
/// </summary>
public class Entry350Tests
{
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
        var window = new Window { Width = 402, Height = 874, Content = shell };
        window.Show();
        Settle();
        shell.Show(Shell.Place.Targets);
        Settle();
        var field = shell.GetVisualDescendants().OfType<TextBox>().First(t => t.IsEffectivelyVisible && t.IsEffectivelyEnabled);
        return (shell, window, field);
    }

    private static bool BarShown(Shell shell) => shell.Keyboard.Bar.GetVisualParent() is not null;

    [AvaloniaFact]
    public void TheBarGoesWhenTheFieldLosesTheFocusWithNoWordFromTheSystem()
    {
        var (shell, window, field) = OnTargets();
        try
        {
            field.Focus();
            shell.Keyboard.Opened(874 - 336);
            Settle();
            Assert.True(BarShown(shell));

            // The keyboard went away and the system said nothing: only the focus moved off the field.
            TopLevel.GetTopLevel(shell)!.FocusManager!.Focus(null);
            Settle();
            Assert.False(BarShown(shell));
            Assert.Null(shell.Keyboard.KeyboardTop);
            Assert.Equal(default, shell.Margin);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void DoneAlwaysPutsEverythingBackAndAsksTheSystemToo()
    {
        var (shell, window, field) = OnTargets();
        int asked = 0;
        KeyboardRoom.HideSystemKeyboard = () => asked++;
        try
        {
            field.Focus();
            shell.Keyboard.Opened(874 - 336);
            Settle();
            shell.Keyboard.CloseKeyboard();
            Settle();
            Assert.False(BarShown(shell));
            Assert.False(field.IsFocused);
            Assert.Equal(1, asked);

            // Done pressed a second time, on a bar already gone, still asks and changes nothing else.
            shell.Keyboard.CloseKeyboard();
            Assert.Equal(2, asked);
            Assert.Null(shell.Keyboard.KeyboardTop);
        }
        finally
        {
            KeyboardRoom.HideSystemKeyboard = null;
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ATapOutsideClosesTheKeyboardEvenWhenTheSystemNeverSaidItOpened()
    {
        var (shell, window, field) = OnTargets();
        int asked = 0;
        KeyboardRoom.HideSystemKeyboard = () => asked++;
        try
        {
            field.Focus();
            Settle();
            Assert.Null(shell.Keyboard.KeyboardTop);

            // A tap on the Targets page well away from the field, as the friend's number pad sat over the buttons below.
            var title = shell.GetVisualDescendants().OfType<TextBlock>().First(t => t.IsEffectivelyVisible && t.TranslatePoint(default, window) is { } p && Math.Abs(p.Y - field.TranslatePoint(default, window)!.Value.Y) > 60);
            var at = title.TranslatePoint(new Avalonia.Point(2, 2), window)!.Value;
            window.MouseDown(at, MouseButton.Left);
            window.MouseUp(at, MouseButton.Left);
            Settle();
            Assert.False(field.IsFocused);
            Assert.Equal(1, asked);
        }
        finally
        {
            KeyboardRoom.HideSystemKeyboard = null;
            window.Close();
        }
    }
}
