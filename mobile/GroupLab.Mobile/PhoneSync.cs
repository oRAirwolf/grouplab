using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Records;
using GroupLab.Core.Sync;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 463 on the phone: synchronization through a folder the person's own cloud app keeps in step, run every five
/// minutes while GroupLab is open and at once from Sync now; the folder is chosen and reached through the platform (Android's folder picker,
/// a folder in Files on iPhone and iPad). The pages read what it last found from here.
/// </summary>
internal static class PhoneSync
{
    private static DispatcherTimer? timer;
    private static Task? running;
    private static bool warnedThisRun;

    /// <summary>Whether the last run found the folder gone.</summary>
    internal static bool Unreachable { get; private set; }

    internal static IReadOnlyList<SyncDeviceSeen> Others { get; private set; } = [];

    internal static (SyncCard Marks, SyncCard Originals, string Basis)? Cards { get; private set; }

    internal static bool Running => running is { IsCompleted: false };

    /// <summary>
    /// Entry 473 section 2: a folder in GroupLab's own storage that the phone walk's Sync picture uses in place of the platform's, since
    /// Android's folder picker cannot be driven; null everywhere else.
    /// </summary>
    internal static ISyncFolder? Sample { get; set; }

    private static T? With<T>(IPhonePlatform platform, Func<ISyncFolder, T> work) => Sample is { } sample ? work(sample) : platform.WithSyncFolder(work);

    /// <summary>Said whenever a run starts or finishes, so a page showing it fills again.</summary>
    internal static event Action? Changed;

    internal static SyncSetup Setup => Phone.Settings.LoadSync(DefaultName(Phone.Platform?.DeviceWords));

    /// <summary>
    /// The name this phone is first given in the sync folder: its maker and model, from the diagnostics line "GroupLab &lt;version&gt; on
    /// &lt;maker&gt; &lt;model&gt;". Entry 473's picture showed the whole line, version and commit, as the name the other devices would see.
    /// </summary>
    internal static string DefaultName(string? words)
    {
        if (words is not { Length: > 0 })
        {
            return "This phone";
        }

        int on = words.LastIndexOf(" on ", StringComparison.Ordinal);
        return on >= 0 && words[(on + 4)..].Trim() is { Length: > 0 } device ? device : words;
    }

    internal static void Save(SyncSetup setup) => Phone.Settings.SaveSync(setup);

    private static ISyncLocal Local() => new FileSyncLocal(Phone.Settings.SheetsFolder, Phone.Settings.Path);

    /// <summary>Starts the five-minute runs where a folder is chosen; called once the shell is up.</summary>
    internal static void Start()
    {
        if (!Setup.On)
        {
            return;
        }

        timer ??= new DispatcherTimer(SyncEngine.Every, DispatcherPriority.Background, (_, _) => _ = Now());
        timer.Start();
        _ = Now();
    }

    internal static async Task Choose(TopLevel? top)
    {
        if (Phone.Platform is not { } platform || await platform.ChooseSyncFolder(top) is not { } name)
        {
            return;
        }

        Save(Setup.Folder == name ? Setup : SyncScreens.InFolder(Setup, name));
        Unreachable = false;
        DiagnosticLog.Info("sync.folder", ("chosen", true));
        Start();
        Changed?.Invoke();
    }

    /// <summary>Stops here only: the folder is forgotten on this phone, and nothing here or in it is deleted.</summary>
    internal static void Stop()
    {
        timer?.Stop();
        Phone.Platform?.ForgetSyncFolder();
        Save(Setup with { Folder = null });
        Unreachable = false;
        DiagnosticLog.Info("sync.folder", ("stopped", true));
        Changed?.Invoke();
    }

    /// <summary>Forgets another device in the folder, as its "Forget this device" does once answered; nothing in the folder is deleted.</summary>
    internal static void Forget(string id)
    {
        if (Phone.Platform is not { } platform || !Setup.On)
        {
            return;
        }

        try
        {
            string me = PhoneAnalysis.Store().SyncDevice();
            bool done = With(platform, folder =>
            {
                if (!folder.Available)
                {
                    return false;
                }

                SyncForget.Forget(folder, me, id, DateTime.UtcNow);
                return true;
            });
            Unreachable = !done;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "sync.forget", e);
            Unreachable = true;
        }

        if (!Unreachable)
        {
            Others = [.. Others.Where(d => d.Id != id)];
            DiagnosticLog.Info("sync.forget", ("forgotten", true));
        }

