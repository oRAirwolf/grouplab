using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 328 section 1, Unholy's TestFlight report of nightly 143 on an iPhone 402 by 874 points: "Keyboard covers
/// the fields to enter information and I can't confirm data entry." Capture's caliber sheet at his size and at the smallest phone, then
/// every screen with a field: the focused field and the button that confirms it sit above the keyboard and its bar, the bar says Next or
/// Done, and Done puts the keyboard away with everything back as it was.
/// </summary>
public class Entry328Tests
{
    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>The iPhone's decimal pad with its suggestion strip at his size, and the shortest a phone's keyboard is.</summary>
    private static readonly (double Width, double Height, double Keyboard)[] Phones = [(402, 874, 336), (320, 568, 253)];

    private static (Shell Shell, Window Window) Started(double width, double height)
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
        var shell = new Shell();
        var window = new Window { Width = width, Height = height, Content = shell };
        window.Show();
        Settle();
        return (shell, window);
    }

    /// <summary>Where a control's bottom edge is in the window, and its top.</summary>
    private static (double Top, double Bottom) InWindow(Control control, Window window)
    {
        var at = control.TranslatePoint(default, window) ?? throw new Xunit.Sdk.XunitException(control + " is not in the window");
        return (at.Y, at.Y + control.Bounds.Height);
    }

    /// <summary>Focuses the field, raises the keyboard, and checks the field and its confirming button are both clear of it.</summary>
    internal static void ClearOfTheKeyboard(Shell shell, Window window, TextBox field, double keyboard, string where)
    {
        field.Focus();
        double top = window.Height - keyboard;
        shell.Keyboard.Opened(top);
        Settle();
        double clear = top - KeyboardRoom.BarHeight + 0.5;
        var (fieldTop, fieldBottom) = InWindow(field, window);
        Assert.True(fieldBottom <= clear && fieldTop >= -0.5, $"{where}: the field runs {fieldTop:0} to {fieldBottom:0}; the keyboard's bar starts at {clear:0}");
        // On the last field the confirming button comes into view with it, wherever the two fit in the room above the bar together;
        // where they cannot, on the smallest phone, Done on the bar is how the entry is confirmed, and the button is a scroll away.
        if (shell.Keyboard.BarWords == "Done" && KeyboardRoom.Confirm(field) is { } button
            && InWindow(button, window).Bottom - fieldTop <= clear - shell.Bounds.Top)
        {
            var (_, buttonBottom) = InWindow(button, window);
            Assert.True(buttonBottom <= clear, $"{where}: \"{button.Content}\" ends at {buttonBottom:0}, under the keyboard's bar at {clear:0}");
        }

        var bar = shell.Keyboard.Bar;
        Assert.True(bar.IsEffectivelyVisible, where + ": the Next or Done bar is not showing");
        Assert.Equal(top - KeyboardRoom.BarHeight, Canvas.GetTop(bar), 1);
    }

    [AvaloniaFact]
    public void TheCaliberSheetKeepsTheDistanceAndContinueAboveTheKeyboard()
    {
        foreach (var (width, height, keyboard) in Phones)
        {
            // A caliber another test remembered would let Capture go straight on without asking, so none is remembered while the page is
            // made, and the one there was is put back afterwards.
            if (Phone.Platform is null)
            {
                Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
            }

            var (was, wasInches) = Phone.Settings.LoadShotSetup();
            Phone.Settings.SaveShotSetup(null, null);
            var (shell, window) = Started(width, height);
            try
            {
                var capture = shell.GetVisualDescendants().OfType<CapturePage>().Single();
                capture.AskFirst(() => { });
                Settle();
                Assert.True(capture.Asking);
                var distance = capture.GetVisualDescendants().OfType<TextBox>().Single(t => Avalonia.Automation.AutomationProperties.GetAutomationId(t) == "capture-distance");
                ClearOfTheKeyboard(shell, window, distance, keyboard, $"the caliber sheet at {width} by {height}");
                var continueButton = KeyboardRoom.Confirm(distance);
                Assert.Equal("capture-ask-continue", continueButton is null ? null : Avalonia.Automation.AutomationProperties.GetAutomationId(continueButton));

                // The distance is the sheet's last field: Done, which keeps what was typed and puts the keyboard away.
                distance.Text = "100";
                Assert.Equal("Done", shell.Keyboard.BarWords);
                shell.Keyboard.Advance();
                Settle();
                Assert.Null(shell.Keyboard.KeyboardTop);
                Assert.False(distance.IsFocused);
                Assert.Equal("100", distance.Text);
                Assert.Equal(default, shell.Margin);
                Assert.False(shell.Keyboard.Bar.IsEffectivelyVisible && shell.Keyboard.Bar.GetVisualParent() is not null);

                // The caliber comes first: Next takes the person to the distance.
                var calibre = capture.GetVisualDescendants().OfType<TextBox>().First(t => t != distance && t.IsEffectivelyVisible);
                calibre.Focus();
                shell.Keyboard.Opened(height - keyboard);
                Settle();
                Assert.Equal("Next", shell.Keyboard.BarWords);
                shell.Keyboard.Advance();
                Settle();
                Assert.True(distance.IsFocused);
                shell.Keyboard.CloseKeyboard();
            }
            finally
            {
                window.Close();
                Phone.Settings.SaveShotSetup(was, wasInches);
            }
        }
    }

    /// <summary>Every screen with a field, each field in turn, at the smallest phone.</summary>
    [AvaloniaFact]
    public void EveryFieldOnEveryScreenStaysAboveTheKeyboard()
    {
        var (width, height, keyboard) = Phones[^1];
        var (shell, window) = Started(width, height);
        var screens = new (string Name, Action Show)[]
        {
            ("Settings", () => shell.Show(Shell.Place.Settings)),
            ("Targets", () => shell.Show(Shell.Place.Targets)),
            ("Ballistics", () => shell.Show(Shell.Place.Ballistics)),
            ("Ballistics, the rifle", () => shell.ShowBallistics(MarkingState.Empty, open: "rifle")),
            ("Ballistics, the load", () => shell.ShowBallistics(MarkingState.Empty, open: "load")),
            ("Ballistics, the air", () => shell.ShowBallistics(MarkingState.Empty, open: "air")),
            ("the chronograph readings", () => shell.ShowInPage(VelocityPages.Chronograph(1, MarkingState.Empty, () => { }))),
            ("the distance shot", () => shell.ShowInPage(VelocityPages.Distance(UnitSettings.Imperial, _ => { }, () => { }))),
            ("the printer check", () => shell.ShowPrinterCheck("Test printer")),
        };
        try
        {
            int fields = 0;
            foreach (var (name, show) in screens)
            {
                show();
                Settle();
                var boxes = shell.GetVisualDescendants().OfType<TextBox>().Where(t => t.IsEffectivelyVisible && t.IsEffectivelyEnabled).ToList();
                foreach (var box in boxes)
                {
                    ClearOfTheKeyboard(shell, window, box, keyboard, $"{name}, field {boxes.IndexOf(box) + 1} of {boxes.Count}");
                    shell.Keyboard.CloseKeyboard();
                    Settle();
                    fields++;
                }
            }

            Assert.True(fields >= 8, $"only {fields} fields were found on the screens");
        }
        finally
        {
            window.Close();
        }
    }
}
