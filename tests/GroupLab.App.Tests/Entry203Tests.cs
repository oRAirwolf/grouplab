using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Publication;
using GroupLab.Core.Updates;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 203: on nightly 102 both consent choices on the first run screen ran off the card mid sentence, because a
/// radio button given a plain string shows it on one line. No text on the first run card, the question after Accept and analyze, or
/// Settings may be cut, at the default window and at narrow ones; and neither consent level is ever chosen for the person.
/// <para>
/// Avalonia lays out in device independent units, so 150 and 200 percent display scale are a narrower window in those units: 960 is a
/// 1920 pixel screen at 200 percent, and 683 a 1366 pixel laptop at 200 percent. The window sets no minimum width of its own.
/// </para>
/// </summary>
public class Entry203Tests
{
    private static readonly double[] Widths = [1400, 960, 683];

    private static RecordedOutsideWorld Outside => TestDefaults.Outside;

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static (MainWindow Window, string Path) Ready(double width, SendingChoice choice, ConsentLevel? level)
    {
        Outside.Forget();
        var (window, path, _) = Entry109Tests.Sheet((int)width, 900);
        window.ReceiverOpen = true;
        window.ErrorsOpen = true;
        window.SettingsStore.SaveSending(choice, level);
        window.Session.SetCalibre(Calibre.Of(0.308));
        window.Session.SetShotDistance(3600);
        window.CalibreAnswered();
        window.FillSendingSettings();
        window.FillErrorSettings();
        Settle();
        return (window, path);
    }

    private static void Finish(MainWindow window, string path)
    {
        window.Close();
        Outside.Forget();
        GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
    }

    /// <summary>
    /// Every visible piece of text under <paramref name="root"/> that does not fit: a line wider than the space it was given, or a text
    /// block reaching past the edge of anything that clips it, the window included. The text blocks inside a radio's or a check box's
    /// template are included, which is where a plain string's words are drawn.
    /// </summary>
    internal static List<string> Cut(Control root)
    {
        var cut = new List<string>();
        foreach (var block in root.GetVisualDescendants().OfType<TextBlock>().Where(b => b.IsEffectivelyVisible && !string.IsNullOrEmpty(b.Text)))
        {
            if (block.TextWrapping == TextWrapping.NoWrap)
            {
                var natural = new TextBlock { Text = block.Text, FontSize = block.FontSize, FontFamily = block.FontFamily, FontWeight = block.FontWeight, FontStyle = block.FontStyle };
                natural.Measure(Size.Infinity);
                if (natural.DesiredSize.Width > block.Bounds.Width + 1)
                {
                    cut.Add($"one line wider than its space ({natural.DesiredSize.Width:0} against {block.Bounds.Width:0}): {block.Text}");
                    continue;
                }
            }

            foreach (var clip in block.GetVisualAncestors().OfType<Visual>().Where(a => a.ClipToBounds || a is TopLevel))
            {
                if (block.TranslatePoint(new Point(block.Bounds.Width, block.Bounds.Height), clip) is { } corner && corner.X > clip.Bounds.Width + 1)
                {
                    cut.Add($"past the edge of a {clip.GetType().Name} by {corner.X - clip.Bounds.Width:0}: {block.Text}");
                    break;
                }
            }
        }

        return cut;
    }

    /// <summary>The first run card: both consent choices whole, at every width.</summary>
    [AvaloniaFact]
    public void TheFirstRunScreenCutsNoText()
    {
        foreach (double width in Widths)
        {
            var (window, path) = Ready(width, SendingChoice.Unset, null);
            try
            {
                window.ShowFirstRunIfDue();
                Settle();
                Assert.True(window.FirstRunShown);
                var said = window.FirstRunCard.GetVisualDescendants().OfType<TextBlock>().Select(b => b.Text ?? "").ToList();
                Assert.Contains("Testing only. " + ReceiverTerms.Current.TestingText, said);
                Assert.Contains("May be published. " + ReceiverTerms.Current.PublishableText, said);
                Assert.Empty(Cut(window.FirstRunCard).Select(c => $"{width}: {c}"));
            }
            finally
            {
                Finish(window, path);
            }
        }
    }

    /// <summary>
    /// The question after Accept and analyze, with its two consent levels, at the widths the analysis screen fits. Its three columns need
    /// 1004 units beside the rail, so below about 1060 the right column, where the question sits, runs past the window whatever it holds;
    /// that is question 58, and the narrow widths are left out here until it is answered.
    /// </summary>
    [AvaloniaFact]
    public void TheQuestionAfterAnalyzeCutsNoText()
    {
        foreach (double width in new double[] { 1400, 1060 })
        {
            var (window, path) = Ready(width, SendingChoice.Ask, null);
            try
            {
                window.Analyse();
                Settle();
                Assert.True(window.QuestionShown);
                Assert.Empty(Cut(window.SendPanel).Select(c => $"{width}: {c}"));
            }
            finally
            {
                Finish(window, path);
            }
        }
    }

    /// <summary>Every section of Settings, sending targets and error reports among them.</summary>
    [AvaloniaFact]
    public void SettingsCutsNoText()
    {
        foreach (double width in Widths)
        {
            var (window, path) = Ready(width, SendingChoice.Ask, null);
            try
            {
                window.ShowSettings();
                Settle();
                Assert.Empty(Cut(window.SettingsBody).Select(c => $"{width}: {c}"));
            }
            finally
            {
                Finish(window, path);
            }
        }
    }

    /// <summary>Section 2.3: consent is chosen, never defaulted, on the first run card, the question and in Settings.</summary>
    [AvaloniaFact]
    public void NeitherConsentLevelIsChosenForThePerson()
    {
        var (window, path) = Ready(1400, SendingChoice.Unset, null);
        try
        {
            window.ShowFirstRunIfDue();
            Settle();
            Assert.All(window.FirstRunCard.GetVisualDescendants().OfType<RadioButton>(), r => Assert.NotEqual(true, r.IsChecked));

            window.ShowSettings();
            Settle();
            var levels = window.SettingsBody.GetVisualDescendants().OfType<RadioButton>().Where(r => r.GroupName == "sendingLevel").ToList();
            Assert.Equal(2, levels.Count);
            Assert.All(levels, r => Assert.NotEqual(true, r.IsChecked));
        }
        finally
        {
            Finish(window, path);
        }

        (window, path) = Ready(1400, SendingChoice.Ask, null);
        try
        {
            window.Analyse();
            Settle();
            Assert.True(window.QuestionShown);
            var offered = window.SendPanel.GetVisualDescendants().OfType<RadioButton>().Where(r => r.GroupName == "sendLevel").ToList();
            Assert.Equal(2, offered.Count);
            Assert.All(offered, r => Assert.NotEqual(true, r.IsChecked));
        }
        finally
        {
            Finish(window, path);
        }
    }
}
