using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.Tests.Support;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 461 section 2.1, on the phone and headless: every place along the bottom read as TalkBack and VoiceOver
/// would read it, from the same automation tree the platforms are handed. Every control a person can tap is in the tree, named in words
/// (never a type's name), says what it is, and is at least 48 by 48 to touch. The emulator and the simulator check the platforms' own
/// trees on top of this (scripts/screen-reader-audit.py).
/// </summary>
public class Entry461PhoneAccessibilityTests
{
    [AvaloniaFact]
    public void EveryPlaceReadsAsAScreenReaderNeedsIt()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        // The first run's questions are read on their own below; answered, every place is reached.
        var firstRun = new Window { Width = 412, Height = 915, Content = new Shell() };
        firstRun.Show();
        Dispatcher.UIThread.RunJobs();
        firstRun.UpdateLayout();
        var asked = ScreenReaderAudit.Findings(firstRun, null, smallestTarget: 48).Select(f => "first run: " + f).ToList();
        firstRun.Close();
        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);

        var shell = new Shell();
        var window = new Window { Width = 412, Height = 915, Content = shell };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try
        {
            var all = new List<string>(asked);
            foreach (var place in Enum.GetValues<Shell.Place>())
            {
                shell.Show(place);
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
                all.AddRange(ScreenReaderAudit.Findings(window, null, smallestTarget: 48).Select(f => place + ": " + f));
            }

            Assert.True(all.Count == 0, all.Count + " findings: " + string.Join("; ", all));
        }
        finally
        {
            window.Close();
        }
    }
}
