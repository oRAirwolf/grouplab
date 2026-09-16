using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.App.Diagnostics;
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
            // An unreadable settings file is the same as none: fall back to the region's default, and say so in the log (entry 41 section 3).
            DiagnosticLog.Exception(LogLevel.Warn, "settings.read", ex, ("setting", "units"), ("fallback", "the region's default"));
        }

        string? region = null;
        try
        {
            region = RegionInfo.CurrentRegion.TwoLetterISORegionName;
        }
        catch (ArgumentException ex)
        {
            // No region is known, so the default is metric.
            DiagnosticLog.Exception(LogLevel.Warn, "settings.region", ex, ("fallback", "metric"));
        }

        return UnitSettings.ForRegion(region);
    }

    /// <summary>Remembers the units. Returns false if the file could not be written, in which case the choice lasts until the application closes.</summary>
    public bool SaveUnits(UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(units);
        return Save(file =>
        {
            file["linear"] = units.Linear.ToString();
            file["angular"] = units.Angular.ToString();
            file["distance"] = units.Distance.ToString();
        });
    }

    /// <summary>The remembered theme, NOTES-FROM-PLANNING.md entry 42 section 2: dark, light, or following the system, which is the default.</summary>
    public ThemeChoice LoadTheme()
    {
        try
        {
            if (File.Exists(Path) && JsonNode.Parse(File.ReadAllText(Path)) is JsonObject file
                && Enum.TryParse((string?)file["theme"], out ThemeChoice theme) && Enum.IsDefined(theme))
            {
                return theme;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // An unreadable settings file is the same as none: follow the system.
            DiagnosticLog.Exception(LogLevel.Warn, "settings.read", ex, ("setting", "theme"), ("fallback", "follow system"));
        }

        return ThemeChoice.System;
    }

    /// <summary>Remembers the theme. Returns false if the file could not be written, in which case the choice lasts until the application closes.</summary>
    public bool SaveTheme(ThemeChoice theme) => Save(file => file["theme"] = theme.ToString());

    /// <summary>Whether DEBUG lines are logged, NOTES-FROM-PLANNING.md entry 41 section 3. Off unless chosen.</summary>
    public bool LoadVerbose() => Read(file => file["verboseLogging"]?.GetValueKind() == JsonValueKind.True);

    public bool SaveVerbose(bool verbose) => Save(file => file["verboseLogging"] = verbose);

    /// <summary>
    /// Whether the statistics panel's further figures are open, DESIGN.md section 19: reference material lives one click away in a panel
    /// that remembers it was opened (NOTES-FROM-PLANNING.md entry 73 section 7). Closed unless a person opened it.
    /// </summary>
    public bool LoadMoreFigures() => Read(file => file["moreFiguresOpen"]?.GetValueKind() == JsonValueKind.True);

    public bool SaveMoreFigures(bool open) => Save(file => file["moreFiguresOpen"] = open);

    /// <summary>
    /// Where a crash report is sent, entry 41 section 7. Empty unless configured, so a fork of GroupLab never posts to anybody's server and
    /// the Send button stays hidden; saving a report to disk works either way.
    /// </summary>
    public string LoadCrashReportUrl() => Read(file => (string?)file["crashReportUrl"]) ?? "";

    /// <summary>Reads one setting, or null when the file is missing or unreadable.</summary>
    private T? Read<T>(Func<JsonObject, T?> get)
    {
        try
        {
            return File.Exists(Path) && JsonNode.Parse(File.ReadAllText(Path)) is JsonObject file ? get(file) : default;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or FormatException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "settings.read", ex);
            return default;
        }
    }

    /// <summary>Writes one setting into the file, keeping every other setting already in it.</summary>
    private bool Save(Action<JsonObject> set)
    {
        try
        {
            JsonObject file;
            try
            {
                file = File.Exists(Path) && JsonNode.Parse(File.ReadAllText(Path)) is JsonObject existing ? existing : new JsonObject();
            }
            catch (JsonException ex)
            {
                DiagnosticLog.Exception(LogLevel.Warn, "settings.read", ex, ("fallback", "a new settings file"));
                file = new JsonObject();
            }

            set(file);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            File.WriteAllText(Path, file.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "settings.write", ex);
            return false;
        }
    }
}

/// <summary>The theme the window uses, NOTES-FROM-PLANNING.md entry 42 section 2. High contrast is DESIGN.md section 19's fourth theme, and later work.</summary>
public enum ThemeChoice
{
    /// <summary>Dark or light as the operating system is set.</summary>
    System,

    Dark,

    Light,
}
