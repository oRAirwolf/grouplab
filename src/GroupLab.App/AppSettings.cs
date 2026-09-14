using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.App;

/// <summary>
/// The application's remembered settings, NOTES-FROM-PLANNING.md entry 25 section 1: the unit choice, defaulted from the system's region
/// on first run and remembered once made, in a small JSON file of its own. Nothing in a marking depends on it.
/// </summary>
public sealed class AppSettingsStore(string path)
{
    /// <summary>The settings file in the user's application data folder.</summary>
    public static AppSettingsStore Default { get; } = new(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GroupLab", "settings.json"));

    public string Path { get; } = path;

    /// <summary>The remembered units, or on first run the default for the system's region.</summary>
    public UnitSettings LoadUnits()
    {
        try
        {
            if (File.Exists(Path) && JsonNode.Parse(File.ReadAllText(Path)) is JsonObject file
                && Enum.TryParse((string?)file["linear"], out LinearUnit linear) && Enum.IsDefined(linear)
                && Enum.TryParse((string?)file["angular"], out AngularUnit angular) && UnitSettings.AngularChoices.Contains(angular)
                && Enum.TryParse((string?)file["distance"], out DistanceUnit distance) && Enum.IsDefined(distance))
            {
                return new UnitSettings(linear, angular, distance);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // An unreadable settings file is the same as none: fall back to the region's default.
        }

        string? region = null;
        try
        {
            region = RegionInfo.CurrentRegion.TwoLetterISORegionName;
        }
        catch (ArgumentException)
        {
            // No region is known, so the default is metric.
        }

        return UnitSettings.ForRegion(region);
    }

    /// <summary>Remembers the units. Returns false if the file could not be written, in which case the choice lasts until the application closes.</summary>
    public bool SaveUnits(UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(units);
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            var file = new JsonObject { ["linear"] = units.Linear.ToString(), ["angular"] = units.Angular.ToString(), ["distance"] = units.Distance.ToString() };
            File.WriteAllText(Path, file.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
