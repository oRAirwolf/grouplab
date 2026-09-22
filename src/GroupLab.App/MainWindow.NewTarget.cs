using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 140 sections 1.4 and 2: leaving a sheet, and starting a new one on purpose.
/// <para>
/// <b>Why the question exists.</b> Section 1 made opening an image throw the last sheet away, which is right, and which is also how work gets
/// lost. A person who has spent ten minutes correcting marks and then opens the next photograph has said nothing about what should happen to
/// the ten minutes. So the sheet that has edits nobody has saved asks, once, before it goes: save it, discard it, or stay where you are.
/// </para>
/// <para>
/// <b>It is a row, not a dialog.</b> Entry 131 section 9's rule holds here: the question appears at the top of the panel beside the crash
/// banner, and cancel leaves everything exactly as it was. Nothing is ever lost by answering and nothing is ever lost by not answering.
/// </para>
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>
    /// The marking as it stood when this sheet was last saved as a session, or null where it has never been saved. Comparing the marking's own
    /// file is what makes "unsaved work" exact: a save followed by an undo and a redo is not unsaved work, and moving one mark is.
    /// </summary>
    private string? savedMarking;

    /// <summary>The question row, entry 140 section 1.4, hidden until a sheet with unsaved work is about to be left.</summary>
    private readonly StackPanel leavingAsk = new() { Spacing = Tokens.Space8, IsVisible = false };

    /// <summary>What to do once the question is answered with save or discard.</summary>
    private Action? afterLeaving;

    /// <summary>Whether this sheet holds work that is not in a saved session, for the headless tests.</summary>
    internal bool HasUnsavedWork =>
        session.State.Shots.Count > 0 && MarkingFile.Write(session.State, units) != savedMarking;

    /// <summary>Whether the save, discard or cancel question is up, for the headless tests.</summary>
    internal bool AskingAboutUnsavedWork => leavingAsk.IsVisible;

    /// <summary>Records that the marking as it now stands is in a saved session, so it is not unsaved work.</summary>
    private void MarkingIsSaved() => savedMarking = session.State.Shots.Count > 0 ? MarkingFile.Write(session.State, units) : null;

    private void BuildLeavingAsk()
    {
        leavingAsk.Children.Add(new TextBlock
        {
            Text = "This target has edits that are not saved. Save it, discard it, or stay here?",
            TextWrapping = TextWrapping.Wrap,
            Classes = { AppStyles.Alert },
        });
        leavingAsk.Children.Add(Row(
            Button("Save this target", () => Answer(save: true)),
            Button("Discard it", () => Answer(save: false)),
            Button("Cancel", CancelLeaving)));
    }

    /// <summary>Answers the question with save, for the headless tests.</summary>
    internal void AnswerSave() => Answer(save: true);

    /// <summary>Answers the question with discard, for the headless tests.</summary>
    internal void AnswerDiscard() => Answer(save: false);

    /// <summary>Answers the question with cancel, for the headless tests.</summary>
    internal void AnswerCancel() => CancelLeaving();

    private void CancelLeaving()
    {
        leavingAsk.IsVisible = false;
        afterLeaving = null;
        Refresh();
    }

    private void Answer(bool save)
    {
        if (save && sessions is null)
        {
            // The session database could not be opened this time, which the panel already says. Going on from here would discard the work
            // under the word "save", so the question stays up and the two answers that can be honoured are still there.
            problem.Text = "This target cannot be saved: the session database is not open. Discard it or cancel.";
            return;
        }

        leavingAsk.IsVisible = false;
        var next = afterLeaving;
        afterLeaving = null;
        if (save)
        {
            SaveSession();
        }

        next?.Invoke();
    }

    /// <summary>
    /// Runs <paramref name="next"/>, asking first where this sheet holds edits nobody has saved. Every way out of a sheet goes through here:
    /// opening an image, opening a marking, and New target.
    /// </summary>
    private void Leaving(Action next)
    {
        if (!HasUnsavedWork)
        {
            next();
            return;
        }

        afterLeaving = next;
        leavingAsk.IsVisible = true;
        Refresh();
    }

    /// <summary>
    /// New target, entry 140 section 2: the same clearing opening an image does, with nothing opened afterwards. The sheet that was there can
    /// be brought back from the toast, which is the difference between a reset and a loss.
    /// </summary>
    internal void NewTarget()
    {
        Leaving(() =>
        {
            var was = session.State;
            session.Load(MarkingState.Empty);
            grey = null;
            valueImage = null;
            metadata = null;
            artwork = null;
            statedSize = null;
            detectedState = null;
            plotDefinition = null;
            registrationResidual = null;
            pendingDetection = null;
            currentSession = null;
            savedMarking = null;
            calibreConfirmed = false;
            sheetChooser.IsVisible = false;
            roundsFired.Text = "";
            calibreBox.Text = "";
            shotDistance.Text = "";
            SetAnalysing(false);
            canvas.SetImage(null, null);
            status.Text = "New target. Open a photograph or scan when you are ready.";
            DiagnosticLog.Info("target.new");
            Refresh();
            toaster.Show(new Confirmation("New target: the sheet is cleared.", was.ImagePath is null && was.Shots.Count == 0 ? null : () =>
            {
                session.Load(was);
                Refresh();
                status.Text = "The last target is back. Its image is not reopened, so open it again to mark on it.";
            }));
        });
    }
}
