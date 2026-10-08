using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;

namespace GroupLab.App;

/// <summary>
/// Knowing the target is saved, NOTES-FROM-PLANNING.md entry 279 section 3 (Unholy: nothing says when a target is saved, or where) and
/// entry 281 section 2, Alan's choice: A, with a setting for B.
/// <list type="bullet">
/// <item>A, the default: the target is saved by itself as soon as it is changed or accepted, and the status bar says "Saved 12 seconds ago in
/// grouplab.db", "Show in folder" and "safe to close".</item>
/// <item>B, in Settings under Saving: a Save button, "Not saved yet" until it is pressed, and the question on leaving a target with
/// unsaved work, which is the one entry 140 already asks.</item>
/// </list>
/// Before this, a session was saved only by Accept and analyze, and nothing on the screen said so.
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>How long after the last change a target saves itself, so a run of edits saves once.</summary>
    private static readonly TimeSpan SaveAfter = TimeSpan.FromSeconds(1.5);

    private readonly TextBlock savedWords = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, Tokens.Space8, 0) };
    private Button? saveNow;
    private DispatcherTimer? saveSoon;
    private DispatcherTimer? savedClock;
    private DateTime? savedAtUtc;

    /// <summary>Whether the person chose B, saving with a button; A, saving by itself, otherwise.</summary>
    private bool SaveByHand => settingsStore.LoadSaveByHand();

    /// <summary>Whether a save is waiting to happen by itself, for the headless tests, whose timers do not run.</summary>
    internal bool SaveIsWaiting => saveSoon?.IsEnabled == true;

    /// <summary>Does now the save that is waiting, as its timer would, for the headless tests.</summary>
    internal void SaveWaitingNow()
    {
        saveSoon?.Stop();
        if (HasUnsavedWork)
        {
            SaveSession();
        }

        ShowSaved();
    }

    /// <summary>The saved line, for the headless tests.</summary>
    internal string SavedText => savedWords.Text ?? "";

    private bool closingAnswered;

    /// <summary>
    /// Closing the window: A saves what is waiting, so nothing is lost to the second and a half before it would have saved by itself; B asks
    /// the same question as leaving a target with unsaved work, and closes once it is answered.
    /// </summary>
    private void SavingOnClose(WindowClosingEventArgs e)
    {
        // Several targets open at once: the other tabs with unsaved marks are asked about first, one at a time.
        if (!closingAnswered && AskAboutOtherTabsOnClose())
        {
            e.Cancel = true;
            return;
        }

        if (closingAnswered || !HasUnsavedWork || sessions is null)
        {
            return;
        }

        if (!SaveByHand)
        {
            if (currentSession is not null || session.CanUndo)
            {
                SaveSession();
            }

            return;
        }

        e.Cancel = true;
        Leaving(() =>
        {
            closingAnswered = true;
            Close();
        });
    }

    private Control BuildSavedLine()
    {
        Closing += (_, e) => SavingOnClose(e);
        saveNow = Button("Save", () =>
        {
            SaveSession();
            ShowSaved();
        });
        var show = Button("Show in folder", () =>
        {
            if (settingsStore.DatabasePath is { } path)
            {
                CrashReporter.Reveal(path);
            }
        });
        ToolTip.SetTip(show, "The folder that holds grouplab.db, where every session is kept");
        savedClock = new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.Background, (_, _) => ShowSaved());
        savedClock.Start();
        return new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space4, Children = { savedWords, saveNow, show } };
    }

    /// <summary>After every change: A saves soon by itself once the target has been changed or accepted; B only says it is not saved.</summary>
    private void SavingAfterChange()
    {
        if (!SaveByHand && sessions is not null && HasUnsavedWork && (currentSession is not null || session.CanUndo))
        {
            saveSoon ??= new DispatcherTimer(SaveAfter, DispatcherPriority.Background, (_, _) =>
            {
                saveSoon!.Stop();
                if (HasUnsavedWork)
                {
                    SaveSession();
                    DiagnosticLog.Info("session.autosave", ("session", currentSession));
                }

                ShowSaved();
            });
            saveSoon.Stop();
            saveSoon.Start();
        }

        ShowSaved();
    }

    /// <summary>The line in the status bar: saved and when, or not yet, and whether it is safe to close.</summary>
    private void ShowSaved()
    {
        if (saveNow is not null)
        {
            saveNow.IsVisible = SaveByHand && session.State.Shots.Count > 0;
        }

        if (session.State.Shots.Count == 0)
        {
            savedWords.Text = "";
            return;
        }

        if (HasUnsavedWork || savedAtUtc is null)
        {
            savedWords.Text = SaveByHand ? "Not saved yet" : "Not saved yet: it saves by itself once you change or accept it";
            return;
        }

        var ago = DateTime.UtcNow - savedAtUtc.Value;
        string when = ago.TotalSeconds < 60 ? string.Create(CultureInfo.CurrentCulture, $"{Math.Max(1, (int)ago.TotalSeconds)} seconds ago")
            : ago.TotalMinutes < 60 ? string.Create(CultureInfo.CurrentCulture, $"{(int)ago.TotalMinutes} minutes ago")
            : savedAtUtc.Value.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture);
        savedWords.Text = $"Saved {when} in grouplab.db · safe to close";
    }
}
