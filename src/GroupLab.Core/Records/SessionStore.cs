using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;
using Microsoft.Data.Sqlite;

namespace GroupLab.Core.Records;

/// <summary>
/// One analysed sheet as a session record, NOTES-FROM-PLANNING.md entry 112 section 1: when and at what distance it was shot, with which
/// rifle, barrel, load and calibre, the marking with every edit and exclusion, the figures as they were computed, and the proof image.
/// <para>
/// DESIGN.md section 18's three tiers. The geometry, the marking, is always kept. A proof image of about 150 dpi is kept by default. The
/// full-resolution original stays where the person has it, and the session keeps only its path and SHA-256. No image is needed to reopen and
/// read a session: the sheet re-renders from its definition, which the session keeps too, so a deleted sheet cannot orphan it.
/// </para>
/// </summary>
public sealed record SessionRecord(
    long Id,
    string CreatedUtc,
    string? ShotDate,
    string SheetName,
    string? DefinitionId,
    string? DefinitionJson,
    double? DistanceInches,
    string? Rifle,
    string? Barrel,
    string? Load,
    double? CalibreInches,
    string MarkingJson,
    int ShotCount,
    double? MeanRadiusInches,
    double? MeanRadiusLowerInches,
    double? MeanRadiusUpperInches,
    string? ImagePath,
    string? ImageSha256,
    byte[]? ProofImage,
    string? ProofImageType);

/// <summary>A session as the Session records list shows it, without its marking or its proof image.</summary>
public sealed record SessionSummary(
    long Id,
    string CreatedUtc,
    string? ShotDate,
    string SheetName,
    double? DistanceInches,
    string? Rifle,
    string? Load,
    int ShotCount,
    double? MeanRadiusInches,
    double? MeanRadiusLowerInches,
    double? MeanRadiusUpperInches);

/// <summary>
/// A chronograph string, DESIGN.md section 15: the velocities in the order the chronograph recorded them. It is kept as its own ordered list,
/// never assumed to align with the shots, because chronographs drop shots and record a neighbour's.
/// </summary>
public sealed record ChronographString(long Id, long SessionId, string Source, string? RecordedUtc, IReadOnlyList<double> VelocitiesFps);

/// <summary>The explicit mapping section 15 requires: this shot of this session is this reading of this string.</summary>
public sealed record ShotVelocity(long SessionId, int ShotId, long StringId, int Ordinal);

/// <summary>
/// GroupLab's storage, DESIGN.md section 15: "SQLite with a documented schema and full JSON export", NOTES-FROM-PLANNING.md entry 112
/// section 1. One database in the application's data folder holds the rifles, barrels and loads and the session records, with room for
/// chronograph strings and their mapping to shots, so adding Xero import later needs no migration of the sessions. The schema is
/// <see cref="Schema"/>, documented in docs/SESSION-SCHEMA.md, which a test holds to the database this creates.
/// </summary>
public sealed class SessionStore
{
    /// <summary>The schema's version, stored in the meta table and in every export.</summary>
    public const int SchemaVersion = 2;

    /// <summary>
    /// What each version before this one needs to become this one, oldest first, NOTES-FROM-PLANNING.md entry 115 section 3. Version 2 adds
    /// where a load's muzzle velocity SD came from, so a figure measured from a chronograph string says so rather than looking typed.
    /// </summary>
    public static IReadOnlyList<(int From, string Statement)> Upgrades { get; } =
    [
        (1, "ALTER TABLE loads ADD COLUMN muzzle_velocity_sd_from TEXT"),
    ];

