using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 353, Fenix's report of TestFlight build 157 on an iPhone: "Take a picture button does nothing. Choose photo
/// does nothing. Done for caliber and distance doesn't work either." These press with the pointer, as a finger does: down, the layout
/// that follows it, then up where the finger still is. A button pressed only through the command bridge or by raising Click never goes
/// through that, which is how the sweep passed while every tap failed.
/// </summary>
public class Entry353Tests
{
    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
    }

    private static (Shell Shell, Window Window, CapturePage Capture) OnCapture(string? calibre)
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
        Phone.Settings.SaveShotSetup(calibre, null);
        var shell = new Shell();
        var window = new Window { Width = 402, Height = 874, Content = shell };
        window.Show();
        Settle();
        return (shell, window, shell.GetVisualDescendants().OfType<CapturePage>().Single());
    }

    private static T Named<T>(Control within, string id) where T : Control =>
        within.GetVisualDescendants().OfType<T>().Single(c => AutomationProperties.GetAutomationId(c) == id);

    /// <summary>A finger's tap on the middle of <paramref name="control"/>: down, the layout pass a real screen runs, then up at the same point.</summary>
    private static void Tap(Window window, Control control)
    {
        var at = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(at, MouseButton.Left);
        Settle();
        window.MouseUp(at, MouseButton.Left);
        Settle();
    }

    [AvaloniaFact]
    public void ContinueOnTheCaliberQuestionWorksWithTheKeyboardUp()
    {
        var (was, wasInches) = Phone.Settings.LoadShotSetup();
        var (shell, window, capture) = OnCapture(null);
        try
        {
            capture.AskFirst(() => { });
            Settle();
            var box = capture.GetVisualDescendants().OfType<AutoCompleteBox>().First(b => b.IsEffectivelyVisible);
            var field = box.GetVisualDescendants().OfType<TextBox>().First();
            field.Focus();
            box.Text = "6.5 Creedmoor";
            box.IsDropDownOpen = false;
            shell.Keyboard.Opened(874 - 336);
            Settle();

            Tap(window, Named<Button>(capture, "capture-ask-continue"));
            Assert.False(capture.Asking, "Continue did nothing");
        }
        finally
        {
            window.Close();
            Phone.Settings.SaveShotSetup(was, wasInches);
        }
    }

    /// <summary>
    /// A suggestion pressed in the caliber box's list takes the focus from the box while the finger is still down. The page must not move
    /// until the finger is up, or the list moves under it and the release picks another line ("makes it 22", issue 21).
    /// </summary>
    [AvaloniaFact]
    public void NothingMovesWhileAFingerIsDown()
    {
        var (shell, window, capture) = OnCapture("6.5 Creedmoor");
        try
        {
            Tap(window, Named<Button>(capture, "capture-change"));
            var field = capture.GetVisualDescendants().OfType<TextBox>().First(t => t.IsEffectivelyVisible);
            field.Focus();
            shell.Keyboard.Opened(874 - 336);
            Settle();
            double margin = shell.Margin.Bottom;

            // Down on the keyboard's bar, which lies outside the Shell as a pop-up's list does; the focus then leaves the field.
            var bar = shell.Keyboard.Bar;
            var at = bar.TranslatePoint(new Point(10, bar.Bounds.Height / 2), window)!.Value;
            window.MouseDown(at, MouseButton.Left);
            TopLevel.GetTopLevel(shell)!.FocusManager!.Focus(null);
            Settle();
            Assert.Equal(margin, shell.Margin.Bottom);
            Assert.NotNull(shell.Keyboard.KeyboardTop);

            // Up: now the keyboard's room is given back.
            window.MouseUp(at, MouseButton.Left);
            Settle();
            Assert.Null(shell.Keyboard.KeyboardTop);
            Assert.Equal(0, shell.Margin.Bottom);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheSetupRowsDoneWorksWithTheKeyboardUp()
    {
        var (was, wasInches) = Phone.Settings.LoadShotSetup();
        var (shell, window, capture) = OnCapture("6.5 Creedmoor");
        try
        {
            var change = Named<Button>(capture, "capture-change");
            Tap(window, change);
            Assert.Equal("Done", change.Content);
            var field = capture.GetVisualDescendants().OfType<TextBox>().Last(t => t.IsEffectivelyVisible);
            field.Focus();
            shell.Keyboard.Opened(874 - 336);
            Settle();

            Tap(window, change);
            Assert.Equal("Change", change.Content);
        }
        finally
        {
            window.Close();
            Phone.Settings.SaveShotSetup(was, wasInches);
        }
    }

    [AvaloniaFact]
    public void TakeAPictureWorksWhileABoxStillHasTheFocus()
    {
        var (was, wasInches) = Phone.Settings.LoadShotSetup();
        var (shell, window, capture) = OnCapture("6.5 Creedmoor");
        try
        {
            // The keyboard went away without a word from the system, and the caliber box kept the focus.
            Tap(window, Named<Button>(capture, "capture-change"));
            capture.GetVisualDescendants().OfType<TextBox>().First(t => t.IsEffectivelyVisible).Focus();
            Settle();

            Tap(window, Named<Button>(capture, "capture-take-picture"));
            Assert.Contains(capture.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == CapturePage.CameraWords);
        }
        finally
        {
            window.Close();
            Phone.Settings.SaveShotSetup(was, wasInches);
        }
    }
}
