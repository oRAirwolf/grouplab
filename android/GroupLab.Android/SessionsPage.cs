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
        column.Children.Add(Screens.Heading("Sessions"));
        var said = Screens.Line("");
        column.Children.Add(Screens.Choice("Open a session file", () => _ = OpenFile(said)));
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

        var units = App.Settings.LoadUnits();
        foreach (var s in saved.OrderByDescending(s => s.CreatedUtc, StringComparer.Ordinal))
        {
            string words = $"{s.ShotDate ?? s.CreatedUtc[..10]}, {s.SheetName}: {s.ShotCount} shots"
                + (s.MeanRadiusInches is { } mr ? $", mean radius {units.Length(mr)}" : "");
            column.Children.Add(Screens.Choice(words, () => Open(s.Id)));
        }

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
