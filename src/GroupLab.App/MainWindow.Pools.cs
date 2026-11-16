using System.Globalization;
using Avalonia.Platform.Storage;
using GroupLab.App.Diagnostics;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 495, question 113 answered (b): a pooled set saved as a record of its own. The pooled tab knows which saved
/// sheets it pools (<see cref="PoolSources"/>); Accept and analyze, Save and closing the tab save the pool, or update the one it was opened
/// from, and never a session. A saved pool opens from Session records into the same pooled tab, its figures worked out again from its sheets,
/// and is shared as one file holding its sheets' session files, so it opens whole on another GroupLab.
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>The saved sheets a pooled tab pools, in order, how they are pooled, and the pool record once there is one.</summary>
    private sealed record PoolSources(IReadOnlyList<long> SessionIds, string Choices, long? PoolId);

    /// <summary>What the pooled tab showing pools; null on a tab that is not a pool.</summary>
    private PoolSources? poolSources;

    /// <summary>The pool record the tab showing is saved as, for the headless tests.</summary>
    internal long? CurrentPool => poolSources?.PoolId;

    /// <summary>
    /// Saves the pool the tab shows, or updates the one it was opened from, and says so. Nothing is saved where no sheet of it is saved: a
    /// pool's record names its sheets, never their shots.
    /// </summary>
    internal PoolRecord? SavePoolHere(bool quietly = false)
    {
        if (sessions is null || !showingPool || poolSources is not { SessionIds.Count: > 0 } sources)
        {
            if (!quietly && showingPool)
            {
                status.Text = SessionPools.NothingToSaveWords;
            }

            return null;
        }

        var pool = SavePool(sources);
        poolSources = sources with { PoolId = pool.Id };
        if (targets.Current.Set is { } set)
        {
            set.PoolId = pool.Id;
        }

        if (!quietly)
        {
            status.Text = SessionPools.SavedWords(pool.Sheets.Count);
        }

        ShowSaved();
        return pool;
    }

    private PoolRecord SavePool(PoolSources sources)
    {
        var pool = SessionPools.Save(sessions!, sources.PoolId, sources.SessionIds, sources.Choices, DateTime.UtcNow);
        DiagnosticLog.Info("sessions.pool.save", ("pool", pool.Id), ("sheets", pool.Sheets.Count), ("updated", sources.PoolId is not null));
        return pool;
    }

    /// <summary>Saves the pools of the tabs not showing, as closing the window does for the one showing.</summary>
    private void SaveParkedPools()
    {
        if (sessions is null)
        {
            return;
        }

        foreach (var tab in targets.All)
        {
            if (!ReferenceEquals(tab, targets.Current) && tab.Parked is ParkedTarget { ShowingPool: true, PoolSources: { SessionIds.Count: > 0 } sources } parked)
            {
                parked.PoolSources = sources with { PoolId = SavePool(sources).Id };
            }
        }
    }

    /// <summary>Opens a saved pool from Session records into its own pooled tab, from its sheets as they are now.</summary>
    internal void OpenSavedPool(long poolId)
    {
        if (sessions?.GetPool(poolId) is not { } pool)
        {
            return;
        }

        OpenedPool opened;
        try
        {
            opened = SessionPools.Open(sessions, pool);
        }
        catch (MarkingFileException ex)
        {
            problem.Text = "A sheet of this pool could not be read: " + ex.Message;
            ShowProblem("The pooled set did not open", problem.Text);
            return;
        }

        if (opened.Sheets.Count == 0)
        {
            status.Text = opened.Said;
            return;
        }

        if (AddTarget(choose: false) is null)
        {
            return;
        }

        targets.Current.PooledScales = opened.Scales;
        ShowPooledHere(opened.Definition, opened.Composite);
        poolSources = new PoolSources(opened.SessionIds, pool.Choices ?? SessionPools.Choices(pool.Pictures), pool.Id);
        ShowSaved();
        status.Text = opened.Said + " Each shot is named sheet.bull. Tap a sheet in the plot's key to show it alone.";
        DiagnosticLog.Info("sessions.pool.reopen", ("pool", pool.Id), ("sheets", opened.Sheets.Count), ("missing", opened.Missing));
    }

    /// <summary>Entry 495 section 4: the pool and its sheets' session files in one file, so it opens whole on another GroupLab.</summary>
    private async Task SharePoolDialog()
    {
        if (sessions is null || poolSources is not { SessionIds.Count: > 0 } sources)
        {
            problem.Text = SessionPools.NothingToSaveWords;
            return;
        }

        var records = sources.SessionIds.Select(sessions.Get).OfType<SessionRecord>().ToList();
        var names = SessionNames.For(records);
        var missing = records.Select((r, i) => (r, i)).Where(x => x.r.ImagePath is not { } p || !File.Exists(p)).Select(x => names[x.i].Name).ToList();
        if (missing.Count > 0)
        {
            problem.Text = SessionPools.NotSharedWords(missing);
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save a pooled set file to share",
            SuggestedFileName = "pooled set " + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + SessionPackage.Extension,
            DefaultExtension = SessionPackage.Extension.TrimStart('.'),
            FileTypeChoices = [new FilePickerFileType("GroupLab pooled set file") { Patterns = ["*" + SessionPackage.Extension] }],
        });
        if (file?.TryGetLocalPath() is { } path)
        {
            SharePoolTo(path);
        }
    }

    /// <summary>Writes the pool's file to <paramref name="path"/>: the share without its picker, for the headless tests too.</summary>
    internal void SharePoolTo(string path)
    {
        if (sessions is null || poolSources is not { SessionIds.Count: > 0 } sources)
        {
            problem.Text = SessionPools.NothingToSaveWords;
            return;
        }

        var records = sources.SessionIds.Select(sessions.Get).OfType<SessionRecord>().ToList();
        var names = SessionNames.For(records);
        var pool = SavePoolHere(quietly: true);
        var sheets = new List<byte[]>();
        foreach (var record in records)
        {
            if (CleanImage.From(record.ImagePath!) is not { } clean)
            {
                problem.Text = SessionPools.NotSharedWords([names[records.IndexOf(record)].Name]);
                return;
            }

            var state = MarkingFile.Read(record.MarkingJson).State;
            var definition = record.DefinitionJson is { } json ? GltdJsonReader.Read(System.Text.Encoding.UTF8.GetBytes(json)).Definition : null;
            using var one = new MemoryStream();
            SessionPackage.Write(one, state, definition, units, clean.Bytes, clean.Extension, DeviceWords, 1, DateTime.UtcNow);
            sheets.Add(one.ToArray());
        }

        using (var to = File.Create(path))
        {
            PoolPackage.Write(to, pool?.Choices ?? sources.Choices, sheets, DeviceWords, DateTime.UtcNow);
        }

        status.Text = SessionPools.SharedWords(sheets.Count);
        DiagnosticLog.Info("sessions.pool.share", ("sheets", sheets.Count));
    }

    /// <summary>
    /// Opens a pool's file: each sheet brought in as a session unless it is here already, the pool saved naming them, and the pool opened in
    /// its own tab.
    /// </summary>
    internal void OpenPoolFile(string path)
    {
        if (sessions is null)
        {
            problem.Text = "The session database could not be opened, so the pooled set cannot be saved here.";
            return;
        }

        PoolBroughtIn brought;
        string device;
        try
        {
            PoolPackageContents contents;
            using (var from = File.OpenRead(path))
            {
                contents = PoolPackage.Read(from);
            }

            device = contents.Device;
            string folder = Path.Combine(SharedSessionsFolder, DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-pool");
            brought = PoolPackage.BringIn(sessions, contents, folder, BuildSharedRecord, DateTime.UtcNow);
        }
        catch (Exception ex) when (ex is SessionPackageException or IOException or UnauthorizedAccessException or Microsoft.Data.Sqlite.SqliteException)
        {
            problem.Text = ex.Message;
            ShowProblem("The pooled set file did not open", ex.Message);
            DiagnosticLog.Info("sessions.pool.file", ("opened", false), ("reason", ex.GetType().Name));
            return;
        }

        OpenSavedPool(brought.Pool.Id);
        status.Text = PoolPackage.OpenedWords(brought, device);
        DiagnosticLog.Info("sessions.pool.file", ("opened", true), ("sheets", brought.Pool.Sheets.Count), ("added", brought.Added));
    }

    /// <summary>A shared sheet's record, as Accept and analyze would save it, with its proof image from the picture unpacked here.</summary>
    private SessionRecord BuildSharedRecord(MarkingState state, TargetDefinition? definition, string hash)
    {
        var (proof, proofType) = ProofImage(state.ImagePath, state.Scale);
        return SessionRecords.Build(state, definition, units, analyseSighters, null, hash, proof, proofType, DateTime.UtcNow, DateTime.Now);
    }

    /// <summary>A pool's rows for Session records, kept to the rifle and load chosen, as the sessions are.</summary>
    private IReadOnlyList<(SessionSummary Row, OpenedPool Pool)> PoolRows(string? rifle, string? load) =>
        sessions is null ? [] : SessionPools.Rows(sessions, units, rifle, load);

    /// <summary>What pooling these chosen sessions is: the sheets of a set, each by its number.</summary>
    private static PoolSources PooledSessions(IReadOnlyList<SessionRecord> records) =>
        new([.. records.Select(r => r.Id)], SessionPools.Choices(pictures: false), null);
}
