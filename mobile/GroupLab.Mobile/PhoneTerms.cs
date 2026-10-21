using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile;

/// <summary>
/// Words explained where they appear, on the phone, NOTES-FROM-PLANNING.md entries 154 and 258: a line of the phone's secondary text that
/// names a glossary word is underlined with dots and explains the word when tapped, as the desktop's does, in the glossary's own plain and
/// precise words. A line that shows a unit is a number to tap for another unit (entry 273), and is left to that. Entry 465, 8A: where a page
/// names one thing after another, as Scale markers does, its lines stay plain and each thing's name is a heading with "What is this?" beside
/// it (<see cref="Named"/>), because a page of dotted lines read as a page of links.
/// </summary>
internal static class PhoneTerms
{
    /// <summary>What the button beside a named heading says (entry 465, 8A).</summary>
    public const string WhatIsThis = "What is this?";

    /// <summary>The line, explained where it names a glossary word.</summary>
    public static TextBlock Explain(TextBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        if (UnitTap.KindOf(block.Text) is not null || Glossary.Find(block.Text ?? "") is not { } term)
        {
            return block;
        }

        block.TextDecorations = [new TextDecoration { Location = TextDecorationLocation.Underline, StrokeDashArray = new AvaloniaList<double> { 1, 2 }, StrokeThickness = 1 }];
        FlyoutBase.SetAttachedFlyout(block, Entry(term));
        block.Tapped += (_, e) =>
        {
            FlyoutBase.ShowAttachedFlyout(block);
            e.Handled = true;
        };
        return block;
    }

    /// <summary>
    /// Entry 465, 8A: a thing's name as a heading, with a small "What is this?" beside it that opens the glossary's entry for it; beside the
    /// name where both fit on one line, under it where they do not (at the largest text sizes). A name the glossary does not know is the
    /// heading alone.
    /// </summary>
    public static Control Named(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var heading = new TextBlock { Text = name, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center };
        if (Glossary.Find(name) is not { } term)
        {
            return heading;
        }

        var button = new Button
        {
            Content = new TextBlock { Text = WhatIsThis, TextWrapping = TextWrapping.Wrap },
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Flyout = Entry(term),
        }.Id("what-is-" + term.Term);
        Avalonia.Automation.AutomationProperties.SetName(button, WhatIsThis + " " + name);
        return new FigureRow { Children = { heading, button } };
    }

    // The glossary's entry: its name, its plain words and, where it has them, its precise ones.
    private static Flyout Entry(GlossaryTerm term)
    {
        var inside = new StackPanel { Spacing = 8, MaxWidth = 320 };
        inside.Children.Add(new TextBlock { Text = term.Name, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold });
        inside.Children.Add(new TextBlock { Text = term.Plain, TextWrapping = TextWrapping.Wrap });
        if (term.Precise is { } precise)
        {
            inside.Children.Add(new TextBlock { Text = "Precisely: " + precise, TextWrapping = TextWrapping.Wrap, Classes = { PhoneStyles.Dim } });
        }

        return new Flyout { Content = inside, Placement = PlacementMode.Bottom };
    }
}