    /// <summary>Every statement that creates the schema, in order, exactly as docs/SESSION-SCHEMA.md gives them.</summary>
    public static IReadOnlyList<string> Schema { get; } =
    [
        "CREATE TABLE meta (key TEXT PRIMARY KEY, value TEXT NOT NULL)",
        "CREATE TABLE rifles (name TEXT PRIMARY KEY COLLATE NOCASE, click_value REAL NOT NULL, click_unit TEXT NOT NULL, sight_height_in REAL, zero_distance_yd REAL, twist_in REAL, twist_direction INTEGER)",
        "CREATE TABLE barrels (name TEXT PRIMARY KEY COLLATE NOCASE, rifle TEXT, rounds INTEGER NOT NULL)",
        "CREATE TABLE loads (name TEXT PRIMARY KEY COLLATE NOCASE, components TEXT, muzzle_velocity_fps REAL, muzzle_velocity_sd_fps REAL, ballistic_coefficient REAL, drag_model TEXT, bc_reference TEXT, bullet_weight_gr REAL, bullet_length_in REAL, bullet_diameter_in REAL, muzzle_velocity_sd_from TEXT)",
        "CREATE TABLE sessions (id INTEGER PRIMARY KEY, created_utc TEXT NOT NULL, shot_date TEXT, sheet_name TEXT NOT NULL, definition_id TEXT, definition_json TEXT, distance_in REAL, rifle TEXT, barrel TEXT, load TEXT, calibre_in REAL, marking_json TEXT NOT NULL, shot_count INTEGER NOT NULL, mean_radius_in REAL, mean_radius_lower_in REAL, mean_radius_upper_in REAL, image_path TEXT, image_sha256 TEXT, proof_image BLOB, proof_image_type TEXT)",
        "CREATE TABLE chronograph_strings (id INTEGER PRIMARY KEY, session_id INTEGER NOT NULL REFERENCES sessions(id) ON DELETE CASCADE, source TEXT NOT NULL, recorded_utc TEXT)",
        "CREATE TABLE chronograph_shots (string_id INTEGER NOT NULL REFERENCES chronograph_strings(id) ON DELETE CASCADE, ordinal INTEGER NOT NULL, velocity_fps REAL NOT NULL, PRIMARY KEY (string_id, ordinal))",
        "CREATE TABLE shot_velocities (session_id INTEGER NOT NULL REFERENCES sessions(id) ON DELETE CASCADE, shot_id INTEGER NOT NULL, string_id INTEGER NOT NULL, ordinal INTEGER NOT NULL, PRIMARY KEY (session_id, shot_id), FOREIGN KEY (string_id, ordinal) REFERENCES chronograph_shots(string_id, ordinal) ON DELETE CASCADE)",
        "CREATE INDEX sessions_rifle ON sessions(rifle)",
        "CREATE INDEX sessions_load ON sessions(load)",
    ];

    private readonly string connection;

    private SessionStore(string path)
    {
        Path = path;
        connection = new SqliteConnectionStringBuilder { DataSource = path, ForeignKeys = true, Pooling = false }.ToString();
    }

    /// <summary>The database file.</summary>
    public string Path { get; }

    /// <summary>
    /// Opens the database at <paramref name="path"/>, creating it with the schema when it does not exist. On the first open, a record book
    /// from before the database, <paramref name="legacyRecords"/>, is copied into it and renamed to <c>records.pre-database.json</c> beside
    /// itself, kept as a backup and never deleted.
    /// </summary>
    public static SessionStore Open(string path, string? legacyRecords = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!);
        var store = new SessionStore(path);
        using var db = store.Connect();
        if (Scalar(db, "SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = 'meta'") is 0L)
        {
            using var create = db.BeginTransaction();
            foreach (string statement in Schema)
            {
                Execute(db, statement);
            }

            Execute(db, "INSERT INTO meta (key, value) VALUES ('schema_version', $v)", ("$v", SchemaVersion.ToString(CultureInfo.InvariantCulture)));
            create.Commit();
        }

        // A database written by an older GroupLab is brought up to this schema, oldest step first; a newer one is refused, since this
        // GroupLab cannot know what it holds (entry 115 section 3).
        string? version = Scalar(db, "SELECT value FROM meta WHERE key = 'schema_version'") as string;
        if (int.TryParse(version, NumberStyles.Integer, CultureInfo.InvariantCulture, out int had) && had < SchemaVersion)
        {
            using var upgrade = db.BeginTransaction();
            foreach (var (from, statement) in Upgrades.Where(u => u.From >= had).OrderBy(u => u.From))
            {
                Execute(db, statement);
            }

            Execute(db, "UPDATE meta SET value = $v WHERE key = 'schema_version'", ("$v", SchemaVersion.ToString(CultureInfo.InvariantCulture)));
            upgrade.Commit();
            version = SchemaVersion.ToString(CultureInfo.InvariantCulture);
        }

