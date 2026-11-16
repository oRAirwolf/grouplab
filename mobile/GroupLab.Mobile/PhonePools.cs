using System.Globalization;
using GroupLab.App.Diagnostics;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 495, question 113 answered (b), on the phone as on the computer: a pooled set saved as a record of its own,
/// opened again from Sessions, shared as one file with its sheets, and a shared one opened whole.
/// </summary>
internal static class PhonePools
{
    /// <summary>Saves the pool of these saved sheets, or updates the one it was opened from; null where none of them is saved.</summary>
    public static PoolRecord? Save(IReadOnlyList<long> sheets, string choices, long? poolId)
    {
        if (sheets.Count == 0)
        {
            return null;
        }

        try
        {
            var pool = SessionPools.Save(PhoneAnalysis.Store(), poolId, sheets, choices, DateTime.UtcNow);
            DiagnosticLog.Info("sessions.pool.save", ("pool", pool.Id), ("sheets", pool.Sheets.Count), ("updated", poolId is not null));
            return pool;
        }
        catch (Microsoft.Data.Sqlite.SqliteException e)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "sessions.pool.save", e);
            return null;
        }
    }

    /// <summary>A saved pool as a result to show, from its sheets as they are now; null with the reason where none of its sheets is here.</summary>
    public static (PhoneResult? Result, string Said) Open(long poolId)
    {
        var store = PhoneAnalysis.Store();
        if (store.GetPool(poolId) is not { } pool)
        {
            return (null, "That pooled set is no longer here.");
        }

        var opened = SessionPools.Open(store, pool);
        DiagnosticLog.Info("sessions.pool.reopen", ("pool", pool.Id), ("sheets", opened.Sheets.Count), ("missing", opened.Missing));
        if (opened.Sheets.Count == 0)
        {
            return (null, opened.Said);
        }

        return (new PhoneResult(opened.Composite, opened.Definition, null, null, Pooled: true, PoolSheets: opened.SessionIds,
            PoolChoices: pool.Choices ?? SessionPools.Choices(pool.Pictures), PoolId: pool.Id), opened.Said);
    }

    /// <summary>Entry 495 section 4: the pool and its sheets' session files in one file, offered to the share sheet. Returns what to say.</summary>
    public static string Share(IReadOnlyList<long> sheets, string choices, UnitSettings units)
    {
        var store = PhoneAnalysis.Store();
        var records = sheets.Select(store.Get).OfType<SessionRecord>().ToList();
        if (records.Count == 0)
        {
            return SessionPools.NothingToSaveWords;
        }

        var names = SessionNames.For(records);
        var missing = records.Select((r, i) => (r, i)).Where(x => x.r.ImagePath is not { } p || !File.Exists(p)).Select(x => names[x.i].Name).ToList();
        if (missing.Count > 0)
        {
            return SessionPools.NotSharedWords(missing);
        }

        var files = new List<byte[]>();
        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            if (CleanImage.From(record.ImagePath!) is not { } clean)
            {
                return SessionPools.NotSharedWords([names[i].Name]);
            }

            var state = MarkingFile.Read(record.MarkingJson).State;
            var definition = record.DefinitionJson is { } json ? GltdJsonReader.Read(System.Text.Encoding.UTF8.GetBytes(json)).Definition : null;
            using var one = new MemoryStream();
            SessionPackage.Write(one, state, definition, units, clean.Bytes, clean.Extension, Phone.Platform.DeviceWords, 1, DateTime.UtcNow);
            files.Add(one.ToArray());
        }

        if (Directory.Exists(SessionFiles.Shared))
        {
            Directory.Delete(SessionFiles.Shared, recursive: true);
        }

        Directory.CreateDirectory(SessionFiles.Shared);
        string path = Path.Combine(SessionFiles.Shared, "pooled set " + DateTime.Now.ToString("yyyy-MM-dd HHmm", CultureInfo.InvariantCulture) + SessionPackage.Extension);
        using (var to = File.Create(path))
        {
            PoolPackage.Write(to, choices, files, Phone.Platform.DeviceWords, DateTime.UtcNow);
        }

        if (Phone.Platform.ShareFile(path, "application/octet-stream", "Share the pooled set") is { } failed)
        {
            return failed;
        }

        DiagnosticLog.Info("sessions.pool.share", ("sheets", files.Count));
        return SessionPools.SharedWords(files.Count);
    }

    /// <summary>A pool's file from the picker: its sheets brought in as sessions unless they are here already, the pool saved, and shown.</summary>
    public static (PhoneResult? Result, string? Why) OpenFile(Stream from)
    {
        try
        {
            var contents = PoolPackage.Read(from);
            string folder = Path.Combine(PhoneAnalysis.SessionsFolder, DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-pool");
            var brought = PoolPackage.BringIn(PhoneAnalysis.Store(), contents, folder, (state, definition, hash) =>
                SessionRecords.Build(state, definition, Phone.Settings.LoadUnits(), analyseSighters: false, null, hash, null, null, DateTime.UtcNow, DateTime.Now), DateTime.UtcNow);
            DiagnosticLog.Info("sessions.pool.file", ("opened", true), ("sheets", brought.Pool.Sheets.Count), ("added", brought.Added));
            var (result, said) = Open(brought.Pool.Id);
            return result is null ? (null, said) : (result, PoolPackage.OpenedWords(brought, contents.Device));
        }
        catch (Exception e) when (e is SessionPackageException or IOException or UnauthorizedAccessException or Microsoft.Data.Sqlite.SqliteException)
        {
            DiagnosticLog.Info("sessions.pool.file", ("opened", false), ("reason", e.GetType().Name));
            return (null, e.Message);
        }
    }
}
