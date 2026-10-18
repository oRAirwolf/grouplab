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
/// Paste menu, which took the next tap. A menu a hold opened for a finger the system says was down for less than the hold's wait is
/// closed as the release is handled; a real hold keeps its menu.
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
    public void AMenuOpenedForATapIsClosedWhenItsReleaseComes()
    {
        var (window, box) = Shown();
        try
        {
            Assert.NotNull(box.ContextFlyout);
            var at = Middle(window, box);
            window.MouseDown(at, MouseButton.Left);
            HoldFires(box);
            Assert.True(box.ContextFlyout!.IsOpen, "the hold did not open the box's menu, so this test proves nothing");
            window.MouseUp(at, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Assert.False(box.ContextFlyout.IsOpen);
        }
        finally
        {
            box.ContextFlyout?.Hide();
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

    [AvaloniaFact]
    public async Task ARealHoldKeepsItsMenu()
    {
        var (window, box) = Shown();
        try
        {
            var at = Middle(window, box);
            window.MouseDown(at, MouseButton.Left);
            await Task.Delay((int)(Avalonia.Application.Current!.PlatformSettings!.HoldWaitDuration.TotalMilliseconds * 2) + 100);
            HoldFires(box);
            window.MouseUp(at, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Assert.True(box.ContextFlyout!.IsOpen);
        }
        finally
        {
            box.ContextFlyout?.Hide();
            window.Close();
        }
    }
}
