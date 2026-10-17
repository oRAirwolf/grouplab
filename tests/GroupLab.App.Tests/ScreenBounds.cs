using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 437 section 2.1: beside each published screen, where every named control is on it, in the picture's own
/// pixels, so a marking in the user guide names a control and never a position, and the site build fails when the control has gone.
/// A control's name is its <c>Name</c>, else its accessible name, else the words on it (a button, tab, check box or menu entry). The first
/// visible control with a name keeps it; a control scrolled or clipped out of sight is not on the picture and is left out.
/// </summary>
internal static partial class ScreenBounds
{
    public static string Json(Window window, int pixelWidth, int pixelHeight)
    {
        ArgumentNullException.ThrowIfNull(window);
        double scale = pixelWidth / window.Bounds.Width;
        var picture = new Rect(0, 0, window.Bounds.Width, window.Bounds.Height);
        var controls = new SortedDictionary<string, int[]>(StringComparer.Ordinal);
        foreach (var control in window.GetVisualDescendants().OfType<Control>())
        {
            // A part of a control's template (its border, its presenter) is not something a reader looks for; the control itself is.
            if (control.TemplatedParent is not null || !control.IsEffectivelyVisible || control.Opacity == 0 || Key(control) is not { } key
                || controls.ContainsKey(key))
            {
                continue;
            }

            var seen = Seen(control, window, picture);
            if (seen is not { Width: >= 2, Height: >= 2 } r)
            {
                continue;
            }

            controls[key] = [(int)Math.Round(r.X * scale), (int)Math.Round(r.Y * scale), (int)Math.Round(r.Width * scale), (int)Math.Round(r.Height * scale)];
        }

        var file = new { about = "Entry 437: each named control on this screen as [x, y, width, height] in the picture's pixels; written by Entry109Tests.", width = pixelWidth, height = pixelHeight, controls };
        return JsonSerializer.Serialize(file, Options) + "\n";
    }

    /// <summary>The part of the control on the picture: its box in the window, cut by every ancestor that clips.</summary>
    private static Rect? Seen(Control control, Window window, Rect picture)
    {
        if (control.TranslatePoint(default, window) is not { } origin)
        {
            return null;
        }

        var r = new Rect(origin, control.Bounds.Size).Intersect(picture);
        foreach (var ancestor in control.GetVisualAncestors().OfType<Visual>())
        {
            if (ancestor == window)
            {
                break;
            }

            if (ancestor.ClipToBounds && ancestor.TranslatePoint(default, window) is { } at)
            {
                r = r.Intersect(new Rect(at, ancestor.Bounds.Size));
            }
        }

        return r.Width > 0 && r.Height > 0 ? r : null;
    }

    private static string? Key(Control control)
    {
        if (!string.IsNullOrWhiteSpace(control.Name))
        {
            return control.Name;
        }

        if (AutomationProperties.GetName(control) is { Length: > 0 } spoken)
        {
            return Words(spoken);
        }

        object? content = control switch
        {
            TabItem tab => tab.Header,
            MenuItem item => item.Header,
            Button or ToggleButton => ((ContentControl)control).Content,
            _ => null,
        };
        string? text = content switch
        {
            string s => s,
            TextBlock block => block.Text,
            Control inner => inner.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t)),
            _ => null,
        };
        return string.IsNullOrWhiteSpace(text) ? null : Words(text);
    }

    private static string Words(string text) => Space().Replace(text, " ").Trim();

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    [GeneratedRegex(@"\s+")]
    private static partial Regex Space();
}
