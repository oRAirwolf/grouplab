using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Records;
using GroupLab.Core.Sync;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 463, the three synchronization screens Alan chose from the canvas on 2026-10-21, on the computer. 1A: one
/// "Sync between devices" section in Settings: the folder in full with Change folder, this device's name, the status line with Sync now,
/// the other devices seen, the originals cards and Stop syncing; a folder that cannot be reached is said at the top of it in amber and,
/// the first time in a run, in the centred warning. 2A: a session changed on two devices side by side, kept both ways until the person
/// chooses, found again from a banner and a "2 versions" tag on Sessions. 3B: two cards, marks and proof pictures or also the originals,
/// each with its size now and a year at the person's pace in bars on one scale a row. The engine is entry 461's <see cref="SyncEngine"/>.
/// </summary>
public partial class MainWindow
{
    private readonly StackPanel syncSection = new() { Spacing = Tokens.Space8 };
    private SyncSetup? syncSetup;
    private bool syncUnreachable;
    private bool syncWarnedThisRun;
    private Task? syncRun;

    private bool syncRunning => syncRun is { IsCompleted: false };
    private DispatcherTimer? syncTimer;
    private (SyncCard Marks, SyncCard Originals, string Basis)? syncCards;
    private IReadOnlyList<SyncDeviceSeen> syncOthers = [];

    /// <summary>This device's synchronization as it now is, for the headless tests.</summary>
    internal SyncSetup Sync => syncSetup ??= settingsStore.LoadSync(Environment.MachineName);

    /// <summary>The words of the sync section, in order, for the headless tests.</summary>
    internal IEnumerable<string> SyncText => syncSection.GetLogicalDescendants()
        .Select(c => c switch { TextBlock t => t.Text, ContentControl { Content: string words } => words, _ => null })
        .OfType<string>().Where(t => t.Length > 0);

    /// <summary>The sync section's buttons, for the headless tests.</summary>
    internal IEnumerable<Button> SyncButtons => syncSection.GetLogicalDescendants().OfType<Button>();

    /// <summary>The words on Sessions, its buttons' included, for the headless tests.</summary>
    internal IEnumerable<string> SessionsText => sessionRows.GetLogicalDescendants()
        .Select(c => c switch { TextBlock t => t.Text, ContentControl { Content: string words } => words, _ => null })
        .OfType<string>().Where(t => t.Length > 0);

    private ISyncLocal SyncLocal() => new FileSyncLocal(settingsStore.SheetsFolder, settingsStore.Path);

    private void BuildSyncSettings(StackPanel column)
    {
        column.Children.Add(syncSection);
        FillSync();
        if (Sync.On)
        {
            StartSyncTimer();
        }
    }

    private void StartSyncTimer()
    {
        syncTimer ??= new DispatcherTimer(SyncEngine.Every, DispatcherPriority.Background, (_, _) => _ = SyncNowAsync());
        syncTimer.Start();
        _ = SyncNowAsync();
    }

    private void SaveSync(SyncSetup setup)
    {
        syncSetup = setup;
        settingsStore.SaveSync(setup);
    }

    /// <summary>This device's name as the other devices see it, as the name box sets it.</summary>
    internal void NameSyncDevice(string name)
    {
        SaveSync(Sync with { DeviceName = name });
        FillSync();
    }

    /// <summary>Chooses the folder to sync through, by its path (the picker gives one), and runs at once.</summary>
    internal void ChooseSyncFolder(string folder)
    {
        SaveSync(Sync.Folder == folder ? Sync : SyncScreens.InFolder(Sync, folder));
        syncUnreachable = false;
        DiagnosticLog.Info("sync.folder", ("chosen", true));
        StartSyncTimer();
        FillSync();
    }

