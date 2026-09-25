using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;
using GroupLab.Core.Updates;
using GroupLab.Core.Reporting;

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

    /// <summary>
    /// The database beside the settings, NOTES-FROM-PLANNING.md entry 112 section 1: <c>grouplab.db</c> beside <c>settings.json</c>, and for a
    /// settings file of any other name, that name with <c>.db</c>, so each test's settings have a database of their own.
    /// </summary>
    public string DatabasePath => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path) ?? ".",
        System.IO.Path.GetFileNameWithoutExtension(Path) == "settings" ? "grouplab.db" : System.IO.Path.GetFileNameWithoutExtension(Path) + ".db");

    /// <summary>The record book from before the database, <c>records.json</c> beside <c>settings.json</c>, named after any other settings file likewise.</summary>
    public string RecordsPath => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path) ?? ".",
        System.IO.Path.GetFileNameWithoutExtension(Path) == "settings" ? "records.json" : System.IO.Path.GetFileNameWithoutExtension(Path) + ".records.json");

    /// <summary>
    /// The person's own sheets, NOTES-FROM-PLANNING.md entry 112 section 3: a <c>sheets</c> folder beside <c>settings.json</c>, and for a settings
    /// file of any other name, that name with <c>.sheets</c>.
    /// </summary>
    public string SheetsFolder => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path) ?? ".",
        System.IO.Path.GetFileNameWithoutExtension(Path) == "settings" ? "sheets" : System.IO.Path.GetFileNameWithoutExtension(Path) + ".sheets");

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

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 189 section 3: a group's size is shown first as an angle where the distance is known, and this puts the
    /// size on the paper first instead, for a shooter who only ever shoots one distance. Off unless chosen.
    /// </summary>
    public bool LoadSizeOnPaperFirst() => Read(file => (bool?)file["sizeOnPaperFirst"]) ?? false;

    public bool SaveSizeOnPaperFirst(bool first) => Save(file => file["sizeOnPaperFirst"] = first);

    /// <summary>Entry 204 section 1.4: the composite plot's toggles, remembered; <see cref="PlotMarks.Default"/> until one is changed.</summary>
    public PlotMarks LoadPlotMarks() => Read(file => file["plotMarks"] is JsonObject o
        ? new PlotMarks((bool?)o["cep50"] ?? true, (bool?)o["cep90"] ?? true, (bool?)o["cep95"] ?? false, (bool?)o["spread"] ?? true)
        : null) ?? PlotMarks.Default;

    /// <summary>Entry 210 section 2.1: whether the composite plot shows the whole target rather than the group; the group until chosen.</summary>
    public bool LoadPlotWholeTarget() => Read(file => (bool?)file["plotWholeTarget"]) ?? false;

    public bool SavePlotWholeTarget(bool whole) => Save(file => file["plotWholeTarget"] = whole);

    public bool SavePlotMarks(PlotMarks shown) => Save(file => file["plotMarks"] = new JsonObject
    {
        ["cep50"] = shown.Cep50,
        ["cep90"] = shown.Cep90,
        ["cep95"] = shown.Cep95,
        ["spread"] = shown.Spread,
    });

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
    /// Whether sighters are analysed, NOTES-FROM-PLANNING.md entry 105 section 8: off unless a person turns it on. Off, they are found and
    /// matched and then set aside; on, they are a group of their own with their own zero readout and review items.
    /// </summary>
    public bool LoadAnalyseSighters() => Read(file => file["analyseSighters"]?.GetValueKind() == JsonValueKind.True);

    public bool SaveAnalyseSighters(bool on) => Save(file => file["analyseSighters"] = on);

    /// <summary>
    /// Whether the work bar, the stage timeline, is shown, entry 105 section 6. Hidden unless a person showed it: a failed stage is still a
    /// prominent error without it, and Show work lights when a stage has failed or degraded.
    /// </summary>
    public bool LoadShowWork() => Read(file => file["showWork"]?.GetValueKind() == JsonValueKind.True);

    public bool SaveShowWork(bool shown) => Save(file => file["showWork"] = shown);

    /// <summary>
    /// Whether one "why" disclosure is open, NOTES-FROM-PLANNING.md entry 109 section 1: the reasoning behind a figure or a judgement sits one
    /// click away on the item it explains, and each remembers it was opened, as the More figures panel does.
    /// </summary>
    public bool LoadWhyOpen(string item) => Read(file => file["whyOpen"]?[item]?.GetValueKind() == JsonValueKind.True);

    public bool SaveWhyOpen(string item, bool open) => Save(file =>
    {
        if (file["whyOpen"] is not JsonObject opened)
        {
            file["whyOpen"] = opened = new JsonObject();
        }

        opened[item] = open;
    });

    /// <summary>A side column's width as a person dragged it, entry 105 section 1, or null where it was never dragged.</summary>
    public double? LoadColumnWidth(string column) => Read(file => file["columnWidths"]?[column]?.GetValueKind() == JsonValueKind.Number ? (double?)file["columnWidths"]![column]!.GetValue<double>() : null);

    public bool SaveColumnWidth(string column, double width) => Save(file =>
    {
        if (file["columnWidths"] is not JsonObject widths)
        {
            file["columnWidths"] = widths = new JsonObject();
        }

        widths[column] = Math.Round(width);
    });

    /// <summary>
    /// Where a crash report is sent, entry 41 section 7. Empty unless configured, so a fork of GroupLab never posts to anybody's server and
    /// the Send button stays hidden; saving a report to disk works either way.
    /// </summary>
    public string LoadCrashReportUrl() => Read(file => (string?)file["crashReportUrl"]) ?? "";

    /// <summary>
    /// What a person has decided about updating, entry 119 sections 4.2 and 4.5, now kept in the settings file rather than in memory: an
    /// update that closes the application would otherwise forget the train and the skipped version at the moment it matters most.
    /// </summary>
    public UpdatePreferences LoadUpdatePreferences(UpdateTrain fallback) => Read(file =>
    {
        var updates = file["updates"] as JsonObject;
        var train = Enum.TryParse((string?)updates?["train"], out UpdateTrain chosen) && Enum.IsDefined(chosen) ? chosen : fallback;
        var interval = Enum.TryParse((string?)updates?["interval"], out UpdateCheckInterval often) && Enum.IsDefined(often) ? often : UpdateCheckInterval.EveryLaunch;
        var last = DateTimeOffset.TryParse((string?)updates?["lastCheckUtc"], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var when) ? when : (DateTimeOffset?)null;
        return new UpdatePreferences(train, interval, (string?)updates?["skipped"], last);
    }) ?? UpdatePreferences.Default(fallback);

    public bool SaveUpdatePreferences(UpdatePreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        return Save(file =>
        {
            if (file["updates"] is not JsonObject updates)
            {
                file["updates"] = updates = new JsonObject();
            }

            updates["train"] = preferences.Train.ToString();
            updates["interval"] = preferences.Interval.ToString();
            updates["skipped"] = preferences.SkippedVersion;
            updates["lastCheckUtc"] = preferences.LastCheckUtc?.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Which units the analysis page is showing, entry 131 section 3.2: "imperial", "metric", or null to follow the Settings units.
    /// <para>
    /// It is a view of the figures and never touches what is stored. A marking holds inches because that is what it was measured in, and a
    /// person switching the page to centimetres is asking to read it differently, not to change it. Keeping the two apart is what lets the
    /// same session be read either way by two people.
    /// </para>
    /// </summary>
    public string? LoadAnalysisUnits() => Read(file => (string?)file["analysisUnits"]);

    public bool SaveAnalysisUnits(string? which) => Save(file => file["analysisUnits"] = which);

    /// <summary>
    /// What the last update check found, entry 119 section 6.2, in the words the settings page showed at the time. It sits beside the time
    /// of that check in <see cref="LoadUpdatePreferences"/>, so the page can say what happened last time on a fresh launch rather than
    /// holding an empty line open until somebody presses Check now.
    /// </summary>
    public string? LoadLastUpdateResult() => Read(file => (string?)(file["updates"]?["lastResult"]));

    public bool SaveLastUpdateResult(string? said) => Save(file =>
    {
        if (file["updates"] is not JsonObject updates)
        {
            file["updates"] = updates = new JsonObject();
        }

        updates["lastResult"] = said;
    });

    /// <summary>
    /// What one version leaves for the next across an update, entry 123 sections 2.3 and 2.4: the version it was, and the screen the person
    /// was on. The new version says one line about it, goes back to that screen, and clears it, so it is said once and never again.
    /// </summary>
    public (string From, string Screen, DateTimeOffset? At)? LoadHandover() => Read(file =>
        file["afterUpdate"] is JsonObject after && (string?)after["from"] is { Length: > 0 } from
            ? ((string From, string Screen, DateTimeOffset? At)?)(
                from,
#if ANDROID
                // The phone compiles this file without the main window, and has no update handover to return from.
                (string?)after["screen"] ?? "",
#else
                (string?)after["screen"] ?? nameof(Destination.Analyse),
#endif
                DateTimeOffset.TryParse((string?)after["at"], System.Globalization.CultureInfo.InvariantCulture, out var at) ? at : null)
            : null);

    /// <summary>
    /// What one version leaves for the next, with the moment the installer was started. The moment is what lets the new version tell a
    /// relaunch that happened on its own from one a person had to do by hand minutes later, which is the fault Alan found: the installer
    /// brought GroupLab back before it had finished writing its files, the new process died, and nothing said so.
    /// </summary>
    public bool SaveHandover(string from, string screen) => Save(file =>
        file["afterUpdate"] = new JsonObject
        {
            ["from"] = from,
            ["screen"] = screen,
            ["at"] = DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        });

    /// <summary>Forgets the handover, which the new version does as soon as it has said its line.</summary>
    public bool ClearHandover() => Save(file => file.Remove("afterUpdate"));

    /// <summary>Reads one setting, or null when the file is missing or unreadable.</summary>
    /// <summary>
    /// Sending targets, NOTES-FROM-PLANNING.md entry 165: the choice, Unset until the person makes one, and the consent level, none until
    /// they choose one. The first run screen and Settings read and write this one setting, so they cannot disagree.
    /// </summary>
    public (GroupLab.Core.Publication.SendingChoice Choice, GroupLab.Core.Publication.ConsentLevel? Level) LoadSending() => Read(file =>
        (Enum.TryParse<GroupLab.Core.Publication.SendingChoice>((string?)file["sending"]?["choice"], out var choice) ? choice : GroupLab.Core.Publication.SendingChoice.Unset,
         Enum.TryParse<GroupLab.Core.Publication.ConsentLevel>((string?)file["sending"]?["level"], out var level) ? level : (GroupLab.Core.Publication.ConsentLevel?)null));

    public bool SaveSending(GroupLab.Core.Publication.SendingChoice choice, GroupLab.Core.Publication.ConsentLevel? level) => Save(file =>
    {
        var sending = file["sending"] as JsonObject ?? [];
        sending["choice"] = choice.ToString();
        sending["level"] = level?.ToString();
        file["sending"] = sending;
    });

    /// <summary>NOTES-FROM-PLANNING.md entry 194 section 2.1: whether error reports go by themselves. Unset until the person chooses, which is asking.</summary>
    public GroupLab.App.Diagnostics.ErrorReportChoice LoadErrorChoice() =>
        Read(file => Enum.TryParse<GroupLab.App.Diagnostics.ErrorReportChoice>((string?)file["errorReports"]?["choice"], out var choice) ? choice : GroupLab.App.Diagnostics.ErrorReportChoice.Unset);

    public bool SaveErrorChoice(GroupLab.App.Diagnostics.ErrorReportChoice choice) => Save(file =>
    {
        var errors = file["errorReports"] as JsonObject ?? [];
        errors["choice"] = choice.ToString();
        file["errorReports"] = errors;
    });

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 219 item A4: the caliber as the person typed it and the distance in inches, from the last target the
    /// phone analyzed, offered again for the next, since most sessions shoot one rifle at one distance.
    /// </summary>
    public (string? Calibre, double? DistanceInches) LoadShotSetup() => Read(file =>
        ((string?)file["shotSetup"]?["caliber"], file["shotSetup"]?["distanceInches"]?.GetValueKind() == JsonValueKind.Number ? (double?)file["shotSetup"]!["distanceInches"]!.GetValue<double>() : null));

    public bool SaveShotSetup(string? calibre, double? distanceInches) => Save(file => file["shotSetup"] = new JsonObject
    {
        ["caliber"] = string.IsNullOrWhiteSpace(calibre) ? null : calibre.Trim(),
        ["distanceInches"] = distanceInches,
    });

    /// <summary>NOTES-FROM-PLANNING.md entry 208: whether the hardware survey may send. Unset until the person answers.</summary>
    public GroupLab.Core.Survey.SurveyChoice LoadSurveyChoice() =>
        Read(file => Enum.TryParse<GroupLab.Core.Survey.SurveyChoice>((string?)file["survey"]?["choice"], out var choice) ? choice : GroupLab.Core.Survey.SurveyChoice.Unset);

    public bool SaveSurveyChoice(GroupLab.Core.Survey.SurveyChoice choice) => Save(file => Survey(file)["choice"] = choice.ToString());

    /// <summary>
    /// docs/SURVEY.md section 2: the random number that stands for this copy of GroupLab, made the first time it is needed and kept until
    /// the person replaces it. Nothing about the machine goes into it.
    /// </summary>
    public string LoadInstallation()
    {
        if (Read(file => (string?)file["survey"]?["installation"]) is { Length: 32 } kept)
        {
            return kept;
        }

        string made = GroupLab.Core.Survey.SurveyReport.NewInstallation();
        Save(file => Survey(file)["installation"] = made);
        return made;
    }

    /// <summary>A new installation number in place of the old one, from Settings.</summary>
    public string ReplaceInstallation()
    {
        string made = GroupLab.Core.Survey.SurveyReport.NewInstallation();
        Save(file => Survey(file)["installation"] = made);
        return made;
    }

    /// <summary>When the last survey report went, or null.</summary>
    public DateTimeOffset? LoadSurveySent() =>
        Read(file => DateTimeOffset.TryParse((string?)file["survey"]?["sent"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at) ? at : (DateTimeOffset?)null);

    public bool SaveSurveySent(DateTimeOffset at) => Save(file => Survey(file)["sent"] = at.ToString("o", CultureInfo.InvariantCulture));

    /// <summary>The last benchmark run on this machine, and whether it has gone with a report yet.</summary>
    public (GroupLab.Core.Survey.BenchmarkResult Result, bool Sent)? LoadBenchmark() => Read(file =>
    {
        if (file["survey"]?["benchmark"] is not JsonObject b)
        {
            return ((GroupLab.Core.Survey.BenchmarkResult, bool)?)null;
        }

        var stages = (b["stages"] as JsonArray ?? []).OfType<JsonObject>()
            .Select(s => new GroupLab.Core.Survey.StageTime((string?)s["stage"] ?? "", (long?)s["milliseconds"] ?? 0)).ToList();
        var result = new GroupLab.Core.Survey.BenchmarkResult((string?)b["workload"] ?? "", (int?)b["width"] ?? 0, (int?)b["height"] ?? 0,
            (long?)b["totalMilliseconds"] ?? 0, stages, (long?)b["peakMegabytes"] ?? 0, (int?)b["holesPlaced"] ?? 0, (int?)b["holesFound"] ?? 0);
        return (result, (bool?)b["sent"] ?? false);
    });

    public bool SaveBenchmark(GroupLab.Core.Survey.BenchmarkResult result, bool sent) => Save(file => Survey(file)["benchmark"] = new JsonObject
    {
        ["workload"] = result.Workload,
        ["width"] = result.Width,
        ["height"] = result.Height,
        ["totalMilliseconds"] = result.TotalMilliseconds,
        ["stages"] = new JsonArray([.. result.Stages.Select(s => (JsonNode)new JsonObject { ["stage"] = s.Stage, ["milliseconds"] = s.Milliseconds })]),
        ["peakMegabytes"] = result.PeakMegabytes,
        ["holesPlaced"] = result.HolesPlaced,
        ["holesFound"] = result.HolesFound,
        ["sent"] = sent,
    });

    private static JsonObject Survey(JsonObject file)
    {
        if (file["survey"] is not JsonObject survey)
        {
            survey = [];
            file["survey"] = survey;
        }

        return survey;
    }

    /// <summary>How many error reports went today, for the day's cap, and in all, for Settings.</summary>
    public (int Today, int InAll) LoadErrorsSent(DateTime now) => Read(file =>
        file["errorReports"] is JsonObject errors
            ? ((string?)errors["day"] == GroupLab.App.Diagnostics.ErrorReports.Today(now) ? (int?)errors["today"] ?? 0 : 0, (int?)errors["inAll"] ?? 0)
            : (0, 0));

    public bool AddErrorsSent(int count, DateTime now) => Save(file =>
    {
        var errors = file["errorReports"] as JsonObject ?? [];
        string day = GroupLab.App.Diagnostics.ErrorReports.Today(now);
        int today = (string?)errors["day"] == day ? (int?)errors["today"] ?? 0 : 0;
        errors["day"] = day;
        errors["today"] = today + count;
        errors["inAll"] = ((int?)errors["inAll"] ?? 0) + count;
        file["errorReports"] = errors;
    });

    /// <summary>The references of the targets sent from this computer, so a person can ask for one to be removed.</summary>
    public IReadOnlyList<string> LoadSent() => Read(file => file["sending"]?["sent"] is JsonArray sent ? (IReadOnlyList<string>)[.. sent.Select(s => (string?)s).OfType<string>()] : null) ?? [];

    public bool AddSent(string reference) => Save(file =>
    {
        var sending = file["sending"] as JsonObject ?? [];
        var sent = sending["sent"] as JsonArray ?? [];
        sent.Add(reference);
        sending["sent"] = sent;
        file["sending"] = sending;
    });

    /// <summary>
    /// The targets whose "which bulls did you fire at" hint was answered or put away, NOTES-FROM-PLANNING.md entry 187 section 6: once is
    /// enough for a target. The newest 200 are kept.
    /// </summary>
    public bool AimHintPutAway(string target) => Read(file => file["aimHintPutAway"] is JsonArray put && put.Any(p => (string?)p == target));

    public bool PutAwayAimHint(string target) => Save(file =>
    {
        var put = file["aimHintPutAway"] as JsonArray ?? [];
        if (!put.Any(p => (string?)p == target))
        {
            put.Add(target);
        }

        while (put.Count > 200)
        {
            put.RemoveAt(0);
        }

        file["aimHintPutAway"] = put;
    });

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

    /// <summary>NOTES-FROM-PLANNING.md entry 93 section 3's fourth theme, derived from the dark tokens at WCAG's AAA ratio.</summary>
    HighContrast,
}
