using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.Core.Marking;
using GroupLab.Core.ScaleMarkers;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 465, Alan's choices for the phone. 8A: Scale markers has no dotted lines, and each marker's name is a heading
/// with "What is this?" beside it that opens the glossary's entry; the library's rows and the optic card's paragraph are plain too. 9A: at
/// Android's largest text "Made for your optic" puts one field to a row, its label above its box; at the ordinary size two to a row.
/// </summary>
public class Entry465PhoneWordsTests
{
    private static Window Targets(double width, bool largest)
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
        var shell = new Shell();
        var window = new Window { Width = width, Height = 915, Content = shell };
        if (largest)
        {
            AndroidText.LargestUntilClosed(window);
        }

        window.Show();
        shell.Show(Shell.Place.Targets);
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static bool Dotted(TextBlock text) => text.TextDecorations is { Count: > 0 } lines && lines[0].StrokeDashArray is { Count: > 0 };

    private static Border CardHeaded(Window window, string heading) => window.GetVisualDescendants().OfType<TextBlock>()
        .First(t => t.Text == heading).FindAncestorOfType<Border>()!;

    [AvaloniaFact]
    public void ScaleMarkersIsPlainWithWhatIsThisByEachMarker()
    {
        var window = Targets(412, largest: false);
        try
        {
            var card = CardHeaded(window, ScaleMarkerWords.Heading);
            Assert.Empty(card.GetVisualDescendants().OfType<TextBlock>().Where(Dotted).Select(t => t.Text));
            foreach (string name in new[] { ScaleMarkerWords.Brackets, ScaleMarkerWords.Bars, ScaleMarkerWords.Stickers, ScaleMarkerWords.Labels, ScaleMarkerWords.Card })
            {
                var term = Glossary.Find(name);
                Assert.True(term is not null, $"\"{name}\" has no glossary entry");
                var row = card.GetVisualDescendants().OfType<FigureRow>().Single(r => r.Children[0] is TextBlock t && t.Text == name);
                var button = Assert.IsType<Button>(row.Children[1]);
                Assert.Equal(PhoneTerms.WhatIsThis, ((TextBlock)button.Content!).Text);
                Assert.Equal("what-is-" + term!.Term, AutomationProperties.GetAutomationId(button));
                Assert.Contains(name, AutomationProperties.GetName(button), StringComparison.Ordinal);
                var entry = Assert.IsType<StackPanel>(Assert.IsType<Flyout>(button.Flyout).Content);
                Assert.Contains(entry.Children.OfType<TextBlock>(), t => t.Text == term.Plain);
                Assert.Equal(FontWeight.SemiBold, ((TextBlock)row.Children[0]).FontWeight);
            }

            // The rest of Targets: the library's rows name paper and bulls and are plain; the one dotted line left is the dot size's label,
            // which names MOA beside the box it is for.
            var dotted = window.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible && Dotted(t)).Select(t => t.Text).ToList();
            Assert.Equal(["Red dot size in MOA, at 1x only"], dotted);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(360)]
    [InlineData(412)]
    [InlineData(600)]
    public void AtTheLargestTextTheOpticFieldsGoOneToARow(int width)
    {
        var window = Targets(width, largest: true);
        try
        {
            var pairs = CardHeaded(window, "Made for your optic").GetVisualDescendants().OfType<FieldPair>().ToList();
            Assert.Equal(4, pairs.Count);
            if (width < 600)
            {
                Assert.All(pairs.Take(2), p => Assert.True(p.Stacked, "two fields side by side at the largest text on a phone"));
            }

            foreach (var pair in pairs.Take(2))
            {
                // Each label above its box; across the card's width, or on one line where two fit (the Fold's open screen).
                foreach (var field in pair.Children.OfType<StackPanel>())
                {
                    var label = field.Children.OfType<TextBlock>().Single();
                    var box = field.Children.OfType<TextBox>().Single();
                    Assert.True(label.Bounds.Bottom <= box.Bounds.Top + 0.5, $"\"{label.Text}\" is not above its box");
                    if (pair.Stacked)
                    {
                        Assert.Equal(pair.Bounds.Width, field.Bounds.Width, 1);
                    }
                    else
                    {
                        Assert.True(label.Bounds.Height < 2 * label.FontSize, $"\"{label.Text}\" wraps beside another field at the largest text");
                    }
                }
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AtTheOrdinarySizeTheOpticFieldsStayTwoToARow()
    {
        var window = Targets(412, largest: false);
        try
        {
            var pairs = CardHeaded(window, "Made for your optic").GetVisualDescendants().OfType<FieldPair>().ToList();
            Assert.Equal(4, pairs.Count);
            Assert.All(pairs, p => Assert.False(p.Stacked, "the fields went one to a row at the ordinary text size"));
        }
        finally
        {
            window.Close();
        }
    }
}
