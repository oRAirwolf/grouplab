using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace GroupLab.Tests.Support;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 462 section 3: what is wrong with a screen as laid out, checked by machine, on the computer and the phone
/// alike. Words cut off (a line trimmed, or wider or taller than the box it is given), a control a person uses pushed past the edge of the
/// window where nothing scrolls to it, two lines of words drawn over each other, and words below WCAG AA contrast against what is behind
/// them (4.5 to 1, or 3 to 1 for large words: 24 px, or 18.66 px bold). A control that is switched off is left out of contrast, as WCAG
/// leaves it. Looks are not judged here: spacing, alignment and colour choices go to Alan as DESIGN NEEDED.
/// </summary>
internal static class LayoutAudit
{
    public static List<string> Findings(TopLevel top)
    {
        var findings = new List<string>();
        var size = top.ClientSize;
        var texts = new List<(TextBlock Text, Rect Box)>();
        foreach (var c in top.GetVisualDescendants().OfType<Control>())
        {
            if (!c.IsEffectivelyVisible || c.Bounds.Width <= 0 || c.Bounds.Height <= 0 || Hidden(c) || c.TransformToVisual(top) is not { } toTop)
            {
                continue;
            }

            var box = new Rect(c.Bounds.Size).TransformToAABB(toTop);
            if (Shown(c, top, box) is not { } shown)
            {
                continue;
            }

            if (IsUsed(c) && (((box.Right > size.Width + 1 || box.X < -1) && !Scrolls(c, across: true)) || ((box.Bottom > size.Height + 1 || box.Y < -1) && !Scrolls(c, across: false))))
            {
                findings.Add($"{Describe(c)}: past the edge of the window ({box.X:0},{box.Y:0} to {box.Right:0},{box.Bottom:0} in {size.Width:0} by {size.Height:0})");
            }

            if (c is not TextBlock { Text: { Length: > 0 } words } text || string.IsNullOrWhiteSpace(words))
            {
                continue;
            }

            // A tooltip or a flyout floats over what it explains by design, so it is not counted as words over words.
            if (!text.GetVisualAncestors().Any(a => a is ToolTip or FlyoutPresenter))
            {
                texts.Add((text, shown));
            }

            var layout = text.TextLayout;
            if (layout.TextLines.Any(l => l.HasCollapsed))
            {
                // Trimmed on purpose, with the whole of it a pointer away (TrimmedTips), loses nothing.
                if (ToolTip.GetTip(text) is string tip && tip == words)
                {
                    continue;
                }

                findings.Add($"\"{Short(words)}\": cut off with an ellipsis");
            }
            else if (text.TextWrapping == TextWrapping.NoWrap && layout.WidthIncludingTrailingWhitespace > text.Bounds.Width - text.Padding.Left - text.Padding.Right + 1)
            {
                findings.Add($"\"{Short(words)}\": wider than its box ({layout.WidthIncludingTrailingWhitespace:0} in {text.Bounds.Width:0})");
            }
            else if (layout.Height > text.Bounds.Height - text.Padding.Top - text.Padding.Bottom + 1)
            {
                findings.Add($"\"{Short(words)}\": taller than its box ({layout.Height:0} in {text.Bounds.Height:0})");
            }
            else if ((shown.Width < box.Width - 2 && !Scrolls(text, across: true)) || (shown.Height < box.Height - 2 && !Scrolls(text, across: false)))
            {
                // Laid out whole but drawn inside a box that cuts it, and nothing scrolls that way to the rest.
                findings.Add($"\"{Short(words)}\": partly hidden by the box around it");
            }

            if (text.IsEffectivelyEnabled && Contrast(text) is { } found)
            {
                bool large = text.FontSize >= 24 || (text.FontSize >= 18.66 && text.FontWeight >= FontWeight.Bold);
                double needed = large ? 3 : 4.5;
                if (found.Ratio < needed - 0.005)
                {
                    findings.Add($"\"{Short(words)}\": contrast {found.Ratio:0.00} to 1, under {needed:0.0} ({found.Front} on {found.Behind})");
                }
            }
        }

        for (int i = 0; i < texts.Count; i++)
        {
            for (int j = i + 1; j < texts.Count; j++)
            {
                var (a, b) = (texts[i], texts[j]);
                var meet = a.Box.Intersect(b.Box);
                if (meet.Width > 3 && meet.Height > Math.Max(3, Math.Min(a.Box.Height, b.Box.Height) / 3)
                    && !a.Text.IsVisualAncestorOf(b.Text) && !b.Text.IsVisualAncestorOf(a.Text) && !Covered(top, meet.Center, a.Text, b.Text))
                {
                    findings.Add($"\"{Short(a.Text.Text!)}\" and \"{Short(b.Text.Text!)}\": drawn over each other");
                }
            }
        }

        return findings.Distinct().ToList();
    }

