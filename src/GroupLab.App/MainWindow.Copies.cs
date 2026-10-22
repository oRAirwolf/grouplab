using Avalonia.Controls;
using Avalonia.Media;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Records;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 468 section 2.1, Alan's priority: a daily copy of the records, on by default, with "Back up now", "Restore a
/// copy..." and "Open the folder", and a line saying when the last copy was made and its size.
/// </summary>
public partial class MainWindow
{
    private RecordsCopies? recordsCopies;
    private readonly TextBlock copiesLine = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };
    private Task copying = Task.CompletedTask;

    /// <summary>The daily copies, for the headless tests.</summary>
    internal RecordsCopies? RecordsCopies => recordsCopies;

    /// <summary>The line saying when the last copy was made, for the headless tests.</summary>
    internal string CopiesLine => copiesLine.Text ?? "";

    /// <summary>The first open of the day's copy, made away from the window, for the headless tests to wait on.</summary>
    internal Task Copying => copying;

    /// <summary>At the start: today's copy where it is due and the setting is on, made off the window's thread.</summary>
    private void StartCopies()
    {
        if (sessions is null)
        {
            return;
        }

        recordsCopies = new RecordsCopies(settingsStore.DatabasePath, () => DateTime.Now);
        ShowCopiesLine();
        if (!settingsStore.LoadDailyCopies())
        {
            return;
        }

        var copies = recordsCopies;
        copying = Task.Run(() =>
        {
            try
            {
                return copies.MakeIfDue();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or Microsoft.Data.Sqlite.SqliteException)
            {
                // A folder gone while the copy was being made: nothing is lost, and the next open tries again.
                DiagnosticLog.Exception(LogLevel.Warn, "copies.daily", e);
                return new CopyResult(null, e.Message);
            }
        }).ContinueWith(done =>
        {
            if (done.Result.Made)
            {
                DiagnosticLog.Info("copies.daily", ("bytes", done.Result.Copy!.Bytes));
            }
            else if (!done.Result.Said.StartsWith("Today", StringComparison.Ordinal))
            {
                DiagnosticLog.Info("copies.daily", ("made", false));
            }

            ShowCopiesLine();
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void ShowCopiesLine() => copiesLine.Text = recordsCopies is null
        ? "There are no records on this computer to copy."
        : (settingsStore.LoadDailyCopies() ? "" : "Daily copies are off. ") + CopiesWords.Last(recordsCopies.Newest);

    private void BuildCopiesCard(StackPanel body, SettingsPages.Page page)
    {
        var (inside, card) = SettingsCard(body, page, CopiesWords.Heading, "backup back up copy copies restore daily records database");
        var keep = new CheckBox { IsChecked = settingsStore.LoadDailyCopies() };
        keep.IsCheckedChanged += (_, _) =>
        {
            settingsStore.SaveDailyCopies(keep.IsChecked == true);
            DiagnosticLog.Info("copies.setting", ("on", keep.IsChecked == true));
            ShowCopiesLine();
        };
        SettingRow(inside, card, CopiesWords.Keep, CopiesWords.KeepSays, keep, "backup back up daily");
        page.OnPutBack(() => keep.IsChecked = true);
        inside.Children.Add(copiesLine);
        inside.Children.Add(Row(
            Button(CopiesWords.BackUpNow, BackUpNow),
            Button(CopiesWords.Restore, AskWhichCopy),
            Button(CopiesWords.OpenFolder, () =>
            {
                if (recordsCopies is { } copies)
                {
                    Directory.CreateDirectory(copies.Folder);
                    GroupLab.Core.Updates.TheOutsideWorld.Current.OpenFolder(copies.Folder);
                }
            })));
    }

    /// <summary>"Back up now": a copy made at once, and the line says so or says why not.</summary>
    internal void BackUpNow()
    {
        if (recordsCopies is not { } copies)
        {
            Failed("There is nothing to copy", "There are no records on this computer to copy.");
            return;
        }

        var result = copies.Make();
        DiagnosticLog.Info("copies.now", ("made", result.Made));
        ShowCopiesLine();
        if (result.Made)
        {
            toaster.Say(result.Said);
        }
        else
        {
            Failed("No copy was made", result.Said);
        }
    }

    /// <summary>"Restore a copy...": the copies with their day and how many sessions each holds, then the one question.</summary>
    private void AskWhichCopy()
    {
        var listed = recordsCopies?.List(countSessions: true) ?? [];
        if (listed.Count == 0)
        {
            ShowProblem(CopiesWords.Restore.TrimEnd('…'), CopiesWords.NoneToRestore);
            return;
        }

        ShowProblem(CopiesWords.Restore.TrimEnd('…'), CopiesWords.WhichCopy,
            [.. listed.Select(copy => (CopiesWords.Choice(copy), (Action)(() => Avalonia.Threading.Dispatcher.UIThread.Post(() => AskToRestore(copy))))),
            (CopiesWords.LeaveIt, () => { })]);
    }

    private void AskToRestore(RecordsCopy copy) =>
        ShowProblem(CopiesWords.RestoreIt + "?", CopiesWords.Ask(copy), (CopiesWords.RestoreIt, () => RestoreCopy(copy)), (CopiesWords.LeaveIt, () => { }));

    /// <summary>Puts <paramref name="copy"/> in place of the records, the records as they are now copied first, and opens them afresh.</summary>
    internal bool RestoreCopy(RecordsCopy copy)
    {
        if (recordsCopies is not { } copies || sessions is null)
        {
            return false;
        }

        var result = copies.Restore(copy);
        DiagnosticLog.Info("copies.restore", ("restored", result.Made));
        ShowCopiesLine();
        if (!result.Made)
        {
            Failed("Nothing was restored", result.Said);
            return false;
        }

        // GroupLab opens the restored records as a fresh start does: the record book, the sessions and the library read again.
        book = sessions.LoadBook();
        FillSessions();
        FillLibrary();
        toaster.Say(result.Said);
        return true;
    }
}
