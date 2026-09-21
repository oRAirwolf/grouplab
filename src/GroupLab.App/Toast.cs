using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using GroupLab.App.Theme;

namespace GroupLab.App;

/// <summary>One thing that happened, and the way to put it back if it can be put back.</summary>
/// <param name="Says">What changed, in the past tense, in one short line.</param>
/// <param name="Undo">What to run if the person presses Undo, or null where the change cannot be undone.</param>
public sealed record Confirmation(string Says, Action? Undo = null);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 9: every change a person makes says so in a small line at the bottom right that goes away by
/// itself, with Undo where the change can be undone.
/// <para>
/// <b>One component, used everywhere, and never a dialog.</b> A dialog for a confirmation is a claim on somebody's attention: it stops what
/// they are doing and demands a click before they can carry on. That is right for "this will delete twenty sessions" and wrong for "the shot
/// moved", which is most of what happens in this application. A toast says what happened, offers the way back, and gets out of the way.
/// </para>
/// <para>
/// It is also the honest place for Undo. A confirmation dialog asks "are you sure?" before the person can see what the change looks like; a
/// toast lets them do it, look at it, and undo it having seen the result, which is the order a person actually decides in.
/// </para>
/// </summary>
public sealed class Toaster
{
    /// <summary>How long a toast stays. Long enough to read a short line twice, short enough not to sit in the way.</summary>
    public static TimeSpan Stays { get; set; } = TimeSpan.FromSeconds(3.5);

    private readonly StackPanel stack = new()
    {
        Orientation = Orientation.Vertical,
        Spacing = Tokens.Space8,
        HorizontalAlignment = HorizontalAlignment.Right,
        VerticalAlignment = VerticalAlignment.Bottom,
        Margin = new Thickness(Tokens.Space16, Tokens.Space16, Tokens.Space16, Tokens.Space24),
        IsHitTestVisible = true,
    };

    /// <summary>The layer the toasts live in, put over the window's content and never taking a click that is not on a toast.</summary>
    public Control Layer => stack;

    /// <summary>What is showing, for the headless tests.</summary>
    public IReadOnlyList<string> Showing =>
        [.. stack.Children.OfType<Border>().Select(b => (b.Child as DockPanel)?.Children.OfType<TextBlock>().FirstOrDefault()?.Text ?? "")];

    /// <summary>
    /// Whether a toast disappears on its own. The tests turn it off, because a control that removes itself on a timer cannot be asserted
    /// about reliably, and what matters to a test is what was said rather than for how long.
    /// </summary>
    public bool FadesByItself { get; set; } = true;

    /// <summary>Says one thing happened.</summary>
    public void Say(string says) => Show(new Confirmation(says));

    /// <summary>Says one thing happened, with the way to put it back.</summary>
    public void Show(Confirmation confirmation)
    {
        ArgumentNullException.ThrowIfNull(confirmation);

        var line = new TextBlock
        {
            Text = confirmation.Says,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 360,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var row = new DockPanel { LastChildFill = true };
        var card = new Border
        {
            Child = row,
            Padding = new Thickness(Tokens.Space16, Tokens.Space12),
            CornerRadius = Tokens.SurfaceRadius,
            Classes = { AppStyles.Bar },
        };

        if (confirmation.Undo is { } undo)
        {
            var back = new Button { Content = "Undo", Margin = new Thickness(Tokens.Space16, 0, 0, 0) };
            back.Click += (_, _) =>
            {
                undo();
                Remove(card);

                // Saying that the undo happened is the same promise as saying the change did. Without it a person cannot tell whether the
                // button worked, and the toast they pressed it on has just vanished.
                Say("Put back.");
            };

            DockPanel.SetDock(back, Dock.Right);
            row.Children.Add(back);
        }

        row.Children.Add(line);
        stack.Children.Add(card);

        // Never more than a few at once: a column of toasts climbing the window is worse than the one thing each of them says.
        while (stack.Children.Count > 3)
        {
            stack.Children.RemoveAt(0);
        }

        if (!FadesByItself)
        {
            return;
        }

        DispatcherTimer.RunOnce(() => Remove(card), Stays);
    }

    /// <summary>Takes everything off, for a test and for a screen change that makes the lines meaningless.</summary>
    public void Clear() => stack.Children.Clear();

    private void Remove(Control card)
    {
        if (stack.Children.Contains(card))
        {
            stack.Children.Remove(card);
        }
    }
}
