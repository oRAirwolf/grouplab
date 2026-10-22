using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace GroupLab.App;

/// <summary>
/// Entry 422 (Jylee): a click that moves a scrolling column lands on a section with its heading and the chosen row fully in view, after
/// the layout has settled, and the section flashes twice so the eye follows it; with the system's animations off it holds a steady tint.
/// Shared by the desktop's side panel and the phone's result screen.
/// </summary>
internal sealed class Landing
{
    /// <summary>The space left above a heading landed on, so it never touches the top edge.</summary>
    internal const double Margin = 8;

    /// <summary>One flash, on and then off: about a third of a second lit, so two of them take about a second.</summary>
    internal static readonly TimeSpan FlashOn = TimeSpan.FromMilliseconds(330);

    internal static readonly TimeSpan FlashOff = TimeSpan.FromMilliseconds(170);

    /// <summary>The steady tint shown instead of flashing when the system asks for less motion.</summary>
    internal static readonly TimeSpan Steady = TimeSpan.FromMilliseconds(1500);

    /// <summary>Whether the system asks for less motion; set by each platform, and by the tests.</summary>
    internal static Func<bool> ReducedMotion { get; set; } = () => false;

    /// <summary>Entry 468 section 2.6: Settings' Reduce motion, set by each platform from its settings; following the system until chosen.</summary>
    internal static Func<MotionChoice> MotionChosen { get; set; } = () => MotionChoice.System;

    /// <summary>Whether every flash and animation is to be a still tint: as chosen in Settings, else as the system asks.</summary>
    internal static bool Still() => MotionChosen() switch
    {
        MotionChoice.On => true,
        MotionChoice.Off => false,
        _ => ReducedMotion(),
    };

    private readonly ScrollViewer scroll;
    private readonly Control content;
    private readonly Border flash;
    private DispatcherTimer? timer;

    /// <summary>
    /// The column to land in: its scroll viewer, the control it scrolls (its own content, so a margin inside does not move the sums), and
    /// the strip that flashes, which sits behind the column's content
    /// in the same panel so the section's words stay on top of it.
    /// </summary>
    internal Landing(ScrollViewer scroll, Control content, Border flash)
    {
        this.scroll = scroll;
        this.content = content;
        this.flash = flash;
        flash.IsHitTestVisible = false;
        flash.Opacity = 0;
        flash.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
    }

    /// <summary>What the last landing did, for the tests: whether the column moved, and whether the strip flashed or held steady.</summary>
    internal (bool Moved, bool Flashed, bool Steady) Last { get; private set; }

    /// <summary>Raised when a landing has settled, for the tests.</summary>
    internal event Action? Landed;

    /// <summary>
    /// Lands on a section: from the top of <paramref name="heading"/> to the bottom of <paramref name="last"/>, with the row from the top
    /// of <paramref name="row"/> to the bottom of <paramref name="rowEnd"/> the part that must be seen. Worked out once the layout has
    /// settled, and checked once more when the work the click queued is done, because a row that opens in place, or a line above that
    /// appears with it, changes the column's height after the click.
    /// </summary>
    internal void On(Control heading, Control last, Control? row = null, Control? rowEnd = null)
    {
        Dispatcher.UIThread.Post(() =>
        {
            bool moved = Place(heading, last, row, rowEnd);
            Dispatcher.UIThread.Post(() =>
            {
                moved |= Place(heading, last, row, rowEnd);
                if (moved)
                {
                    Flash(heading, last);
                }
                else
                {
                    Last = (false, false, false);
                }

                Landed?.Invoke();
            }, DispatcherPriority.Background);
        }, DispatcherPriority.Loaded);
    }

    /// <summary>Where the column should be scrolled to land on a section, or null where it is in view already.</summary>
    internal static double? Target(double offset, double viewport, double extent, (double Top, double Bottom) section, (double Top, double Bottom) row)
    {
        double top = section.Top - Margin;
        bool headingSeen = top >= offset && section.Top + 1 <= offset + viewport;
        bool rowSeen = row.Top >= offset && row.Bottom <= offset + viewport;
        if (headingSeen && rowSeen && (section.Bottom <= offset + viewport || row.Bottom - top > viewport))
        {
            return null;
        }

        // The heading a small margin below the top, unless the section is taller than the column: then the row wins.
        double wanted = row.Bottom - top <= viewport ? top
            : row.Bottom - row.Top + Margin <= viewport ? row.Bottom + Margin - viewport
            : row.Top - Margin;
        double clamped = Math.Clamp(wanted, 0, Math.Max(0, extent - viewport));
        return Math.Abs(clamped - offset) < 0.5 ? null : clamped;
    }

    private bool Place(Control heading, Control last, Control? row, Control? rowEnd)
    {
        if (heading.TranslatePoint(default, content) is not { } headingAt || last.TranslatePoint(new Point(0, last.Bounds.Height), content) is not { } lastAt)
        {
            return false;
        }

        var chosen = row ?? heading;
        var end = rowEnd ?? chosen;
        if (chosen.TranslatePoint(default, content) is not { } rowAt || end.TranslatePoint(new Point(0, end.Bounds.Height), content) is not { } endAt)
        {
            return false;
        }

        double? to = Target(scroll.Offset.Y, scroll.Viewport.Height, scroll.Extent.Height, (headingAt.Y, lastAt.Y), (rowAt.Y, endAt.Y));
        if (to is not { } y)
        {
            return false;
        }

        scroll.Offset = new Vector(scroll.Offset.X, y);
        scroll.UpdateLayout();
        return true;
    }

    private void Flash(Control heading, Control last)
    {
        if (heading.TranslatePoint(default, flash.Parent as Visual ?? content) is not { } top
            || last.TranslatePoint(new Point(0, last.Bounds.Height), flash.Parent as Visual ?? content) is not { } bottom)
        {
            return;
        }

        flash.Margin = new Thickness(0, Math.Max(0, top.Y - (Margin / 2)), 0, 0);
        flash.Height = Math.Max(1, bottom.Y - top.Y + Margin);
        timer?.Stop();
        bool steady = Still();
        Last = (true, !steady, steady);
        var steps = steady ? new[] { (1.0, Steady), (0.0, TimeSpan.Zero) }
            : new[] { (1.0, FlashOn), (0.0, FlashOff), (1.0, FlashOn), (0.0, TimeSpan.Zero) };
        int at = 0;
        flash.Opacity = steps[0].Item1;
        timer = new DispatcherTimer { Interval = steps[0].Item2 };
        timer.Tick += (_, _) =>
        {
            at++;
            flash.Opacity = steps[at].Item1;
            if (steps[at].Item2 == TimeSpan.Zero)
            {
                timer!.Stop();
                return;
            }

            timer!.Interval = steps[at].Item2;
        };
        timer.Start();
    }

    /// <summary>The strip that flashes: the accent's tint, with its edge, behind a section.</summary>
    internal static Border Strip(Color tint, Color edge) => new()
    {
        Background = new SolidColorBrush(tint),
        BorderBrush = new SolidColorBrush(edge),
        BorderThickness = new Thickness(0, 1),
    };
}
