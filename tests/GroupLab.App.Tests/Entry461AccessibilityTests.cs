using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;

using GroupLab.Tests.Support;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 461 section 2.1: the screen reader audit on the computer finds what it looks for. Every window the entry
/// 447 tests tab round is also audited (<see cref="ScreenReaderAudit"/>); this holds the audit itself to a window made to fail it.
/// </summary>
public class Entry461AccessibilityTests
{
    [AvaloniaFact]
    public void TheAuditFindsAControlWithNoNameOneHiddenAndOneTabNeverReaches()
    {
        var named = new Button { Content = "Analyze" };
        var iconOnly = new Button { Content = new Ellipse { Width = 12, Height = 12 } };
        var hidden = new Button { Content = "Hidden" };
        AutomationProperties.SetAccessibilityView(hidden, AccessibilityView.Raw);
        var unreachable = new Button { Content = "Never reached", Focusable = false };
        var window = new Window { Width = 400, Height = 300, Content = new StackPanel { Children = { named, iconOnly, hidden, unreachable } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try
        {
            var (stops, _, _) = Entry447Tests.TabRound(window);
            var found = ScreenReaderAudit.Findings(window, stops);
            Assert.True(found.Count == 3, string.Join("; ", found) + " | names: " + string.Join(", ", new[] { named, iconOnly, hidden, unreachable }.Select(b => Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(b).GetName() ?? "(null)")));
            Assert.Contains("Button \"\": no name, it reads \"Avalonia.Controls.Shapes.Ellipse\"", found);
            Assert.Contains("Button \"Hidden\": hidden from the screen reader's tree", found);
            Assert.Contains("Button \"Never reached\": Tab never reaches it", found);
            Assert.DoesNotContain(found, f => f.Contains("\"Analyze\"", StringComparison.Ordinal));
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>VoiceOver's audit read each empty status line as an element with nothing to say: an empty line is out of the tree until it has words.</summary>
    [AvaloniaFact]
    public void AnEmptyLineOfTextIsOutOfTheTreeUntilItHasWords()
    {
        var status = new TextBlock();
        var kept = new TextBlock { Text = "" };
        AutomationProperties.SetAccessibilityView(kept, AccessibilityView.Content);
        var window = new Window { Width = 300, Height = 200, Content = new StackPanel { Children = { status, kept } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try
        {
            Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(status));
            status.Text = "3 unsure holes";
            Dispatcher.UIThread.RunJobs();
            Assert.NotEqual(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(status));
            status.Text = "";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(status));

            // A choice the code made is left alone.
            Assert.Equal(AccessibilityView.Content, AutomationProperties.GetAccessibilityView(kept));
        }
        finally
        {
            window.Close();
        }
    }
}
