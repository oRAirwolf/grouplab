using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Records;

/// <summary>A designed sheet in an export: its file name in the own sheets folder and its definition's JSON.</summary>
public sealed record ExportedSheet(string File, string Definition);

/// <summary>One session in an export, with its chronograph strings and which shot is which reading.</summary>
public sealed record ExportedSession(SessionRecord Session, IReadOnlyList<ChronographString> Strings, IReadOnlyList<ShotVelocity> Velocities);

/// <summary>What an export file holds, read and checked, before anything is written.</summary>
public sealed record DataFile(int Version, string? ExportedUtc, string? FromApp, RecordBook Book, IReadOnlyList<ExportedSession> Sessions,
    IReadOnlyList<ExportedSheet> Sheets, JsonObject Settings);

/// <summary>
/// What an import would do, worked out before anything is written (NOTES-FROM-PLANNING.md entry 307 section 2): the items it adds, the items
/// already here and the same, which it leaves alone, and the conflicts, an item already here that differs, which it lists and never
/// overwrites.
/// </summary>
public sealed record ImportPlan(
    IReadOnlyList<ExportedSession> NewSessions, int SameSessions,
    RecordBook NewBook, int NewBookItems, int SameBookItems,
    IReadOnlyList<ExportedSheet> NewSheets, int SameSheets,
    JsonObject NewSettings, int NewSettingsItems,
    IReadOnlyList<string> Conflicts)
{
    /// <summary>How many items the import adds.</summary>
    public int Adds => NewSessions.Count + NewBookItems + NewSheets.Count + NewSettingsItems;

    /// <summary>What the import will do, in a few plain lines, for the question before it is written.</summary>
    public IReadOnlyList<string> Summary() =>
    [
        $"{NewSessions.Count} new session{(NewSessions.Count == 1 ? "" : "s")}, {SameSessions} already here",
        $"{NewBookItems} new rifle{(NewBookItems == 1 ? "" : "s")}, barrel{(NewBookItems == 1 ? "" : "s")} or load{(NewBookItems == 1 ? "" : "s")}, {SameBookItems} already here",
        $"{NewSheets.Count} new designed sheet{(NewSheets.Count == 1 ? "" : "s")}, {SameSheets} already here",
        $"{NewSettingsItems} printer{(NewSettingsItems == 1 ? "" : "s")} or setting{(NewSettingsItems == 1 ? "" : "s")} new here",
        Conflicts.Count == 0 ? "Nothing here differs from the file." : $"{Conflicts.Count} item{(Conflicts.Count == 1 ? "" : "s")} here differ from the file and are kept as they are here:",
    ];
}

/// <summary>
/// Everything a person has in GroupLab in one file, and read back on any GroupLab, NOTES-FROM-PLANNING.md entry 307, Alan: "make the ability to
/// export all of your data from any of the apps into a file that any of the applications can read and import ... as a stop gap" until cloud
/// backup. One format for the desktop, Android and iOS: JSON with a format name and a version, every session with its proof picture and its
/// marks, the rifles, barrels and loads, the chronograph strings, the designed sheets, and the printers and settings that travel between
/// devices. Every build from this one on reads version 1.
/// <para>
/// Privacy (section 4): the file follows GroupLab's rule for photographs. A session keeps its proof picture, which GroupLab made from the
/// photograph's pixels and which carries no metadata, and the original photograph's SHA-256; the original's path on this device is left out,
/// and so is anything GPS or location, which GroupLab never reads. The file stays with the person: nothing is sent anywhere.
/// </para>
/// <para>
/// Import merges (section 2). A session is the same session where it was made at the same moment on the same sheet from the same picture; a
/// rifle, barrel or load, a printer, a sheet or a setting by its name. What is new is added, what is the same is left alone, and what differs
/// is listed as a conflict and kept as it is here, so nothing is overwritten or duplicated; the plan is made before anything is written, so
/// the import can be cancelled.
/// </para>
/// </summary>
public static class DataExport
{
    public const string Format = "grouplab-data";

    public const int Version = 1;

