using System.Globalization;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.Android;

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

    private Control List()
    {
        var column = new StackPanel { Spacing = 8 };
        column.Children.Add(Screens.Title("Sessions"));
        var said = Screens.Line("");
        column.Children.Add(Screens.Choice("Open a session file", () => _ = OpenFile(said)));
        column.Children.Add(Screens.Choice("Import shots from a CSV file", () => _ = ImportCsv(said)));
        column.Children.Add(said);
        IReadOnlyList<SessionSummary> saved;
        try
        {
            saved = PhoneAnalysis.Store().List();
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

        // Entry 246, look B: the sessions as rows on one card, the sheet's name and beneath it the date, the shots and the mean radius.
        var units = App.Settings.LoadUnits();
        var rows = new StackPanel();
        foreach (var s in saved.OrderByDescending(s => s.CreatedUtc, StringComparer.Ordinal))
        {
            string detail = $"{s.ShotDate ?? s.CreatedUtc[..10]} · {s.ShotCount} shots" + (s.MeanRadiusInches is { } mr ? $" · mean radius {units.Length(mr)}" : "");
            rows.Children.Add(Screens.Row(s.SheetName, detail, () => Open(s.Id)));
        }

        if (rows.Children.Count > 0)
        {
            column.Children.Add(new Border { Child = rows, Classes = { PhoneStyles.Card } });
        }

        // Entry 259 screen 4: two or more sessions compared, one figure at a time.
        if (saved.Count >= 2)
        {
            column.Children.Add(Screens.Choice("Compare loads", () => Content = Choose(saved, [])));
        }

        return Screens.Page(column);
    }

    /// <summary>The sessions to compare, each a box to tick; Compare once two or more are ticked.</summary>
    private Control Choose(IReadOnlyList<SessionSummary> saved, HashSet<long> chosen)
    {
        var units = App.Settings.LoadUnits();
        var column = new StackPanel { Spacing = 8, Children = { Screens.Title("Compare loads"), Screens.Line("Choose two or more sessions.") } };
        var compare = Screens.Primary("Compare", () =>
        {
            var store = PhoneAnalysis.Store();
            var records = saved.OrderBy(s => s.CreatedUtc, StringComparer.Ordinal).Where(s => chosen.Contains(s.Id)).Select(s => store.Get(s.Id)).OfType<SessionRecord>().ToList();
            Content = new ComparePage(records, units, () => Content = Choose(saved, chosen));
        });
        compare.IsEnabled = chosen.Count >= 2;
        var rows = new StackPanel { Spacing = 4 };
        foreach (var s in saved.OrderByDescending(s => s.CreatedUtc, StringComparer.Ordinal))
        {
            var box = new CheckBox
            {
                IsChecked = chosen.Contains(s.Id),
                MinHeight = Screens.Touch,
                Content = new StackPanel { Children = { Screens.Line(s.SheetName), Screens.Dim($"{s.ShotDate ?? s.CreatedUtc[..10]} · {s.ShotCount} shots" + (s.MeanRadiusInches is { } mr ? $" · mean radius {units.Length(mr)}" : "")) } },
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
        column.Children.Add(Screens.Choice("Back to Sessions", () => Content = List()));
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
        var units = App.Settings.LoadUnits();
        var (result, why) = SessionFiles.Open(copy, units);
        if (result is null)
        {
            said.Text = why ?? "";
            return;
        }

        Content = new ResultView(result, new ShotSetup(result.State.Calibre, result.State.ShotDistanceInches), units, () => Content = List());
    }

    /// <summary>Entry 278 section 2: shots from any program's CSV, through GroupLab's guesses at what each column is.</summary>
    private async Task ImportCsv(TextBlock said)
    {
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { } storage)
        {
            return;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Import shots from a CSV file", AllowMultiple = false });
        if (files.Count == 0)
        {
            return;
        }

        CsvTable table;
        try
        {
            await using var from = await files[0].OpenReadAsync();
            using var reader = new StreamReader(from);
            table = ShotCsv.Read(await reader.ReadToEndAsync());
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException)
        {
            said.Text = "That file could not be read as CSV: " + e.Message;
            return;
        }

        var units = App.Settings.LoadUnits();
        Content = new CsvImportPage(table, files[0].Name, () => Content = List(),
            result => Content = new ResultView(result, new ShotSetup(null, result.State.ShotDistanceInches), units, () => Content = List()));
    }

    private void Open(long id)
    {
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

        var definition = record.DefinitionJson is { } json ? GltdJsonReader.Read(System.Text.Encoding.UTF8.GetBytes(json)).Definition : null;
        DiagnosticLog.Info("session.open", ("session", id.ToString(CultureInfo.InvariantCulture)));
        Content = new ResultView(new PhoneResult(state, definition, null, id), new ShotSetup(state.Calibre, state.ShotDistanceInches), App.Settings.LoadUnits(), () => Content = List());
    }
}
