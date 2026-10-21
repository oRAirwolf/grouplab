using System.Text.Json.Nodes;
using GroupLab.Core.Records;

namespace GroupLab.Core.Sync;

/// <summary>
/// What a device keeps outside its database, which synchronization carries as the export does: the designed sheets, and the settings that
/// travel between devices (<see cref="DataExport.PortableSettings"/>). Where they live is each platform's own.
/// </summary>
public interface ISyncLocal
{
    IReadOnlyList<ExportedSheet> Sheets();

    void SaveSheet(ExportedSheet sheet);

    void DeleteSheet(string file);

    /// <summary>The portable settings that are set here, by key.</summary>
    JsonObject Settings();

    /// <summary>Sets one portable setting, or takes it away where <paramref name="value"/> is null.</summary>
    void SaveSetting(string key, JsonNode? value);
}

/// <summary>Sheets in a folder and settings in one JSON file: the command line's, and the tests'.</summary>
public sealed class FileSyncLocal(string sheetsFolder, string settingsFile) : ISyncLocal
{
    public IReadOnlyList<ExportedSheet> Sheets() =>
        Directory.Exists(sheetsFolder)
            ? [.. Directory.EnumerateFiles(sheetsFolder, "*.json").Order(StringComparer.Ordinal).Select(f => new ExportedSheet(Path.GetFileName(f), File.ReadAllText(f)))]
            : [];

    public void SaveSheet(ExportedSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        Directory.CreateDirectory(sheetsFolder);
        File.WriteAllText(Path.Combine(sheetsFolder, Path.GetFileName(sheet.File)), sheet.Definition);
    }

    public void DeleteSheet(string file) => File.Delete(Path.Combine(sheetsFolder, Path.GetFileName(file)));

    public JsonObject Settings()
    {
        var all = File.Exists(settingsFile) ? JsonNode.Parse(File.ReadAllText(settingsFile)) as JsonObject ?? [] : [];
        var portable = new JsonObject();
        foreach (string key in DataExport.PortableSettings)
        {
            if (all[key] is { } value)
            {
                portable[key] = value.DeepClone();
            }
        }

        return portable;
    }

    public void SaveSetting(string key, JsonNode? value)
    {
        var all = File.Exists(settingsFile) ? JsonNode.Parse(File.ReadAllText(settingsFile)) as JsonObject ?? [] : [];
        if (value is null)
        {
            all.Remove(key);
        }
        else
        {
            all[key] = value.DeepClone();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(settingsFile))!);
        File.WriteAllText(settingsFile, all.ToJsonString());
    }
}
