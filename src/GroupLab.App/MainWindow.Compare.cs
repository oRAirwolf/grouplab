using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Statistics;
using GroupLab.Core.Trace;
using GroupLab.Core.Reporting;

using GroupLab.Core.Gltd.Model;

namespace GroupLab.App;

/// <summary>
/// Comparing loads, NOTES-FROM-PLANNING.md entry 113 section 2, the concept's compare-loads screen and the rail's chart slot. Two or more
/// sessions chosen in Session records, or the subgroups of the sheet open in the analysis, side by side: each group's composite plot and
/// figures with their intervals, the tests with their verdicts, what every test could have detected, and the planning table. The groups stay
/// in the order they were chosen; nothing here ranks them by a point estimate, and when the intervals overlap it says the data do not
/// separate them, which matters more than any chart on the screen.
/// </summary>
public sealed partial class MainWindow
{
    private readonly StackPanel compareColumn = new() { Margin = new Thickness(Tokens.Space24, Tokens.Space20), Spacing = Tokens.Space12 };
    private readonly HashSet<long> sessionChosen = [];
    private readonly Control compareBody;
    private List<(string Name, IReadOnlyList<PointD> Offsets, string Detail, string? Speed)> compareGroups = [];

    /// <summary>The distance every compared group is given at: their own where they share one, the first's where they were scaled to it.</summary>
    private double? compareDistance;
    private string? compareFooting;
    private LoadComparisonReport? comparison;

    private Control BuildCompare() => new ScrollViewer { Content = compareColumn, IsVisible = false };

    /// <summary>Chooses or unchooses a session for comparing, as its row's box does.</summary>
    internal void ChooseSession(long id, bool chosen)
    {
        if (chosen)
        {
            sessionChosen.Add(id);
        }
        else
        {
            sessionChosen.Remove(id);
        }
    }

