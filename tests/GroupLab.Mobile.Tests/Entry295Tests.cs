using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 295, from Alan's screenshots of nightly 126 on the Fold 7: Compare drew each load's name under its range
/// and value, left an empty area under the rows, named both loads by their sheet, and said on extreme spread that there was a difference
/// over a card saying there was none; and All figures squeezed "Zero, elevation" to a letter a line. Each is held here at the narrowest
/// phone width and a large text size.
/// </summary>
public class Entry295Tests
{
    private static readonly TestPhone ThePhone = new();

    /// <summary>The narrowest phone width GroupLab is laid out for, in device-independent pixels.</summary>
    private const double Narrowest = 320;

    private static Window Started()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        var window = new Window { Width = Narrowest, Height = 900, FontSize = 28 };
        window.Show();
        return window;
    }

    private static PhoneResult Analyzed()
    {
        string copy = Path.Combine(ThePhone.CacheFolder, "chosen-295.png");
        File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), copy, overwrite: true);
        var result = PhoneAnalysis.Run(copy, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
        Assert.Null(result.Failure);
        return result;
    }

    [AvaloniaFact]
    public void CompareDrawsNothingOverAnythingAndNamesTheSessionsApart()
    {
        var window = Started();
        var result = Analyzed();
        var store = PhoneAnalysis.Store();
        var saved = store.Get(result.SessionId!.Value)!;

        // Alan's case: two sessions on one sheet on one day, with no load given.
        long first = store.Save(saved with { Id = 0, ShotDate = "2026-09-29", CreatedUtc = "2026-09-29T04:40:00Z", Load = null });
        long second = store.Save(saved with { Id = 0, ShotDate = "2026-09-29", CreatedUtc = "2026-09-29T05:01:00Z", Load = null });
        var page = new ComparePage([store.Get(first)!, store.Get(second)!], UnitSettings.Imperial, () => { });
        window.Content = page;
        Dispatcher.UIThread.RunJobs();

        var chart = page.GetVisualDescendants().OfType<IntervalChart>().Single();
        var names = chart.Rows.Select(r => r.Label).ToList();
        Assert.Equal(2, names.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(names, n => n.Contains(saved.SheetName, StringComparison.Ordinal));

        foreach (string figure in ComparePage.Figures)
        {
            var chip = page.GetVisualDescendants().OfType<Button>().First(b => Equals(b.Content, figure));
            chip.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            chart = page.GetVisualDescendants().OfType<IntervalChart>().Single();
            Assert.True(chart.Bounds.Width > 100 && chart.Bounds.Width < Narrowest, $"the chart is {chart.Bounds.Width} wide");
            var (rows, height) = chart.Places(chart.Bounds.Width);
            var boxes = rows.SelectMany(r => new[] { r.Name, r.Value, r.Bar }).ToList();
            for (int i = 0; i < boxes.Count; i++)
            {
                for (int j = i + 1; j < boxes.Count; j++)
                {
                    Assert.False(boxes[i].Intersects(boxes[j]), $"{figure}: {boxes[i]} and {boxes[j]} overlap");
                }
            }

            // Sized to its rows: no empty area beneath them.
            Assert.InRange(chart.Bounds.Height, height - 1, height + 1);

            // The sentence beneath and the verdict card never disagree.
            var words = page.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
            Assert.Contains(chart.Description, words);
            if (words.Any(w => w.Contains("not distinguishable", StringComparison.Ordinal)))
            {
                Assert.DoesNotContain(words, w => w.Contains("a difference these shots can see", StringComparison.Ordinal));
            }

            if (figure == "Extreme spread")
            {
                Assert.Contains("no range to compare", chart.Description, StringComparison.Ordinal);
                Assert.Contains("mean radius", chart.Description, StringComparison.Ordinal);
                Assert.DoesNotContain(words, w => w.Contains("with the range each could really be", StringComparison.Ordinal));
            }
        }

        // Plain words in the verdict card, and sigma kept behind the dotted underline.
        var plain = page.GetLogicalDescendants().OfType<TextBlock>().Single(t => (t.Text ?? "").StartsWith("Each load's spread could really be", StringComparison.Ordinal));
        Assert.NotNull(plain.TextDecorations);
        Assert.DoesNotContain(page.GetLogicalDescendants().OfType<TextBlock>(), t => (t.Text ?? "").StartsWith("The sigma intervals overlap", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void AValueTooLongForItsLabelGoesUnderIt()
    {
        var window = Started();
        var label = new TextBlock { Text = "Zero, elevation", TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        var value = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = "0.1 MOA high", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new TextBlock { Text = "too small to dial yet; about 44 shots would settle it", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
            },
        };
        var row = new FigureRow { Children = { label, value } };
        window.FontSize = 16;
        window.Content = new Border { Padding = new Thickness(16), Child = row };
        Dispatcher.UIThread.RunJobs();

        Assert.True(row.Stacked);
        Assert.Single(label.TextLayout.TextLines);
        Assert.True(value.Bounds.Top >= label.Bounds.Bottom);
        Assert.Equal(row.Bounds.Width, value.Bounds.Width, 1);

        // A short value stays beside its label.
        var near = new FigureRow { Children = { new TextBlock { Text = "CEP 90" }, new TextBlock { Text = "0.895 in" } } };
        window.Content = new Border { Padding = new Thickness(16), Child = near };
        Dispatcher.UIThread.RunJobs();
        Assert.False(near.Stacked);
    }

    /// <summary>Every figure on a real result at the narrowest width and a large text size: no label broken inside a word, nothing over anything.</summary>
    [AvaloniaFact]
    public void AllFiguresNeverBreakALabelInsideAWord()
    {
        var window = Started();
        var result = Analyzed();
        var view = new ResultView(result, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { });
        window.Content = view;
        Dispatcher.UIThread.RunJobs();

        var rows = view.GetVisualDescendants().OfType<FigureRow>().Where(r => r.IsEffectivelyVisible && r.Bounds.Width > 0).ToList();
        Assert.True(rows.Count >= 5, $"{rows.Count} figure rows are showing");
        Assert.Contains(rows, r => r.Stacked);
        foreach (var row in rows)
        {
            var label = (TextBlock)row.Children[0];
            string text = label.Text ?? "";
            foreach (var line in label.TextLayout.TextLines.Skip(1))
            {
                int start = line.FirstTextSourceIndex;
                Assert.True(start > 0 && start <= text.Length && char.IsWhiteSpace(text[start - 1]), $"\"{text}\" is broken inside a word");
            }

            Assert.False(label.Bounds.Intersects(row.Children[1].Bounds), $"\"{text}\" and its value overlap");
            Assert.True(row.Children[1].Bounds.Right <= row.Bounds.Width + 0.5, $"\"{text}\"'s value runs past the edge");

            // Beside its name, a value is one line: a value that wraps there should have gone under the name.
            if (!row.Stacked && row.Children[1] is Panel { Children: [TextBlock first, ..] })
            {
                Assert.Single(first.TextLayout.TextLines);
            }
        }
    }
}