    /// <summary>
    /// Whether one of two words drawn at <paramref name="at"/> is covered there by something solid drawn between them (a dialog's card over
    /// the screen), so only the upper is seen.
    /// </summary>
    private static bool Covered(TopLevel top, Point at, Visual a, Visual b)
    {
        var drawn = top.GetVisualsAt(at).ToList();
        int ia = drawn.IndexOf(a), ib = drawn.IndexOf(b);
        if (ia < 0 || ib < 0)
        {
            return ia >= 0 || ib >= 0;
        }

        var lower = ia < ib ? b : a;
        for (int k = Math.Min(ia, ib) + 1; k < Math.Max(ia, ib); k++)
        {
            var v = drawn[k];
            IBrush? back = v switch { Panel p => p.Background, Border o => o.Background, TemplatedControl tc => tc.Background, _ => null };
            if (back is ISolidColorBrush { Color.A: 255, Opacity: >= 0.99 } && !v.IsVisualAncestorOf(lower))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The part of a control's box that is drawn: its box cut by every ancestor that clips. Null where nothing of it shows.</summary>
    private static Rect? Shown(Control c, TopLevel top, Rect box)
    {
        var shown = box.Intersect(new Rect(top.ClientSize));
        for (var a = c.GetVisualParent(); a is not null && a != top; a = a.GetVisualParent())
        {
            if (a is Visual { ClipToBounds: true } clip && clip.TransformToVisual(top) is { } t)
            {
                shown = shown.Intersect(new Rect(clip.Bounds.Size).TransformToAABB(t));
            }
        }

        return shown.Width > 0.5 && shown.Height > 0.5 ? shown : null;
    }

    private static bool Hidden(Control c)
    {
        for (Visual? v = c; v is not null; v = v.GetVisualParent())
        {
            if (v.Opacity <= 0.01)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Inside something that scrolls that way, so a person can bring the rest into view; a scroller held to its width does not.</summary>
    private static bool Scrolls(Control c, bool across) => c.GetVisualAncestors().OfType<ScrollViewer>().Any(s =>
        (across ? s.HorizontalScrollBarVisibility : s.VerticalScrollBarVisibility) != ScrollBarVisibility.Disabled);

    private static bool IsUsed(Control c) =>
        c is Button or ToggleButton or TextBox or ComboBox or Slider or NumericUpDown or ListBoxItem or TabItem or MenuItem or AutoCompleteBox
        && !c.GetVisualAncestors().Any(a => a is Button or ComboBox or NumericUpDown or AutoCompleteBox && a != c);

    /// <summary>The words' colour against the first solid colour behind them, each with its opacity laid over what is behind it.</summary>
    private static (double Ratio, Color Front, Color Behind)? Contrast(TextBlock text)
    {
        if (text.Foreground is not ISolidColorBrush fore)
        {
            return null;
        }

        var layers = new List<(Color Colour, double Alpha)>();
        for (Visual? v = text; v is not null; v = v.GetVisualParent())
        {
            IBrush? back = v switch
            {
                Panel p => p.Background,
                Border b => b.Background,
                ContentPresenter cp => cp.Background,
                TopLevel tl => tl.Background,
                TemplatedControl tc => tc.Background,
                _ => null,
            };
            if (back is null)
            {
                continue;
            }

            if (back is not ISolidColorBrush solid)
            {
                return null;
            }

            double alpha = solid.Color.A / 255.0 * solid.Opacity;
            if (alpha <= 0.01)
            {
                continue;
            }

            layers.Add((solid.Color, alpha));
            if (alpha >= 0.99)
            {
                break;
            }
        }

        if (layers.Count == 0 || layers[^1].Alpha < 0.99)
        {
            return null;
        }

        var behind = layers[^1].Colour;
        for (int i = layers.Count - 2; i >= 0; i--)
        {
            behind = Over(layers[i].Colour, layers[i].Alpha, behind);
        }

        var front = Over(fore.Color, fore.Color.A / 255.0 * fore.Opacity * text.Opacity, behind);
        double l1 = Luminance(front), l2 = Luminance(behind);
        return ((Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05), front, behind);
    }

    private static Color Over(Color top, double alpha, Color under) => Color.FromRgb(
        (byte)Math.Round((top.R * alpha) + (under.R * (1 - alpha))),
        (byte)Math.Round((top.G * alpha) + (under.G * (1 - alpha))),
        (byte)Math.Round((top.B * alpha) + (under.B * (1 - alpha))));

    private static double Luminance(Color c)
    {
        static double Channel(byte v)
        {
            double s = v / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(c.R)) + (0.7152 * Channel(c.G)) + (0.0722 * Channel(c.B));
    }

    private static string Short(string words) => words.Length <= 50 ? words.Replace('\n', ' ') : words[..47].Replace('\n', ' ') + "...";

    private static string Describe(Control c) => c.GetType().Name + " \"" + Short(c switch
    {
        ContentControl { Content: string words } => words,
        TextBox box => box.PlaceholderText ?? box.Text ?? "",
        _ => Avalonia.Automation.AutomationProperties.GetName(c) ?? c.Name ?? "",
    }) + "\"";
}