    /// <summary>The file name's ending, which the phone and the desktop offer to open.</summary>
    public const string Extension = ".grouplab";

    /// <summary>The settings that travel between devices: the printers and each sheet's bull color, merged by name, and the units.</summary>
    public static IReadOnlyList<string> PortableSettings { get; } = ["printers", "bullColours", "scope", "angular", "linear", "distance"];

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Writes everything to <paramref name="output"/>, a session at a time, so a library of hundreds of sessions with their pictures is never
    /// held in memory whole.
    /// </summary>
    public static void Write(Stream output, SessionStore store, IReadOnlyList<ExportedSheet> sheets, JsonObject settings, string fromApp, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(sheets);
        ArgumentNullException.ThrowIfNull(settings);
        using var json = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = false });
        json.WriteStartObject();
        json.WriteString("format", Format);
        json.WriteNumber("version", Version);
        json.WriteString("exportedUtc", nowUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));
        json.WriteString("fromApp", fromApp);
        json.WritePropertyName("book");
        JsonSerializer.Serialize(json, store.LoadBook(), Options);
        json.WritePropertyName("sessions");
        json.WriteStartArray();
        foreach (var summary in store.List().OrderBy(s => s.Id))
        {
            if (store.Get(summary.Id) is not { } session)
            {
                continue;
            }

            // The original photograph's path is this device's, and says nothing another device can use; its SHA-256 stays.
            JsonSerializer.Serialize(json, new ExportedSession(session with { ImagePath = null }, store.ChronographStrings(session.Id), store.ShotVelocities(session.Id)), Options);
            json.Flush();
        }

        json.WriteEndArray();
        json.WritePropertyName("sheets");
        JsonSerializer.Serialize(json, sheets, Options);
        json.WritePropertyName("settings");
        var portable = new JsonObject();
        foreach (string key in PortableSettings)
        {
            if (settings[key] is { } value)
            {
                portable[key] = value.DeepClone();
            }
        }

        portable.WriteTo(json);
        json.WriteEndObject();
    }

    /// <summary>
    /// Reads an export and checks it, or refuses it with a sentence a person can act on: not a GroupLab file, damaged, or written by a newer
    /// GroupLab than this one.
    /// </summary>
    public static DataFile Read(Stream input)
    {
        ArgumentNullException.ThrowIfNull(input);
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(input);
        }
        catch (JsonException)
        {
            throw new InvalidDataException("This file is damaged or is not a GroupLab data file, so nothing was imported.");
        }

        if (root is not JsonObject file || (string?)file["format"] != Format)
        {
            throw new InvalidDataException("This is not a GroupLab data file, so nothing was imported. Choose a file GroupLab exported, ending " + Extension + ".");
        }

        int version = file["version"]?.GetValueKind() == JsonValueKind.Number ? (int)file["version"]! : 0;
        if (version < 1)
        {
            throw new InvalidDataException("This GroupLab data file has no version, so it is damaged, and nothing was imported.");
        }

        if (version > Version)
        {
            throw new InvalidDataException($"This file was written by a newer GroupLab (data version {version}; this one reads up to {Version}). Update GroupLab, then import it again.");
        }

        try
        {
            var book = file["book"]?.Deserialize<RecordBook>(Options) ?? RecordBook.Empty;
            var sessions = file["sessions"]?.Deserialize<List<ExportedSession>>(Options) ?? [];
            var sheets = file["sheets"]?.Deserialize<List<ExportedSheet>>(Options) ?? [];
            if (sessions.Any(s => s.Session is null || string.IsNullOrEmpty(s.Session.MarkingJson) || string.IsNullOrEmpty(s.Session.CreatedUtc)))
            {
                throw new InvalidDataException("A session in this file is incomplete, so the file is damaged, and nothing was imported.");
            }

            return new DataFile(version, (string?)file["exportedUtc"], (string?)file["fromApp"], book with
            {
                Rifles = [.. book.Rifles ?? []],
                Barrels = [.. book.Barrels ?? []],
                Loads = [.. book.Loads ?? []],
            }, sessions, sheets, file["settings"] as JsonObject is { } s ? (JsonObject)s.DeepClone() : []);
        }
        catch (Exception e) when (e is JsonException or NotSupportedException or InvalidOperationException or FormatException or ArgumentException)
        {
            throw new InvalidDataException("This file is damaged, so nothing was imported.");
        }
    }

    /// <summary>A session's identity across devices: made at the same moment, on the same sheet, from the same picture.</summary>
    public static string Identity(SessionRecord s) => $"{s.CreatedUtc}|{s.SheetName}|{s.DefinitionId}|{s.ImageSha256}";

    private static bool SameContent(SessionRecord a, SessionRecord b) =>
        a with { Id = 0, ImagePath = null, ProofImage = null } == b with { Id = 0, ImagePath = null, ProofImage = null }
        && (a.ProofImage ?? []).AsSpan().SequenceEqual(b.ProofImage ?? []);

    /// <summary>What importing <paramref name="file"/> here would do. Nothing is written.</summary>
    public static ImportPlan Plan(DataFile file, SessionStore store, IReadOnlyList<ExportedSheet> localSheets, JsonObject localSettings)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(localSheets);
        ArgumentNullException.ThrowIfNull(localSettings);
        var conflicts = new List<string>();

        // Two sessions can share an identity (the same picture read twice in one second), here or in the file; an incoming session is the
        // same as any one of those here with its identity, and new only where none is.
        var here = new Dictionary<string, List<SessionRecord>>(StringComparer.Ordinal);
        foreach (var local in store.List().OrderBy(s => s.Id).Select(s => store.Get(s.Id)).OfType<SessionRecord>())
        {
            (here.TryGetValue(Identity(local), out var list) ? list : here[Identity(local)] = []).Add(local);
        }

        var newSessions = new List<ExportedSession>();
        var arriving = new Dictionary<string, List<SessionRecord>>(StringComparer.Ordinal);
        int sameSessions = 0;
        foreach (var s in file.Sessions)
        {
            string identity = Identity(s.Session);
            if (!here.TryGetValue(identity, out var existing))
            {
                // New here; a second one with the same identity in the file is new too unless it repeats one already taken.
                var taken = arriving.TryGetValue(identity, out var list) ? list : arriving[identity] = [];
                if (taken.Any(t => SameContent(t, s.Session)))
                {
                    sameSessions++;
                }
                else
                {
                    newSessions.Add(s);
                    taken.Add(s.Session);
                }
            }
            else if (existing.Any(e => SameContent(e, s.Session with { ImagePath = e.ImagePath })))
            {
                sameSessions++;
            }
            else
            {
                conflicts.Add($"Session {s.Session.SheetName}, {s.Session.ShotDate ?? s.Session.CreatedUtc}: its marks or details differ.");
            }
        }

        var book = store.LoadBook();
        var addBook = RecordBook.Empty;
        int sameBook = 0;
        void Merge<T>(IEnumerable<T> incoming, Func<string, T?> find, Func<T, string> name, string kind, Action<T> add)
            where T : class
        {
            foreach (var item in incoming)
            {
                if (find(name(item)) is not { } existing)
                {
                    add(item);
                }
                else if (existing.Equals(item))
                {
                    sameBook++;
                }
                else
                {
                    conflicts.Add($"{kind} {name(item)}: its details differ.");
                }
            }
        }

        Merge(file.Book.Rifles, book.FindRifle, r => r.Name, "Rifle", r => addBook = addBook.With(r));
        Merge(file.Book.Barrels, book.FindBarrel, b => b.Name, "Barrel", b => addBook = addBook.With(b));
        Merge(file.Book.Loads, book.FindLoad, l => l.Name, "Load", l => addBook = addBook.With(l));

        var sheetsHere = localSheets.ToDictionary(s => s.File, StringComparer.OrdinalIgnoreCase);
        var newSheets = new List<ExportedSheet>();
        int sameSheets = 0;
        foreach (var sheet in file.Sheets)
        {
            if (!sheetsHere.TryGetValue(sheet.File, out var existing))
            {
                newSheets.Add(sheet);
            }
            else if (JsonNode.DeepEquals(JsonNode.Parse(existing.Definition), JsonNode.Parse(sheet.Definition)))
            {
                sameSheets++;
            }
            else
            {
                conflicts.Add($"Designed sheet {sheet.File}: its design differs.");
            }
        }

        var newSettings = new JsonObject();
        int newSettingItems = 0;
        foreach (var (key, value) in file.Settings)
        {
            if (!PortableSettings.Contains(key) || value is null)
            {
                continue;
            }

            if (key == "printers" && value is JsonArray printers)
            {
                var mine = (localSettings["printers"] as JsonArray ?? []).OfType<JsonObject>().ToDictionary(p => (string?)p["name"] ?? "", StringComparer.Ordinal);
                var adding = new JsonArray();
                foreach (var printer in printers.OfType<JsonObject>())
                {
                    string name = (string?)printer["name"] ?? "";
                    if (!mine.TryGetValue(name, out var existing))
                    {
                        adding.Add(printer.DeepClone());
                    }
                    else if (!JsonNode.DeepEquals(existing, printer))
                    {
                        conflicts.Add($"Printer {name}: its measured scale differs.");
                    }
                }

                if (adding.Count > 0)
                {
                    newSettings["printers"] = adding;
                    newSettingItems += adding.Count;
                }
            }
            else if (key == "bullColours" && value is JsonObject colours)
            {
                var mine = localSettings["bullColours"] as JsonObject ?? [];
                var adding = new JsonObject();
                foreach (var (sheet, colour) in colours)
                {
                    if (mine[sheet] is null)
                    {
                        adding[sheet] = colour?.DeepClone();
                    }
                    else if (!JsonNode.DeepEquals(mine[sheet], colour))
                    {
                        conflicts.Add($"The bull color of {sheet} differs.");
                    }
                }

                if (adding.Count > 0)
                {
                    newSettings["bullColours"] = adding;
                    newSettingItems += adding.Count;
                }
            }
            else if (localSettings[key] is null)
            {
                newSettings[key] = value.DeepClone();
                newSettingItems++;
            }
            else if (!JsonNode.DeepEquals(localSettings[key], value))
            {
                conflicts.Add($"The setting {key} differs.");
            }
        }

        int bookItems = addBook.Rifles.Count + addBook.Barrels.Count + addBook.Loads.Count;
        return new ImportPlan(newSessions, sameSessions, addBook, bookItems, sameBook, newSheets, sameSheets, newSettings, newSettingItems, conflicts);
    }

    /// <summary>
    /// Writes what <paramref name="plan"/> adds: the sessions with their chronograph strings, mapped to the new ids, and the rifles, barrels and
    /// loads. The designed sheets and the settings are the caller's to write, since where they live is each platform's own. Returns how many
    /// sessions were added.
    /// </summary>
    public static int Apply(ImportPlan plan, SessionStore store)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(store);
        var book = store.LoadBook();
        foreach (var rifle in plan.NewBook.Rifles)
        {
            book = book.With(rifle);
        }

        foreach (var barrel in plan.NewBook.Barrels)
        {
            book = book.With(barrel);
        }

        foreach (var load in plan.NewBook.Loads)
        {
            book = book.With(load);
        }

        if (plan.NewBookItems > 0)
        {
            store.SaveBook(book);
        }

        foreach (var item in plan.NewSessions)
        {
            long id = store.Save(item.Session with { Id = 0, ImagePath = null });
            var strings = new Dictionary<long, long>();
            foreach (var chronograph in item.Strings)
            {
                strings[chronograph.Id] = store.AddChronographString(id, chronograph.Source, chronograph.RecordedUtc, chronograph.VelocitiesFps);
            }

            foreach (var velocity in item.Velocities)
            {
                if (strings.TryGetValue(velocity.StringId, out long stringId))
                {
                    store.MapShot(velocity with { SessionId = id, StringId = stringId });
                }
            }
        }

        return plan.NewSessions.Count;
    }
}
