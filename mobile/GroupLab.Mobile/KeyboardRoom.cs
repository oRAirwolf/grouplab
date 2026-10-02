using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App.Diagnostics;

namespace GroupLab.Mobile;

/// <summary>
/// The keyboard never covers what is typed, NOTES-FROM-PLANNING.md entry 328 section 1, from Unholy's TestFlight report of nightly 143:
/// "Keyboard covers the fields to enter information and I can't confirm data entry." On Capture's caliber sheet the number pad covered the
/// distance field, the sheet did not move, and the iPhone's decimal pad has no return key, so nothing closed it or confirmed the entry.
/// <para>
/// One place for every screen: while the keyboard is up, the Shell gives up the height it covers, so a sheet pinned to the bottom rises
/// above it and a page scrolls in what is left; the focused field and the button that confirms it are brought into view; a bar sits on
/// the keyboard with Next, to the following field, or Done on the last, which closes it; and a tap outside any field closes it too. The
/// keyboard's top edge is read from the system's input pane in the window's own coordinates, so a window the system has already shrunk
/// for the keyboard gives up nothing more.
/// </para>
/// <para>
/// Entry 350, from a friend's TestFlight reports of build 150 on the Targets screen: the bar was left floating over the screen after the
/// keyboard had gone, and Done did nothing; and the number pad stayed up with no bar, so nothing closed it. Both came from trusting the
/// system's keyboard events to arrive, and in order. So nothing here waits on them any more: a tap outside a field closes the keyboard
/// whenever a field has the focus; Done always takes the focus away, asks the system to end editing, and puts everything back; the bar is
/// shown only while a field has the focus, and takes itself away when none has; and when a field takes the focus with the keyboard
/// already up, the pane's own state raises the bar.
/// </para>
/// </summary>
internal sealed class KeyboardRoom
{
    /// <summary>The bar's height, a thumb's reach as every other control here.</summary>
    internal const double BarHeight = 48;

