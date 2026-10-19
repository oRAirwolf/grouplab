using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.Mobile.Dev;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 447 section 3: the iOS self-test's lost first Next. The tap that put the focus in the distance box was
/// handled late, while the keyboard first came up, so the wait for a hold ran out before its release and the box opened its Cut, Copy and
/// Paste menu, which took the next tap. The press that first puts the focus in a field cannot be taken for a hold; on a field that already has the focus a hold is a hold.
/// </summary>
public class Entry447HoldTests
{
    private static (Window Window, TextBox Box) Shown()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        Scenario.AnswerFirstRun(Phone.Settings);
        var shell = new Shell();
        var window = new Window { Width = 402, Height = 874, Content = shell };
        window.Show();
        shell.Show(Shell.Place.Targets);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var box = shell.GetVisualDescendants().OfType<TextBox>().Single(b => AutomationProperties.GetAutomationId(b) == "targets-distance");
        return (window, box);
    }

    private static Point Middle(Window window, Control control) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

    private static void HoldFires(TextBox box)
    {
        // What the hold's wait running out does: the box asks for its menu while the finger is still down as far as the window knows.
        box.RaiseEvent(new ContextRequestedEventArgs { RoutedEvent = InputElement.ContextRequestedEvent, Source = box });
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void ThePressThatFocusesABoxCannotBecomeAHold()
    {
        var (window, box) = Shown();
        try
        {
            var at = Middle(window, box);
            Assert.False(box.IsFocused);
            window.MouseDown(at, MouseButton.Left);
            Assert.False(box.GetValue(InputElement.IsHoldingEnabledProperty), "holding is on during the press that focuses the box");
            window.MouseUp(at, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Assert.True(box.GetValue(InputElement.IsHoldingEnabledProperty), "holding did not come back after the release");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ABoxThatHasTheFocusKeepsItsHold()
    {
        var (window, box) = Shown();
        try
        {
            box.Focus();
            Dispatcher.UIThread.RunJobs();
            var at = Middle(window, box);
            window.MouseDown(at, MouseButton.Left);
            Assert.True(box.GetValue(InputElement.IsHoldingEnabledProperty));
            window.MouseUp(at, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>While a menu is open the scripted taps' check says so, rather than that the tap will reach its control.</summary>
    [AvaloniaFact]
    public void TheTapCheckSeesAnOpenMenu()
    {
        var (window, box) = Shown();
        try
        {
            Assert.True(Scenario.HoldFor("targets-magnification")["reached"]!.GetValue<bool>());
            HoldFires(box);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var said = Scenario.HoldFor("targets-magnification");
            Assert.False(said["reached"]!.GetValue<bool>());
            Assert.Equal("LightDismissOverlayLayer", said["under"]!.GetValue<string>());
        }
        finally
        {
            box.ContextFlyout?.Hide();
            window.Close();
        }
    }

    /// <summary>
    /// What decides a hit test is said for a control a press cannot reach and each of its holders, one log field each: the bar's Next went
    /// unreached for seconds after the first keyboard in every simulator run, and the log could not say why.
    /// </summary>
    [AvaloniaFact]
    public void AHolderIsSaidAsAHitTestSeesIt()
    {
        var (window, box) = Shown();
        try
        {
            box.IsHitTestVisible = false;
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            string said = Scenario.Holder(box);
            Assert.StartsWith("TextBox ", said, StringComparison.Ordinal);
            Assert.Contains(" hit=False on=True seen=True ", said, StringComparison.Ordinal);
            Assert.Matches(" drawn=-?[0-9]+,-?[0-9]+/[0-9]+x[0-9]+/shown/", said);
        }
        finally
        {
            window.Close();
        }
    }
}
