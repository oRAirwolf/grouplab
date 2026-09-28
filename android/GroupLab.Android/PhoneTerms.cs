using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.Android;

/// <summary>
/// Words explained where they appear, on the phone, NOTES-FROM-PLANNING.md entries 154 and 258: a line of the phone's secondary text that
/// names a glossary word is underlined with dots and explains the word when tapped, as the desktop's does, in the glossary's own plain and
/// precise words. A line that shows a unit is a number to tap for another unit (entry 273), and is left to that.
/// </summary>
internal static class PhoneTerms
{
    /// <summary>The line, explained where it names a glossary word.</summary>
    public static TextBlock Explain(TextBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        if (UnitTap.KindOf(block.Text) is not null || Glossary.Find(block.Text ?? "") is not { } term)
        {
            return block;
        }

        block.TextDecorations = [new TextDecoration { Location = TextDecorationLocation.Underline, StrokeDashArray = new AvaloniaList<double> { 1, 2 }, StrokeThickness = 1 }];
        var inside = new StackPanel { Spacing = 8, MaxWidth = 320 };
        inside.Children.Add(new TextBlock { Text = term.Name, FontWeight = FontWeight.SemiBold });
        inside.Children.Add(new TextBlock { Text = term.Plain, TextWrapping = TextWrapping.Wrap });
        if (term.Precise is { } precise)
        {
            inside.Children.Add(new TextBlock { Text = "Precisely: " + precise, TextWrapping = TextWrapping.Wrap, Classes = { PhoneStyles.Dim } });
        }

        FlyoutBase.SetAttachedFlyout(block, new Flyout { Content = inside, Placement = PlacementMode.Bottom });
        block.Tapped += (_, e) =>
        {
            FlyoutBase.ShowAttachedFlyout(block);
            e.Handled = true;
        };
        return block;
    }
}
