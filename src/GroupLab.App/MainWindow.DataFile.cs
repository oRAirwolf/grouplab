using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Records;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 307: Settings, Your data. "Export all my data" writes every session with its pictures and marks, the rifles,
/// barrels and loads, the designed sheets, and the printers and settings that travel, to one file any GroupLab reads (<see cref="DataExport"/>);
/// "Import data" reads one, says what it would add and what differs here, and writes nothing until the person says Import.
/// </summary>
public sealed partial class MainWindow
{
    private readonly StackPanel dataPanel = new() { Spacing = Tokens.Space8 };
    private ImportPlan? pendingImport;

    /// <summary>What the export screen says about privacy, the same words on the phone (entry 307 section 4).</summary>
    internal const string DataPrivacyWords = "The file holds every session with GroupLab's own picture of the sheet and its marks, your rifles, barrels and loads, your designed sheets, and your printers and units. It follows GroupLab's rule for photographs: no location and no metadata, and not where the original photograph is on this computer. It stays with you: nothing is sent anywhere.";

    private void BuildDataSettings(StackPanel column)
    {
        column.Children.Add(Ruled("Your data"));
        column.Children.Add(Line(DataPrivacyWords));
        column.Children.Add(Row(Button("Export all my data…", () => _ = ExportAllDialog()), Button("Import data…", () => _ = ImportDialog())));
        column.Children.Add(dataPanel);
    }

    private async Task ExportAllDialog()
    {
        DiagnosticLog.Info("dialog.open", ("dialog", "export-all"));
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export all my data",
            SuggestedFileName = "grouplab-data-" + DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) + DataExport.Extension,
            DefaultExtension = DataExport.Extension.TrimStart('.'),
        });
        DiagnosticLog.Info("dialog.result", ("dialog", "export-all"), ("chosen", file is not null));
        if (file?.TryGetLocalPath() is { } path)
        {
            ExportAll(path);
        }
    }

    /// <summary>Writes everything to <paramref name="path"/> and says what it wrote. False with the reason said where it could not.</summary>
    internal bool ExportAll(string path)
    {
        if (sessions is null)
        {
            Failed("There is nothing to export", "There is no database to export on this computer.");
            return false;
        }

        try
        {
            using (var stream = File.Create(path))
            {
                DataExport.Write(stream, sessions, ownSheets.Exported(), settingsStore.LoadAll(), AppInfo.Version, DateTime.UtcNow);
            }

            int count = sessions.List().Count;
            DiagnosticLog.Info("data.export", ("sessions", count), ("bytes", new FileInfo(path).Length));
            toaster.Say($"Exported {count} session{(count == 1 ? "" : "s")} and everything else to {Path.GetFileName(path)}.");
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "data.export", e);
            Failed("The file could not be written", "The file could not be written: " + e.Message);
            return false;
        }
    }

    private async Task ImportDialog()
    {
        DiagnosticLog.Info("dialog.open", ("dialog", "import-data"));
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import data",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("GroupLab data") { Patterns = ["*" + DataExport.Extension] }, FilePickerFileTypes.All],
        });
        DiagnosticLog.Info("dialog.result", ("dialog", "import-data"), ("chosen", files.Count > 0));
        if (files.Count > 0 && files[0].TryGetLocalPath() is { } path)
        {
            PlanImport(path);
        }
    }

    /// <summary>Reads a data file and shows what importing it would do, with Import and Cancel; nothing is written yet.</summary>
    internal ImportPlan? PlanImport(string path)
    {
        dataPanel.Children.Clear();
        pendingImport = null;
        if (sessions is null)
        {
            dataPanel.Children.Add(Line("There is no database on this computer to import into."));
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            var file = DataExport.Read(stream);
            var plan = DataExport.Plan(file, sessions, ownSheets.Exported(), settingsStore.LoadAll());
            pendingImport = plan;
            dataPanel.Children.Add(FieldLabel("Import " + Path.GetFileName(path) + (file.FromApp is { } from ? $", from GroupLab {from}" : "")));
            foreach (string line in plan.Summary())
            {
                dataPanel.Children.Add(Line(line));
            }

            foreach (string conflict in plan.Conflicts)
            {
                dataPanel.Children.Add(Line("• " + conflict));
            }

            dataPanel.Children.Add(Row(Button(plan.Adds == 0 ? "Nothing to import" : "Import", () => ApplyImport()), Button("Cancel", () =>
            {
                pendingImport = null;
                dataPanel.Children.Clear();
                toaster.Say("Nothing was imported.");
            })));
            DiagnosticLog.Info("data.plan", ("adds", plan.Adds), ("conflicts", plan.Conflicts.Count));
            return plan;
        }
        catch (Exception e) when (e is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            dataPanel.Children.Add(Line(e is InvalidDataException ? e.Message : "The file could not be read: " + e.Message));
            DiagnosticLog.Info("data.refused", ("reason", e.GetType().Name));
            return null;
        }
    }

    /// <summary>Writes what the plan showing adds, and nothing else.</summary>
    internal int ApplyImport()
    {
        if (pendingImport is not { } plan || sessions is null)
        {
            return 0;
        }

        int added = DataExport.Apply(plan, sessions);
        int sheets = plan.NewSheets.Count(ownSheets.Import);
        settingsStore.MergeImported(plan.NewSettings);
        book = sessions.LoadBook();
        pendingImport = null;
        dataPanel.Children.Clear();
        FillSessions();
        FillLibrary();
        DiagnosticLog.Info("data.import", ("sessions", added), ("sheets", sheets), ("book", plan.NewBookItems), ("settings", plan.NewSettingsItems));
        toaster.Say($"Imported {added} session{(added == 1 ? "" : "s")}, {plan.NewBookItems} rifles, barrels or loads, {sheets} designed sheets and {plan.NewSettingsItems} printers or settings.");
        return added;
    }
}
