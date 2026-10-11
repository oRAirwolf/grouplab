using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;

namespace GroupLab.App;

/// <summary>
/// How a sheet's shots are read against its bulls. NOTES-FROM-PLANNING.md entry 113 section 4 gave it as a "Shots per bull" dropdown with a box
/// for bull numbers; entry 407 section 2 (concept 1A, Alan's pick) says it as one question, "How did you shoot this sheet?", with three answers
/// as cards, and bulls tapped on the sheet rather than typed. The rule underneath is unchanged (<see cref="SheetReading"/>), so a saved session
/// reads as it always did.
/// <para>
/// Under the answers, the bulls left blank: found by GroupLab, and corrected by tapping them. That set is the whole-sheet offset's "bulls you
/// fired at" (entry 130 section 3.1). Found by itself it is the whole-sheet reading's (entry 229 section 4), and where the holes fit more than
/// one set of bulls the question "Which bulls did you fire at?" is still asked (entry 376 section A2), so the offset never loses its input.
/// "Only the bulls I aimed at", the old fourth choice, is that tap on answer one.
/// </para>
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>What the bulls tapped on the sheet are doing now.</summary>
    internal enum BullTaps
    {
        None,
        TookTwo,
        FiredAt,
    }

    private readonly List<(ShotPattern Pattern, RadioButton Card)> patternCards = [];
    private readonly TextBlock unusedLine = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock tappingSays = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Warn } };

    /// <summary>What GroupLab will make of the sheet, read back: entry 141 section 5.3.4 and entry 407 section 2.3.</summary>
    private readonly TextBlock ruleSays = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };

    private ToggleButton? tookTwoButton;
    private ToggleButton? firedAtButton;
    private BullTaps tapping;
    private bool showingPattern;

    private void BuildShotsPerBull(StackPanel panel)
    {
        panel.Children.Add(FieldLabel("How did you shoot this sheet?"));
        foreach (var (pattern, words, line) in SheetReading.Patterns)
        {
            var card = new RadioButton
            {
                GroupName = "shot-pattern",
                IsChecked = pattern == ShotPattern.OnePerBull,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Top,
                Padding = new Avalonia.Thickness(Tokens.Space8, Tokens.Space4),
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = words, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold },
                        new TextBlock { Text = line, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } },
                    },
                },
            };
            Avalonia.Automation.AutomationProperties.SetName(card, words + ". " + line);
            var chosen = pattern;
            card.IsCheckedChanged += (_, _) =>
            {
                if (card.IsChecked == true && !showingPattern)
                {
                    ChooseShotPattern(chosen);
                }
            };
            patternCards.Add((pattern, card));
            panel.Children.Add(new Border { Child = card, Classes = { AppStyles.ResultCard } });
        }

        tookTwoButton = new ToggleButton { Content = "Tap the bulls that took two" };
        tookTwoButton.Click += (_, _) => TapBulls(tookTwoButton.IsChecked == true ? BullTaps.TookTwo : BullTaps.None);
        firedAtButton = new ToggleButton { Content = "Tap the bulls you fired at" };
        firedAtButton.Click += (_, _) => TapBulls(firedAtButton.IsChecked == true ? BullTaps.FiredAt : BullTaps.None);
        panel.Children.Add(unusedLine);
        panel.Children.Add(Row(tookTwoButton, firedAtButton));
        panel.Children.Add(tappingSays);
        panel.Children.Add(aimedAtLines);
    }

    /// <summary>Answers "How did you shoot this sheet?", keeping the bulls already said, and has the marking read that way.</summary>
    internal void ChooseShotPattern(ShotPattern pattern)
    {
        session.SetAssignmentRule(SheetReading.Choose(session.State, pattern));
        status.Text = pattern switch
        {
            ShotPattern.Nearest => "Every shot goes to its nearest bull, and a bull holding more than one is not raised.",
            ShotPattern.SomeTookMore => "Tap the bulls that took two on the sheet.",
            _ => "One shot a bull, matched.",
        };
        TapBulls(pattern == ShotPattern.SomeTookMore ? BullTaps.TookTwo : BullTaps.None);
        DiagnosticLog.Info("assignment.rule", ("choice", pattern.ToString()));
    }

    /// <summary>Starts or stops taking taps on the sheet's bulls for this section.</summary>
    internal void TapBulls(BullTaps what)
    {
        tapping = what;
        canvas.Pick(null);
        canvas.BullTapTaker = what == BullTaps.None ? null : TapBullForGroups;
        ShowRule();
    }

    /// <summary>A bull tapped while this section takes taps: it takes two or one again, or goes into or out of the bulls fired at.</summary>
    internal void TapBullForGroups(int bull)
    {
        var state = session.State;
        var rule = tapping == BullTaps.TookTwo ? SheetReading.TapDoubled(state, bull) : SheetReading.TapFiredAt(state, bull);
        session.SetAssignmentRule(rule);
        DiagnosticLog.Info("assignment.tap", ("what", tapping.ToString()));
    }

    /// <summary>The choices last asked about, so one ambiguous sheet is asked once and dismissing it keeps GroupLab's guess.</summary>
    private string? askedWhichBulls;

    /// <summary>
    /// Entry 376 section A2: where the holes fit more than one set of bulls equally well and nobody has said which were fired at, the question
    /// in the middle of the window, GroupLab's guess first; "Choose the bulls myself" starts tapping the bulls fired at. Run after every change
    /// to the marking, and asked once for each set of choices.
    /// </summary>
    internal void AskWhichBulls()
    {
        if (session.WhichBulls() is not { } question || problemLayer.IsVisible)
        {
            return;
        }

        string key = string.Join("|", question.Choices.Select(c => c.Bulls));
        if (key == askedWhichBulls)
        {
            return;
        }

        askedWhichBulls = key;
        var choices = question.Choices.Select(c => (c.Guess ? c.Words + " (GroupLab's guess)" : c.Words, (Action)(() =>
        {
            session.AnswerWhichBulls(c);
            ShowRule();
            status.Text = AimedBulls.Says(session.State.Rule, session.State.Bulls);
        }))).ToList();
        choices.Add((BullsQuestion.Other, () => TapBulls(BullTaps.FiredAt)));
        ShowProblem(BullsQuestion.Title, BullsQuestion.Why, [.. choices]);
    }

    /// <summary>Writes the answer, the bulls unused and the read-back under the section, so a person can check what they told GroupLab.</summary>
    private void ShowRule()
    {
        var state = session.State;
        bool any = state.Bulls.Count(b => b.Scoring) > 1;
        var pattern = SheetReading.PatternOf(state.Rule);
        showingPattern = true;
        foreach (var (p, card) in patternCards)
        {
            card.IsChecked = p == pattern;
        }

        showingPattern = false;
        if (tapping == BullTaps.TookTwo && pattern != ShotPattern.SomeTookMore && SheetReading.Doubled(state.Rule).Count == 0 && state.Rule is not null)
        {
            tapping = BullTaps.None;
            canvas.BullTapTaker = null;
        }

        if (tookTwoButton is not null && firedAtButton is not null)
        {
            tookTwoButton.IsVisible = pattern == ShotPattern.SomeTookMore;
            tookTwoButton.IsChecked = tapping == BullTaps.TookTwo;
            firedAtButton.IsVisible = pattern != ShotPattern.Nearest && any;
            firedAtButton.IsChecked = tapping == BullTaps.FiredAt;
        }

        unusedLine.Text = any ? SheetReading.UnusedLine(state) : "";
        unusedLine.IsVisible = unusedLine.Text.Length > 0;
        tappingSays.Text = tapping switch
        {
            BullTaps.TookTwo => "Click each bull that took two shots on the sheet; click it again for one. Press the button again when done.",
            BullTaps.FiredAt => "Click each bull to say you fired at it, or to take it out. Press the button again when done.",
            _ => "",
        };
        tappingSays.IsVisible = tappingSays.Text.Length > 0;
        ruleSays.Text = SheetReading.ReadBack(state);
        ruleSays.IsVisible = ruleSays.Text.Length > 0;
    }

    /// <summary>For the screenshot walk: the Groups section scrolled into view.</summary>
    internal void BringGroupsIntoView()
    {
        // From below and then up to the heading, so the section's top sits at the top of the panel rather than at its foot.
        ruleSays.BringIntoView();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        patternCards.FirstOrDefault().Card?.BringIntoView();
    }

    /// <summary>For the headless tests: the answer showing, and the read-back.</summary>
    internal ShotPattern PatternShown => patternCards.FirstOrDefault(c => c.Card.IsChecked == true).Pattern;

    internal string ReadBackText => ruleSays.Text ?? "";

    internal string UnusedText => unusedLine.Text ?? "";

    /// <summary>For the headless tests: says these bulls were fired at, as tapping each would, in one step.</summary>
    internal void SayFiredAt(IEnumerable<int> bulls)
    {
        var set = bulls.ToHashSet();
        var state = session.State;
        session.SetAssignmentRule(SheetReading.Rule(SheetReading.PatternOf(state.Rule) is ShotPattern.Nearest ? ShotPattern.OnePerBull : SheetReading.PatternOf(state.Rule),
            state.Bulls, set, SheetReading.Doubled(state.Rule)));
    }
}
