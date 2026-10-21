using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using GroupLab.App.Theme;
using GroupLab.Tests.Support;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 462 section 3: every place along the bottom laid out at three phone widths (a small phone, the Fold 7's
/// cover screen and its open screen), in both themes, at the ordinary text size and at Android's largest. The iPhone's largest is held at
/// Android's for every size (<see cref="TextSize.ByFactor"/>), so one pass at the largest holds both. <see cref="LayoutAudit"/> finds words
/// cut off, a control past the edge, words over words and words below AA contrast.
/// </summary>
public class Entry462PhoneLayoutTests
{
    private static readonly (int Width, int Height)[] Sizes = [(360, 780), (412, 915), (600, 900)];

    [AvaloniaFact]
    public void EveryPlaceIsLaidOutWholeAtEveryWidthThemeAndTextSize()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);

        var all = new List<string>();
        foreach (bool largest in new[] { false, true })
        {
            foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
            {
                foreach (var (width, height) in Sizes)
                {
                    var shell = new Shell();
                    var window = new Window { Width = width, Height = height, RequestedThemeVariant = theme, Content = shell };
                    if (largest)
                    {
                        AndroidText.LargestUntilClosed(window);
                    }

                    window.Show();
                    Dispatcher.UIThread.RunJobs();
                    try
                    {
                        foreach (var place in Enum.GetValues<Shell.Place>())
                        {
                            shell.Show(place);
                            Dispatcher.UIThread.RunJobs();
                            window.UpdateLayout();
                            Dispatcher.UIThread.RunJobs();
                            // The note along the top floats over the page by design and has its own Close, which is pressed here.
                            foreach (var note in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(window).OfType<TextBlock>()
                                .Where(t => t.Text?.StartsWith("GroupLab hit an error", StringComparison.Ordinal) == true).ToList())
                            {
                                if (Avalonia.VisualTree.VisualExtensions.FindAncestorOfType<Border>(note) is { } card && card.Classes.Contains(PhoneStyles.Card))
                                {
                                    card.IsVisible = false;
                                }
                            }

                            window.UpdateLayout();
                            string where = $"{place}, {width} wide, {theme}{(largest ? ", largest text" : "")}";
                            all.AddRange(LayoutAudit.Findings(window).Select(f => where + ": " + f));
                        }
                    }
                    finally
                    {
                        window.Close();
                    }
                }
            }
        }

        // GROUPLAB_LAYOUT_AUDIT naming a file gets every finding, one a line, as the desktop's screen walk does.
        if (Environment.GetEnvironmentVariable("GROUPLAB_LAYOUT_AUDIT") is { Length: > 0 } file)
        {
            File.AppendAllLines(file, all);
        }

        Assert.True(all.Count == 0, all.Count + " findings: " + string.Join("; ", all.Take(40)));
    }
}