        Changed?.Invoke();
    }

    /// <summary>One run now, or the one already running.</summary>
    internal static Task Now()
    {
        if (running is { IsCompleted: false } busy)
        {
            return busy;
        }

        if (Phone.Platform is not { } platform || Setup is not { On: true } setup)
        {
            return Task.CompletedTask;
        }

        running = Run(platform, setup);
        Changed?.Invoke();
        return running;
    }

    private static async Task Run(IPhonePlatform platform, SyncSetup setup)
    {
        try
        {
            var store = PhoneAnalysis.Store();
            var result = await Task.Run(() => With(platform, folder =>
            {
                var report = folder.Available ? SyncEngine.Run(store, Local(), folder, setup.DeviceName, DateTime.UtcNow, new SyncOptions(setup.Originals)) : null;
                return new Ran(report, SyncScreens.Others(folder, store.SyncDevice()));
            }));
            Cards = await Task.Run(() => SyncScreens.Cards(SyncEngine.Measure(store)));
            if (result?.Report is not { Reached: true } report)
            {
                Unreachable = true;
                DiagnosticLog.Info("sync.run", ("reached", false));
                if (!warnedThisRun && Shell.Current?.PageContent is Control page)
                {
                    // The first time in a run, in the middle of the screen as well as on the Sync page (Alan, 2026-10-02).
                    warnedThisRun = true;
                    ProblemSheet.Stop(page, null, SyncScreens.UnreachableTitle, SyncScreens.LikelyReason(phone: true) + " " + SyncScreens.NothingLost,
                        (SyncScreens.ChooseAgain, () => _ = Choose(TopLevel.GetTopLevel(page))), ("Later", () => { }));
                }

                return;
            }

            Unreachable = false;
            Others = result.Others;
            Save(SyncScreens.WithTidied(Setup with { LastUtc = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture), LastIn = report.Applied, LastOut = report.Written },
                report, DateTime.UtcNow));
            DiagnosticLog.Info("sync.run", ("in", report.Applied), ("out", report.Written), ("toChoose", report.ToChoose), ("waiting", report.FilesWaiting),
                ("tidied", report.Tidied?.Moved ?? 0), ("removed", report.Tidied?.Removed ?? 0));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or Microsoft.Data.Sqlite.SqliteException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "sync.run", e);
            Unreachable = true;
        }
        finally
        {
            Dispatcher.UIThread.Post(() => Changed?.Invoke());
        }
    }

    private sealed record Ran(SyncReport? Report, IReadOnlyList<SyncDeviceSeen> Others);

    /// <summary>The sessions changed on two devices, each with the session it is here (null where it is gone here).</summary>
    internal static IReadOnlyList<(SyncConflict Conflict, SessionRecord? Here, SessionRecord There)> ToChoose()
    {
        var store = PhoneAnalysis.Store();
        var waiting = new List<(SyncConflict, SessionRecord?, SessionRecord)>();
        foreach (var conflict in store.SyncConflicts().Where(c => c.Kind == SyncEngine.SessionKind))
        {
            if (SyncEngine.Versions(store, conflict) is { } versions)
            {
                waiting.Add((conflict, versions.Here, versions.There));
            }
        }

        return waiting;
    }

    /// <summary>The other device's name as it wrote it in the folder, where it can be read now.</summary>
    internal static string DeviceName(string code) =>
        (Phone.Platform is { } platform ? With(platform, folder =>
            folder.Available && folder.Read($"{SyncEngine.Root}/devices/{code}.json") is { } bytes
                && System.Text.Json.Nodes.JsonNode.Parse(bytes)?["name"]?.GetValue<string>() is { Length: > 0 } name ? name : null) : null) ?? "the other device";

    /// <summary>
    /// 2A on the phone: the two versions stacked, each with its device, time, small plot and rows, every row that differs tagged; then Keep
    /// each, Keep both and Decide later, as a centred sheet over <paramref name="page"/>'s content, which comes back when it closes.
    /// </summary>
    internal static void ShowConflict(UserControl page, long conflictId, Action done)
    {
        if (ToChoose().FirstOrDefault(w => w.Conflict.Id == conflictId) is not { Conflict: { } conflict } waiting || page.Content is not Control behind)
        {
            return;
        }

        string here = Setup.DeviceName, there = DeviceName(conflict.FromDevice);
        var compared = SyncScreens.Compare(waiting.Here, waiting.There, here, there, conflict.FoundUtc, Phone.Settings.LoadUnits());
        double reach = SyncHolesPlot.Reach(compared.HereHoles, compared.ThereHoles);
        Control Side(string device, string when, IReadOnlyList<GroupLab.Core.Imaging.PointD> holes, Func<SyncRow, string> value)
        {
            var side = new StackPanel { Spacing = 6 };
            side.Children.Add(Screens.Heading(device));
            side.Children.Add(Screens.Quiet(when));
            side.Children.Add(new SyncHolesPlot(holes, reach) { HorizontalAlignment = HorizontalAlignment.Left });
            foreach (var row in compared.Rows)
            {
                var line = new WrapPanel { ItemSpacing = 8 };
                line.Children.Add(Screens.Quiet(row.What));
                line.Children.Add(Screens.Line(value(row)));
                if (row.Differs)
                {
                    line.Children.Add(new Border { Child = new TextBlock { Text = SyncScreens.Differs, TextWrapping = TextWrapping.Wrap }, Padding = new Thickness(6, 1), Classes = { PhoneStyles.Pill } });
                }

                side.Children.Add(line);
            }

            return new Border { Child = side, Classes = { PhoneStyles.Card } };
        }

        void Close()
        {
            if (page.Content is Panel layer)
            {
                layer.Children.Remove(behind);
            }

            page.Content = behind;
            done();
        }

        void Choose(SyncChoice choice)
        {
            SyncEngine.Resolve(PhoneAnalysis.Store(), Local(), conflict.Id, choice, DateTime.UtcNow);
            DiagnosticLog.Info("sync.choose", ("choice", choice.ToString()));
            Close();
        }

        var body = new StackPanel { Spacing = 10 };
        body.Children.Add(Screens.Line(compared.Heading));
        body.Children.Add(Screens.Quiet(SyncScreens.BothKept));
        body.Children.Add(Side(here, compared.HereWhen, compared.HereHoles, r => r.Here));
        body.Children.Add(Side(there, compared.ThereWhen, compared.ThereHoles, r => r.There));
        Button[] choices =
        [
            ProblemSheet.Choice(SyncScreens.KeepThis(here), () => Choose(SyncChoice.KeepThisOne), primary: true),
            ProblemSheet.Choice(SyncScreens.KeepThis(there), () => Choose(SyncChoice.KeepTheOther)),
            ProblemSheet.Choice(SyncScreens.KeepBoth, () => Choose(SyncChoice.KeepBoth)),
            ProblemSheet.Choice(SyncScreens.DecideLater, Close),
        ];
        var buttons = new StackPanel { Spacing = 8 };
        foreach (var choice in choices)
        {
            buttons.Children.Add(choice);
        }

        body.Children.Add(buttons);
        page.Content = null;
        page.Content = ProblemSheet.Over(behind, SyncScreens.ConflictTitle, body, choices, Close);
    }
}

