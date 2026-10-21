using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using GroupLab.Core.Publication;
using GroupLab.Core.Survey;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 465 item 5A: the first run asks one question a step, with "Question N of M", a step bar, Back, Next and
/// Skip for now. Every step fits a 1280 by 720 window with its buttons in view, the words asked are the words asked before, word for
/// word, and Skip for now records nothing, so a skipped question keeps the setting it had and is asked again at the next start.
/// </summary>
public class Entry465FirstRunTests
{
    private static void Settle(Window window)
    {
        for (int i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    /// <summary>A first run with every question open: a new install, or somebody asked once more about what they chose before.</summary>
    private static (MainWindow Window, string Folder) Open(bool askedAgain, int width = 1400, int height = 900, string? folder = null)
    {
        TestDefaults.Outside.Forget();
        SharingSwitches.EverythingOverride = true;
        SharingSwitches.FullLogOverride = true;
        folder ??= Path.Combine(Path.GetTempPath(), $"grouplab-entry465-{Guid.NewGuid():N}");
        var store = new AppSettingsStore(Path.Combine(folder, "settings.json"));
        if (askedAgain)
        {
            store.SaveSending(SendingChoice.Always, ConsentLevel.Publishable);
            store.SaveErrorChoice(ErrorReportChoice.Always);
            store.SavePrinterOffered();
        }

        var window = new MainWindow(store) { Width = width, Height = height, ReceiverOpen = true, ErrorsOpen = true, SurveyOpen = true, AskScope = true };
        window.Show();
        window.ShowFirstRunIfDue();
        Settle(window);
        return (window, folder);
    }

    private static void Close(MainWindow window, string folder)
    {
        window.Close();
        SharingSwitches.EverythingOverride = null;
        SharingSwitches.FullLogOverride = null;
        TestDefaults.Outside.Forget();
        GroupLab.Tests.Support.Temp.Delete(folder);
    }

    private static bool Frame(string words) => words is FirstRunSteps.Back or FirstRunSteps.Next or FirstRunSteps.Skip
        || words.StartsWith("Question ", StringComparison.Ordinal) && words.Contains(" of ", StringComparison.Ordinal);

    /// <summary>
    /// The words the step showing says, in order, as the recording of the screen before took them, without the steps' own. A radio's words
    /// are not in the visual tree until it is first shown, so what shows is read from the logical tree, as the buttons are pressed.
    /// </summary>
    private static List<string> StepWords(MainWindow window) => [.. window.FirstRunCard.GetLogicalDescendants().OfType<Control>()
        .Where(c => c.IsVisible && c.GetLogicalAncestors().OfType<Control>().All(a => a.IsVisible))
        .Select(c => c switch
        {
            TextBlock t => t.Text ?? "",
            Button { Content: string s } => s,
            _ => null,
        })
        .OfType<string>()
        .Where(s => !Frame(s))];

    private static string Of(MainWindow window) =>
        window.FirstRunCard.GetVisualDescendants().OfType<TextBlock>().First(t => t.IsEffectivelyVisible && (t.Text ?? "").StartsWith("Question ", StringComparison.Ordinal)).Text!;

    private static Button Shown(MainWindow window, string words) =>
        window.FirstRunCard.GetVisualDescendants().OfType<Button>().Single(b => b.IsEffectivelyVisible && Equals(b.Content, words));

    [Fact]
    public void TheStepsMoveOnWhenAnsweredOrSkippedAndBackReturns()
    {
        var flow = new FirstRunSteps(3);
        Assert.Equal("Question 1 of 3", flow.Of);
        Assert.False(flow.CanBack);
        Assert.False(flow.CanNext);
        Assert.True(flow.CanSkip);

        flow.SkipForNow();
        Assert.Equal(1, flow.At);
        Assert.False(flow.Answered(0));
        Assert.Equal("Question 1 of 3, skipped for now", flow.BarName(0));

        flow.Answer();
        Assert.Equal(2, flow.At);
        Assert.True(flow.Answered(1));
        Assert.Equal("Question 2 of 3, answered", flow.BarName(1));
        Assert.Equal("Question 3 of 3, now", flow.BarName(2));

        flow.MoveBack();
        Assert.Equal(1, flow.At);
        Assert.True(flow.CanNext);
        Assert.False(flow.CanSkip);
        flow.SkipForNow();
        Assert.Equal(1, flow.At);
        flow.MoveNext();
        Assert.Equal(2, flow.At);
        Assert.True(flow.IsLast);
        Assert.False(flow.CanNext);
        flow.MoveNext();
        Assert.Equal(2, flow.At);

        flow.SkipForNow();
        Assert.True(flow.Finished);
        Assert.False(flow.CanBack);
        flow.Answer();
        Assert.Equal(3, flow.At);
        Assert.Equal(new[] { false, true, false }, Enumerable.Range(0, 3).Select(flow.Answered));
    }

    /// <summary>The words of every question, step by step, are the words the one screen showed before, in the same order.</summary>
    [AvaloniaFact]
    public void EveryQuestionSaysWhatItSaidBeforeWordForWord()
    {
        foreach (var (askedAgain, before) in new[] { (false, FirstRunWordsBefore.Fresh), (true, FirstRunWordsBefore.AskedAgain) })
        {
            var (window, folder) = Open(askedAgain);
            try
            {
                var said = new List<string>();
                int count = int.Parse(Of(window).Split(' ')[^1], System.Globalization.CultureInfo.InvariantCulture);
                Assert.Equal(4, count);
                for (int step = 1; step <= count; step++)
                {
                    Assert.Equal($"Question {step} of {count}", Of(window));
                    said.AddRange(StepWords(window));
                    window.PressSend(FirstRunSteps.Skip);
                    Settle(window);
                }

                Assert.False(window.FirstRunShown);
                Assert.Equal(before, said);
            }
            finally
            {
                Close(window, folder);
            }
        }
    }

    /// <summary>Skipping every question records nothing: each setting stays as it was, and the questions are asked again at the next start.</summary>
    [AvaloniaFact]
    public void SkipForNowRecordsNothingAndAsksAgainAtTheNextStart()
    {
        var (window, folder) = Open(askedAgain: false);
        try
        {
            for (int step = 0; step < 4; step++)
            {
                window.PressSend(FirstRunSteps.Skip);
                Settle(window);
            }

            Assert.False(window.FirstRunShown);
            var store = new AppSettingsStore(window.SettingsStore.Path);
            Assert.Equal(ScopeAnswer.Unset, store.LoadScopeAnswer());
            Assert.Equal((SendingChoice.Unset, (ConsentLevel?)null), store.LoadSending());
            Assert.Equal(ErrorReportChoice.Unset, store.LoadErrorChoice());
            Assert.Equal(SurveyChoice.Unset, store.LoadSurveyChoice());
        }
        finally
        {
            window.Close();
        }

        var (again, _) = Open(askedAgain: false, folder: folder);
        try
        {
            Assert.True(again.FirstRunShown);
            Assert.Equal("Question 1 of 4", Of(again));
        }
        finally
        {
            Close(again, folder);
        }

        // Somebody asked again keeps exactly what they chose before: no consent is widened or narrowed by a skip.
        var (asked, askedFolder) = Open(askedAgain: true);
        try
        {
            for (int step = 0; step < 4; step++)
            {
                asked.PressSend(FirstRunSteps.Skip);
                Settle(asked);
            }

            var store = new AppSettingsStore(asked.SettingsStore.Path);
            Assert.Equal((SendingChoice.Always, (ConsentLevel?)ConsentLevel.Publishable), store.LoadSending());
            Assert.True(store.EverythingQuestionDue());
            Assert.Equal(ErrorReportChoice.Always, store.LoadErrorChoice());
            Assert.True(store.ErrorWordingDue());
        }
        finally
        {
            Close(asked, askedFolder);
        }
    }

    /// <summary>An answer moves on; Back returns to it with Next to pass over it again, and Skip for now is not offered for an answered one.</summary>
    [AvaloniaFact]
    public void AnAnswerMovesOnAndBackAndNextGoOverIt()
    {
        var (window, folder) = Open(askedAgain: false);
        try
        {
            Assert.False(Shown(window, FirstRunSteps.Back).IsEnabled);
            Assert.False(Shown(window, FirstRunSteps.Next).IsEnabled);
            window.PressSend("Sizes in millimeters");
            window.PressSend("Mil");
            Settle(window);
            Assert.Equal("Question 2 of 4", Of(window));
            Assert.Equal(ScopeAnswer.Mil, window.SettingsStore.LoadScopeAnswer());
            Assert.Contains(SharingWords.TargetsQuestion, StepWords(window));

            Shown(window, FirstRunSteps.Back).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            Assert.Equal("Question 1 of 4", Of(window));
            Assert.True(Shown(window, FirstRunSteps.Next).IsEnabled);
            Assert.False(Shown(window, FirstRunSteps.Skip).IsEnabled);
            Shown(window, FirstRunSteps.Next).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            Assert.Equal("Question 2 of 4", Of(window));
            Assert.Equal(ScopeAnswer.Mil, window.SettingsStore.LoadScopeAnswer());
        }
        finally
        {
            Close(window, folder);
        }
    }

    /// <summary>
    /// Every step at 1280 by 720, and at 1060 by 720, the narrowest the sweep holds: the step's words need no scrolling, every button is
    /// inside the window, and the keyboard, screen reader and layout audits find nothing.
    /// </summary>
    [AvaloniaTheory]
    [InlineData(1280, 720)]
    [InlineData(1060, 720)]
    public void EveryStepFitsTheWindowWithItsButtonsInView(int width, int height)
    {
        var found = new List<string>();
        foreach (bool askedAgain in new[] { false, true })
        {
            var (window, folder) = Open(askedAgain, width, height);
            try
            {
                for (int step = 1; window.FirstRunShown; step++)
                {
                    string where = $"{(askedAgain ? "asked again" : "new install")}, step {step}";
                    var scroll = window.FirstRunCard.GetVisualDescendants().OfType<ScrollViewer>().First(s => s.IsEffectivelyVisible);
                    if (scroll.Extent.Height > scroll.Viewport.Height + 1)
                    {
                        found.Add($"{where}: the words are {scroll.Extent.Height:0} high in {scroll.Viewport.Height:0}");
                    }

                    foreach (var button in window.FirstRunCard.GetVisualDescendants().OfType<Button>().Where(b => b.IsEffectivelyVisible))
                    {
                        var box = new Rect(button.TranslatePoint(default, window) ?? default, button.Bounds.Size);
                        if (box.Bottom > window.Bounds.Height + 1 || box.Right > window.Bounds.Width + 1 || box.Y < 0 || box.X < 0)
                        {
                            found.Add($"{where}: '{button.Content}' at {box} is outside {window.Bounds.Size}");
                        }
                    }

                    found.AddRange(DesktopSweepTests.Problems(window, where));
                    var (stops, unnamed, _) = Entry447Tests.TabRound(window);
                    found.AddRange(unnamed.Select(u => $"{where}: {u} has no name"));
                    found.AddRange(GroupLab.Tests.Support.ScreenReaderAudit.Findings(window, stops).Select(f => $"{where}: {f}"));
                    found.AddRange(GroupLab.Tests.Support.LayoutAudit.Findings(window).Select(f => $"{where}: {f}"));
                    window.PressSend(FirstRunSteps.Skip);
                    Settle(window);
                }
            }
            finally
            {
                Close(window, folder);
            }
        }

        Assert.True(found.Count == 0, $"at {width} by {height}:{Environment.NewLine}" + string.Join(Environment.NewLine, found.Distinct().Take(40)));
    }
}
