using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.StoreTargets;

namespace GroupLab.App;

/// <summary>
/// Store-bought targets on the desktop, NOTES-FROM-PLANNING.md entries 340 and 341. A picture whose codes name no GroupLab sheet is tried
/// against the store-bought targets GroupLab has fingerprints of; one it recognizes is named, its bulls placed and its printed size set as
/// the scale, with the warning beside the scale and on the result and the scale check one click away, so Find holes is offered at once. A
/// family member the picture cannot tell from its sizes asks "Which target is this?" first, in a bar along the bottom of the picture. Nothing is recognized: the screen asks which
/// GroupLab sheet it is, as before.
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>What recognition decided about the picture open now, for the headless tests.</summary>
    internal StoreTargetRecognition? LastRecognition { get; private set; }

    /// <summary>The tool chosen now, for the headless tests.</summary>
    internal MarkingTool ToolNow => canvas.Tool;

    /// <summary>The scale's pill on the analysis, for the headless tests.</summary>
    internal string ScalePillText => registrationText.Text ?? "";

    /// <summary>The warning under the scale, with the way to check it.</summary>
    private StackPanel StoreTargetWarning()
    {
        var check = Button(StoreTargetMatch.CheckScale, CheckTheScale);
        Avalonia.Automation.AutomationProperties.SetName(check, StoreTargetMatch.CheckScale);
        ToolTip.SetTip(check, "Measure a known length, a ruler or a GroupLab sheet beside the target, to set the scale yourself");
        return new StackPanel { Name = "StoreTargetWarning", Spacing = Tokens.Space4, Children = { Note(StoreTargetMatch.Warning), Row(check) } };
    }

    /// <summary>The scale check: the length tool, so a known length measured on the picture replaces the printed size.</summary>
    internal void CheckTheScale()
    {
        SetTool(MarkingTool.Length);
        status.Text = "Click the two ends of a length you know, a ruler's marks or a GroupLab sheet beside the target, then type how long it is. It replaces the printed size as the scale.";
    }

    /// <summary>
    /// After a picture named no GroupLab sheet: tries the store-bought targets, away from the screen's thread. True where one was
    /// recognized, placed or asked about; false where none was, or where another picture has been opened since.
    /// </summary>
    private async Task<bool> RecognizeStoreTarget(GrayImage g, CancellationToken token)
    {
        if (session.State.ImagePath is not { } path)
        {
            return false;
        }

        status.Text = "Looking for a store-bought target GroupLab knows…";
        StoreTargetRecognition? seen;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            seen = await Task.Run(() => StoreTargetRecognizer.Recognize(path, new OpenCvFingerprintBackend()), token);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or OpenCvSharp.OpenCVException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "storetarget.recognize", ex);
            return false;
        }

        token.ThrowIfCancellationRequested();
        DiagnosticLog.Info("storetarget.recognize", ("decided", seen?.Describe()), ("ms", clock.ElapsedMilliseconds));
        if (!ReferenceEquals(g, grey) || seen is not { Found: true })
        {
            return false;
        }

        await ApplyRecognition(seen);
        return true;
    }

    /// <summary>
    /// What was recognized, applied: the product named and placed, or the family's question asked first. "Not sure" places the bulls and
    /// leaves the scale to be measured. A picture left while the question was open is left as it was: no answer is applied to it.
    /// </summary>
    internal async Task ApplyRecognition(StoreTargetRecognition seen)
    {
        ArgumentNullException.ThrowIfNull(seen);
        LastRecognition = seen;
        var asked = grey;
        StoreTargetMatch? match;
        if (seen.AsksWhichSize)
        {
            var (answered, chosen) = await AskWhichTarget(seen);
            if (!answered || !ReferenceEquals(asked, grey))
            {
                return;
            }

            match = chosen;
        }
        else
        {
            match = seen.Named;
        }

        int width = grey?.Width ?? 0, height = grey?.Height ?? 0;
        problem.Text = "";
        if (match is null)
        {
            // The bull is where every member's fit puts it, whichever size it is; only the scale waits for the person.
            foreach (var bull in seen.Family.FirstOrDefault()?.Bulls(width, height) ?? [])
            {
                session.AddBull(bull);
            }

            status.Text = FamilyQuestion.NotSureSaid;
            DiagnosticLog.Info("storetarget.answer", ("answer", "not sure"));
            Refresh();
            return;
        }

        session.PlaceStoreTarget(match, width, height);
        status.Text = match.Said(session.State.Bulls.Count);
        DiagnosticLog.Info("storetarget.placed", ("product", match.Target.Id), ("bulls", session.State.Bulls.Count));
        Refresh();
    }

    /// <summary>"Which target is this?" along the bottom of the picture while it is asked; hidden otherwise.</summary>
    private readonly Border whichTargetBar = new() { Name = "WhichTargetBar", IsVisible = false, Classes = { AppStyles.QuestionBar } };

    /// <summary>The answer the open question waits for: whether it was answered, and the member chosen or null for "Not sure".</summary>
    private TaskCompletionSource<(bool Answered, StoreTargetMatch? Chosen)>? whichTargetAnswer;

    /// <summary>The question's answers in the order shown, then "Not sure", while it is open.</summary>
    private readonly List<Button> whichTargetChoices = [];

    /// <summary>The "Which target is this?" bar while it is open, for the headless tests.</summary>
    internal Border? WhichTargetBar => whichTargetBar.IsVisible ? whichTargetBar : null;

    /// <summary>
    /// Entry 340 section 2, laid out as entry 465 3B chose: "Which target is this?" as a short bar docked along the bottom of the picture
    /// area. The question and one line of why on the first line; the family's sizes as buttons side by side, the last answer for the family
    /// first, and "Not sure". The picture is moved up so the target asked about is whole in view above the bar. Returns whether it was
    /// answered (false where the picture was left first) and the member chosen, or null for "Not sure".
    /// </summary>
    internal async Task<(bool Answered, StoreTargetMatch? Chosen)> AskWhichTarget(StoreTargetRecognition seen)
    {
        ArgumentNullException.ThrowIfNull(seen);
        EndWhichTarget();
        string? family = FamilyQuestion.Family(seen);
        var answers = FamilyQuestion.Answers(seen, family is null ? null : settingsStore.LoadFamilyAnswer(family));
        var waiting = new TaskCompletionSource<(bool Answered, StoreTargetMatch? Chosen)>();
        whichTargetAnswer = waiting;
        void Answer(StoreTargetMatch? chosen)
        {
            if (ReferenceEquals(whichTargetAnswer, waiting))
            {
                EndWhichTarget(answered: true, chosen);
            }
        }

        var title = new TextBlock { Text = FamilyQuestion.Title, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, Tokens.Space8, 0) };
        DockPanel.SetDock(title, Dock.Left);
        var question = new DockPanel { Children = { title, new TextBlock { Text = FamilyQuestion.Why, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Secondary } } } };
        var choices = new WrapPanel { Orientation = Orientation.Horizontal };
        whichTargetChoices.Clear();
        foreach (var answer in answers)
        {
            var button = Button(answer.Name, () => Answer(answer.Match));
            Avalonia.Automation.AutomationProperties.SetName(button, answer.Name);
            Avalonia.Automation.AutomationProperties.SetHelpText(button, answer.Printed);
            ToolTip.SetTip(button, answer.Printed);
            whichTargetChoices.Add(button);
        }

        var notSure = Button(FamilyQuestion.NotSure, () => Answer(null));
        Avalonia.Automation.AutomationProperties.SetName(notSure, FamilyQuestion.NotSure);
        whichTargetChoices.Add(notSure);
        whichTargetChoices[0].Classes.Add(AppStyles.Primary);
        foreach (var button in whichTargetChoices)
        {
            button.Margin = new Thickness(0, 0, Tokens.Space8, 0);
            choices.Children.Add(button);
        }

        whichTargetBar.Child = new StackPanel { Spacing = Tokens.Space8, Children = { question, choices } };
        Avalonia.Automation.AutomationProperties.SetName(whichTargetBar, FamilyQuestion.Title);
        whichTargetBar.IsVisible = true;
        DiagnosticLog.Info("dialog.open", ("dialog", FamilyQuestion.Title));

        // The canvas is shorter by the bar now: the target asked about is moved up, whole, into the room left above it.
        UpdateLayout();
        if (grey is { } g && FamilyQuestion.AskedAbout(seen, g.Width, g.Height) is { } part)
        {
            canvas.KeepWhole(part.TopLeft, part.BottomRight, Tokens.Space24);
        }

        whichTargetChoices[0].Focus();
        var (answered, chosen) = await waiting.Task;
        if (answered && chosen is not null && family is not null)
        {
            settingsStore.SaveFamilyAnswer(family, chosen.Target.Id);
            DiagnosticLog.Info("storetarget.answer", ("answer", chosen.Target.Id));
        }

        return (answered, chosen);
    }

    /// <summary>The question closed: with its answer, or unanswered where the picture is left while it is open.</summary>
    private void EndWhichTarget(bool answered = false, StoreTargetMatch? chosen = null)
    {
        var waiting = whichTargetAnswer;
        whichTargetAnswer = null;
        whichTargetBar.IsVisible = false;
        whichTargetBar.Child = null;
        whichTargetChoices.Clear();
        waiting?.TrySetResult((answered, chosen));
    }

    /// <summary>While the question is open, Enter takes its first answer and Escape is "Not sure"; a button reached by Tab takes its own.</summary>
    private bool WhichTargetKey(Avalonia.Input.KeyEventArgs e)
    {
        if (whichTargetAnswer is null || whichTargetChoices.Count == 0)
        {
            return false;
        }

        var pick = e.Key switch
        {
            Avalonia.Input.Key.Escape => whichTargetChoices[^1],
            Avalonia.Input.Key.Enter when !(focusFromKeyboard && e.Source is Avalonia.Controls.Button) => whichTargetChoices[0],
            _ => null,
        };
        if (pick is null)
        {
            return false;
        }

        pick.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
        e.Handled = true;
        return true;
    }
}