/// <summary>
/// 1A and 3B on the phone: the Sync page under Settings, its items stacked as the computer's section has them: the warning where the folder
/// cannot be reached, the folder and Change folder, this phone's name, the status line with Sync now, the other devices, the two cards and
/// Stop syncing.
/// </summary>
internal sealed class SyncPage : UserControl
{
    private readonly Action back;

    internal SyncPage(Action back)
    {
        this.back = back;
        PhoneSync.Changed += Refill;
        DetachedFromVisualTree += (_, _) => PhoneSync.Changed -= Refill;
        Refill();
    }

    private void Refill() => Content = Build();

    private Control Build()
    {
        var setup = PhoneSync.Setup;
        var column = new StackPanel { Spacing = 10 };
        column.Children.Add(Screens.BackButton("Back to Settings", back).Id("sync-back"));
        column.Children.Add(Screens.Title(SyncScreens.Title));
        if (PhoneSync.Unreachable && setup.On)
        {
            column.Children.Add(new Border
            {
                Child = new StackPanel
                {
                    Spacing = 6,
                    Children =
                    {
                        Screens.Heading(SyncScreens.UnreachableTitle),
                        Screens.Line(SyncScreens.LikelyReason(phone: true)),
                        Screens.Line(SyncScreens.NothingLost),
                        Screens.Primary(SyncScreens.ChooseAgain, () => _ = PhoneSync.Choose(TopLevel.GetTopLevel(this))).Id("sync-choose-again"),
                    },
                },
                Classes = { PhoneStyles.Card, PhoneStyles.Warn },
            }.Id("sync-unreachable"));
        }

        column.Children.Add(Screens.Dim(SyncScreens.Explained));
        if (!setup.On)
        {
            column.Children.Add(Screens.Primary(SyncScreens.ChooseFolder, () => _ = PhoneSync.Choose(TopLevel.GetTopLevel(this))).Id("sync-choose"));
            return Screens.Page(column);
        }

        column.Children.Add(Screens.Card(Screens.Line(setup.Folder ?? ""), Screens.Choice(SyncScreens.ChangeFolder, () => _ = PhoneSync.Choose(TopLevel.GetTopLevel(this))).Id("sync-change")));
        var name = new TextBox { Text = setup.DeviceName, MinHeight = Screens.Touch };
        Avalonia.Automation.AutomationProperties.SetName(name, SyncScreens.DeviceNameLabel);
        name.LostFocus += (_, _) =>
        {
            if (name.Text is { Length: > 0 } typed && typed.Trim() != PhoneSync.Setup.DeviceName)
            {
                PhoneSync.Save(PhoneSync.Setup with { DeviceName = typed.Trim() });
            }
        };
        column.Children.Add(Screens.Quiet(SyncScreens.DeviceNameLabel));
        column.Children.Add(name.Id("sync-name"));
        column.Children.Add(Screens.Line(PhoneSync.Running ? "Syncing…" : SyncScreens.Status(setup, DateTime.UtcNow) ?? "").Id("sync-status"));
        if (SyncScreens.TidyLine(setup, DateTime.UtcNow) is { } tidied)
        {
            // Entry 464: what the last tidying of old change files did, or nothing when there has been none.
            column.Children.Add(Screens.Dim(tidied).Id("sync-tidied"));
        }

        var now = Screens.Primary(SyncScreens.SyncNow, () => _ = PhoneSync.Now()).Id("sync-now");
        now.IsEnabled = !PhoneSync.Running;
        column.Children.Add(now);
        column.Children.Add(Screens.Heading(SyncScreens.OtherDevicesHeading));
        column.Children.Add(PhoneSync.Others.Count == 0
            ? Screens.Line(SyncScreens.NoOtherDevices)
            : Screens.Card([.. PhoneSync.Others.Select(DeviceRow)]));
        column.Children.Add(Screens.Heading(SyncScreens.OriginalsHeading));
        column.Children.Add(Cards(setup));
        column.Children.Add(Screens.Choice(SyncScreens.StopSyncing, PhoneSync.Stop).Id("sync-stop"));
        column.Children.Add(Screens.Dim(SyncScreens.StopsHereOnly));
        return Screens.Page(column);
    }