    private async Task PickSyncFolderAsync()
    {
        var picked = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = SyncScreens.ChooseFolder, AllowMultiple = false });
        if (picked.Count > 0 && picked[0].TryGetLocalPath() is { } path)
        {
            ChooseSyncFolder(path);
        }
    }

    /// <summary>Stops synchronizing on this device only: the folder is forgotten here, and nothing here or in it is deleted.</summary>
    internal void StopSyncing()
    {
        syncTimer?.Stop();
        SaveSync(Sync with { Folder = null });
        syncUnreachable = false;
        DiagnosticLog.Info("sync.folder", ("stopped", true));
        FillSync();
    }

    /// <summary>One run now, as Sync now and the timer start it, or the one already running; a folder that is gone is said, and nothing is changed or lost.</summary>
    internal Task SyncNowAsync()
    {
        if (syncRun is { IsCompleted: false } running)
        {
            return running;
        }

        if (sessions is null || Sync is not { On: true } setup)
        {
            return Task.CompletedTask;
        }

        syncRun = RunSyncAsync(sessions, setup);
        FillSync();
        return syncRun;
    }

    private async Task RunSyncAsync(SessionStore store, SyncSetup setup)
    {
        try
        {
            var folder = new DirectorySyncFolder(setup.Folder!);
            var (report, others, cards) = await Task.Run(() =>
            {
                var r = folder.Available ? SyncEngine.Run(store, SyncLocal(), folder, setup.DeviceName, DateTime.UtcNow, new SyncOptions(setup.Originals)) : null;
                return (r, SyncScreens.Others(folder, store.SyncDevice()), SyncScreens.Cards(SyncEngine.Measure(store)));
            });
            syncCards = cards;
            syncOthers = others;
            if (report is not { Reached: true })
            {
                SyncUnreachable();
                return;
            }

            syncUnreachable = false;
            SaveSync(SyncScreens.WithTidied(Sync with { LastUtc = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture), LastIn = report.Applied, LastOut = report.Written },
                report, DateTime.UtcNow));
            DiagnosticLog.Info("sync.run", ("in", report.Applied), ("out", report.Written), ("toChoose", report.ToChoose), ("waiting", report.FilesWaiting),
                ("tidied", report.Tidied?.Moved ?? 0), ("removed", report.Tidied?.Removed ?? 0));
            if (report.Applied > 0 || report.ToChoose > 0)
            {
                FillSessions();
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "sync.run", e);
            SyncUnreachable();
        }
        finally
        {
            Dispatcher.UIThread.Post(FillSync);
        }
    }

    /// <summary>
    /// The folder cannot be reached: said at the top of the section and, the first time in this run, in the centred warning (Alan, 2026-10-02).
    /// Nothing is deleted or overwritten because it is missing; changes made here wait on this device.
    /// </summary>
    private void SyncUnreachable()
    {
        syncUnreachable = true;
        DiagnosticLog.Info("sync.run", ("reached", false));
        if (!syncWarnedThisRun)
        {
            syncWarnedThisRun = true;
            ShowProblem(SyncScreens.UnreachableTitle, SyncScreens.LikelyReason(phone: false) + " " + SyncScreens.NothingLost,
                (SyncScreens.ChooseAgain, () => _ = PickSyncFolderAsync()), ("Later", () => { }));
        }
    }

    internal void FillSync()
    {
        syncSection.Children.Clear();
        var setup = Sync;
        if (syncUnreachable && setup.On)
        {
            var warning = new StackPanel { Spacing = Tokens.Space4 };
            warning.Children.Add(new TextBlock { Text = SyncScreens.UnreachableTitle, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
            warning.Children.Add(new TextBlock { Text = SyncScreens.LikelyReason(phone: false), TextWrapping = TextWrapping.Wrap });
            warning.Children.Add(new TextBlock { Text = SyncScreens.NothingLost, TextWrapping = TextWrapping.Wrap });
            warning.Children.Add(Row(Button(SyncScreens.ChooseAgain, () => _ = PickSyncFolderAsync())));
            syncSection.Children.Add(new Border { Child = warning, Padding = new Thickness(Tokens.Space12), Classes = { AppStyles.TableRow, AppStyles.Warn } });
        }

        syncSection.Children.Add(Line(SyncScreens.Explained));
        if (!setup.On)
        {
            syncSection.Children.Add(Row(Button(SyncScreens.ChooseFolder + "…", () => _ = PickSyncFolderAsync())));
            return;
        }

        // The folder, in full, with Change folder.
        syncSection.Children.Add(new TextBlock { Text = setup.Folder, FontFamily = Mono, TextWrapping = TextWrapping.Wrap });
        syncSection.Children.Add(Row(Button(SyncScreens.ChangeFolder + "…", () => _ = PickSyncFolderAsync())));

        // This device's name, as the others see it.
        var name = new TextBox { Text = setup.DeviceName, MinWidth = 260, HorizontalAlignment = HorizontalAlignment.Left };
        Avalonia.Automation.AutomationProperties.SetName(name, SyncScreens.DeviceNameLabel);
        name.LostFocus += (_, _) =>
        {
            if (name.Text is { Length: > 0 } typed && typed.Trim() != Sync.DeviceName)
            {
                SaveSync(Sync with { DeviceName = typed.Trim() });
            }
        };
        syncSection.Children.Add(new TextBlock { Text = SyncScreens.DeviceNameLabel, Classes = { AppStyles.Label } });
        syncSection.Children.Add(name);

        // The status line, with Sync now.
        var status = new TextBlock { Text = syncRunning ? "Syncing…" : SyncScreens.Status(setup, DateTime.UtcNow), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
        var now = Button(SyncScreens.SyncNow, () => _ = SyncNowAsync());
        now.IsEnabled = !syncRunning;
        syncSection.Children.Add(new WrapPanel { ItemSpacing = Tokens.Space12, LineSpacing = Tokens.Space4, Children = { status, now } });

        // Entry 464: what the last tidying of old change files did, or nothing when there has been none.
        if (SyncScreens.TidyLine(setup, DateTime.UtcNow) is { } tidied)
        {
            syncSection.Children.Add(Line(tidied));
        }

        // The other devices seen in the folder.
        syncSection.Children.Add(new TextBlock { Text = SyncScreens.OtherDevicesHeading, Classes = { AppStyles.Label } });
        if (syncOthers.Count == 0)
        {
            syncSection.Children.Add(Line(SyncScreens.NoOtherDevices));
        }

        // Entry 471 section 2: each with Forget this device, which asks once on the page before it forgets.
        foreach (var device in syncOthers)
        {
            syncSection.Children.Add(SyncDeviceRow(device));
        }

        // 3B: the two cards.
        syncSection.Children.Add(new TextBlock { Text = SyncScreens.OriginalsHeading, Classes = { AppStyles.Label } });
        syncSection.Children.Add(SyncCardsView(setup));

        // Stop syncing, and what it does.
        syncSection.Children.Add(Row(Button(SyncScreens.StopSyncing, StopSyncing)));
        syncSection.Children.Add(Line(SyncScreens.StopsHereOnly));
    }

    /// <summary>
    /// Entry 471 section 2, question 109 (a): another device's line with "Forget this device", which asks once on the page, saying that
    /// nothing in the folder is deleted, before it forgets.
    /// </summary>
    private Control SyncDeviceRow(SyncDeviceSeen device)
    {
        var ask = new StackPanel { Spacing = Tokens.Space4, IsVisible = false };
        ask.Children.Add(Line(SyncScreens.ForgetAsks(device.Name)));
        ask.Children.Add(Row(Button(SyncScreens.ForgetDevice, () => ForgetSyncDevice(device.Id)), Button(SyncScreens.LeaveIt, () => ask.IsVisible = false)));
        var forget = new Button { Content = SyncScreens.ForgetDevice, Classes = { AppStyles.Link } };
        Avalonia.Automation.AutomationProperties.SetName(forget, $"{SyncScreens.ForgetDevice}: {device.Name}");
        forget.Click += (_, _) => ask.IsVisible = true;
        var line = Line(SyncScreens.SeenLine(device, DateTime.UtcNow));
        line.VerticalAlignment = VerticalAlignment.Center;
        return new StackPanel { Spacing = Tokens.Space4, Children = { Row(line, forget), ask } };
    }

    /// <summary>Forgets another device in the folder, as its "Forget this device" does once answered; nothing in the folder is deleted.</summary>
    internal void ForgetSyncDevice(string id)
    {
        if (sessions is null || Sync is not { On: true } setup)
        {
            return;
        }

        try
        {
            var folder = new DirectorySyncFolder(setup.Folder!);
            if (!folder.Available)
            {
                SyncUnreachable();
                return;
            }

            SyncForget.Forget(folder, sessions.SyncDevice(), id, DateTime.UtcNow);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            SyncUnreachable();
            return;
        }

        syncOthers = [.. syncOthers.Where(d => d.Id != id)];
        DiagnosticLog.Info("sync.forget", ("forgotten", true));
        FillSync();
    }

    /// <summary>3B: the two cards side by side, one chosen, each with its size now and a year at the person's pace, the bars on one scale a row.</summary>
    private Control SyncCardsView(SyncSetup setup)
    {
        if (syncCards is not { } cards)
        {
            return Line("Working out the sizes from your records…");
        }

        long nowMost = Math.Max(cards.Marks.NowBytes, cards.Originals.NowBytes), yearMost = Math.Max(cards.Marks.YearBytes, cards.Originals.YearBytes);
        Control Bar(string label, long bytes, long most)
        {
            var bar = new Border { Height = 8, Width = Math.Max(2, 220.0 * bytes / Math.Max(1, most)), HorizontalAlignment = HorizontalAlignment.Left, Classes = { AppStyles.SyncBar } };
            var words = new TextBlock { Text = $"{label}: {SyncScreens.Size(bytes)}", Classes = { AppStyles.Secondary } };
            return new StackPanel { Spacing = Tokens.Space4, Children = { words, bar } };
        }

        Control Card(SyncCard card, bool originals)
        {
            var choose = new RadioButton { GroupName = "syncOriginals", IsChecked = setup.Originals == originals, Content = card.Title, FontWeight = FontWeight.SemiBold };
            choose.IsCheckedChanged += (_, _) =>
            {
                if (choose.IsChecked == true && Sync.Originals != originals)
                {
                    SaveSync(Sync with { Originals = originals });
                }
            };
            var body = new StackPanel { Spacing = Tokens.Space8, Children = { choose, new TextBlock { Text = card.Says, TextWrapping = TextWrapping.Wrap } } };
            body.Children.Add(Bar($"Your {SyncEngineSessions} sessions now", card.NowBytes, nowMost));
            body.Children.Add(Bar("A year at your pace", card.YearBytes, yearMost));
            return new Border { Child = body, Padding = new Thickness(Tokens.Space12), MinWidth = 260, Classes = { AppStyles.TableRow } };
        }

        var both = new WrapPanel { ItemSpacing = Tokens.Space12, LineSpacing = Tokens.Space12, Children = { Card(cards.Marks, false), Card(cards.Originals, true) } };
        var column = new StackPanel { Spacing = Tokens.Space8, Children = { both } };
        column.Children.Add(Line(cards.Basis + " " + SyncScreens.ScaleLine(Math.Max(nowMost, yearMost))));
        return column;
    }

    private int SyncEngineSessions => sessions?.List().Count ?? 0;

    /// <summary>The sessions changed on two devices, waiting to be chosen between, each with the session it is here (null where it is gone here).</summary>
    internal IReadOnlyList<(SyncConflict Conflict, SessionRecord? Here, SessionRecord There)> SessionsToChoose()
    {
        if (sessions is null)
        {
            return [];
        }

        var waiting = new List<(SyncConflict, SessionRecord?, SessionRecord)>();
        foreach (var conflict in sessions.SyncConflicts().Where(c => c.Kind == SyncEngine.SessionKind))
        {
            if (SyncEngine.Versions(sessions, conflict) is { } versions)
            {
                waiting.Add((conflict, versions.Here, versions.There));
            }
        }

        return waiting;
    }

    /// <summary>The banner over Sessions while a session waits to be chosen between, or null.</summary>
    private Control? SyncChooseBanner(IReadOnlyList<(SyncConflict Conflict, SessionRecord? Here, SessionRecord There)> waiting)
    {
        if (waiting.Count == 0)
        {
            return null;
        }

        var line = new TextBlock { Text = SyncScreens.ToChooseBanner(waiting.Count), FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center };
        var open = Button("Choose", () => ShowSyncConflict(waiting[0].Conflict.Id));
        return new Border
        {
            Child = new WrapPanel { ItemSpacing = Tokens.Space12, Children = { line, open } },
            Padding = new Thickness(Tokens.Space12, Tokens.Space8),
            Margin = new Thickness(0, 0, 0, Tokens.Space8),
            Classes = { AppStyles.TableRow, AppStyles.Warn },
        };
    }

    /// <summary>
    /// 2A: the session changed on two devices, side by side: the heading's rifle or load, sheet and date, each version's device, time, small
    /// plot and rows, every row that differs tagged. Keeping one keeps the other where it can be got back (entry 461 section 3.1 point 6).
    /// </summary>
    internal void ShowSyncConflict(long conflictId)
    {
        if (sessions is null || SessionsToChoose().FirstOrDefault(w => w.Conflict.Id == conflictId) is not { Conflict: { } conflict } waiting)
        {
            return;
        }

        string here = Sync.DeviceName;
        string there = SyncDeviceName(conflict.FromDevice);
        var compared = SyncScreens.Compare(waiting.Here, waiting.There, here, there, conflict.FoundUtc, units);
        double reach = SyncHolesPlot.Reach(compared.HereHoles, compared.ThereHoles);
        Control Side(string device, string when, IReadOnlyList<GroupLab.Core.Imaging.PointD> holes, Func<SyncRow, string> value)
        {
            var side = new StackPanel { Spacing = Tokens.Space8 };
            side.Children.Add(new TextBlock { Text = device, FontWeight = FontWeight.SemiBold });
            side.Children.Add(new TextBlock { Text = when, Classes = { AppStyles.Secondary } });
            side.Children.Add(new SyncHolesPlot(holes, reach) { HorizontalAlignment = HorizontalAlignment.Left });
            foreach (var row in compared.Rows)
            {
                var line = new WrapPanel { ItemSpacing = Tokens.Space8 };
                line.Children.Add(new TextBlock { Text = row.What, Classes = { AppStyles.Label } });
                line.Children.Add(new TextBlock { Text = value(row), TextWrapping = TextWrapping.Wrap });
                if (row.Differs)
                {
                    line.Children.Add(new Border { Child = new TextBlock { Text = SyncScreens.Differs, Classes = { AppStyles.PillText, AppStyles.Warn } }, Classes = { AppStyles.Pill } });
                }

                side.Children.Add(line);
            }

            return side;
        }

        var columns = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = Tokens.Space24 };
        var mine = Side(here, compared.HereWhen, compared.HereHoles, r => r.Here);
        var theirs = Side(there, compared.ThereWhen, compared.ThereHoles, r => r.There);
        Grid.SetColumn(theirs, 1);
        columns.Children.Add(mine);
        columns.Children.Add(theirs);
        var body = new StackPanel { Spacing = Tokens.Space12 };
        body.Children.Add(new TextBlock { Text = compared.Heading, TextWrapping = TextWrapping.Wrap });
        body.Children.Add(new TextBlock { Text = SyncScreens.BothKept, Classes = { AppStyles.Secondary } });
        body.Children.Add(columns);
        var choices = new List<(string Label, Action Act)>
        {
            (SyncScreens.KeepThis(here), () => ResolveSync(conflict.Id, SyncChoice.KeepThisOne)),
            (SyncScreens.KeepThis(there), () => ResolveSync(conflict.Id, SyncChoice.KeepTheOther)),
            (SyncScreens.KeepBoth, () => ResolveSync(conflict.Id, SyncChoice.KeepBoth)),
            (SyncScreens.DecideLater, () => { }),
        };
        var row = new WrapPanel { ItemSpacing = Tokens.Space8, LineSpacing = Tokens.Space8 };
        for (int k = 0; k < choices.Count; k++)
        {
            row.Children.Add(ProblemButton(choices[k].Label, primary: k == 0));
        }

        body.Children.Add(row);
        OpenProblem(SyncScreens.ConflictTitle, ProblemCard(SyncScreens.ConflictTitle, 760, body), choices);
    }

    private void ResolveSync(long conflictId, SyncChoice choice)
    {
        if (sessions is null)
        {
            return;
        }

        SyncEngine.Resolve(sessions, SyncLocal(), conflictId, choice, DateTime.UtcNow);
        DiagnosticLog.Info("sync.choose", ("choice", choice.ToString()));
        FillSessions();
    }

    /// <summary>The other device's name as it wrote it in the folder, or its code where the folder is not reachable now.</summary>
    private string SyncDeviceName(string code)
    {
        if (Sync is { On: true } setup)
        {
            var folder = new DirectorySyncFolder(setup.Folder!);
            if (folder.Available && folder.Read($"{SyncEngine.Root}/devices/{code}.json") is { } bytes
                && System.Text.Json.Nodes.JsonNode.Parse(bytes)?["name"]?.GetValue<string>() is { Length: > 0 } name)
            {
                return name;
            }
        }

        return "the other device";
    }
}