    /// <summary>
    /// The chosen sessions as groups: each its shots about their own bulls, excluded ones left out as every comparison figure leaves them out,
    /// and sighters never in. Groups shot at different distances are compared as angles, scaled to the first one's distance, and it says so.
    /// </summary>
    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 243 section 3.1: the chosen sessions, when they are sheets of one set from "Made for your optic", pooled
    /// into one group, every shot about its own bull; and which sheets of the set are still missing. Scanned or photographed in any order,
    /// each sheet knows from its own codes which of the set it is.
    /// </summary>
    internal void PoolChosen()
    {
        sessionPoolPanel.Children.Clear();
        if (sessions is null || sessionChosen.Count == 0)
        {
            sessionPoolPanel.Children.Add(Line("Tick the sheets of one set, then Pool the chosen."));
            return;
        }

        var records = sessions.List().Where(s => sessionChosen.Contains(s.Id)).Select(s => sessions.Get(s.Id)!).ToList();
        var ids = records.Select(r => r.DefinitionId).Distinct().ToList();
        var definitions = records.GroupBy(r => r.DefinitionId).ToDictionary(g => g.Key ?? "", g => g.First().DefinitionJson is { } json
            ? GroupLab.Core.Gltd.Json.GltdJsonReader.Read(System.Text.Encoding.UTF8.GetBytes(json)).Definition : null);
        var definition = ids.Count == 1 ? definitions[ids[0] ?? ""] : null;
        var states = records.Select(r => MarkingFile.Read(r.MarkingJson).State).ToList();
        if (ids.Count > 1 && SetPool.FindStandIn([.. records.Select((r, i) => (r.DefinitionId, definitions[r.DefinitionId ?? ""], states[i]))]) is { } standIn)
        {
            PoolWithStandIns(records, states, standIn);
            return;
        }

        if (definition is null || definition.Tiling is not { } set || set.Cols * set.Rows < 2 || states.Any(s => s.SetSheet is null))
        {
            Refused(ids.Count > 1 ? PoolRefusal.DifferentDesigns(records) : PoolRefusal.NotASet);
            return;
        }

        PoolSet(definition, records, states, null, null);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 512 section 3 (report 57's suggestion): loose sheets ticked with sheets of a set, whose bulls are the
    /// set's, each counted as a sheet of the set missing from those ticked, so a spoiled sheet can be replaced. Where one place is left for
    /// one loose sheet it takes it and says so; where it could be several, it asks which, one loose sheet at a time.
    /// </summary>
    private void PoolWithStandIns(IReadOnlyList<SessionRecord> records, IReadOnlyList<MarkingState> states, StandIn standIn)
    {
        if (standIn.Loose.Count > standIn.Places.Count)
        {
            Refused(PoolRefusal.NoPlaceLeft(standIn.Set.Name, standIn.Loose.Count, standIn.Places.Count));
            return;
        }

        var names = SessionNames.For(records);
        var numbers = new int?[records.Count];
        var placed = new List<string>();

        void Next(int j)
        {
            if (j == standIn.Loose.Count)
            {
                var counted = states.Select((s, i) => numbers[i] is { } k ? s with { SetSheet = k } : s).ToList();
                PoolSet(standIn.Set, records, counted, numbers, string.Join(" ", placed));
                return;
            }

            int at = standIn.Loose[j];
            var free = standIn.Places.Where(k => !numbers.Contains(k)).ToList();

            void Take(int k)
            {
                numbers[at] = k;
                placed.Add(PoolRefusal.CountsAs(names[at].Name, k));
                DiagnosticLog.Info("sessions.pool.stand-in", ("place", k + 1), ("asked", free.Count > 1));
                Next(j + 1);
            }

            if (free.Count == 1)
            {
                Take(free[0]);
                return;
            }

            ShowProblem(PoolRefusal.WhichSheetTitle, PoolRefusal.WhichSheetWhy(names[at].Name, standIn.Set.Name),
                [.. free.Select(k => (PoolRefusal.SheetChoice(k), (Action)(() => Take(k))))]);
        }

        Next(0);
    }

    /// <summary>The sheets of one set pooled and opened in their own tab; <paramref name="numbers"/> are the places of loose sheets standing in.</summary>
    private void PoolSet(TargetDefinition definition, IReadOnlyList<SessionRecord> records, IReadOnlyList<MarkingState> states, IReadOnlyList<int?>? numbers, string? standIns)
    {
        var pooled = SetPool.Pool(definition, states);
        DiagnosticLog.Info("sessions.pool", ("sheets", states.Count), ("set", pooled.SetSize), ("missing", pooled.Missing.Count), ("shots", pooled.Shots));
        sessionPoolPanel.Children.Add(new TextBlock { Text = definition.Name, Classes = { AppStyles.Title } });
        sessionPoolPanel.Children.Add(Line(pooled.Said));
        if (!string.IsNullOrEmpty(standIns))
        {
            sessionPoolPanel.Children.Add(Line(standIns));
        }

        if (pooled.Figures is { } figures)
        {
            string Inches(GroupLab.Core.Marking.ReportedEstimate? e) => e is { } v
                ? string.Create(CultureInfo.InvariantCulture, $"{v.Value:0.000} in") + (v.Lower is { } lo && v.Upper is { } hi ? string.Create(CultureInfo.InvariantCulture, $", 95% interval {lo:0.000} to {hi:0.000} in") : "")
                : "not quoted";
            sessionPoolPanel.Children.Add(Line($"Mean radius {Inches(figures.MeanRadius)}"));
            sessionPoolPanel.Children.Add(Line($"Sigma {Inches(figures.Sigma)}"));
            sessionPoolPanel.Children.Add(Line($"Extreme spread {Inches(figures.ExtremeSpread)}"));
        }

        // Entry 420 section 1, Quack's question: the pooled set opens in the analysis screen, in its own tab, as a sheet does.
        if (pooled.Shots > 0)
        {
            OpenPooled(definition, SetPool.Composite(definition, states), [.. states.Select(s => s.Scale)]);
            // Entry 495: the pool is saved with these sheets when it is accepted, saved or closed; entry 512, with a stand-in's place.
            poolSources = numbers is null ? PooledSessions(records) : new PoolSources([.. records.Select(r => r.Id)], SessionPools.Choices(pictures: false, numbers), null);
        }
    }

    /// <summary>
    /// Error report 57: Pool the chosen refused a loose sheet ticked with two sheets of a set in a small grey line under the buttons, which
    /// the person did not see, so the button seemed to do nothing. The refusal is now in amber, names the designs ticked, is said in the
    /// status bar too, and is brought into view.
    /// </summary>
    private void Refused(string words)
    {
        var said = new TextBlock { Text = words, TextWrapping = TextWrapping.Wrap, MaxWidth = 900, FontWeight = FontWeight.SemiBold, Classes = { AppStyles.Warn } };
        sessionPoolPanel.Children.Add(said);
        status.Text = words;
        DiagnosticLog.Info("sessions.pool.refused", ("sheets", sessionChosen.Count));
        Avalonia.Threading.Dispatcher.UIThread.Post(() => said.BringIntoView(), Avalonia.Threading.DispatcherPriority.Background);
    }

    /// <summary>Whether Pool the chosen's last answer was a refusal shown in amber, for the headless tests.</summary>
    internal bool PoolRefusedInAmber => sessionPoolPanel.Children.OfType<TextBlock>().Any(t => t.Classes.Contains(AppStyles.Warn));

    /// <summary>
    /// Entry 420 section 1: a pooled set in its own tab, on the analysis screen: every shot on one plot about its own bull, each sheet in its
    /// own shade with a key, every figure, and the Shots list naming each shot sheet.bull. It is opened as a saved sheet with no picture is,
    /// and is not saved again as a session of its own.
    /// </summary>
    internal void OpenPooled(TargetDefinition definition, MarkingState pooled, IReadOnlyList<ScaleReference?>? scales = null)
    {
        if (AddTarget(choose: false) is null)
        {
            return;
        }

        targets.Current.PooledScales = scales;

        ShowPooledHere(definition, pooled);
        status.Text = $"{pooled.SheetLabel}, in its own tab. Each shot is named sheet.bull: 2.5 is bull 5 on sheet 2. Tap a sheet in the plot's key to show it alone.";
    }

    /// <summary>A pooled marking shown in the tab showing, as a saved sheet with no picture is: entry 420's own tab, or entry 423's set tab.</summary>
    private void ShowPooledHere(TargetDefinition? definition, MarkingState pooled)
    {
        grey = valueImage = artwork = null;
        metadata = null;
        canvas.SetImage(null, null);
        canvas.Artwork = null;
        plotDefinition = definition;
        registrationResidual = null;
        detectedState = null;
        plot.OnlySheet = null;
        session.Load(pooled);
        currentSession = null;
        showingPool = true;
        MarkingIsSaved();
        destination = Destination.Analyse;
        SetAnalysing(true);
        DiagnosticLog.Info("sessions.pool.open", ("shots", pooled.Shots.Count));
    }

    /// <summary>
    /// Error report 46: whether the tab shows a pool, Pool the chosen's own tab, a saved pool or a set tab's pool. A pool is never saved as a
    /// session of its own (entry 420): it is one marking of every sheet laid side by side against one sheet's design, so a record of it named
    /// one sheet and held the shots of all of them. Since entry 495 Accept, Save and closing save it as a pool record instead (MainWindow.Pools.cs).
    /// </summary>
    private bool showingPool;

    /// <summary>Whether the tab shows a pool, for the headless tests.</summary>
    internal bool ShowingPool => showingPool;

    /// <summary>The tab now holds a sheet of its own, not a pool: opened, reopened or cleared.</summary>
    private void NotAPool()
    {
        showingPool = false;
        poolSources = null;
        targets.Current.PooledScales = null;
    }

    /// <summary>What the pooled set says, for the headless tests.</summary>
    internal IReadOnlyList<string> PoolText => [.. sessionPoolPanel.Children.OfType<TextBlock>().Select(t => t.Text ?? "")];

    internal void CompareChosen()
    {
        if (sessions is null || sessionChosen.Count < 2)
        {
            status.Text = "Choose two or more sessions to compare.";
            return;
        }

        // Entry 258: the sessions made into groups by the shared code the phone uses too.
        // Entry 495 section 5: a chosen pooled set is one group beside the sessions.
        var records = SessionPools.Chosen(sessions, units, sessionChosen).ToList();
        var setup = CompareSessions.From(records, units);
        compareFooting = setup.Footing;
        compareDistance = setup.Distance;
        if (setup.Refusal is { } refusal)
        {
            ShowComparison([], refusal);
            return;
        }

        var groups = setup.Groups.Select(g => (g.Name, g.Offsets, g.Detail, SpeedOf(g.Load))).ToList();
        compareTopGun = GroupLab.Core.Ballistics.TopGun.Lines(records.Zip(setup.Groups, (r, g) => (r.Rifle, r.Load, g.Name, g.Offsets)), book, setup.Distance);
        DiagnosticLog.Info("compare.sessions", ("groups", groups.Count));
        ShowComparison(groups, null);
    }

    /// <summary>Entry 456 section 3: Top Gun for each compared load, shown under the tests.</summary>
    private IReadOnlyList<string> compareTopGun = [];

    /// <summary>The sheet in the analysis, its subgroups as the groups, when its marking names two or more.</summary>
    internal void CompareSubgroups()
    {
        var state = session.State;
        if (state.Subgroups is not { } map || map.Names.Count < 2 || state.Scale is null)
        {
            status.Text = "This sheet has no subgroups to compare.";
            return;
        }

        var sighters = state.Bulls.Where(b => !b.Scoring).Select(b => b.Index).ToHashSet();
        var groups = new List<(string, IReadOnlyList<PointD>, string, string?)>();
        foreach (string name in map.Names)
        {
            var shots = state.Shots.Where(s => s.IsShot && s.Exclusion is null && s.Bull is { } b && !sighters.Contains(b) && map.For(b) == name).ToList();
            groups.Add((name, GroupAnalysis.CompositeOffsets(state, shots), $"{shots.Count} shots, bulls {string.Join(", ", map.ByBull.Where(p => p.Value == name).Select(p => BullLabel(p.Key)))}",
                SpeedOf(name) ?? SpeedOf(state.Load)));
        }

        // A subgroup is named by its load, so each is Top Gun's load in the sheet's rifle.
        compareTopGun = GroupLab.Core.Ballistics.TopGun.Lines(groups.Select(g => (state.Rifle?.Name, (string?)g.Item1, g.Item1, g.Item2)), book, state.ShotDistanceInches);
        compareFooting = null;
        compareDistance = state.ShotDistanceInches;
        DiagnosticLog.Info("compare.subgroups", ("groups", groups.Count));
        ShowComparison(groups, null);
    }

    /// <summary>
    /// What a load was chronographed at, for the card, entry 131 section 10: the muzzle velocity and its spread where the record book has
    /// them, and null where it does not. A figure GroupLab worked out from readings says so, because a measured spread and a typed one are
    /// not the same claim (entry 115 section 3).
    /// </summary>
    private string? SpeedOf(string? loadName) => ComparisonPages.Speed(book, loadName, units);

    private void ShowComparison(List<(string, IReadOnlyList<PointD>, string, string?)> groups, string? refusal)
    {
        compareGroups = groups;
        comparison = null;
        if (refusal is null && groups.Count >= 2)
        {
            try
            {
                comparison = LoadComparison.Compare([.. groups.Select(g => (g.Item1, g.Item2))], units.Length);
            }
            catch (ArgumentException ex)
            {
                refusal = ex.Message.Split(" (Parameter", StringSplitOptions.None)[0];
            }
        }

        compareRefusal = refusal;
        Go(Destination.Compare);
    }

    private string? compareRefusal;

    /// <summary>Lays the comparison out, or says how to start one.</summary>
    private void FillCompare()
    {
        compareColumn.Children.Clear();
        compareColumn.Children.Add(new TextBlock { Text = "Compare loads", Classes = { AppStyles.Title } });
        bool subgroups = session.State.Subgroups is { } map && map.Names.Count >= 2;
        if (comparison is not { } report)
        {
            compareColumn.Children.Add(Line(compareRefusal ?? "Choose two or more sessions in Session records and press Compare the chosen, or compare the subgroups of the sheet open in the analysis."));
            var start = Row(Button("Session records", () => ShowSessions()));
            if (subgroups)
            {
                start.Children.Add(Button("Compare this sheet's subgroups", CompareSubgroups));
            }

            compareColumn.Children.Add(start);
            return;
        }

        compareCharts.Clear();
        compareColumn.Children.Add(Line(CompareSummary(report)));
        compareColumn.Children.Add(Row(ExportComparisonButton()));

        // Entry 309 section 2: the groups first, each about its own center at one scale in its load's color, and a tap stacks them.
        // Entry 465 item 7B: with two loads the card's free space says what the difference means, from this comparison's own words.
        compareGroupsCard = new LoadGroups([.. report.Groups.Select(g => new LoadGroup(g.Name, g.Offsets, g.Centre, g.MeanRadius.Value))],
            inches => Sized(inches, compareDistance).Value, cell: 200, report: report);
        compareColumn.Children.Add(new Border { Child = compareGroupsCard, Classes = { AppStyles.ResultCard } });

        // Each load's figures with their intervals, side by side, its name in its color.
        var cards = new Grid { ColumnDefinitions = new ColumnDefinitions(string.Join(",", report.Groups.Select(_ => "*"))) };
        for (int i = 0; i < report.Groups.Count; i++)
        {
            var group = report.Groups[i];
            var card = new StackPanel { Spacing = Tokens.Space4, Margin = new Thickness(0, 0, Tokens.Space12, 0) };
            var title = LoadGroups.Named(i, group.Name);
            ((TextBlock)title.Children[1]).Classes.Add(AppStyles.Section);
            card.Children.Add(title);
            card.Children.Add(Detail(compareGroups[i].Detail));
            if (compareGroups[i].Speed is { } speed)
            {
                card.Children.Add(Detail(speed));
            }
            string Interval(Estimate e) => $"{units.Number(e.Lower)} to {units.Length(e.Upper)}";
            card.Children.Add(Rowed(Readout("Sigma", units.Length(group.Rayleigh.Sigma.Value), Tokens.ValueSize)));
            card.Children.Add(Detail("95% interval " + Interval(group.Rayleigh.Sigma)));
            // Entry 189 section 3: the same order as the analysis, the angle first where there is a distance.
            var mr = Sized(group.MeanRadius.Value, compareDistance);
            card.Children.Add(Rowed(Readout("Mean radius", mr.Value, Tokens.ValueSize)));
            if (mr.Beneath is { } mrBeneath)
            {
                card.Children.Add(Detail(mrBeneath));
            }

            card.Children.Add(Detail("95% interval " + Interval(group.MeanRadius)));
            var es = Sized(group.ExtremeSpread, compareDistance);
            card.Children.Add(Rowed(Readout("Extreme spread", es.Value, Tokens.ValueSize, subordinate: true)));
            if (es.Beneath is { } esBeneath)
            {
                card.Children.Add(Detail(esBeneath));
            }
            Grid.SetColumn(card, i);
            cards.Children.Add(card);
        }

        compareColumn.Children.Add(cards);

        // Entry 131 section 10: the figures with their intervals, drawn. This is the one picture that makes the project's whole argument
        // visible. Two loads reading 0.42 in and 0.51 in look like a winner and a loser in a table; drawn with their intervals, anybody can
        // see in a moment whether these shots can tell them apart at all.
        foreach (var (title, rows) in new (string, List<IntervalRow>)[]
        {
            ("Mean radius", [.. report.Groups.Select(g => new IntervalRow(g.Name, g.MeanRadius.Value, g.MeanRadius.Lower, g.MeanRadius.Upper))]),
            ("Sigma", [.. report.Groups.Select(g => new IntervalRow(g.Name, g.Rayleigh.Sigma.Value, g.Rayleigh.Sigma.Lower, g.Rayleigh.Sigma.Upper))]),
        })
        {
            // Entry 189 section 3: the charts read in the same order as the cards, as angles where there is a distance.
            // Entry 295 section 1.4: the sentence beneath is the comparison's own, so it can never disagree with the verdict.
            var chart = new IntervalChart
            {
                Rows = rows,
                Length = inches => Sized(inches, compareDistance).Value,
                Says = LoadComparison.ChartSays(report, title, hasRange: true),
                RowsAreLoads = true,
            };
            compareCharts[title] = chart;
            compareColumn.Children.Add(Ruled(title + ", with the range each could really be"));
            compareColumn.Children.Add(chart);
            compareColumn.Children.Add(Note(chart.Description));
        }

        // The verdict, which never ranks by point estimate.
        var verdict = new StackPanel { Spacing = Tokens.Space4 };
        verdict.Children.Add(LoadGroups.Key([.. report.Groups.Select(g => g.Name)]));
        verdict.Children.Add(new TextBlock { Text = report.Headline, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Warn } });
        foreach (string line in report.Explanation)
        {
            verdict.Children.Add(Line(line));
        }

        compareColumn.Children.Add(new Border { Child = verdict, Classes = { AppStyles.JudgementCard } });

        if (compareTopGun.Count > 0)
        {
            compareColumn.Children.Add(Heading("Top Gun"));
            foreach (string line in compareTopGun)
            {
                compareColumn.Children.Add(Line(line));
            }

            compareColumn.Children.Add(Note(GroupLab.Core.Ballistics.TopGun.Note));
        }

        compareColumn.Children.Add(Heading("Tests run"));
        foreach (var test in report.Tests)
        {
            var block = new StackPanel { Spacing = Tokens.Space4 };
            block.Children.Add(Readout(test.Name, test.PValue < 0.001 ? "p < 0.001" : "p = " + test.PValue.ToString("0.000", CultureInfo.InvariantCulture), Tokens.SecondarySize));
            block.Children.Add(Line(test.Verdict));
            block.Children.Add(Note(test.Power));
            compareColumn.Children.Add(Rowed(block));
        }

        if (report.Groups.Count > 2)
        {
            compareColumn.Children.Add(Heading("Every pair's sigma ratio"));
            foreach (var pair in report.Pairs)
            {
                compareColumn.Children.Add(Detail(string.Create(CultureInfo.InvariantCulture,
                    $"{report.Groups[pair.First].Name} against {report.Groups[pair.Second].Name}: {pair.Ratio.Value:0.00}, 95% interval {pair.Ratio.Lower:0.00} to {pair.Ratio.Upper:0.00}; p = {pair.PValue:0.000}, {pair.HolmPValue:0.000} after Holm's adjustment")));
            }
        }

        compareColumn.Children.Add(Heading("To resolve a difference of"));
        int loads = report.Groups.Count;
        foreach (var row in report.Resolve)
        {
            compareColumn.Children.Add(Readout(
                string.Create(CultureInfo.InvariantCulture, $"{100 * (row.Ratio - 1):0} percent"),
                string.Create(CultureInfo.InvariantCulture, $"{row.ShotsPerLoad} shots per load, {row.ShotsPerLoad * loads} rounds in all"),
                Tokens.SecondarySize));
        }

        compareColumn.Children.Add(Note("With 80 percent power at the 5 percent level, by the F test on the Rayleigh sigmas (docs/STATISTICS.md section 9.2)."));
    }

    /// <summary>The line under the title: how many loads and shots, and the footing they were put on.</summary>
    private string CompareSummary(LoadComparisonReport report) =>
        $"{report.Groups.Count} loads, {report.Groups.Sum(g => g.Shots)} shots, in the order chosen." + (compareFooting is null ? "" : " " + compareFooting);

    /// <summary>Suggestion 61: the comparison as one page, the screen's own words, for a PDF or a picture.</summary>
    internal ComparisonPage? BuildComparisonPage()
    {
        if (comparison is not { } report)
        {
            return null;
        }

        var loads = compareGroups.Select(g => new ExportedLoad(g.Item1, g.Item3, g.Item4)).ToList();
        return ComparisonPages.For(report, loads, CompareSummary(report), compareTopGun, units, compareDistance,
            OnePageReports.PaperFor(AppSettingsStore.LetterPaper()), DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), AppInfo.Version);
    }

