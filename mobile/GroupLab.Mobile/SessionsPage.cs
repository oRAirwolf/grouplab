using System.Globalization;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A4: every session saved on this phone, newest first, from the desktop's own database. A tap opens
/// its result again from the marking as it was saved, against the sheet saved with it.
/// </summary>
public sealed class SessionsPage : UserControl
{
    public SessionsPage()
    {
        Content = List();
    }

    /// <summary>
    /// A result opened here is one of the open targets (several targets open at once, Alan 2026-10-07), shown on this place again when it
    /// is switched to.
    /// </summary>
    private ResultView Opened(ResultView view)
    {
        Shell.Current?.OpenTarget(view, Shell.Place.Sessions);
        return view;
    }

    /// <summary>An open target switched to, shown here as it was left.</summary>
    internal void ShowOpen(ResultView view) => Content = Screens.Detach(view);

    /// <summary>
    /// Back to the list of sessions from a result opened here. An open target may be showing on a Sessions page made since it was opened,
    /// after a switch, so the list goes on the page showing it.
    /// </summary>
    private void ToList()
    {
        var page = TopLevel.GetTopLevel(this) is null && Shell.Current?.PageContent is SessionsPage showing ? showing : this;
        page.Content = page.List();
    }

    private Control List()
    {
        var column = new StackPanel { Spacing = 8 };
        column.Children.Add(Screens.Title("Sessions"));

        // Entry 463 section 2: a session changed on two devices waits here to be chosen between, banner first and its row tagged.
        // Entry 498 section 3: a pooled set changed on two devices waits the same way, its row tagged by the pool's row id.
        IReadOnlyList<(SyncConflict Conflict, SessionRecord? Here, SessionRecord There)> toChoose;
        IReadOnlyList<(SyncConflict Conflict, PoolRecord? Here, PoolRecord There)> poolsToChoose;
        // Entry 506 section 2: a printer with two checks waiting is counted in the same banner, and Choose opens it last.
        int printersToChoose;
        long? firstPrinter;
        try
        {
            toChoose = PhoneSync.ToChoose();
            poolsToChoose = PhoneSync.PoolsToChoose();
            var printers = PhoneSync.PrintersToChoose();
            printersToChoose = printers.Count;
            firstPrinter = printers.Count > 0 ? printers[0].Conflict.Id : null;
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            toChoose = [];
            poolsToChoose = [];
            printersToChoose = 0;
            firstPrinter = null;
        }

        if (toChoose.Count + poolsToChoose.Count + printersToChoose > 0)
        {
            long first = toChoose.Count > 0 ? toChoose[0].Conflict.Id : poolsToChoose.Count > 0 ? poolsToChoose[0].Conflict.Id : firstPrinter!.Value;
            column.Children.Add(new Border
            {
                Child = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        Screens.Heading(GroupLab.Core.Sync.SyncScreens.ToChooseBanner(toChoose.Count, poolsToChoose.Count, printersToChoose)),
                        Screens.Primary("Choose", () => PhoneSync.ShowConflict(this, first, ToList)).Id("sessions-sync-choose"),
                    },
                },
                Classes = { PhoneStyles.Card, PhoneStyles.Warn },
            }.Id("sessions-sync-banner"));
        }

        var twoVersions = toChoose.Where(w => w.Here is not null).Select(w => w.Here!.Id)
            .Concat(poolsToChoose.Where(w => w.Here is not null).Select(w => SessionPools.RowId(w.Here!.Id))).ToHashSet();
        var said = Screens.Line("");
        column.Children.Add(Screens.Choice("Open a session file", () => _ = OpenFile(said)).Id("sessions-open-file"));
        column.Children.Add(Screens.Choice("Import shots from a CSV file", () => _ = ImportCsv(said)).Id("sessions-import-csv"));
        column.Children.Add(said);
        IReadOnlyList<SessionSummary> saved;
        try
        {
            // Entry 495 section 2: a saved pool is one row among the sessions, dated by its newest sheet.
            var store = PhoneAnalysis.Store();
            saved = SessionPools.Merged(store.List(), SessionPools.Rows(store, Phone.Settings.LoadUnits()).Select(p => p.Row));
        }
        catch (Microsoft.Data.Sqlite.SqliteException e)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "session.list", e);
            saved = [];
        }

        if (saved.Count == 0)
        {
            column.Children.Add(Screens.Line("Each target you analyze on this phone is kept here, to open again. There are none yet."));
        }

        // Entry 246, look B: the sessions as rows on one card. Entry 295 section 3: each named by what tells it from the others, its load, its
        // date and where needed its time, and beneath it the sheet's name, the shots and the mean radius.
        var units = Phone.Settings.LoadUnits();
        var rows = new StackPanel();
        foreach (var (s, name) in Named(saved))
        {
            string detail = Detail(s, name, units);
            long id = s.Id;
            rows.Children.Add(Screens.Row(name.Name, twoVersions.Contains(id) ? GroupLab.Core.Sync.SyncScreens.TwoVersions + " · " + detail : detail,
                () => Open(id, said), explain: false));
        }

        if (rows.Children.Count > 0)
        {
            column.Children.Add(new Border { Child = rows, Classes = { PhoneStyles.Card } }.Id("sessions-list"));
        }

        // Entry 259 screen 4: two or more sessions compared, one figure at a time.
        if (saved.Count >= 2)
        {
            column.Children.Add(Screens.Choice("Compare loads", () => Content = Choose(saved, [])).Id("sessions-compare-loads"));
        }

        return Screens.Page(column);
    }

    /// <summary>The sessions to compare, each a box to tick; Compare once two or more are ticked.</summary>
    private static IEnumerable<(SessionSummary Session, SessionName Name)> Named(IReadOnlyList<SessionSummary> saved)
    {
        var newest = saved.OrderByDescending(s => s.CreatedUtc, StringComparer.Ordinal).ToList();
        // Entry 495: a pool is named a pooled set of N sheets, with its newest sheet's date; a session as entry 295 names it.
        return newest.Zip(SessionNames.For(newest)).Select(x => x.First.Id < 0
            ? (x.First, new SessionName(SessionPools.Called(x.First), x.Second.When, x.First.SheetName.Contains(", ", StringComparison.Ordinal) ? x.First.SheetName[(x.First.SheetName.IndexOf(", ", StringComparison.Ordinal) + 2)..] : "pictures"))
            : x);
    }

    /// <summary>The line beneath a session's name: the date where the name does not already carry it, the sheet, the shots, the mean radius.</summary>
    internal static string Detail(SessionSummary s, SessionName name, UnitSettings units) =>
        string.Join(" · ", new[]
        {
            name.Name.Contains(name.When, StringComparison.Ordinal) ? null : name.When,
            name.Sheet,
            s.ShotCount.ToString(CultureInfo.InvariantCulture) + " shots",
            s.MeanRadiusInches is { } mr ? "mean radius " + units.Length(mr) : null,
        }.OfType<string>());

    private Control Choose(IReadOnlyList<SessionSummary> saved, HashSet<long> chosen)
    {
        var units = Phone.Settings.LoadUnits();
        var column = new StackPanel { Spacing = 8, Children = { Screens.Title("Compare loads"), Screens.Line("Choose two or more sessions.") } };
        var compare = Screens.Primary("Compare", () =>
        {
            // Entry 495 section 5: a chosen pool is one group beside the sessions.
            var records = SessionPools.Chosen(PhoneAnalysis.Store(), units, chosen);
            Content = new ComparePage(records, units, () => Content = Choose(saved, chosen), () => Content = List());
        });
        compare.IsEnabled = chosen.Count >= 2;
        var rows = new StackPanel { Spacing = 4 };
        foreach (var (s, name) in Named(saved))
        {
            var box = new CheckBox
            {
                IsChecked = chosen.Contains(s.Id),
                MinHeight = Screens.Touch,
                Content = new StackPanel { Children = { Screens.Line(name.Name), Screens.Quiet(Detail(s, name, units)) } },
            };
            box.IsCheckedChanged += (_, _) =>
            {
                if (box.IsChecked == true)
                {
                    chosen.Add(s.Id);
                }
                else
                {
                    chosen.Remove(s.Id);
                }

                compare.IsEnabled = chosen.Count >= 2;
            };
            rows.Children.Add(box);
        }

        column.Children.Add(new Border { Child = rows, Classes = { PhoneStyles.Card } });
        column.Children.Add(compare);
        column.Children.Add(Screens.BackButton("Back to Sessions", () => Content = List()).Id("sessions-back"));
        return Screens.Page(column);
    }

    /// <summary>Entry 219 item A5: a session file from the phone's files, a share, email or USB, opened as a session of its own.</summary>
    private async Task OpenFile(TextBlock said)
    {
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { } storage)
        {
            return;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Open a GroupLab session file", AllowMultiple = false });
        if (files.Count == 0)
        {
            return;
        }

        await using var from = await files[0].OpenReadAsync();
        using var copy = new MemoryStream();
        await from.CopyToAsync(copy);
        copy.Position = 0;
        var units = Phone.Settings.LoadUnits();
        // Entry 495: a pooled set's file has the same ending; it brings its sheets in and opens the pool.
        if (PoolPackage.IsPool(copy))
        {
            var (pool, poolSaid) = PhonePools.OpenFile(copy);
            said.Text = poolSaid ?? "";
            if (pool is not null)
            {
                Content = Opened(new ResultView(pool, new ShotSetup(pool.State.Calibre, pool.State.ShotDistanceInches), units, ToList));
            }

            return;
        }

        var (result, why) = SessionFiles.Open(copy, units);
        if (result is null)
        {
            said.Text = why ?? "";
            return;
        }

        Content = Opened(new ResultView(result, new ShotSetup(result.State.Calibre, result.State.ShotDistanceInches), units, ToList));
    }

    /// <summary>Entry 278 section 2: shots from any program's CSV, through GroupLab's guesses at what each column is.</summary>
    private async Task ImportCsv(TextBlock said)
    {
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { } storage)
        {
            return;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Import shots from one or more CSV files", AllowMultiple = true });
        if (files.Count == 0)
        {
            return;
        }

        // Entry 374 section 4: several files with the same columns are put together as one group, as on the computer.
        var read = new List<(string Name, CsvTable Table)>();
        foreach (var file in files)
        {
            try
            {
                await using var from = await file.OpenReadAsync();
                using var reader = new StreamReader(from);
                read.Add((file.Name, ShotCsv.Read(await reader.ReadToEndAsync())));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException)
            {
                ProblemSheet.Stop(said, said, "That file could not be read", $"{file.Name} could not be read as CSV: " + e.Message);
                return;
            }
        }

        var (table, refused) = ShotCsv.Pool(read);
        if (table is null)
        {
            ProblemSheet.Stop(said, said, "The files could not be put together", refused!);
            return;
        }

        string name = read.Count == 1 ? read[0].Name : $"{read.Count} files put together";
        var units = Phone.Settings.LoadUnits();
        Content = new CsvImportPage(table, name, () => Content = List(),
            result => Content = Opened(new ResultView(result, new ShotSetup(null, result.State.ShotDistanceInches), units, ToList)));
    }

    private void Open(long id, TextBlock? said = null)
    {
        // Entry 495: a pool's row opens the pooled group from its sheets as they are now.
        if (SessionPools.PoolOf(id) is { } poolId)
        {
            var (pool, poolSaid) = PhonePools.Open(poolId);
            if (pool is null)
            {
                if (said is not null)
                {
                    said.Text = poolSaid;
                }

                return;
            }

            Content = Opened(new ResultView(pool, new ShotSetup(pool.State.Calibre, pool.State.ShotDistanceInches), Phone.Settings.LoadUnits(), ToList));
            return;
        }

        // A session already open is switched to, as it was left, rather than opened twice, where two copies would save over each other.
        if (Shell.Current is { } shell && ReferenceEquals(shell.PageContent, this) && shell.Targets.Find(id) is { } open)
        {
            shell.SwitchTo(open);
            return;
        }

        if (PhoneAnalysis.Store().Get(id) is not { } record)
        {
            return;
        }

        MarkingState state;
        try
        {
            (state, _) = MarkingFile.Read(record.MarkingJson);
        }
        catch (MarkingFileException e)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "session.open", e);
            return;
        }

        // Entry 383: a marking holds its photo's file name only; the photo itself is where this device's record says.
        state = state with { ImagePath = record.ImagePath ?? state.ImagePath };
        var definition = record.DefinitionJson is { } json ? GltdJsonReader.Read(System.Text.Encoding.UTF8.GetBytes(json)).Definition : null;
        DiagnosticLog.Info("session.open", ("session", id.ToString(CultureInfo.InvariantCulture)));
        Content = Opened(new ResultView(new PhoneResult(state, definition, null, id), new ShotSetup(state.Calibre, state.ShotDistanceInches), Phone.Settings.LoadUnits(), ToList));
    }
}