    private readonly Shell shell;
    private readonly Button next = new() { MinHeight = 40, MinWidth = 88, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
    private readonly Border bar;
    private IInputPane? pane;
    private bool barShown;
    private readonly DispatcherTimer watch = new() { Interval = TimeSpan.FromMilliseconds(400) };

    /// <summary>
    /// Asks the system itself to put the keyboard away, where taking the focus alone may not: the iOS head sets it to end editing in every
    /// window (entry 350). Null elsewhere.
    /// </summary>
    internal static Action? HideSystemKeyboard { get; set; }

    private KeyboardRoom(Shell shell)
    {
        this.shell = shell;
        next.Classes.Add(PhoneStyles.Primary);
        next.Click += (_, _) => Advance();
        bar = new Border
        {
            Height = BarHeight,
            Padding = new Thickness(12, 4),
            Child = next,
            Classes = { PhoneStyles.Card },
            CornerRadius = new CornerRadius(0),
        }.Id("keyboard-bar");
        shell.AddHandler(InputElement.PointerPressedEvent, OutsidePressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        shell.AddHandler(InputElement.GotFocusEvent, (_, _) => Dispatcher.UIThread.Post(Follow, DispatcherPriority.Background), RoutingStrategies.Bubble, handledEventsToo: true);
        shell.AddHandler(InputElement.LostFocusEvent, (_, _) => Dispatcher.UIThread.Post(Check, DispatcherPriority.Background), RoutingStrategies.Bubble, handledEventsToo: true);
        watch.Tick += (_, _) => Check();
        shell.AttachedToVisualTree += (_, _) => Listen();
        shell.DetachedFromVisualTree += (_, _) => Forget();
    }

    /// <summary>The keyboard handling for a Shell, made once.</summary>
    public static KeyboardRoom For(Shell shell) => new(shell);

    /// <summary>The keyboard's top edge in the window's coordinates while it is up, or null when it is down.</summary>
    internal double? KeyboardTop { get; private set; }

    /// <summary>The bar on the keyboard, for the tests.</summary>
    internal Border Bar => bar;

    /// <summary>What the bar's button says: Next where another field follows, Done on the last.</summary>
    internal string BarWords => next.Content as string ?? "";

    private void Listen()
    {
        pane = TopLevel.GetTopLevel(shell)?.InputPane;
        if (pane is not null)
        {
            pane.StateChanged += PaneChanged;
        }
    }

    private void Forget()
    {
        if (pane is not null)
        {
            pane.StateChanged -= PaneChanged;
            pane = null;
        }
    }

    private void PaneChanged(object? sender, InputPaneStateEventArgs e)
    {
        if (e.NewState == InputPaneState.Open && e.EndRect is { Width: > 0, Height: > 0 })
        {
            Opened(e.EndRect.Y);
        }
        else
        {
            Closed();
        }
    }

    /// <summary>The keyboard is up with its top edge at <paramref name="top"/> in the window's coordinates; the tests call this directly.</summary>
    internal void Opened(double top)
    {
        var level = TopLevel.GetTopLevel(shell);
        double height = level?.Bounds.Height ?? shell.Bounds.Height;
        double covered = Math.Max(0, height - top);
        KeyboardTop = top;
        // The safe area at the bottom is under the keyboard too, so it is not given up twice; the bar sits on top of what is left.
        double give = covered <= 0 ? 0 : Math.Max(0, covered - shell.Padding.Bottom) + BarHeight;
        shell.Margin = new Thickness(0, 0, 0, give);
        shell.KeyboardUp(covered > 0);
        ShowBar(level, top, covered > 0);
        DiagnosticLog.Info("keyboard.open", ("covered", Math.Round(covered)));
        Dispatcher.UIThread.Post(Follow, DispatcherPriority.Background);
    }

    /// <summary>The keyboard is down: everything back as it was, whatever this room believed before (entry 350).</summary>
    internal void Closed()
    {
        bool was = KeyboardTop is not null || barShown;
        KeyboardTop = null;
        shell.Margin = default;
        shell.KeyboardUp(false);
        ShowBar(null, 0, false);
        if (was)
        {
            DiagnosticLog.Info("keyboard.closed");
        }
    }

    /// <summary>Whether a field on this Shell has the focus, which is when the keyboard belongs up.</summary>
    private bool Typing => Focused is { } f && (f is TextBox || f.GetVisualAncestors().Any(v => v is TextBox));

    /// <summary>
    /// Entry 350: the bar and the room given up stay only while a field has the focus and the system has not said the keyboard is down.
    /// </summary>
    internal void Check()
    {
        if (KeyboardTop is null && !barShown)
        {
            return;
        }

        if (!Typing || pane is { State: InputPaneState.Closed })
        {
            Closed();
        }
    }

    private void ShowBar(TopLevel? level, double top, bool show)
    {
        var layer = OverlayLayer.GetOverlayLayer(shell);
        if (layer is null)
        {
            if (!show && barShown && bar.GetVisualParent() is Panel holder)
            {
                holder.Children.Remove(bar);
                barShown = false;
            }

            watch.IsEnabled = barShown;
            return;
        }

        if (show && level is not null)
        {
            bar.Width = level.Bounds.Width;
            Canvas.SetLeft(bar, 0);
            Canvas.SetTop(bar, top - BarHeight);
            if (!barShown)
            {
                layer.Children.Add(bar);
                barShown = true;
            }

            Words();
        }
        else if (barShown)
        {
            // Taken from whatever holds it, so a bar is never left behind where the layer has changed (entry 350).
            (bar.GetVisualParent() as Panel ?? layer).Children.Remove(bar);
            barShown = false;
        }

        watch.IsEnabled = barShown;
    }

    private Control? Focused => TopLevel.GetTopLevel(shell)?.FocusManager?.GetFocusedElement() as Control;

    /// <summary>The field after the focused one, where it is a field on the same screen.</summary>
    private static TextBox? Following(Control from)
    {
        var holder = from.GetVisualAncestors().OfType<Control>().FirstOrDefault(v => v is ScrollViewer || (v is Border border && border.Classes.Contains(PhoneStyles.Card)));
        if (holder is null)
        {
            return null;
        }

        // The fields on the same sheet or page in reading order, row by row and left to right in a row; the one after this one. Two
        // fields side by side are one row even where a longer label above one pushes its box a little lower.
        var placed = holder.GetVisualDescendants().OfType<TextBox>()
            .Where(b => b.IsEffectivelyVisible && b.IsEffectivelyEnabled && b.TranslatePoint(default, holder) is not null)
            .Select(b => (Box: b, At: b.TranslatePoint(default, holder)!.Value))
            .OrderBy(b => b.At.Y).ToList();
        var rows = new List<List<(TextBox Box, Point At)>>();
        foreach (var each in placed)
        {
            if (rows.Count > 0 && each.At.Y - rows[^1][0].At.Y < rows[^1][0].Box.Bounds.Height)
            {
                rows[^1].Add(each);
            }
            else
            {
                rows.Add([each]);
            }
        }

        var fields = rows.SelectMany(r => r.OrderBy(b => b.At.X)).Select(b => b.Box).ToList();
        int at = fields.IndexOf(from as TextBox ?? from.GetVisualAncestors().OfType<TextBox>().FirstOrDefault()!);
        return at >= 0 && at + 1 < fields.Count ? fields[at + 1] : null;
    }

    private void Words() => next.Content = Focused is { } field && Following(field) is not null ? "Next" : "Done";

    /// <summary>Next to the following field, or Done: the field keeps what was typed and the keyboard closes.</summary>
    internal void Advance()
    {
        if (Focused is { } field && Following(field) is { } box)
        {
            box.Focus(NavigationMethod.Tab);
            return;
        }

        CloseKeyboard();
    }

    /// <summary>Takes the focus off the field, which on iOS and Android puts the keyboard away.</summary>
    internal void CloseKeyboard()
    {
        TopLevel.GetTopLevel(shell)?.FocusManager?.Focus(null);
        HideSystemKeyboard?.Invoke();
        Closed();
    }

    private void OutsidePressed(object? sender, PointerPressedEventArgs e)
    {
        // Entry 350: a field with the focus is enough; the keyboard may be up without the system having said so.
        if ((KeyboardTop is null && !barShown && !Typing) || e.Source is not Visual source)
        {
            return;
        }

        if (source is TextBox || source.GetVisualAncestors().Any(v => v is TextBox) || source == bar || source.GetVisualAncestors().Contains(bar))
        {
            return;
        }

        CloseKeyboard();
    }

    /// <summary>
    /// The focused field in view, and on the last field of a sheet or page the button that confirms it with it: the first primary button
    /// after it, so "Continue" or "Keep this distance" is never left under the keyboard.
    /// </summary>
    internal void Follow()
    {
        // Entry 350: the keyboard already up when a field takes the focus, and no event to say so; the pane's own state raises the bar.
        if (KeyboardTop is null && Typing && pane is { State: InputPaneState.Open, OccludedRect: { Height: > 0 } shown })
        {
            Opened(shown.Y);
            return;
        }

        if (KeyboardTop is null || Focused is not { } field || field is not TextBox && !field.GetVisualAncestors().Any(v => v is TextBox))
        {
            return;
        }

        if (field is not TextBox)
        {
            field = field.GetVisualAncestors().OfType<TextBox>().First();
        }

        Words();
        var area = new Rect(field.Bounds.Size);
        // With another field after this one, Next is the way on; the confirming button joins the field on the last one, where it is Done.
        if (Following(field) is null && Confirm(field) is { } button && button.TranslatePoint(default, field) is { } at)
        {
            area = area.Union(new Rect(at, button.Bounds.Size));
        }

        field.BringIntoView(area);
    }

    /// <summary>The first primary button that comes after the field on its sheet or page.</summary>
    internal static Button? Confirm(Control field)
    {
        var holder = field.GetVisualAncestors().OfType<Control>().FirstOrDefault(v => v is ScrollViewer || (v is Border border && border.Classes.Contains(PhoneStyles.Card)));
        if (holder is null || field.TranslatePoint(default, holder) is not { } fieldAt)
        {
            return null;
        }

        return holder.GetVisualDescendants().OfType<Button>()
            .Where(b => b.IsEffectivelyVisible && b.Classes.Contains(PhoneStyles.Primary))
            .Select(b => (Button: b, At: b.TranslatePoint(default, holder)))
            .Where(b => b.At is { } p && p.Y > fieldAt.Y)
            .OrderBy(b => b.At!.Value.Y)
            .Select(b => b.Button)
            .FirstOrDefault();
    }
}
