using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Records;
using GroupLab.Core.Rendering;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 307 on the phone: Settings, Your data. Export all my data writes the same file the desktop does and hands it
/// to the share sheet, where it can be saved to Files or sent anywhere; Import data opens the file picker, says what importing would add and
/// what differs here, and writes nothing until Import is pressed. A file opened with GroupLab from another app comes here too.
/// </summary>
internal sealed class DataSection
{
    private readonly AppSettingsStore settings;
    private readonly StackPanel panel = new() { Spacing = 8 };
    private ImportPlan? pending;

    public DataSection(AppSettingsStore settings) => this.settings = settings;

    /// <summary>The section showing in Settings, which a file opened from another app is handed to.</summary>
    internal static DataSection? Current { get; private set; }

    /// <summary>A data file opened with GroupLab from another app, waiting for Settings to show it.</summary>
    internal static string? Waiting { get; set; }

    /// <summary>Designed sheets on the phone, kept so a file from the computer comes back whole when the phone exports again.</summary>
    internal static OwnSheets Sheets => new(Path.Combine(PhoneAnalysis.Files, "own-sheets"));

    /// <summary>The section's controls, for Settings.</summary>
    public IEnumerable<Control> Controls()
    {
        Current = this;
        yield return Screens.Heading("Your data");
        yield return Screens.Dim("Every session with GroupLab's own picture of the sheet and its marks, your rifles, barrels and loads, your designed sheets, and your printers and units, in one file any GroupLab can import. It follows GroupLab's rule for photographs: no location and no metadata, and not where the original photograph is on this phone. It stays with you: nothing is sent anywhere.");
        yield return Screens.Choice("Export all my data", () => Export());
        yield return Screens.Choice("Import data", () => _ = Pick());
        yield return panel;
        if (Waiting is { } path)
        {
            Waiting = null;
            Plan(path);
        }
    }

    /// <summary>Writes everything to a file in the cache and hands it to the share sheet; the path, for the tests.</summary>
    internal string? Export()
    {
        panel.Children.Clear();
        string path = Path.Combine(Phone.Platform.CacheFolder, "grouplab-data-" + DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) + DataExport.Extension);
        try
        {
            using (var stream = File.Create(path))
            {
                DataExport.Write(stream, PhoneAnalysis.Store(), Sheets.Exported(), settings.LoadAll(), AppInfo.Version, DateTime.UtcNow);
            }

            DiagnosticLog.Info("data.export", ("bytes", new FileInfo(path).Length), ("phone", true));
            if (Phone.Platform.ShareFile(path, "application/octet-stream", "GroupLab data") is { } said)
            {
                panel.Children.Add(Screens.Line(said));
            }

            return path;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "data.export", e);
            panel.Children.Add(Screens.Line("The file could not be written: " + e.Message));
            return null;
        }
    }

    private async Task Pick()
    {
        if (TopLevel.GetTopLevel(panel)?.StorageProvider is not { } storage)
        {
            return;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Import data", AllowMultiple = false });
        if (files.Count == 0)
        {
            return;
        }

        // The picker hands a content address; the file is copied into the cache and read from there.
        string copy = Path.Combine(Phone.Platform.CacheFolder, "import" + DataExport.Extension);
        await using (var from = await files[0].OpenReadAsync())
        await using (var to = File.Create(copy))
        {
            await from.CopyToAsync(to);
        }

        Plan(copy);
    }

    /// <summary>Reads a data file and shows what importing it would do, with Import and Cancel; nothing is written yet.</summary>
    internal ImportPlan? Plan(string path)
    {
        panel.Children.Clear();
        pending = null;
        try
        {
            using var stream = File.OpenRead(path);
            var file = DataExport.Read(stream);
            var plan = DataExport.Plan(file, PhoneAnalysis.Store(), Sheets.Exported(), settings.LoadAll());
            pending = plan;
            var card = new StackPanel { Spacing = 6 };
            card.Children.Add(Screens.Line(file.FromApp is { } from ? $"From GroupLab {from}:" : "This file holds:"));
            foreach (string line in plan.Summary().Concat(plan.Conflicts.Select(c => "• " + c)))
            {
                card.Children.Add(Screens.Dim(line));
            }

            panel.Children.Add(Screens.Card(card));
            panel.Children.Add(Screens.Primary(plan.Adds == 0 ? "Nothing to import" : "Import", () => Apply()));
            panel.Children.Add(Screens.Choice("Cancel", () =>
            {
                pending = null;
                panel.Children.Clear();
                panel.Children.Add(Screens.Line("Nothing was imported."));
            }));
            DiagnosticLog.Info("data.plan", ("adds", plan.Adds), ("conflicts", plan.Conflicts.Count), ("phone", true));
            return plan;
        }
        catch (Exception e) when (e is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            panel.Children.Add(Screens.Line(e is InvalidDataException ? e.Message : "The file could not be read: " + e.Message));
            return null;
        }
    }

    /// <summary>Writes what the plan showing adds, and nothing else.</summary>
    internal int Apply()
    {
        if (pending is not { } plan)
        {
            return 0;
        }

        int added = DataExport.Apply(plan, PhoneAnalysis.Store());
        var sheets = Sheets;
        int designed = plan.NewSheets.Count(sheets.Import);
        settings.MergeImported(plan.NewSettings);
        pending = null;
        panel.Children.Clear();
        panel.Children.Add(Screens.Line($"Imported {added} session{(added == 1 ? "" : "s")}, {plan.NewBookItems} rifles, barrels or loads, {designed} designed sheets and {plan.NewSettingsItems} printers or settings."));
        DiagnosticLog.Info("data.import", ("sessions", added), ("phone", true));
        return added;
    }
}
