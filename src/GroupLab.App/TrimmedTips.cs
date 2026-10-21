using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 462 section 3: a line of words cut short with an ellipsis (a long file name in a tab, the path at the top, a
/// session's name in its list) keeps its whole text as its tooltip, so nothing a narrow window trims is lost. Only a line that is trimmed
/// gets one, and only a tooltip this put there is taken away again.
/// </summary>
internal static class TrimmedTips
{
    private static bool installed;

    public static void Install()
    {
        if (installed)
        {
            return;
        }

        installed = true;
        TextBlock.BoundsProperty.Changed.AddClassHandler<TextBlock>((text, _) => Update(text));
        TextBlock.TextProperty.Changed.AddClassHandler<TextBlock>((text, _) => Update(text));
    }

    private static readonly Avalonia.AttachedProperty<bool> Ours = Avalonia.AvaloniaProperty.RegisterAttached<TextBlock, TextBlock, bool>("TrimmedTipIsOurs");

    private static void Update(TextBlock text)
    {
        if (text.TextTrimming == TextTrimming.None)
        {
            return;
        }

        bool trimmed = text.Bounds.Width > 0 && !string.IsNullOrEmpty(text.Text) && text.TextLayout.TextLines.Any(l => l.HasCollapsed);
        if (trimmed && (ToolTip.GetTip(text) is null || text.GetValue(Ours)))
        {
            ToolTip.SetTip(text, text.Text);
            text.SetValue(Ours, true);
        }
        else if (!trimmed && text.GetValue(Ours))
        {
            ToolTip.SetTip(text, null);
            text.SetValue(Ours, false);
        }
    }
}
