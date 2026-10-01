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
/// family member the picture cannot tell from its sizes asks "Which target is this?" first. Nothing is recognized: the screen asks which
/// GroupLab sheet it is, as before.
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>What recognition decided about the picture open now, for the headless tests.</summary>
    internal StoreTargetRecognition? LastRecognition { get; private set; }

    /// <summary>The "Which target is this?" window while it is open, for the headless tests.</summary>
    internal Window? WhichTargetWindow { get; private set; }

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
    /// leaves the scale to be measured.
    /// </summary>
    internal async Task ApplyRecognition(StoreTargetRecognition seen)
    {
        ArgumentNullException.ThrowIfNull(seen);
        LastRecognition = seen;
        var match = seen.AsksWhichSize ? await AskWhichTarget(seen) : seen.Named;
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

    /// <summary>
    /// Entry 340 section 2: "Which target is this?", each family member with its name, printed size and GroupLab's drawing of its outline,
    /// the last answer for the family first, and "Not sure". Returns the member chosen, or null for "Not sure" or a closed window.
    /// </summary>
    internal async Task<StoreTargetMatch?> AskWhichTarget(StoreTargetRecognition seen)
    {
        ArgumentNullException.ThrowIfNull(seen);
        string? family = FamilyQuestion.Family(seen);
        var answers = FamilyQuestion.Answers(seen, family is null ? null : settingsStore.LoadFamilyAnswer(family));
        StoreTargetMatch? chosen = null;
        var dialog = new Window
        {
            Title = FamilyQuestion.Title,
            Width = 460,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        var body = new StackPanel { Margin = Tokens.SectionPadding, Spacing = Tokens.Space8 };
        body.Children.Add(new TextBlock { Text = FamilyQuestion.Title, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
        body.Children.Add(Note(FamilyQuestion.Why));
        foreach (var answer in answers)
        {
            var words = new StackPanel { Spacing = Tokens.Space4, VerticalAlignment = VerticalAlignment.Center };
            words.Children.Add(new TextBlock { Text = answer.Name, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
            words.Children.Add(Note(answer.Printed));
            var content = new DockPanel();
            var outline = new TargetOutline(answer) { Margin = new Thickness(0, 0, Tokens.Space8, 0), VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(outline, Dock.Left);
            content.Children.Add(outline);
            content.Children.Add(words);
            var button = new Button { Content = content, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left, Margin = new Thickness(2) };
            Avalonia.Automation.AutomationProperties.SetName(button, answer.Name);
            button.Click += (_, _) =>
            {
                chosen = answer.Match;
                dialog.Close();
            };
            body.Children.Add(button);
        }

        var notSure = Button(FamilyQuestion.NotSure, dialog.Close);
        Avalonia.Automation.AutomationProperties.SetName(notSure, FamilyQuestion.NotSure);
        body.Children.Add(Row(notSure));
        dialog.Content = body;
        WhichTargetWindow = dialog;
        DiagnosticLog.Info("dialog.open", ("dialog", FamilyQuestion.Title));
        await dialog.ShowDialog(this);
        WhichTargetWindow = null;
        if (chosen is not null && family is not null)
        {
            settingsStore.SaveFamilyAnswer(family, chosen.Target.Id);
            DiagnosticLog.Info("storetarget.answer", ("answer", chosen.Target.Id));
        }

        return chosen;
    }
}
