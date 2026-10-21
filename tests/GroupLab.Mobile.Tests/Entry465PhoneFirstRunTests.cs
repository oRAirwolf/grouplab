using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using GroupLab.Core.Publication;
using GroupLab.Core.Survey;
using GroupLab.Tests.Support;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 465 item 5A on the phone: one question a step, as on the computer. The words asked are the words asked
/// before, word for word, Skip for now records nothing, and every step is laid out whole and read whole by a screen reader at the phone
/// widths, in both themes and at the largest text.
/// </summary>
public class Entry465PhoneFirstRunTests
{
    private static void Start()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }
    }

    private static AppSettingsStore Fresh(bool askedAgain, out string folder)
    {
        folder = Path.Combine(Path.GetTempPath(), $"grouplab-entry465-{Guid.NewGuid():N}");
        var store = new AppSettingsStore(Path.Combine(folder, "settings.json"));
        if (askedAgain)
        {
            store.SaveSending(SendingChoice.Always, ConsentLevel.Publishable);
            store.SaveErrorChoice(ErrorReportChoice.Always);
            store.SavePrinterOffered();
        }

        return store;
    }

    private static void Settle(Window window)
    {
        for (int i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static bool Shows(Control c) => c.IsVisible && c.GetLogicalAncestors().OfType<Control>().All(a => a.IsVisible);

    /// <summary>The words the step showing says, in order, without the page's title, the steps' own words or the printer card.</summary>
    private static List<string> StepWords(FirstRunView view)
    {
        var said = view.GetLogicalDescendants().OfType<TextBlock>().Where(Shows).Select(t => t.Text ?? "")
            .Where(s => s is not ("Before you start" or FirstRunSteps.Back or FirstRunSteps.Next or FirstRunSteps.Skip)
                && !(s.StartsWith("Question ", StringComparison.Ordinal) && s.Contains(" of ", StringComparison.Ordinal))).ToList();
        int printer = said.IndexOf("Check your printer");
        return printer < 0 ? said : said[..printer];
    }

    private static Button Pressable(FirstRunView view, string words) =>
        view.GetVisualDescendants().OfType<Button>().Single(b => b.IsEffectivelyVisible && b.Content is TextBlock t && t.Text == words);

    private static void Press(Window window, FirstRunView view, string words)
    {
        Pressable(view, words).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Settle(window);
    }

    private static string Of(FirstRunView view) =>
        view.GetVisualDescendants().OfType<TextBlock>().First(t => t.IsEffectivelyVisible && (t.Text ?? "").StartsWith("Question ", StringComparison.Ordinal)).Text!;

    [AvaloniaFact]
    public void EveryQuestionSaysWhatItSaidBeforeWordForWord()
    {
        Start();
        SharingSwitches.EverythingOverride = true;
        SharingSwitches.FullLogOverride = true;
        try
        {
            foreach (var (askedAgain, before) in new[] { (false, FirstRunWordsBefore.Fresh), (true, FirstRunWordsBefore.AskedAgain) })
            {
                var store = Fresh(askedAgain, out string folder);
                bool done = false;
                var view = new FirstRunView(store, () => done = true);
                var window = new Window { Width = 412, Height = 915, Content = view };
                window.Show();
                Settle(window);
                try
                {
                    var said = new List<string>();
                    for (int step = 1; !done; step++)
                    {
                        Assert.Equal($"Question {step} of 4", Of(view));
                        said.AddRange(StepWords(view));
                        Press(window, view, FirstRunSteps.Skip);
                    }

                    Assert.Equal("Before you start", before[0]);
                    Assert.Equal(before[1..], said);
                }
                finally
                {
                    window.Close();
                    Directory.Delete(folder, recursive: true);
                }
            }
        }
        finally
        {
            SharingSwitches.EverythingOverride = null;
            SharingSwitches.FullLogOverride = null;
        }
    }

    [AvaloniaFact]
    public void SkipForNowRecordsNothingAndBackReturnsToAnAnswer()
    {
        Start();
        var store = Fresh(askedAgain: false, out string folder);
        bool done = false;
        var view = new FirstRunView(store, () => done = true);
        var window = new Window { Width = 412, Height = 915, Content = view };
        window.Show();
        Settle(window);
        try
        {
            Assert.False(Pressable(view, FirstRunSteps.Back).IsEnabled);
            Press(window, view, "Mil");
            Assert.Equal(ScopeAnswer.Mil, store.LoadScopeAnswer());
            Assert.Equal("Question 2 of 4", Of(view));
            Press(window, view, FirstRunSteps.Back);
            Assert.Equal("Question 1 of 4", Of(view));
            Assert.False(Pressable(view, FirstRunSteps.Skip).IsEnabled);
            Press(window, view, FirstRunSteps.Next);
            for (int step = 2; step <= 4; step++)
            {
                Assert.Equal($"Question {step} of 4", Of(view));
                Press(window, view, FirstRunSteps.Skip);
            }

            Assert.True(done);
            var kept = new AppSettingsStore(store.Path);
            Assert.Equal(ScopeAnswer.Mil, kept.LoadScopeAnswer());
            Assert.Equal((SendingChoice.Unset, (ConsentLevel?)null), kept.LoadSending());
            Assert.Equal(ErrorReportChoice.Unset, kept.LoadErrorChoice());
            Assert.Equal(SurveyChoice.Unset, kept.LoadSurveyChoice());
            Assert.Equal(ReceiverTerms.Current.AppOpen || ReceiverTerms.Current.ErrorReportsOpen || ReceiverTerms.Current.SurveyOpen, FirstRunView.Due(kept));
        }
        finally
        {
            window.Close();
            Directory.Delete(folder, recursive: true);
        }
    }

    [AvaloniaFact]
    public void EveryStepIsLaidOutWholeAndReadWhole()
    {
        Start();
        var found = new List<string>();
        foreach (bool largest in new[] { false, true })
        {
            foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
            {
                foreach (var (width, height) in new[] { (360, 780), (412, 915) })
                {
                    var store = Fresh(askedAgain: false, out string folder);
                    bool done = false;
                    var view = new FirstRunView(store, () => done = true);
                    var window = new Window { Width = width, Height = height, RequestedThemeVariant = theme, Content = view };
                    if (largest)
                    {
                        AndroidText.LargestUntilClosed(window);
                    }

                    window.Show();
                    Settle(window);
                    try
                    {
                        for (int step = 1; !done; step++)
                        {
                            string where = $"step {step}, {width} wide, {theme}{(largest ? ", largest text" : "")}";
                            found.AddRange(LayoutAudit.Findings(window).Select(f => where + ": " + f));
                            found.AddRange(ScreenReaderAudit.Findings(window, null, smallestTarget: 48).Select(f => where + ": " + f));
                            foreach (string words in new[] { FirstRunSteps.Back, FirstRunSteps.Skip, FirstRunSteps.Next })
                            {
                                var button = Pressable(view, words);
                                var box = new Avalonia.Rect(button.TranslatePoint(default, window) ?? default, button.Bounds.Size);
                                if (box.Bottom > window.Bounds.Height + 1 || box.Right > window.Bounds.Width + 1)
                                {
                                    found.Add($"{where}: {words} at {box} is outside {window.Bounds.Size}");
                                }
                            }

                            Press(window, view, FirstRunSteps.Skip);
                        }
                    }
                    finally
                    {
                        window.Close();
                        Directory.Delete(folder, recursive: true);
                    }
                }
            }
        }

        Assert.True(found.Count == 0, string.Join(Environment.NewLine, found.Distinct().Take(40)));
    }
}
