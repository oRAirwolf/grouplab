using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 514 section 3: a sensible order by the accessibility tree on the phone. TalkBack and VoiceOver move through
/// a screen in the order of the automation tree the platforms are handed, so on every place along the bottom, in both themes, the controls
/// a person can tap are met in reading order: top to bottom, and left to right along a row. The bar along the bottom is the one exception
/// a phone expects, read after the page it sits under.
/// </summary>
public class Entry514PhoneReadingOrderTests
{

    [AvaloniaFact]
    public void EveryPlaceIsReadTopToBottom()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
        var all = new List<string>();
        foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            var shell = new Shell();
            var window = new Window { Width = 412, Height = 915, RequestedThemeVariant = theme, Content = shell };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            try
            {
                foreach (var place in Enum.GetValues<Shell.Place>())
                {
                    shell.Show(place);
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                    Dispatcher.UIThread.RunJobs();
                    all.AddRange(OutOfOrder(window).Select(f => $"{theme}, {place}: {f}"));
                }
            }
            finally
            {
                window.Close();
            }
        }

        Assert.True(all.Count == 0, all.Count + " findings:\n" + string.Join("\n", all.Distinct()));
    }

    /// <summary>The check itself, against a window made to fail it: a button drawn at the top but placed last in the tree.</summary>
    [AvaloniaFact]
    public void TheCheckFindsAButtonReadOutOfOrder()
    {
        var grid = new Grid { RowDefinitions = new RowDefinitions("60,60,60") };
        var second = new Button { Content = "Second", Height = 48 };
        var third = new Button { Content = "Third", Height = 48 };
        var first = new Button { Content = "First", Height = 48 };
        Grid.SetRow(second, 1);
        Grid.SetRow(third, 2);
        Grid.SetRow(first, 0);
        grid.Children.Add(second);
        grid.Children.Add(third);
        grid.Children.Add(first);
        var window = new Window { Width = 300, Height = 300, Content = grid };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try
        {
            var found = OutOfOrder(window);
            Assert.Single(found);
            Assert.Contains("\"First\"", found[0], StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Each tappable control met in the tree after one it lies wholly above.</summary>
    internal static List<string> OutOfOrder(TopLevel top)
    {
        var order = new List<Control>();
        Walk(ControlAutomationPeer.CreatePeerForElement(top), order);
        var placed = order
            .Where(c => c.IsEffectivelyVisible && IsTappable(c) && !InTheBar(c) && !c.GetVisualAncestors().OfType<ScrollBar>().Any())
            .Select(c => (Control: c, At: c.TranslatePoint(default, top)))
            .Where(c => c.At is { } p && p.Y >= 0 && p.Y < top.Bounds.Height)
            .Select(c => (c.Control, At: c.At!.Value))
            .ToList();
        var findings = new List<string>();
        // The lowest control read so far; one met later that lies wholly above it, sharing no height with it, is out of order. Two that
        // share some height are one row, as a label that wraps to two lines sets one box a little lower than its neighbour.
        Control? lowest = null;
        Point lowestAt = default;
        foreach (var (control, at) in placed)
        {
            if (lowest is not null && at.Y + control.Bounds.Height <= lowestAt.Y)
            {
                findings.Add($"{Describe(control)} at {at.Y:0} is read after {Describe(lowest)} at {lowestAt.Y:0}");
            }

            if (lowest is null || at.Y > lowestAt.Y)
            {
                lowest = control;
                lowestAt = at;
            }
        }

        return findings;
    }

    private static void Walk(AutomationPeer peer, List<Control> into)
    {
        if (peer is ControlAutomationPeer c)
        {
            into.Add(c.Owner);
        }

        foreach (var child in peer.GetChildren())
        {
            Walk(child, into);
        }
    }

    private static bool IsTappable(Control c) => c is Button or ToggleButton or TextBox or ComboBox or Slider or ListBoxItem or ToggleSwitch;

    /// <summary>The bar of places along the bottom, read after the page, as phones read a tab bar.</summary>
    private static bool InTheBar(Control c) => c.GetSelfAndVisualAncestors().OfType<Control>().Any(v =>
        Avalonia.Automation.AutomationProperties.GetAutomationId(v) is { } id && id.StartsWith("tab-", StringComparison.Ordinal));

    private static string Describe(Control c) =>
        (Avalonia.Automation.AutomationProperties.GetAutomationId(c) is { Length: > 0 } id ? id : c.GetType().Name)
        + (ControlAutomationPeer.CreatePeerForElement(c).GetName() is { Length: > 0 } name ? $" \"{name}\"" : "");
}
