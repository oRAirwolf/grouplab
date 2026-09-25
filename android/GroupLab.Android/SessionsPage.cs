using System.Globalization;
using Avalonia.Controls;
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
