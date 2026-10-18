using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App.Theme;

namespace GroupLab.App;

/// <summary>
/// Keyboard use on the desktop, NOTES-FROM-PLANNING.md entry 442, Quack's request on the Discord from nightly 198 on macOS.
/// <para>
/// <b>Focus survives a rebuild.</b> Choosing a shot in the analysis's table, like choosing a tab or a session, rebuilds the panel it sits
/// in, so the control that had the focus left the window and the focus with it: the next Tab started again from the top. When the focused
/// control leaves the window this way and nothing else has taken the focus, it goes to the control that replaced it: the one with the same
/// <see cref="Control.Tag"/> where the control had one (a shot row carries its shot), otherwise the same kind of control in the same place
/// of the rebuilt panel. It comes back the way it went, so a ring drawn for the keyboard is drawn again and a click draws none.
/// </para>
/// <para>
/// <b>Arrows move between buttons.</b> Left and Right go to the button beside on the same line, within the bar or panel the button sits
/// in (the tab strip, the header, a toolbar, a scrolled column); Up and Down do the same between the rail's buttons and a list's rows.
/// A control that takes its own arrows says so with <see cref="OwnsArrowsProperty"/>, as the analysis's shot table does. Text boxes,
/// number boxes, radio buttons, check boxes and drop downs are never focused buttons here, so they keep their own arrows, and moving the
/// focus never presses anything.
/// </para>
/// </summary>
internal static class KeyboardFocus
{
    /// <summary>Set on a panel whose own handler answers the arrow keys, so the general movement leaves them to it.</summary>
    public static readonly AttachedProperty<bool> OwnsArrowsProperty = AvaloniaProperty.RegisterAttached<Control, bool>("OwnsArrows", typeof(KeyboardFocus));

    public static void SetOwnsArrows(Control control, bool value) => control.SetValue(OwnsArrowsProperty, value);

    public static bool GetOwnsArrows(Control control) => control.GetValue(OwnsArrowsProperty);

    /// <summary>Gives a window both behaviors: the focus kept through a rebuild, and the arrows between buttons.</summary>
    public static void Attach(TopLevel top)
    {
        ArgumentNullException.ThrowIfNull(top);
        var keeper = new Keeper(top);
        top.AddHandler(InputElement.GotFocusEvent, (_, e) => keeper.Remember(e), RoutingStrategies.Bubble, handledEventsToo: true);
        top.AddHandler(InputElement.KeyDownEvent, (_, e) => Arrow(top, e), RoutingStrategies.Tunnel);
    }

    /// <summary>A button the arrows move between: not a check box, radio button or switch, which have arrows or values of their own.</summary>
    internal static bool Moves(Control c) =>
        c is Button and not (CheckBox or RadioButton or ToggleSwitch) && c.Focusable && c.IsEffectivelyVisible && c.IsEffectivelyEnabled && c.Bounds is { Width: > 0, Height: > 0 };

    private static void Arrow(TopLevel top, KeyEventArgs e)
    {
        if (e.Handled || e.KeyModifiers != KeyModifiers.None || e.Key is not (Key.Left or Key.Right or Key.Up or Key.Down)
            || top.FocusManager?.GetFocusedElement() is not Button from || !Moves(from)
            || GetOwnsArrows(from) || from.GetVisualAncestors().OfType<Control>().Any(GetOwnsArrows))
        {
            return;
        }

        // Up and Down belong to columns of buttons: the rail, and a list's rows. Elsewhere they are left to scroll the page as they did.
        if (e.Key is Key.Up or Key.Down && !from.Classes.Contains(AppStyles.RailButton) && !from.Classes.Contains(AppStyles.TableRow))
        {
            return;
        }

        if (Beside(top, from, e.Key) is { } to)
        {
            to.Focus(NavigationMethod.Directional);
            e.Handled = true;
        }
    }

    /// <summary>
    /// The nearest button in the direction of <paramref name="key"/> on the same line (Left, Right) or in the same column (Up, Down), within
    /// the bar or scrolled panel <paramref name="from"/> sits in; null at the end of the line.
    /// </summary>
    internal static Button? Beside(TopLevel top, Button from, Key key)
    {
        if (Rect(top, from) is not { } here)
        {
            return null;
        }

        var region = from.GetVisualAncestors().OfType<Control>().FirstOrDefault(Region) ?? top;
        Button? best = null;
        double nearest = double.MaxValue;
        foreach (var c in region.GetVisualDescendants().OfType<Button>().Where(b => b != from && Moves(b)))
        {
            if (Rect(top, c) is not { } there)
            {
                continue;
            }

            bool sameLine = there.Top < here.Bottom - 1 && there.Bottom > here.Top + 1;
            bool sameColumn = there.Left < here.Right - 1 && there.Right > here.Left + 1;
            double d = key switch
            {
                Key.Right when sameLine => there.Center.X - here.Center.X,
                Key.Left when sameLine => here.Center.X - there.Center.X,
                Key.Down when sameColumn => there.Center.Y - here.Center.Y,
                Key.Up when sameColumn => here.Center.Y - there.Center.Y,
                _ => -1,
            };
            if (d > 1 && d < nearest)
            {
                (best, nearest) = (c, d);
            }
        }

        return best;
    }