        if (version != SchemaVersion.ToString(CultureInfo.InvariantCulture))
        {
            throw new InvalidDataException($"{path} has schema version {version}, and this GroupLab reads version {SchemaVersion}.");
        }

        if (legacyRecords is not null && File.Exists(legacyRecords) && Scalar(db, "SELECT value FROM meta WHERE key = 'records_migrated'") is null)
        {
            store.SaveBook(RecordBook.Read(File.ReadAllText(legacyRecords)));
            string folder = System.IO.Path.GetDirectoryName(legacyRecords)!;
            string backup = System.IO.Path.Combine(folder, "records.pre-database.json");
            if (File.Exists(backup))
            {
                // A backup from an earlier move is kept too: nothing here is ever overwritten.
                backup = System.IO.Path.Combine(folder, $"records.pre-database-{DateTime.UtcNow:yyyyMMddHHmmss}.json");
            }

            File.Move(legacyRecords, backup, overwrite: false);
            Execute(db, "INSERT INTO meta (key, value) VALUES ('records_migrated', $f)", ("$f", System.IO.Path.GetFileName(backup)));
        }

        return store;
    }

    private SqliteConnection Connect()
    {
        var db = new SqliteConnection(connection);
        db.Open();
        return db;
    }

    private static object? Scalar(SqliteConnection db, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = Command(db, sql, parameters);
        return command.ExecuteScalar();
    }

    private static void Execute(SqliteConnection db, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = Command(db, sql, parameters);
        command.ExecuteNonQuery();
    }

    private static SqliteCommand Command(SqliteConnection db, string sql, (string Name, object? Value)[] parameters)
    {
        var command = db.CreateCommand();
#pragma warning disable CA2100 // Every statement is a constant of this class; values go in as parameters.
        command.CommandText = sql;
#pragma warning restore CA2100
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return command;
    }

    private static double? Real(SqliteDataReader r, int i) => r.IsDBNull(i) ? null : r.GetDouble(i);

    private static string? Text(SqliteDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);

    private static int? Integer(SqliteDataReader r, int i) => r.IsDBNull(i) ? null : r.GetInt32(i);

    // ---- Rifles, barrels and loads.

    /// <summary>The rifles, barrels and loads, in name order.</summary>
    public RecordBook LoadBook()
    {
        using var db = Connect();
        var rifles = new List<Rifle>();
        using (var command = Command(db, "SELECT name, click_value, click_unit, sight_height_in, zero_distance_yd, twist_in, twist_direction FROM rifles ORDER BY name", []))
        using (var r = command.ExecuteReader())
        {
            while (r.Read())
            {
                rifles.Add(new Rifle(r.GetString(0), r.GetDouble(1), Enum.Parse<AngularUnit>(r.GetString(2)))
                {
                    SightHeightInches = Real(r, 3),
                    ZeroDistanceYards = Real(r, 4),
                    TwistInches = Real(r, 5),
                    TwistDirection = Integer(r, 6),
                });
            }
        }

        var barrels = new List<Barrel>();
        using (var command = Command(db, "SELECT name, rifle, rounds FROM barrels ORDER BY name", []))
        using (var r = command.ExecuteReader())
        {
            while (r.Read())
            {
                barrels.Add(new Barrel(r.GetString(0), Text(r, 1), r.GetInt32(2)));
            }
        }

        var loads = new List<Load>();
        using (var command = Command(db, "SELECT name, components, muzzle_velocity_fps, muzzle_velocity_sd_fps, ballistic_coefficient, drag_model, bc_reference, bullet_weight_gr, bullet_length_in, bullet_diameter_in, muzzle_velocity_sd_from FROM loads ORDER BY name", []))
        using (var r = command.ExecuteReader())
        {
            while (r.Read())
            {
                loads.Add(new Load(r.GetString(0), Text(r, 1))
                {
                    MuzzleVelocityFps = Real(r, 2),
                    MuzzleVelocitySdFps = Real(r, 3),
                    BallisticCoefficient = Real(r, 4),
                    DragModel = Text(r, 5) is { } model ? Enum.Parse<DragModel>(model) : null,
                    BcReference = Text(r, 6) is { } reference ? Enum.Parse<ReferenceAtmosphere>(reference) : null,
                    BulletWeightGrains = Real(r, 7),
                    BulletLengthInches = Real(r, 8),
                    BulletDiameterInches = Real(r, 9),
                    MuzzleVelocitySdFrom = Text(r, 10),
                });
            }
        }

        return new RecordBook([.. rifles], [.. barrels], [.. loads]);
    }

    /// <summary>Replaces every rifle, barrel and load with the book's, in one transaction.</summary>
    public void SaveBook(RecordBook book)
    {
        ArgumentNullException.ThrowIfNull(book);
        using var db = Connect();
        using var transaction = db.BeginTransaction();
        WriteBook(db, book);
        transaction.Commit();
    }

    private static void WriteBook(SqliteConnection db, RecordBook book)
    {
        Execute(db, "DELETE FROM rifles");
        Execute(db, "DELETE FROM barrels");
        Execute(db, "DELETE FROM loads");
        foreach (var rifle in book.Rifles)
        {
            Execute(db, "INSERT INTO rifles VALUES ($n, $c, $u, $s, $z, $t, $d)", ("$n", rifle.Name), ("$c", rifle.ClickValue), ("$u", rifle.ClickUnit.ToString()),
                ("$s", rifle.SightHeightInches), ("$z", rifle.ZeroDistanceYards), ("$t", rifle.TwistInches), ("$d", rifle.TwistDirection));
        }

        foreach (var barrel in book.Barrels)
        {
            Execute(db, "INSERT INTO barrels VALUES ($n, $r, $k)", ("$n", barrel.Name), ("$r", barrel.Rifle), ("$k", barrel.Rounds));
        }

        foreach (var load in book.Loads)
        {
            Execute(db, "INSERT INTO loads VALUES ($n, $c, $v, $sd, $bc, $m, $ref, $w, $l, $d, $from)", ("$n", load.Name), ("$c", load.Components),
                ("$v", load.MuzzleVelocityFps), ("$sd", load.MuzzleVelocitySdFps), ("$bc", load.BallisticCoefficient), ("$m", load.DragModel?.ToString()),
                ("$ref", load.BcReference?.ToString()), ("$w", load.BulletWeightGrains), ("$l", load.BulletLengthInches), ("$d", load.BulletDiameterInches),
                ("$from", load.MuzzleVelocitySdFrom));
        }
    }

    // ---- Sessions.

    private const string SessionColumns = "id, created_utc, shot_date, sheet_name, definition_id, definition_json, distance_in, rifle, barrel, load, calibre_in, marking_json, shot_count, mean_radius_in, mean_radius_lower_in, mean_radius_upper_in, image_path, image_sha256, proof_image, proof_image_type";

    /// <summary>Saves a session. An id of 0 adds one and returns its new id; any other id replaces that session.</summary>
    public long Save(SessionRecord session)
    {
        ArgumentNullException.ThrowIfNull(session);
        using var db = Connect();
        using var transaction = db.BeginTransaction();
        long id = WriteSession(db, session);
        transaction.Commit();
        return id;
    }

    private static long WriteSession(SqliteConnection db, SessionRecord s)
    {
        var values = new (string, object?)[]
        {
            ("$created", s.CreatedUtc), ("$date", s.ShotDate), ("$sheet", s.SheetName), ("$defid", s.DefinitionId), ("$def", s.DefinitionJson),
            ("$distance", s.DistanceInches), ("$rifle", s.Rifle), ("$barrel", s.Barrel), ("$load", s.Load), ("$calibre", s.CalibreInches),
            ("$marking", s.MarkingJson), ("$count", s.ShotCount), ("$mr", s.MeanRadiusInches), ("$mrl", s.MeanRadiusLowerInches),
            ("$mru", s.MeanRadiusUpperInches), ("$image", s.ImagePath), ("$hash", s.ImageSha256), ("$proof", s.ProofImage), ("$proofType", s.ProofImageType),
        };
        const string columns = "created_utc, shot_date, sheet_name, definition_id, definition_json, distance_in, rifle, barrel, load, calibre_in, marking_json, shot_count, mean_radius_in, mean_radius_lower_in, mean_radius_upper_in, image_path, image_sha256, proof_image, proof_image_type";
        const string parameters = "$created, $date, $sheet, $defid, $def, $distance, $rifle, $barrel, $load, $calibre, $marking, $count, $mr, $mrl, $mru, $image, $hash, $proof, $proofType"; // British on purpose: SQL parameter names.
        if (s.Id > 0)
        {
            Execute(db, $"INSERT OR REPLACE INTO sessions (id, {columns}) VALUES ($id, {parameters})", [.. values, ("$id", s.Id)]);
            return s.Id;
        }

        Execute(db, $"INSERT INTO sessions ({columns}) VALUES ({parameters})", values);
        return (long)Scalar(db, "SELECT last_insert_rowid()")!;
    }

    /// <summary>One session, with its marking and proof image, or null when there is none with that id.</summary>
    public SessionRecord? Get(long id)
    {
        using var db = Connect();
        using var command = Command(db, $"SELECT {SessionColumns} FROM sessions WHERE id = $id", [("$id", id)]);
        using var r = command.ExecuteReader();
        return r.Read() ? ReadSession(r) : null;
    }

    private static SessionRecord ReadSession(SqliteDataReader r) => new(
        r.GetInt64(0), r.GetString(1), Text(r, 2), r.GetString(3), Text(r, 4), Text(r, 5), Real(r, 6), Text(r, 7), Text(r, 8), Text(r, 9),
        Real(r, 10), r.GetString(11), r.GetInt32(12), Real(r, 13), Real(r, 14), Real(r, 15), Text(r, 16), Text(r, 17),
        r.IsDBNull(18) ? null : (byte[])r[18], Text(r, 19));

    /// <summary>The sessions, newest first by when they were saved, optionally only those of one rifle or one load.</summary>
    public IReadOnlyList<SessionSummary> List(string? rifle = null, string? load = null)
    {
        using var db = Connect();
        using var command = Command(db,
            "SELECT id, created_utc, shot_date, sheet_name, distance_in, rifle, load, shot_count, mean_radius_in, mean_radius_lower_in, mean_radius_upper_in FROM sessions "
            + "WHERE ($rifle IS NULL OR rifle = $rifle COLLATE NOCASE) AND ($load IS NULL OR load = $load COLLATE NOCASE) ORDER BY created_utc DESC, id DESC",
            [("$rifle", rifle), ("$load", load)]);
        using var r = command.ExecuteReader();
        var list = new List<SessionSummary>();
        while (r.Read())
        {
            list.Add(new SessionSummary(r.GetInt64(0), r.GetString(1), Text(r, 2), r.GetString(3), Real(r, 4), Text(r, 5), Text(r, 6), r.GetInt32(7), Real(r, 8), Real(r, 9), Real(r, 10)));
        }

        return list;
    }

    /// <summary>Deletes a session and its chronograph strings and mapping.</summary>
    public bool Delete(long id)
    {
        using var db = Connect();
        using var command = Command(db, "DELETE FROM sessions WHERE id = $id", [("$id", id)]);
        return command.ExecuteNonQuery() > 0;
    }

    /// <summary>The sessions analysed against a sheet, by its definition identifier.</summary>
    public int CountUsing(string definitionId)
    {
        using var db = Connect();
        return Convert.ToInt32(Scalar(db, "SELECT count(*) FROM sessions WHERE definition_id = $d", ("$d", definitionId)), CultureInfo.InvariantCulture);
    }

    // ---- Chronograph strings: the room section 15 needs, kept apart from the shots.

    /// <summary>Adds a chronograph string to a session and returns its id.</summary>
    public long AddChronographString(long sessionId, string source, string? recordedUtc, IReadOnlyList<double> velocitiesFps)
    {
        ArgumentNullException.ThrowIfNull(velocitiesFps);
        using var db = Connect();
        using var transaction = db.BeginTransaction();
        Execute(db, "INSERT INTO chronograph_strings (session_id, source, recorded_utc) VALUES ($s, $src, $at)", ("$s", sessionId), ("$src", source), ("$at", recordedUtc));
        long id = (long)Scalar(db, "SELECT last_insert_rowid()")!;
        for (int i = 0; i < velocitiesFps.Count; i++)
        {
            Execute(db, "INSERT INTO chronograph_shots VALUES ($id, $o, $v)", ("$id", id), ("$o", i + 1), ("$v", velocitiesFps[i]));
        }

        transaction.Commit();
        return id;
    }

    /// <summary>Records that a shot is a given reading of a string: the explicit mapping, made by a person or a reconciliation, never assumed.</summary>
    public void MapShot(ShotVelocity mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        using var db = Connect();
        Execute(db, "INSERT OR REPLACE INTO shot_velocities VALUES ($s, $shot, $str, $o)", ("$s", mapping.SessionId), ("$shot", mapping.ShotId), ("$str", mapping.StringId), ("$o", mapping.Ordinal));
    }

    public IReadOnlyList<ChronographString> ChronographStrings(long sessionId)
    {
        using var db = Connect();
        return [.. ReadStrings(db, sessionId)];
    }

    public IReadOnlyList<ShotVelocity> ShotVelocities(long sessionId)
    {
        using var db = Connect();
        return [.. ReadMappings(db, sessionId)];
    }

    private static List<ChronographString> ReadStrings(SqliteConnection db, long? sessionId)
    {
        var strings = new List<(long Id, long Session, string Source, string? At)>();
        using (var command = Command(db, "SELECT id, session_id, source, recorded_utc FROM chronograph_strings WHERE $s IS NULL OR session_id = $s ORDER BY id", [("$s", sessionId)]))
        using (var r = command.ExecuteReader())
        {
            while (r.Read())
            {
                strings.Add((r.GetInt64(0), r.GetInt64(1), r.GetString(2), Text(r, 3)));
            }
        }

        var result = new List<ChronographString>();
        foreach (var s in strings)
        {
            var velocities = new List<double>();
            using var command = Command(db, "SELECT velocity_fps FROM chronograph_shots WHERE string_id = $id ORDER BY ordinal", [("$id", s.Id)]);
            using var r = command.ExecuteReader();
            while (r.Read())
            {
                velocities.Add(r.GetDouble(0));
            }

            result.Add(new ChronographString(s.Id, s.Session, s.Source, s.At, velocities));
        }

        return result;
    }

    private static List<ShotVelocity> ReadMappings(SqliteConnection db, long? sessionId)
    {
        var list = new List<ShotVelocity>();
        using var command = Command(db, "SELECT session_id, shot_id, string_id, ordinal FROM shot_velocities WHERE $s IS NULL OR session_id = $s ORDER BY session_id, shot_id", [("$s", sessionId)]);
        using var r = command.ExecuteReader();
        while (r.Read())
        {
            list.Add(new ShotVelocity(r.GetInt64(0), r.GetInt32(1), r.GetInt64(2), r.GetInt32(3)));
        }

        return list;
    }

    // ---- Full JSON export and import, section 15.

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    /// <summary>
    /// Everything in the database as one JSON document: the schema version, the rifles, barrels and loads, every session with its proof image
    /// in base64, the chronograph strings and the mapping. <see cref="Import"/> reads it back, and the two round-trip exactly.
    /// </summary>
    public string Export()
    {
        var book = LoadBook();
        using var db = Connect();
        var sessions = new JsonArray();
        using (var command = Command(db, $"SELECT {SessionColumns} FROM sessions ORDER BY id", []))
        using (var r = command.ExecuteReader())
        {
            while (r.Read())
            {
                var s = ReadSession(r);
                sessions.Add(new JsonObject
                {
                    ["id"] = s.Id, ["createdUtc"] = s.CreatedUtc, ["shotDate"] = s.ShotDate, ["sheetName"] = s.SheetName, ["definitionId"] = s.DefinitionId,
                    ["definitionJson"] = s.DefinitionJson, ["distanceInches"] = s.DistanceInches, ["rifle"] = s.Rifle, ["barrel"] = s.Barrel, ["load"] = s.Load,
                    ["calibreInches"] = s.CalibreInches, ["markingJson"] = s.MarkingJson, ["shotCount"] = s.ShotCount, ["meanRadiusInches"] = s.MeanRadiusInches,
                    ["meanRadiusLowerInches"] = s.MeanRadiusLowerInches, ["meanRadiusUpperInches"] = s.MeanRadiusUpperInches, ["imagePath"] = s.ImagePath,
                    ["imageSha256"] = s.ImageSha256, ["proofImage"] = s.ProofImage is { } proof ? Convert.ToBase64String(proof) : null, ["proofImageType"] = s.ProofImageType,
                });
            }
        }

        var root = new JsonObject
        {
            ["format"] = "grouplab-export",
            ["schemaVersion"] = SchemaVersion,
            ["rifles"] = new JsonArray([.. book.Rifles.Select(x => (JsonNode)new JsonObject
            {
                ["name"] = x.Name, ["clickValue"] = x.ClickValue, ["clickUnit"] = x.ClickUnit.ToString(), ["sightHeightInches"] = x.SightHeightInches,
                ["zeroDistanceYards"] = x.ZeroDistanceYards, ["twistInches"] = x.TwistInches, ["twistDirection"] = x.TwistDirection,
            })]),
            ["barrels"] = new JsonArray([.. book.Barrels.Select(x => (JsonNode)new JsonObject { ["name"] = x.Name, ["rifle"] = x.Rifle, ["rounds"] = x.Rounds })]),
            ["loads"] = new JsonArray([.. book.Loads.Select(x => (JsonNode)new JsonObject
            {
                ["name"] = x.Name, ["components"] = x.Components, ["muzzleVelocityFps"] = x.MuzzleVelocityFps, ["muzzleVelocitySdFps"] = x.MuzzleVelocitySdFps,
                ["muzzleVelocitySdFrom"] = x.MuzzleVelocitySdFrom,
                ["ballisticCoefficient"] = x.BallisticCoefficient, ["dragModel"] = x.DragModel?.ToString(), ["bcReference"] = x.BcReference?.ToString(),
                ["bulletWeightGrains"] = x.BulletWeightGrains, ["bulletLengthInches"] = x.BulletLengthInches, ["bulletDiameterInches"] = x.BulletDiameterInches,
            })]),
            ["sessions"] = sessions,
            ["chronographStrings"] = new JsonArray([.. ReadStrings(db, null).Select(c => (JsonNode)new JsonObject
            {
                ["id"] = c.Id, ["sessionId"] = c.SessionId, ["source"] = c.Source, ["recordedUtc"] = c.RecordedUtc,
                ["velocitiesFps"] = new JsonArray([.. c.VelocitiesFps.Select(v => (JsonNode)v)]),
            })]),
            ["shotVelocities"] = new JsonArray([.. ReadMappings(db, null).Select(m => (JsonNode)new JsonObject
            {
                ["sessionId"] = m.SessionId, ["shotId"] = m.ShotId, ["stringId"] = m.StringId, ["ordinal"] = m.Ordinal,
            })]),
        };
        return root.ToJsonString(Indented) + "\n";
    }

    /// <summary>
    /// Reads an export into this database, which must hold no sessions, keeping every id so the mapping still points where it did. A different
    /// schema version is refused with the reason rather than read as if it were this one.
    /// </summary>
    public void Import(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var root = JsonNode.Parse(json) as JsonObject ?? throw new InvalidDataException("This is not a GroupLab export.");
        if ((string?)root["format"] != "grouplab-export")
        {
            throw new InvalidDataException("This is not a GroupLab export.");
        }

        if ((int?)root["schemaVersion"] != SchemaVersion)
        {
            throw new InvalidDataException($"This export has schema version {(int?)root["schemaVersion"]}, and this GroupLab reads version {SchemaVersion}.");
        }

        using var db = Connect();
        if ((long)Scalar(db, "SELECT count(*) FROM sessions")! > 0)
        {
            throw new InvalidOperationException("An export is read into an empty database only, so nothing already here is overwritten.");
        }

        using var transaction = db.BeginTransaction();
        var book = new RecordBook(
            [.. (root["rifles"] as JsonArray ?? []).Select(x => new Rifle((string)x!["name"]!, (double)x["clickValue"]!, Enum.Parse<AngularUnit>((string)x["clickUnit"]!))
            {
                SightHeightInches = (double?)x["sightHeightInches"], ZeroDistanceYards = (double?)x["zeroDistanceYards"], TwistInches = (double?)x["twistInches"],
                TwistDirection = (int?)x["twistDirection"],
            })],
            [.. (root["barrels"] as JsonArray ?? []).Select(x => new Barrel((string)x!["name"]!, (string?)x["rifle"], (int)x["rounds"]!))],
            [.. (root["loads"] as JsonArray ?? []).Select(x => new Load((string)x!["name"]!, (string?)x["components"])
            {
                MuzzleVelocityFps = (double?)x["muzzleVelocityFps"], MuzzleVelocitySdFps = (double?)x["muzzleVelocitySdFps"],
                MuzzleVelocitySdFrom = (string?)x["muzzleVelocitySdFrom"],
                BallisticCoefficient = (double?)x["ballisticCoefficient"],
                DragModel = (string?)x["dragModel"] is { } model ? Enum.Parse<DragModel>(model) : null,
                BcReference = (string?)x["bcReference"] is { } reference ? Enum.Parse<ReferenceAtmosphere>(reference) : null,
                BulletWeightGrains = (double?)x["bulletWeightGrains"], BulletLengthInches = (double?)x["bulletLengthInches"], BulletDiameterInches = (double?)x["bulletDiameterInches"],
            })]);
        WriteBook(db, book);
        foreach (var x in root["sessions"] as JsonArray ?? [])
        {
            WriteSession(db, new SessionRecord(
                (long)x!["id"]!, (string)x["createdUtc"]!, (string?)x["shotDate"], (string)x["sheetName"]!, (string?)x["definitionId"], (string?)x["definitionJson"],
                (double?)x["distanceInches"], (string?)x["rifle"], (string?)x["barrel"], (string?)x["load"], (double?)x["calibreInches"], (string)x["markingJson"]!,
                (int)x["shotCount"]!, (double?)x["meanRadiusInches"], (double?)x["meanRadiusLowerInches"], (double?)x["meanRadiusUpperInches"],
                (string?)x["imagePath"], (string?)x["imageSha256"], (string?)x["proofImage"] is { } proof ? Convert.FromBase64String(proof) : null, (string?)x["proofImageType"]));
        }

        foreach (var x in root["chronographStrings"] as JsonArray ?? [])
        {
            long id = (long)x!["id"]!;
            Execute(db, "INSERT INTO chronograph_strings VALUES ($id, $s, $src, $at)", ("$id", id), ("$s", (long)x["sessionId"]!), ("$src", (string)x["source"]!), ("$at", (string?)x["recordedUtc"]));
            int ordinal = 0;
            foreach (var v in x["velocitiesFps"] as JsonArray ?? [])
            {
                Execute(db, "INSERT INTO chronograph_shots VALUES ($id, $o, $v)", ("$id", id), ("$o", ++ordinal), ("$v", (double)v!));
            }
        }

        foreach (var x in root["shotVelocities"] as JsonArray ?? [])
        {
            Execute(db, "INSERT INTO shot_velocities VALUES ($s, $shot, $str, $o)", ("$s", (long)x!["sessionId"]!), ("$shot", (int)x["shotId"]!), ("$str", (long)x["stringId"]!), ("$o", (int)x["ordinal"]!));
        }

        transaction.Commit();
    }

    /// <summary>The statements that created this database, as SQLite keeps them, in the order they were run, for the schema document's test.</summary>
    public IReadOnlyList<string> CreatedSchema()
    {
        using var db = Connect();
        using var command = Command(db, "SELECT sql FROM sqlite_master WHERE sql IS NOT NULL ORDER BY rowid", []);
        using var r = command.ExecuteReader();
        var list = new List<string>();
        while (r.Read())
        {
            list.Add(r.GetString(0));
        }

        return list;
    }
}
