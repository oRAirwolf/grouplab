using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App.Theme;
using GroupLab.Core.Records;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 351, pairing A on the phone: a card with the proposal and its reasons ending "Tap any mark to change it.", a
/// row per reading in the order fired with one mark saying what it goes with, a pause as a labelled divider, and a sheet from the bottom with
/// the choices the computer's rows offer. A shot another reading has swaps the two and says so; nothing else changes. "Keep this pairing" and
/// "Leave unpaired" at the bottom. A string of 93 readings scrolls at 320 wide with large text in both themes.
/// </summary>
public class Entry351Tests
{
    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
    }

    private static string? IdOf(Control control) => AutomationProperties.GetAutomationId(control);

    private static void Press(Control within, string id)
    {
        within.GetLogicalDescendants().OfType<Button>().First(b => IdOf(b) == id).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle();
    }

    private static IEnumerable<string> Words(Control view) => view.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");

    /// <summary>The drawing's sample: four readings of another group, a six minute pause, then ten, the first marked clean bore and the fifth left out in ShotView.</summary>
    internal static (List<ChronographShot> Readings, int[] Shots) Sample()
    {
        double[] fps = [1046, 1052, 1049, 1041, 1058, 1047, 1050, 1044, 1013, 1051, 1049, 1048, 1052, 1046];
        var start = new TimeSpan(14, 2, 0);
        var readings = fps.Select((v, i) => new ChronographShot(i + 1, v, CleanBore: i == 4, LeftOutByChronograph: i == 8,
            Time: start + TimeSpan.FromSeconds(30 * i) + (i >= 4 ? TimeSpan.FromMinutes(6) : TimeSpan.Zero))).ToList();
        return (readings, [.. Enumerable.Range(101, 10)]);
    }

    private static ChronographMarks Marks(IReadOnlyList<ChronographShot> readings, IReadOnlyList<int> shots) =>
        new(shots, [.. readings.Select(r => r.Fps)], readings, id => (id - 100).ToString(System.Globalization.CultureInfo.InvariantCulture));

    [AvaloniaFact]
    public void ARowPerReadingAndASheetThatSwapsAndSaysSo()
    {
        var (readings, shots) = Sample();
        var marks = Marks(readings, shots);
        IReadOnlyList<ChronographPair>? kept = null;
        bool keptCalled = false;
        var view = new PairingView(marks, p => (kept, keptCalled) = (p, true), () => { });
        var window = new Window { Width = 390, Height = 844, Content = view };
        window.Show();
        Settle();
        try
        {
            // The card: the proposal, its reasons as entry 342 writes them, and how to change a mark.
            Assert.Contains("Proposed: readings 5 to 14 are this group", Words(view));
            Assert.Contains(Words(view), w => w.Contains("only shots 5 to 14", StringComparison.Ordinal) && w.EndsWith("Tap any mark to change it.", StringComparison.Ordinal));

            // A row per reading, its mark in teal, amber or plain, and the pause between them.
            string[] expected = ["Not this group", "Not this group", "Not this group", "Not this group", "Shot 1, clean bore", "Shot 2", "Shot 3", "Shot 4",
                "Shot 5, left out in ShotView", "Shot 6", "Shot 7", "Shot 8", "Shot 9", "Shot 10"];
            for (int i = 0; i < expected.Length; i++)
            {
                var mark = view.GetLogicalDescendants().OfType<Button>().Single(b => IdOf(b) == $"chrono-mark-{i + 1}");
                Assert.Equal(expected[i], ((TextBlock)mark.Content!).Text);
                bool teal = mark.Classes.Contains(AppStyles.Good), amber = mark.Classes.Contains(AppStyles.Warn);
                Assert.True(i < 4 ? !teal && !amber : i is 4 or 8 ? amber : teal, $"reading {i + 1} is drawn wrong");
            }

            var pause = view.GetLogicalDescendants().OfType<Border>().Single(b => IdOf(b) == "chrono-pause-4");
            Assert.Equal("6 minute pause", ((TextBlock)pause.Child!).Text);

            // The sheet: the reading, its speed and what it goes with, then the four choices in the computer's words.
            Press(view, "chrono-mark-4");
            Assert.Equal(3, view.Changing);
            var sheet = view.GetLogicalDescendants().OfType<Border>().Single(b => IdOf(b) == "chrono-sheet");
            Assert.True(sheet.IsVisible);
            Assert.Contains("Reading 4, 1041 fps, goes with", Words(sheet));
            foreach (var (kind, words) in ChronographMarks.Choices)
            {
                var choice = sheet.GetLogicalDescendants().OfType<Button>().Single(b => IdOf(b) == "chrono-choice-" + kind.ToString().ToLowerInvariant());
                Assert.StartsWith(words, choice.GetLogicalDescendants().OfType<TextBlock>().Last().Text, StringComparison.Ordinal);
            }

            // A shot of this group, then shot 2, which reading 6 had: the two swap, and it says so.
            Press(view, "chrono-choice-shot");
            Press(view, "chrono-shot-2");
            Assert.Null(view.Changing);
            Assert.Equal(new ReadingMark(ReadingGoesWith.Shot, 102), marks[3]);
            Assert.Equal(new ReadingMark(ReadingGoesWith.NotThisGroup), marks[5]);
            Assert.Contains("swapped", view.Said, StringComparison.Ordinal);

            // Leave it out on reading 7: shot 3 has no reading, said; no other mark moves.
            var before = Enumerable.Range(0, marks.Count).Select(i => marks[i]).ToList();
            Press(view, "chrono-mark-7");
            Press(view, "chrono-choice-leftout");
            Assert.Equal(new ReadingMark(ReadingGoesWith.LeftOut), marks[6]);
            Assert.Contains("shot 3 has no reading", view.Said, StringComparison.Ordinal);
            for (int i = 0; i < marks.Count; i++)
            {
                if (i != 6)
                {
                    Assert.Equal(before[i], marks[i]);
                }
            }

            // Cancel closes the sheet and changes nothing.
            Press(view, "chrono-mark-1");
            Press(view, "chrono-choice-cancel");
            Assert.Null(view.Changing);
            Assert.Equal(before[0], marks[0]);

            Press(view, "chrono-keep-paired");
            Assert.True(keptCalled);
            Assert.Null(kept!.Single(p => p.ShotId == 103).Reading);
            Assert.Equal(3, kept!.Single(p => p.ShotId == 102).Reading);
            Press(view, "chrono-keep-unpaired");
            Assert.Null(kept);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Alan's run to 93 shots: the list scrolls and the card stays whole, at 320 wide with large text, in both themes.</summary>
    [AvaloniaFact]
    public void ANinetyThreeShotStringScrollsAtTheNarrowestPhoneWithLargeText()
    {
        var start = new TimeSpan(9, 0, 0);
        var readings = Enumerable.Range(0, 93).Select(i => new ChronographShot(i + 1, 2700 + ((i * 7) % 23),
            Time: start + TimeSpan.FromSeconds(35 * i) + (i >= 40 ? TimeSpan.FromMinutes(9) : TimeSpan.Zero))).ToList();
        var shots = Enumerable.Range(1, 93).ToArray();
        foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            var marks = new ChronographMarks(shots, [.. readings.Select(r => r.Fps)], readings, id => id.ToString(System.Globalization.CultureInfo.InvariantCulture));
            var view = new PairingView(marks, _ => { }, () => { });
            var window = new Window { Width = 320, Height = 640, FontSize = 22, RequestedThemeVariant = theme, Content = view };
            window.Show();
            Settle();
            try
            {
                Assert.Equal(93, view.GetLogicalDescendants().OfType<Button>().Count(b => IdOf(b)?.StartsWith("chrono-mark-", StringComparison.Ordinal) == true));
                var scroll = view.GetVisualDescendants().OfType<ScrollViewer>().First();
                Assert.True(scroll.Extent.Height > scroll.Viewport.Height * 4, "the 93 readings do not scroll");
                foreach (var control in view.GetVisualDescendants().OfType<Control>().Where(c => c is Button && c.IsEffectivelyVisible))
                {
                    string named = AutomationProperties.GetName(control) is { Length: > 0 } n ? n
                        : control is ContentControl { Content: TextBlock { Text: { } t } } ? t : control is ContentControl { Content: string s } ? s : "";
                    Assert.False(string.IsNullOrWhiteSpace(named), $"{theme}: a button has no name");
                    var right = control.TranslatePoint(new Point(control.Bounds.Width, 0), window);
                    Assert.True(right is { } p && p.X <= 320.5, $"{theme}: {named} runs past the right edge to {right?.X:0}");
                }

                var card = view.GetLogicalDescendants().OfType<Border>().Single(b => IdOf(b) == "chrono-proposal");
                foreach (var text in card.GetVisualDescendants().OfType<TextBlock>().Where(t => !string.IsNullOrEmpty(t.Text)))
                {
                    string words = text.Text!;
                    foreach (var line in text.TextLayout.TextLines.Skip(1))
                    {
                        int at = line.FirstTextSourceIndex;
                        Assert.True(at > 0 && at <= words.Length && (char.IsWhiteSpace(words[at - 1]) || words[at - 1] is '-' or '/' or ','), $"{theme}: \"{words}\" is broken inside a word");
                    }
                }

                // The sheet for the last reading still fits, with all 93 shots a scroll inside it.
                Press(view, "chrono-mark-93");
                Press(view, "chrono-choice-shot");
                var sheet = view.GetLogicalDescendants().OfType<Border>().Single(b => IdOf(b) == "chrono-sheet");
                Assert.True(sheet.Bounds.Height <= 640.5, $"the sheet is {sheet.Bounds.Height:0} tall in a 640 window");
                Assert.Equal(93, sheet.GetLogicalDescendants().OfType<Button>().Count(b => IdOf(b)?.StartsWith("chrono-shot-", StringComparison.Ordinal) == true));
            }
            finally
            {
                window.Close();
            }
        }
    }

    /// <summary>The phone's page opens the rows from a list read, and the bottom's words are entry 351's.</summary>
    [AvaloniaFact]
    public void ReadingTheListOpensTheRows()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        var page = VelocityPages.Chronograph(1, GroupLab.Core.Marking.MarkingState.Empty, () => { });
        var window = new Window { Width = 390, Height = 844, Content = page };
        window.Show();
        Settle();
        try
        {
            page.GetLogicalDescendants().OfType<TextBox>().Single(b => IdOf(b) == "chrono-readings").Text = "2705, 2711, 2698";
            Press(page, "chrono-read");
            var view = page.GetLogicalDescendants().OfType<PairingView>().Single();
            Assert.Equal(3, view.Marks.Count);
            Assert.Contains(view.GetLogicalDescendants().OfType<Button>(), b => IdOf(b) == "chrono-keep-paired" && b.Content is TextBlock { Text: ChronographMarks.KeepWords });
            Assert.Contains(view.GetLogicalDescendants().OfType<Button>(), b => IdOf(b) == "chrono-keep-unpaired" && b.Content is TextBlock { Text: ChronographMarks.UnpairedWords });
            Press(page, "chrono-pairing-back");
            Assert.Empty(page.GetLogicalDescendants().OfType<PairingView>());
        }
        finally
        {
            window.Close();
        }
    }
}
