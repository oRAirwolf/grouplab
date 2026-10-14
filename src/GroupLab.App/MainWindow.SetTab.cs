using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using GroupLab.App.Diagnostics;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Trace;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 423, Alan, 2026-10-13: "you should be able to drag multiple scans or photos into a single tab and it should
/// recognize that you want all 3 photos to be pooled together." Asked for by Quack and Jylee. It replaces entry 420 section 2, a tab for each
/// picture and then a "Read these as one set" button.
/// <para>
/// <b>How it is built.</b> A set's tab holds each picture as a tab holds one target, parked (<see cref="SetGroup"/>), and shows either the
/// pool or one picture. The pool is worked out again from the pictures each time it is shown, so a sheet checked and fixed alone is in it the
/// moment the pool is shown again. Each picture is saved as its own session record when it is read, so Session records shows what it would
/// have shown had the sheets been opened and pooled by hand.
/// </para>
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>True while a set's pictures are being read one after another in its tab; switching tabs waits for it.</summary>
    private bool readingSet;

    /// <summary>What the status line says before each stage of the detection while a set is read: "Reading sheet 2 of 3."</summary>
    private string? readingWords;

    /// <summary>The pool shown in a set's tab, for the headless tests: what pooling found, or null on pictures with no codes.</summary>
    internal SetPoolResult? SetPoolShown { get; private set; }

    /// <summary>What the pool shown last said: which sheets are here and missing, or which pictures still need a scale.</summary>
    private string poolWords = "";

    /// <summary>The set in the tab showing, for the headless tests.</summary>
    internal SetGroup? CurrentSet => targets.Current.Set;

    private Button setSheets = new();

    /// <summary>Says so and returns true while a set is being read, so a tab is not switched under it.</summary>
    private bool SetIsReading()
    {
        if (readingSet)
        {
            status.Text = SetTabWords.WaitForSet;
        }

        return readingSet;
    }

    /// <summary>The button on the analysis screen of a set's tab: the sheets to check one at a time, or the way back to the set.</summary>
    private Button BuildSetSheetsButton()
    {
        setSheets = new Button { Content = SetTabWords.CheckOneSheet, Margin = new Thickness(2), IsVisible = false };
        setSheets.Click += (_, _) =>
        {
            if (targets.Current.Set is { Showing: not null })
            {
                BackToSet();
            }
        };
        ToolTip.SetTip(setSheets, "Open one sheet of the set alone, to check its holes and its scale and fix them; the pool follows");
        return setSheets;
    }

    /// <summary>The button as the tab showing needs it, from every refresh.</summary>
    private void ShowSetSheetsButton()
    {
        var set = targets.Current.Set;
        setSheets.IsVisible = set is not null && analysing && !readingSet;
        if (set is null)
        {
            return;
        }

        if (set.Showing is not null)
        {
            setSheets.Content = "‹ " + SetTabWords.BackToSet;
            setSheets.Flyout = null;
            return;
        }

        setSheets.Content = SetTabWords.CheckOneSheet + " ▾";
        var menu = new MenuFlyout();
        foreach (var member in set.Members)
        {
            int shots = (member.Parked as ParkedTarget)?.Session.State.Shots.Count(s => s.IsShot) ?? 0;
            string words = string.Create(CultureInfo.InvariantCulture, $"{Upper(set.Called(member))}: {member.Name}, {shots} shot{(shots == 1 ? "" : "s")}")
                + (set.Counts(member) ? "" : ", not counted");
            var item = new MenuItem { Header = words };
            var chosen = member;
            item.Click += (_, _) => ShowSetSheet(set.Members.IndexOf(chosen));
            menu.Items.Add(item);
        }

        setSheets.Flyout = menu;
    }

    private static string Upper(string words) => char.ToUpperInvariant(words[0]) + words[1..];

    /// <summary>
    /// Entry 423 section 2: one picture of the set shown alone, with its own picture and marking, so Fix holes, the bull lines and Accept act
    /// on that sheet only. Under saving by itself it is saved as it is left; under saving with a button, leaving it asks first.
    /// </summary>
    internal void ShowSetSheet(int index)
    {
        if (targets.Current.Set is not { } set || index < 0 || index >= set.Members.Count || readingSet || ProblemOpen)
        {
            return;
        }

        var member = set.Members[index];
        if (ReferenceEquals(set.Showing, member))
        {
            return;
        }

        FlushWaitingSave();
        Leaving(() =>
        {
            SwitchSetView(set, member);
            status.Text = string.Create(CultureInfo.InvariantCulture,
                $"{Upper(set.Called(member))} of the set, alone: {member.Name}. Check and fix it as any sheet; {SetTabWords.BackToSet} shows the pool with what you changed.");
            DiagnosticLog.Info("set.sheet", ("member", index + 1), ("members", set.Members.Count));
        });
    }

    /// <summary>Entry 423 section 2: back from one picture to the pool, which is worked out again so a fix to the sheet is in it.</summary>
    internal void BackToSet()
    {
        if (targets.Current.Set is not { Showing: not null } set || readingSet)
        {
            return;
        }

        FlushWaitingSave();
        Leaving(() => ShowSetPool(set));
    }

    /// <summary>Parks what the set's tab is showing, a picture or the pool, and binds the picture asked for, or nothing for the pool.</summary>
    private void SwitchSetView(SetGroup set, SetMember? to)
    {
        var parked = TakeParked();
        if (set.Showing is { } was)
        {
            was.Parked = parked;
        }
        else
        {
            // The pool is worked out afresh each time it is shown, so its view is not kept.
            parked.View.Picture?.Dispose();
        }

        set.Showing = to;
        var arriving = to?.Parked as ParkedTarget ?? ParkedTarget.Fresh();
        if (to is not null)
        {
            to.Parked = null;
        }

        BindParked(arriving);
    }

    /// <summary>
    /// The pool in the set's tab: every sheet read that counts, each shot about its own bull with its own sheet's scale, exactly as Session
    /// records' Pool the chosen pools the same sheets; and which sheets are missing, read twice, or still waiting for a scale.
    /// </summary>
    private void ShowSetPool(SetGroup set)
    {
        if (set.Showing is not null || readingSet)
        {
            SwitchSetView(set, null);
        }

        var read = set.Members.Where(m => m.Read && m.Parked is ParkedTarget).ToList();
        // A sheet whose detection did not say which of the set it is keeps the number its codes gave when it was dropped.
        var states = read.Select(m => ((ParkedTarget)m.Parked!).Session.State).Select((s, i) => read[i].Sheet is { } k ? s with { SetSheet = s.SetSheet ?? k } : s).ToList();
        string said;
        MarkingState pooled;
        if (set.Definition is { } definition)
        {
            var counted = read.Select((m, i) => (m, i)).Where(x => set.Counts(x.m)).Select(x => states[x.i]).ToList();
            var result = SetPool.Pool(definition, counted);
            SetPoolShown = result;
            pooled = SetPool.Composite(definition, counted);
            said = result.Said;
            if (set.Contested.Count > 0)
            {
                said += " " + string.Join(" ", set.Contested.Select(k => string.Create(CultureInfo.InvariantCulture,
                    $"Sheet {k + 1} is in two pictures and is left out until you choose which one counts.")));
            }
        }
        else
        {
            SetPoolShown = null;
            pooled = SetPool.CompositeOfPictures(states);
            var unscaled = read.Where((m, i) => states[i].Scale is null).Select(set.Called).ToList();
            said = pooled.SheetLabel + ".";
            if (unscaled.Count > 0)
            {
                said += string.Create(CultureInfo.InvariantCulture,
                    $" {Upper(string.Join(", ", unscaled))} {(unscaled.Count == 1 ? "has" : "have")} no scale yet: choose each under {SetTabWords.CheckOneSheet}, set its scale and mark its shots.");
            }
        }

        ShowPooledHere(set.Definition, pooled);
        poolWords = said;
        DiagnosticLog.Info("set.pool", ("members", set.Members.Count), ("read", read.Count), ("shots", pooled.Shots.Count), ("contested", set.Contested.Count));
        status.Text = said + " Each shot is named sheet.bull. Tap a sheet in the plot's key to show it alone; " + SetTabWords.CheckOneSheet + " opens one to check or fix.";
        Refresh();
    }

    /// <summary>
    /// Entry 423 section 3: the same sheet in two different pictures. Neither counts until the person says which one does, asked in the middle
    /// of the window rather than in the status line.
    /// </summary>
    private void AskWhichCounts(SetGroup set)
    {
        if (set.Contested is not [var k, ..] || ProblemOpen)
        {
            return;
        }

        var both = set.Members.Where(m => m.Sheet == k).ToList();
        string names = string.Join(" and ", both.Select(m => m.Name));
        ShowProblem(string.Create(CultureInfo.InvariantCulture, $"Sheet {k + 1} is in two pictures"),
            string.Create(CultureInfo.InvariantCulture, $"{names} are both sheet {k + 1} of this set. Until you choose, neither counts in the pool. Which one counts?"),
            [.. both.Select(m => (string.Create(CultureInfo.InvariantCulture, $"Count {m.Name}"), (Action)(() =>
            {
                set.Chosen[k] = m;
                DiagnosticLog.Info("set.chosen", ("sheet", k + 1));
                if (ReferenceEquals(targets.Current.Set, set) && set.Showing is null)
                {
                    ShowSetPool(set);
                }

                AskWhichCounts(set);
            })))]);
    }

    /// <summary>
    /// Several pictures dropped or pasted together, entry 423 sections 1 and 3. Each one's codes are read first, one after another, never two
    /// at once. Then: the sheets of one set open in ONE tab and are read and pooled there; a set's tab dropped on takes more sheets of its own
    /// set; any other picture opens in a tab of its own; and two or more pictures with no codes are asked about once, in the middle of the
    /// window. A tab holding a target is never replaced; only an empty one is filled.
    /// </summary>
    private async Task DropSeveral(IReadOnlyList<string> paths)
    {
        if (detection is not null)
        {
            // A sheet's codes are never read on two threads at once (entry 414), and the detection running is reading one.
            status.Text = TargetTabWords.WaitForDetection;
            return;
        }

        readingSet = true;
        List<DroppedPicture> pictures;
        try
        {
            pictures = await ReadCodes(paths).ConfigureAwait(true);
        }
        finally
        {
            readingSet = false;
        }

        // A set's tab dropped on takes the pictures of its own set, and a pool of pictures with no codes takes more pictures with none.
        var said = new List<string>();
        var first = DropSorting.Of(pictures);
        var joining = new List<DroppedPicture>();
        if (targets.Current.Set is { } into)
        {
            joining = into.Definition is null
                ? [.. first.Unnamed]
                : [.. first.Sets.SelectMany(s => s).Concat(first.Alone).Where(p => p.DefinitionId == into.DefinitionId && p.SetSheet is not null)];
            var fresh = joining.Where(p => into.Members.All(m => m.Hash != p.Hash)).ToList();
            if (joining.Count > 0)
            {
                into.Members.AddRange(fresh.Select(p => new SetMember(p.Path, p.Hash, p.SetSheet)));
                said.Add(string.Create(CultureInfo.InvariantCulture, $"{fresh.Count} added to {into.Name}{(fresh.Count < joining.Count ? $" ({joining.Count - fresh.Count} already in it)" : "")}."));
                if (fresh.Count > 0)
                {
                    await ReadSet(into).ConfigureAwait(true);
                }
            }
        }

        var sorting = DropSorting.Of([.. pictures.Where(p => joining.All(j => j.Hash != p.Hash))]);
        var sets = sorting.Sets;
        var alone = sorting.Alone;
        var unnamed = sorting.Unnamed.ToList();

        bool poolUnnamed = false;
        if (unnamed.Count > 1)
        {
            poolUnnamed = await AskToPoolPictures(unnamed.Count).ConfigureAwait(true);
        }

        foreach (var set in sets)
        {
            var group = new SetGroup(set[0].Definition, set[0].DefinitionId);
            group.Members.AddRange(set.OrderBy(p => p.SetSheet).Select(p => new SetMember(p.Path, p.Hash, p.SetSheet)));
            if (!await IntoATab(group).ConfigureAwait(true))
            {
                said.Add(string.Create(CultureInfo.InvariantCulture, $"{group.Name} could not open: a question is still open."));
                continue;
            }

            await ReadSet(group).ConfigureAwait(true);
            said.Add(string.Create(CultureInfo.InvariantCulture, $"{group.Name}, in one tab."));
        }

        if (poolUnnamed)
        {
            var group = new SetGroup(null, null);
            group.Members.AddRange(unnamed.Select(p => new SetMember(p.Path, p.Hash, null)));
            if (await IntoATab(group).ConfigureAwait(true))
            {
                await ReadSet(group).ConfigureAwait(true);
                said.Add(string.Create(CultureInfo.InvariantCulture, $"{group.Name}, in one tab."));
            }

            unnamed = [];
        }

        int opened = 0;
        var single = alone.Concat(unnamed).ToList();
        foreach (var picture in single)
        {
            if (!await QuietWithin().ConfigureAwait(true) || !await IntoATab(null).ConfigureAwait(true) || !await OpenImageSafely(picture.Path).ConfigureAwait(true))
            {
                break;
            }

            opened++;
        }

        if (single.Count > 0)
        {
            said.Add(opened == single.Count
                ? string.Create(CultureInfo.InvariantCulture, $"{string.Join(", ", single.Select(p => Path.GetFileName(p.Path)))}: {(single.Count == 1 ? "a tab of its own" : "a tab each")}.")
                : string.Create(CultureInfo.InvariantCulture, $"{opened} of {single.Count} other pictures opened, a tab each; the rest could not be opened."));
        }

        int twice = pictures.Count - pictures.Select(p => p.Hash).Distinct(StringComparer.Ordinal).Count();
        DiagnosticLog.Info("drop.several", ("pictures", paths.Count), ("sets", sets.Count), ("alone", single.Count), ("pooledUnnamed", poolUnnamed));
        status.Text = string.Create(CultureInfo.InvariantCulture, $"{paths.Count} pictures dropped together: ") + string.Join(" ", said)
            + (twice > 0 ? string.Create(CultureInfo.InvariantCulture, $" {twice} {(twice == 1 ? "was the same picture" : "were the same pictures")} as another, so opened once.") : "")
            + (targets.Current.Set is { Showing: null } ? " " + poolWords : "");
    }

    /// <summary>
    /// The tab a drop's next target goes into: the tab showing when it is empty and nothing of this drop is in it yet, or a new one. Never a
    /// tab holding a target (entry 423 section 0). False where no tab could be shown, because a question is open.
    /// </summary>
    private async Task<bool> IntoATab(SetGroup? set)
    {
        if (!await QuietWithin().ConfigureAwait(true))
        {
            return false;
        }

        if (TabHoldsATarget && AddTarget(choose: false) is null)
        {
            return false;
        }

        targets.Current.Set = set;
        UpdateCurrentTab();
        return true;
    }

    /// <summary>Waits for the detection running and any question open to finish, at most ten minutes; whether they did.</summary>
    private async Task<bool> QuietWithin()
    {
        for (int waited = 0; (detection is not null || ProblemOpen) && waited < 6000; waited++)
        {
            await Task.Delay(100).ConfigureAwait(true);
        }

        return detection is null && !ProblemOpen;
    }

    /// <summary>Each picture's codes, read one after another off this thread: which sheet of which set it is, or nothing.</summary>
    private async Task<List<DroppedPicture>> ReadCodes(IReadOnlyList<string> paths)
    {
        var candidates = ShippedDefinitions().Concat(ownSheets.List().Select(s => s.Definition)).ToList();
        var read = new List<DroppedPicture>();
        for (int k = 0; k < paths.Count; k++)
        {
            status.Text = string.Create(CultureInfo.InvariantCulture, $"Reading the codes of picture {k + 1} of {paths.Count}…");
            string path = paths[k];
            read.Add(await Task.Run(() => ReadOneCode(path, candidates)).ConfigureAwait(true));
        }

        return read;
    }

    private static DroppedPicture ReadOneCode(string path, IReadOnlyList<GroupLab.Core.Gltd.Model.TargetDefinition> candidates)
    {
        try
        {
            string hash = Sha256(path);
            var (grey, _) = ImageLoader.Load(path);
            var identity = SheetIdentification.Identify(grey, candidates, new OpenCvSharpBackend(), new TraceRecorder());
            int? sheet = identity.Definition?.Tiling is { } tiling && tiling.Cols * tiling.Rows > 1 ? identity.TileIndex : null;
            DiagnosticLog.Info("drop.codes", [.. DiagnosticLog.File(path), ("definition", identity.DefinitionId), ("sheet", sheet)]);
            return new DroppedPicture(path, hash, identity.Definition, identity.DefinitionId, sheet);
        }
        catch (Exception ex) when (ImageLoader.IsTooLarge(ex) || ex is InvalidDataException or IOException or UnauthorizedAccessException or OpenCvSharp.OpenCVException)
        {
            // It opens, or says why it cannot, as any picture does; here it only has no codes to sort it by.
            return new DroppedPicture(path, "unread:" + path, null, null, null);
        }
    }

    /// <summary>Entry 423 section 3: two or more pictures with no codes. "Pool them in one tab" is the default; closing the question is "One tab each".</summary>
    private Task<bool> AskToPoolPictures(int n)
    {
        var answer = new TaskCompletionSource<bool>();
        ShowProblem(SetTabWords.PoolQuestion(n),
            "Their codes could not be read, or they have none: a target from a shop, a photograph with no GroupLab codes, or a sheet whose codes did not read. Pooled, they are one group in one tab, and each picture still needs its own scale.",
            (SetTabWords.PoolQuestionDefault, () => answer.TrySetResult(true)),
            (SetTabWords.PoolQuestionOther, () => answer.TrySetResult(false)));
        problemDismissed = () => answer.TrySetResult(false);
        return answer.Task;
    }

    /// <summary>
    /// Entry 423 section 1: the set's pictures not yet read, opened and read one after another inside its tab, the status saying "Reading sheet
    /// 2 of 3", each saved as its own session record once read; then the pool. Pictures with no codes are opened without detection, which
    /// cannot read them; each waits for its scale.
    /// </summary>
    private async Task ReadSet(SetGroup set)
    {
        var unread = set.Members.Where(m => !m.Read).ToList();
        bool detect = DetectOnOpen;
        readingSet = true;
        try
        {
            DetectOnOpen = detect && set.Definition is not null;
            for (int k = 0; k < unread.Count; k++)
            {
                var member = unread[k];
                SwitchSetView(set, member);
                readingWords = string.Create(CultureInfo.InvariantCulture, $"Reading {(set.Definition is null ? "picture" : "sheet")} {k + 1} of {unread.Count}. ");
                if (!await OpenImageSafely(member.Path).ConfigureAwait(true))
                {
                    set.Members.Remove(member);
                    set.Showing = null;
                    continue;
                }

                status.Text = readingWords + status.Text;
                if (DetectionTask is { } running && DetectOnOpen)
                {
                    await running.ConfigureAwait(true);
                }

                await QuietWithin().ConfigureAwait(true);
                member.Read = true;
                if (sessions is not null && session.State.Scale is not null)
                {
                    SaveSession();
                }
            }
        }
        finally
        {
            DetectOnOpen = detect;
            readingSet = false;
            readingWords = null;
        }

        ShowSetPool(set);
        UpdateCurrentTab();
        AskWhichCounts(set);
    }
}