    /// <summary>Suggestion 61: Export, a PDF of the comparison or a picture of the same page, saved where the person chooses.</summary>
    private Button ExportComparisonButton()
    {
        var menu = new MenuFlyout();
        foreach (var (words, picture) in new (string, bool)[] { ("PDF…", false), ("Picture (PNG)…", true) })
        {
            var item = new MenuItem { Header = words };
            item.Click += async (_, _) => await ExportComparisonDialog(picture);
            menu.Items.Add(item);
        }

        var button = new Button { Content = "Export", Flyout = menu, Margin = new Thickness(2) };
        ToolTip.SetTip(button, "This comparison as a PDF, or as a picture of the same page to post, with the groups, the figures, the verdict and the tests");
        return button;
    }

    private async Task ExportComparisonDialog(bool picture)
    {
        string kind = picture ? "comparison-picture" : "comparison-pdf";
        DiagnosticLog.Info("dialog.open", ("dialog", kind));
        if (BuildComparisonPage() is not { } page)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = picture ? "Save the comparison as a picture" : "Save the comparison as a PDF",
            SuggestedFileName = ComparisonPages.FileName(page) + (picture ? ".png" : ".pdf"),
            DefaultExtension = picture ? "png" : "pdf",
        });
        DiagnosticLog.Info("dialog.result", ("dialog", kind), ("chosen", file is not null));
        if (file?.TryGetLocalPath() is { } path)
        {
            WriteComparison(path, picture, page);
        }
    }

    /// <summary>Writes the comparison as a PDF or a picture, and says where; also the headless tests' way in.</summary>
    internal void WriteComparison(string path, bool picture, ComparisonPage? page = null)
    {
        page ??= BuildComparisonPage();
        if (page is null)
        {
            status.Text = "Nothing to export: choose two or more sessions to compare first.";
            return;
        }

        byte[] bytes = picture ? ComparisonPages.Picture(page) : ComparisonPages.Write(page);
        File.WriteAllBytes(path, bytes);
        status.Text = (picture ? "Comparison picture saved to " : "Comparison PDF saved to ") + path;
        DiagnosticLog.Info("file.save", [.. DiagnosticLog.File(path), ("kind", picture ? "comparison-picture" : "comparison-pdf"), ("bytes", bytes.Length)]);
    }

    /// <summary>Shows or leaves the comparison.</summary>
    internal void ShowCompare(bool on = true) => Go(on ? Destination.Compare : Destination.Analyse);

    internal bool ShowingCompare => destination == Destination.Compare;

    /// <summary>The interval charts by figure, so a headless test can read what each one says.</summary>
    private readonly Dictionary<string, IntervalChart> compareCharts = [];

    /// <summary>What the comparison's interval charts say, for the headless tests.</summary>
    internal IReadOnlyList<string> CompareChartSays => [.. compareCharts.Values.Select(c => c.Description)];

    internal LoadComparisonReport? Comparison => comparison;

    internal IEnumerable<string> CompareText => compareColumn.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");

    /// <summary>The Compare card's plots, for the headless tests.</summary>
    internal LoadGroups? CompareGroups => compareGroupsCard;

    private LoadGroups? compareGroupsCard;
}