    /// <summary>Where the arrows stop looking: a scrolled panel, or one of the window's bars, rail and side columns.</summary>
    private static bool Region(Control c) =>
        c is ScrollViewer || (c is Border && (c.Classes.Contains(AppStyles.Rail) || c.Classes.Contains(AppStyles.Bar) || c.Classes.Contains(AppStyles.Breadcrumb)
            || c.Classes.Contains(AppStyles.Side) || c.Classes.Contains(AppStyles.StatusBar)));

    private static Rect? Rect(Visual top, Control c) =>
        c.TranslatePoint(default, top) is { } at ? new Rect(at, c.Bounds.Size) : null;

    /// <summary>The control last focused and where it sat, so a rebuilt panel can give the focus to its replacement.</summary>
    private sealed class Keeper(TopLevel top)
    {
        private readonly Dictionary<Control, int> places = new(ReferenceEqualityComparer.Instance);
        private Control? focused;
        private NavigationMethod how;

        /// <summary>The focused control and its ancestors when it took the focus: once it has left, its own parents are gone.</summary>
        private List<Control> chain = [];

        public void Remember(FocusChangedEventArgs e)
        {
            if (e.Source is not Control c || ReferenceEquals(c, focused))
            {
                return;
            }

            if (focused is not null)
            {
                focused.DetachedFromVisualTree -= Left;
            }

            (focused, how) = (c, e.NavigationMethod);
            c.DetachedFromVisualTree += Left;
            places.Clear();
            chain = [.. c.GetVisualAncestors().OfType<Control>().Prepend(c)];
            foreach (var a in chain)
            {
                if (a.GetVisualParent() is Panel parent)
                {
                    places[a] = parent.Children.IndexOf(a);
                }
            }
        }

        /// <summary>The focused control left the window: once the code that rebuilt its panel has finished, find what replaced it.</summary>
        private void Left(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (sender is not Control gone)
            {
                return;
            }

            gone.DetachedFromVisualTree -= Left;
            focused = null;
            var ancestors = chain;
            var method = how;
            Dispatcher.UIThread.Post(() => Restore(gone, ancestors, method), DispatcherPriority.Background);
        }

        private void Restore(Control gone, List<Control> ancestors, NavigationMethod method)
        {
            if (top.FocusManager?.GetFocusedElement() is Control now && !ReferenceEquals(now, top) && now.IsAttachedToVisualTree())
            {
                return;
            }

            // The panel that was rebuilt is the nearest ancestor still in the window; its old child held the focused control.
            int i = ancestors.FindIndex(a => a.IsAttachedToVisualTree());
            if (i <= 0 || ancestors[i] is not Panel panel || ancestors[i - 1] is not { } oldChild)
            {
                return;
            }

            var candidates = panel.GetVisualDescendants().OfType<Control>().Where(c => c.GetType() == gone.GetType() && c.Focusable && c.IsEffectivelyVisible && c.IsEffectivelyEnabled);
            var replacement = gone.Tag is { } tag
                ? candidates.FirstOrDefault(c => Equals(c.Tag, tag))
                : null;
            if (replacement is null && gone.Tag is null && panel.Children.Count > 0)
            {
                // The same kind of control, as many along in the rebuilt child as it was in the old one, in the child at the same place.
                int place = Math.Min(Place(oldChild), panel.Children.Count - 1);
                var same = new[] { panel.Children[place] }.Concat(panel.Children[place].GetVisualDescendants().OfType<Control>())
                    .Where(c => c.GetType() == gone.GetType() && c.Focusable && c.IsEffectivelyVisible && c.IsEffectivelyEnabled).ToList();
                int along = new[] { oldChild }.Concat(oldChild.GetVisualDescendants().OfType<Control>()).Where(c => c.GetType() == gone.GetType()).ToList().IndexOf(gone);
                replacement = along >= 0 && along < same.Count ? same[along] : null;
            }

            replacement?.Focus(method == NavigationMethod.Pointer ? NavigationMethod.Pointer : NavigationMethod.Tab);
        }

        /// <summary>Where the old child sat among its panel's children when it was focused, which its panel no longer remembers.</summary>
        private int Place(Control oldChild) => places.TryGetValue(oldChild, out int at) ? at : 0;
    }
}
