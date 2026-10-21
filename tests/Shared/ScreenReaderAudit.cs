using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace GroupLab.Tests.Support;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 461 section 2.1, on the computer: what Narrator would find, checked by machine. Avalonia hands Windows UI
/// Automation its automation peers, so the peer tree walked here is the tree Narrator reads. Every control a person can press, type into
/// or choose, shown and enabled, must be in that tree (not hidden from it), carry a name and a control type that says what it is, and be
/// reached by Tab: a stop itself, or inside a stop that takes it with the arrow keys (a list, a row of tabs, a group of choices). On the
/// phone, where nothing is reached by Tab, the same walk holds every control to a touch target of at least 48 by 48 (dp on Android,
/// points on iOS), what Android's accessibility checks ask; the file is shared with the phone's tests.
/// </summary>
internal static class ScreenReaderAudit
{
    public static List<string> Findings(TopLevel top, IReadOnlyCollection<Control>? stops, double smallestTarget = 0)
    {
        var inTree = new HashSet<Control>();
        Walk(ControlAutomationPeer.CreatePeerForElement(top), inTree, 0);

        var findings = new List<string>();
        foreach (var c in top.GetVisualDescendants().OfType<Control>())
        {
            if (!IsInteractive(c) || !c.IsEffectivelyVisible || !c.IsEffectivelyEnabled || IsInsideAnother(c) || IsClippedAway(c, top))
            {
                continue;
            }

            string what = Describe(c);
            if (!inTree.Contains(c))
            {
                findings.Add(what + ": hidden from the screen reader's tree");
                continue;
            }

            var peer = ControlAutomationPeer.CreatePeerForElement(c);
            if (!IsSpokenName(peer.GetName()))
            {
                findings.Add(what + ": no name" + (string.IsNullOrWhiteSpace(peer.GetName()) ? "" : ", it reads \"" + peer.GetName() + "\""));
            }

            var type = peer.GetAutomationControlType();
            if (type is AutomationControlType.None or AutomationControlType.Custom or AutomationControlType.Pane or AutomationControlType.Group)
            {
                findings.Add(what + ": its control type is " + type + ", which does not say what it does");
            }

            if (stops is not null && !Reached(c, stops))
            {
                findings.Add(what + ": Tab never reaches it");
            }

            if (smallestTarget > 0 && (c.Bounds.Width < smallestTarget - 0.5 || c.Bounds.Height < smallestTarget - 0.5))
            {
                findings.Add($"{what}: {c.Bounds.Width:0} by {c.Bounds.Height:0}, smaller than {smallestTarget:0} by {smallestTarget:0} to touch");
            }
        }

        return findings;
    }

    /// <summary>
    /// A button holding a picture and no words is named after the picture's type by Avalonia ("Avalonia.Controls.Shapes.Ellipse"), which is
    /// what Narrator would read, so a name shaped like a type is no name.
    /// </summary>
    public static bool IsSpokenName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && !System.Text.RegularExpressions.Regex.IsMatch(name, @"^(Avalonia|GroupLab|System)(\.[A-Za-z_][A-Za-z0-9_`]*)+$");

    private static void Walk(AutomationPeer peer, HashSet<Control> seen, int depth)
    {
        if (depth > 200)
        {
            return;
        }

        // Narrator reads UI Automation's control view, which leaves out a peer that is not a control element (an AccessibilityView of
        // Raw) and lifts its children to its parent, so those are still walked.
        if (peer is ControlAutomationPeer owned && peer.IsControlElement())
        {
            seen.Add(owned.Owner);
        }

        foreach (var child in peer.GetChildren())
        {
            Walk(child, seen, depth + 1);
        }
    }

    /// <summary>
    /// What a person presses, types into or chooses. Scroll bars and a text box's own parts are the platform's, not GroupLab's. A box that
    /// suggests as you type is read through the text box inside it, which is what takes the focus and what Narrator announces as an edit.
    /// </summary>
    private static bool IsInteractive(Control c) => c switch
    {
        ScrollBar or Thumb or RepeatButton { TemplatedParent: ScrollBar } => false,
        Button or ToggleButton or TextBox or ComboBox or Slider or ListBoxItem or TabItem or MenuItem or ToggleSwitch or NumericUpDown
            or CalendarDatePicker => true,
        _ => false,
    };

    /// <summary>
    /// A part of another control's template (a combo box's drop button, a number box's spinner, a text box inside a number box) is read
    /// as that control, so only the outer one is checked.
    /// </summary>
    private static bool IsInsideAnother(Control c) =>
        c.TemplatedParent is Control parent && parent != c && IsInteractive(parent);

    /// <summary>A control scrolled far out of its viewer is still in the tree; it is checked when it is shown, so nothing is skipped by it.</summary>
    private static bool IsClippedAway(Control c, TopLevel top) => c.Bounds.Width <= 0 || c.Bounds.Height <= 0;

    private static bool Reached(Control c, IReadOnlyCollection<Control> stops)
    {
        if (stops.Contains(c))
        {
            return true;
        }

        foreach (var s in stops)
        {
            // A stop around it (a list taking its items with the arrows) or inside it (a number box's own text box).
            if (s.IsVisualAncestorOf(c) || c.IsVisualAncestorOf(s))
            {
                return true;
            }
        }

        // A group the Tab key enters once and the arrows move through (KeyboardNavigation.TabNavigation Once, as the rows of buttons, the
        // tabs and the lists are): reached when any stop is inside the same group.
        for (var parent = c.GetVisualParent(); parent is not null; parent = parent.GetVisualParent())
        {
            if (parent is InputElement group && KeyboardNavigation.GetTabNavigation(group) is KeyboardNavigationMode.Once or KeyboardNavigationMode.Local)
            {
                if (KeyboardNavigation.GetTabNavigation(group) == KeyboardNavigationMode.Once && stops.Any(s => group.IsVisualAncestorOf(s)))
                {
                    return true;
                }
            }

            if (parent is TopLevel)
            {
                break;
            }
        }

        return false;
    }

    private static string Describe(Control c)
    {
        string label = c switch
        {
            ContentControl { Content: string words } => words,
            TextBox box => box.PlaceholderText ?? box.Text ?? "",
            _ => AutomationProperties.GetName(c) ?? "",
        };
        string id = AutomationProperties.GetAutomationId(c) ?? c.Name ?? "";
        return $"{c.GetType().Name} \"{label}\"{(id.Length > 0 ? " " + id : "")}";
    }
}
