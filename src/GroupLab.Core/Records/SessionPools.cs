using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Marking;


namespace GroupLab.Core.Records;

/// <summary>A saved pool opened: its sheets as they are now, how many it has lost, and the pooled marking and what pooling found.</summary>
public sealed record OpenedPool(PoolRecord Pool, IReadOnlyList<SessionRecord> Sheets, IReadOnlyList<long> SessionIds, int Missing, TargetDefinition? Definition,
    MarkingState Composite, SetPoolResult? Result, IReadOnlyList<ScaleReference?> Scales, string Said)
{
    /// <summary>Entry 495 section 2: a pool needs two sheets to be one; with fewer it still opens, and says so.</summary>
    public bool TooFew => Sheets.Count < 2;
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 495, question 113 answered (b): a pooled set saved as a record of its own, on the computer and the phone
/// alike. Making one, finding the one already made of the same sheets, opening it from its sheets as they are now, and how it is named in
/// Session records and Compare. A pool's figures are never stored: they are worked out from its sheets each time, as entry 420's pool was.
/// </summary>
public static class SessionPools
{
    /// <summary>
    /// A pool's row in a list of sessions has the negative of its id, so one list can hold both, sorted and filtered as one, and no session's
    /// id (always positive) is ever taken for a pool's.
    /// </summary>
    public static long RowId(long poolId) => -poolId;

    /// <summary>The pool a row stands for, or null for a session's row.</summary>
    public static long? PoolOf(long rowId) => rowId < 0 ? -rowId : null;

    /// <summary>A new key: sixteen hex digits at random, which names the pool on every device it reaches.</summary>
    public static string NewKey() => Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();

    /// <summary>
    /// The choices a pool is made with: pictures in order or the sheets of a set, and for a set, each sheet's number in the set where its
    /// own marking may not say (the set tab's numbers, entry 423), in the order of the sheets.
    /// </summary>
    public static string Choices(bool pictures, IReadOnlyList<int?>? numbers = null)
    {
        var choices = new JsonObject { ["as"] = pictures ? PoolRecord.AsPictures : PoolRecord.AsSet };
        if (!pictures && numbers is not null && numbers.Any(n => n is not null))
        {
            choices["numbers"] = new JsonArray([.. numbers.Select(n => n is { } k ? JsonValue.Create(k) : null)]);
        }

        return choices.ToJsonString();
    }

    /// <summary>
    /// Saves the pool of these sessions and returns it: the pool <paramref name="poolId"/> names, updated to them; otherwise the pool already
    /// made of the same sheets pooled the same way; otherwise a new one.
    /// </summary>
    public static PoolRecord Save(SessionStore store, long? poolId, IReadOnlyList<long> sessionIds, string choices, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(sessionIds);
        var sheets = sessionIds.Select(store.Get).OfType<SessionRecord>().Select(DataExport.Identity).ToList();
        var existing = poolId is { } id ? store.GetPool(id) : null;
        existing ??= store.Pools().FirstOrDefault(p => p.Choices == choices && p.Sheets.SequenceEqual(sheets, StringComparer.Ordinal));
        var pool = existing is null
            ? new PoolRecord(0, NewKey(), utcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture), sheets, choices)
            : existing with { Sheets = sheets, Choices = choices };
        if (existing is not null && existing.SameAs(pool))
        {
            return existing;
        }

        long saved = store.SavePool(pool);
        return pool with { Id = saved };
    }

    /// <summary>The pool's sheets that are here, in its order, with their ids, and how many it names that are not.</summary>
    public static (IReadOnlyList<SessionRecord> Sheets, IReadOnlyList<long> Ids, int Missing) SheetsOf(SessionStore store, PoolRecord pool)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(pool);
        var identities = store.SessionIdentities();
        var sheets = new List<SessionRecord>();
        var ids = new List<long>();
        int missing = 0;
        // Each session once: a pool that names one identity twice is two sessions sharing it, taken oldest first.
        foreach (string identity in pool.Sheets)
        {
            if (identities.TryGetValue(identity, out var sharing) && sharing.FirstOrDefault(i => !ids.Contains(i)) is var id and > 0 && store.Get(id) is { } sheet)
            {
                sheets.Add(sheet);
                ids.Add(id);
            }
            else
            {
                missing++;
            }
        }

        return (sheets, ids, missing);
    }

    /// <summary>
    /// The pool opened from its sheets as they are now, the way the pooled tab showed it when it was made: a set by each sheet's number in the
    /// set, or pictures each its own sheet in order.
    /// </summary>
    public static OpenedPool Open(SessionStore store, PoolRecord pool)
    {
        var (sheets, ids, missing) = SheetsOf(store, pool);
        var numbers = Numbers(pool);
        var places = new List<int>();
        foreach (var s in sheets)
        {
            // The sheet's place in the pool's list, each place once where two sheets share an identity.
            string identity = DataExport.Identity(s);
            places.Add(Enumerable.Range(0, pool.Sheets.Count).First(i => pool.Sheets[i] == identity && !places.Contains(i)));
        }

        var states = sheets.Select((s, i) =>
        {
            var state = MarkingFile.Read(s.MarkingJson).State;
            int at = places[i];
            return state.SetSheet is null && at < numbers.Count && numbers[at] is { } k ? state with { SetSheet = k } : state;
        }).ToList();
        var definition = pool.Pictures ? null : SharedDefinition(sheets);
        MarkingState composite;
        SetPoolResult? result = null;
        string said;
        if (definition is { Tiling: { } set } && set.Cols * set.Rows >= 2 && states.Count > 0 && states.All(s => s.SetSheet is not null))
        {
            result = SetPool.Pool(definition, states);
            composite = SetPool.Composite(definition, states);
            said = result.Said;
        }
        else
        {
            composite = SetPool.CompositeOfPictures(states);
            said = composite.SheetLabel + ".";
            definition = null;
        }

        if (missing > 0)
        {
            said += string.Create(CultureInfo.InvariantCulture, $" {Counted(missing, "sheet")} of this pool {(missing == 1 ? "is" : "are")} not in Session records here.");
        }

        if (sheets.Count < 2)
        {
            said += " " + TooFewWords(sheets.Count);
        }

        return new OpenedPool(pool, sheets, ids, missing, definition, composite, result, [.. states.Select(s => s.Scale)], said);
    }

    /// <summary>
    /// The pool as one saved group, for Session records' row and for Compare (entry 495 sections 2 and 5): its pooled marking and figures,
    /// named a pooled set of N sheets, dated by its newest sheet, and its row's id (<see cref="RowId"/>).
    /// </summary>
    public static SessionRecord AsRecord(OpenedPool opened, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(opened);
        ArgumentNullException.ThrowIfNull(units);
        var newest = opened.Sheets.OrderByDescending(s => s.ShotDate ?? s.CreatedUtc[..10], StringComparer.Ordinal).ThenByDescending(s => s.CreatedUtc, StringComparer.Ordinal).FirstOrDefault();
        var built = SessionRecords.Build(opened.Composite, null, units, false, null, null, null, null, DateTime.UtcNow, DateTime.Now);
        string? Same(Func<SessionRecord, string?> of) => opened.Sheets.Select(of).Distinct().ToList() is [var only] ? only : null;
        return built with
        {
            Id = RowId(opened.Pool.Id),
            CreatedUtc = newest?.CreatedUtc ?? opened.Pool.CreatedUtc,
            ShotDate = newest?.ShotDate ?? opened.Pool.CreatedUtc[..10],
            SheetName = RowName(opened.Sheets.Count) + (Same(s => s.DefinitionJson is null ? null : DefinitionName(s)) is { } name ? ", " + name : ""),
            Rifle = Same(s => s.Rifle),
            Barrel = Same(s => s.Barrel),
            Load = Same(s => s.Load),
        };
    }

    /// <summary>The pool's row for a list of sessions.</summary>
    public static SessionSummary Summary(SessionRecord asRecord)
    {
        ArgumentNullException.ThrowIfNull(asRecord);
        return new SessionSummary(asRecord.Id, asRecord.CreatedUtc, asRecord.ShotDate, asRecord.SheetName, asRecord.DistanceInches, asRecord.Rifle, asRecord.Load,
            asRecord.ShotCount, asRecord.MeanRadiusInches, asRecord.MeanRadiusLowerInches, asRecord.MeanRadiusUpperInches);
    }

    /// <summary>
    /// Every saved pool as a row, kept to <paramref name="rifle"/> and <paramref name="load"/> as the sessions are, so one list sorts and
    /// filters both as one.
    /// </summary>
    public static IReadOnlyList<(SessionSummary Row, OpenedPool Pool)> Rows(SessionStore store, UnitSettings units, string? rifle = null, string? load = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        var rows = new List<(SessionSummary, OpenedPool)>();
        foreach (var pool in store.Pools())
        {
            OpenedPool opened;
            try
            {
                opened = Open(store, pool);
            }
            catch (MarkingFileException)
            {
                continue;
            }

            var row = Summary(AsRecord(opened, units));
            if ((rifle is null || string.Equals(row.Rifle, rifle, StringComparison.OrdinalIgnoreCase)) && (load is null || string.Equals(row.Load, load, StringComparison.OrdinalIgnoreCase)))
            {
                rows.Add((row, opened));
            }
        }

        return rows;
    }

    /// <summary>Sessions and pools in one list, newest first by when each was saved (a pool by its newest sheet), as Session records lists them.</summary>
    public static IReadOnlyList<SessionSummary> Merged(IReadOnlyList<SessionSummary> sessions, IEnumerable<SessionSummary> pools) =>
        [.. sessions.Concat(pools).OrderByDescending(s => s.CreatedUtc, StringComparer.Ordinal).ThenByDescending(s => s.Id < 0 ? long.MaxValue : s.Id)];

    /// <summary>
    /// Entry 495 section 5: the chosen rows of Session records as records to compare, oldest first, a pool as one group beside the sessions
    /// and other pools (<see cref="CompareSessions.From"/>).
    /// </summary>
    public static IReadOnlyList<SessionRecord> Chosen(SessionStore store, UnitSettings units, IReadOnlyCollection<long> rowIds)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(rowIds);
        var pools = rowIds.Any(id => id < 0) ? Rows(store, units).ToDictionary(p => p.Row.Id, p => AsRecord(p.Pool, units)) : [];
        return [.. Merged(store.List(), pools.Values.Select(Summary)).Where(s => rowIds.Contains(s.Id)).Reverse()
            .Select(s => s.Id < 0 ? pools[s.Id] : store.Get(s.Id)).OfType<SessionRecord>()];
    }

    // ---- Words, the same on the computer and the phone.

    /// <summary>What a pool's row is called in Session records: "Pooled set of 3 sheets".</summary>
    public static string RowName(int sheets) => "Pooled set of " + Counted(sheets, "sheet");

    /// <summary>Entry 495 section 3: what the pooled tab says once the pool is saved.</summary>
    public static string SavedWords(int sheets) => string.Create(CultureInfo.InvariantCulture,
        $"Saved in Session records as a pooled set of {Counted(sheets, "sheet")}. Its figures are worked out again from its sheets each time it opens, so a hole fixed on one sheet changes the pool too.");

    /// <summary>What the pooled tab says when it cannot be saved, with nothing saved to pool.</summary>
    public const string NothingToSaveWords = "This pool has no saved sheets to keep: accept each sheet first, then the pool is saved with them.";

    /// <summary>Entry 495 section 4: what sharing a pool says once its file is written.</summary>
    public static string SharedWords(int sheets) => string.Create(CultureInfo.InvariantCulture,
        $"Saved a pooled set file to share: the pool and its {Counted(sheets, "sheet")}, each with its marks, its sheet and its picture, and nothing else from the photographs. It opens whole on another GroupLab.");

    /// <summary>A pool that cannot be shared whole, because a sheet's picture is not here.</summary>
    public static string NotSharedWords(IReadOnlyList<string> sheets) => sheets.Count == 1
        ? $"The picture of {sheets[0]} is not where it was, so the pool cannot be shared whole. Put the picture back, or export the pooled shots as a CSV."
        : $"The pictures of {string.Join(", ", sheets.Take(sheets.Count - 1))} and {sheets[^1]} are not where they were, so the pool cannot be shared whole. Put them back, or export the pooled shots as a CSV.";

    /// <summary>A pool opened with fewer than two sheets.</summary>
    public static string TooFewWords(int sheets) => sheets == 0
        ? "None of its sheets is here any more, so there is nothing to pool."
        : "Only one of its sheets is here now, and a pool needs two: this shows that sheet alone.";

    /// <summary>Asked before a pool is deleted.</summary>
    public const string DeletePoolQuestion = "Delete this pooled set? Its sheets stay in Session records.";

    /// <summary>Said once a pool is deleted.</summary>
    public static string DeletedWords(int sheets) => string.Create(CultureInfo.InvariantCulture,
        $"Deleted the pooled set. Its {Counted(sheets, "sheet")} {(sheets == 1 ? "is" : "are")} still in Session records.");

    /// <summary>
    /// Entry 495 section 2: asked before a sheet a pool uses is deleted, naming each pool and what happens to it, or null where no pool uses it.
    /// </summary>
    public static string? DeleteSheetQuestion(IReadOnlyList<(PoolRecord Pool, string Name)> using_)
    {
        ArgumentNullException.ThrowIfNull(using_);
        if (using_.Count == 0)
        {
            return null;
        }

        string names = using_.Count == 1 ? using_[0].Name : string.Join(", ", using_.Take(using_.Count - 1).Select(u => u.Name)) + " and " + using_[^1].Name;
        bool tooFew = using_.Any(u => u.Pool.Sheets.Count <= 2);
        string where = using_.Count == 1 ? "a pooled set" : string.Create(CultureInfo.InvariantCulture, $"{using_.Count} pooled sets");
        string keeps = using_.Count == 1 ? "That pool keeps" : "Each keeps";
        string after = tooFew ? ", and a pool left with one sheet says so when it is opened." : ".";
        return $"Delete this session? It is in {where}: {names}. {keeps} its other sheets{after}";
    }

    /// <summary>What a pool is called where it is named alone: "Pooled set of 3 sheets, 12 Nov 2026".</summary>
    public static string Called(SessionSummary row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return row.SheetName.Split(", ")[0] + (row.ShotDate is { Length: >= 10 } date && DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
            ? ", " + day.ToString("d MMM yyyy", CultureInfo.InvariantCulture)
            : "");
    }

    private static string Counted(int n, string noun) => string.Create(CultureInfo.InvariantCulture, $"{n} {noun}{(n == 1 ? "" : "s")}");

    private static List<int?> Numbers(PoolRecord pool)
    {
        try
        {
            return pool.Choices is { } json && JsonNode.Parse(json) is JsonObject o && o["numbers"] is JsonArray a
                ? [.. a.Select(n => n is JsonValue v && v.TryGetValue(out int k) ? (int?)k : null)]
                : [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    private static TargetDefinition? SharedDefinition(IReadOnlyList<SessionRecord> sheets)
    {
        if (sheets.Count == 0 || sheets.Select(s => s.DefinitionId).Distinct().Count() != 1 || sheets[0].DefinitionJson is not { } json)
        {
            return null;
        }

        try
        {
            return GltdJsonReader.Read(System.Text.Encoding.UTF8.GetBytes(json)).Definition;
        }
        catch (Exception e) when (e is System.Text.Json.JsonException or InvalidDataException or FormatException or ArgumentException)
        {
            return null;
        }
    }

    private static string? DefinitionName(SessionRecord sheet) => SharedDefinition([sheet])?.Name;
}