/// <summary>What Pool the chosen says when it will not pool the sessions ticked, error report 57.</summary>
internal static class PoolRefusal
{
    internal const string NotASet = "These are not sheets of a set, so Pool the chosen cannot read them as one group. A set is what Made for your "
        + "optic makes when the shots need several sheets; each sheet's codes say which of the set it is.";

    /// <summary>The designs ticked, each with how many of its sheets, and what to tick instead.</summary>
    internal static string DifferentDesigns(IReadOnlyList<SessionRecord> records)
    {
        var designs = records
            .GroupBy(r => r.DefinitionId ?? "")
            .Select(g => (Name: SessionNames.TargetOf(g.First().SheetName, SessionNames.LabelOf(g.First().MarkingJson)), Count: g.Count()))
            .Select(d => $"{d.Name} ({(d.Count == 1 ? "1 sheet" : d.Count.ToString(CultureInfo.InvariantCulture) + " sheets")})")
            .ToList();
        string named = designs.Count == 2 ? $"{designs[0]} and {designs[1]}" : string.Join(", ", designs.Take(designs.Count - 1)) + " and " + designs[^1];
        return $"These are sheets of different designs: {named}. Pool the chosen reads only the sheets of one set together, so tick only those; "
            + "a loose sheet can stand in for a sheet of a set only when its bulls are the set's.";
    }