    /// <summary>
    /// Entry 471 section 2, question 109 (a): another device's line with "Forget this device", which asks once on the page, saying that
    /// nothing in the folder is deleted, before it forgets.
    /// </summary>
    private static Control DeviceRow(SyncDeviceSeen device)
    {
        var ask = new StackPanel { Spacing = 8, IsVisible = false };
        ask.Children.Add(Screens.Dim(SyncScreens.ForgetAsks(device.Name)));
        ask.Children.Add(Screens.Choice(SyncScreens.ForgetDevice, () => PhoneSync.Forget(device.Id)).Id("sync-forget-yes"));
        ask.Children.Add(Screens.Choice(SyncScreens.LeaveIt, () => ask.IsVisible = false));
        var forget = new Button { Content = SyncScreens.ForgetDevice, MinHeight = Screens.Touch, Classes = { GroupLab.App.Theme.AppStyles.Link } }.Id("sync-forget");
        Avalonia.Automation.AutomationProperties.SetName(forget, $"{SyncScreens.ForgetDevice}: {device.Name}");
        forget.Click += (_, _) => ask.IsVisible = true;
        return new StackPanel { Spacing = 4, Children = { Screens.Line(SyncScreens.SeenLine(device, DateTime.UtcNow)), forget, ask } };
    }

    /// <summary>3B stacked: the two cards, one chosen, each with its size now and a year at the person's pace in bars on one scale a row.</summary>
    private static Control Cards(SyncSetup setup)
    {
        if (PhoneSync.Cards is not { } cards)
        {
            return Screens.Line("Working out the sizes from your records…");
        }

        long nowMost = Math.Max(cards.Marks.NowBytes, cards.Originals.NowBytes), yearMost = Math.Max(cards.Marks.YearBytes, cards.Originals.YearBytes);
        int sessions = PhoneAnalysis.Store().List().Count;
        Control Bar(string label, long bytes, long most) => new StackPanel
        {
            Spacing = 2,
            Children =
            {
                Screens.Quiet($"{label}: {SyncScreens.Size(bytes)}"),
                new Border { Height = 8, Width = Math.Max(2, 200.0 * bytes / Math.Max(1, most)), HorizontalAlignment = HorizontalAlignment.Left, Classes = { PhoneStyles.SyncBar } },
            },
        };

        Control Card(SyncCard card, bool originals)
        {
            var choose = Screens.Radio("syncOriginals", card.Title, setup.Originals == originals);
            choose.IsCheckedChanged += (_, _) =>
            {
                if (choose.IsChecked == true && PhoneSync.Setup.Originals != originals)
                {
                    PhoneSync.Save(PhoneSync.Setup with { Originals = originals });
                }
            };
            return Screens.Card(choose, Screens.Line(card.Says), Bar($"Your {sessions} sessions now", card.NowBytes, nowMost), Bar("A year at your pace", card.YearBytes, yearMost));
        }

        return new StackPanel
        {
            Spacing = 10,
            Children = { Card(cards.Marks, false), Card(cards.Originals, true), Screens.Dim(cards.Basis + " " + SyncScreens.ScaleLine(Math.Max(nowMost, yearMost))) },
        };
    }
}