    /// <summary>Entry 512 section 3: more loose sheets ticked than the set has sheets missing from those ticked.</summary>
    internal static string NoPlaceLeft(string set, int loose, int places) => places == 0
        ? $"Every sheet of {set} is ticked already, so the loose {(loose == 1 ? "sheet has" : "sheets have")} no sheet to stand in for. Untick the sheet of the set it replaces, then Pool the chosen."
        : string.Create(CultureInfo.InvariantCulture, $"{loose} loose sheets are ticked, but only {places} {(places == 1 ? "sheet" : "sheets")} of {set} {(places == 1 ? "is" : "are")} missing from those ticked, so they cannot all stand in. Untick the sheets of the set they replace, then Pool the chosen.");

    internal const string WhichSheetTitle = "Which sheet of the set does this one replace?";

    internal static string WhichSheetWhy(string loose, string set) =>
        $"{loose} has the same bulls as every sheet of {set}, so it can count as any sheet missing from those ticked. Choose the one it replaces; each shot is still measured about its own bull.";

    internal static string SheetChoice(int place) => string.Create(CultureInfo.InvariantCulture, $"Sheet {place + 1}");

    internal static string CountsAs(string loose, int place) => string.Create(CultureInfo.InvariantCulture, $"{loose} counts as sheet {place + 1} of the set.");
}
