using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;
using GroupLab.Core.Trace;

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
    internal void CompareChosen()
    {
        if (sessions is null || sessionChosen.Count < 2)
        {
            status.Text = "Choose two or more sessions to compare.";
            return;
        }

        var records = sessions.List().Where(s => sessionChosen.Contains(s.Id)).Reverse().Select(s => sessions.Get(s.Id)!).ToList();
        var groups = new List<(string, IReadOnlyList<PointD>, string, string?)>();
        var distances = new List<double?>();
        foreach (var record in records)
        {
            var state = MarkingFile.Read(record.MarkingJson).State;
            var (offsets, excluded) = KeptOffsets(state);
            string name = record.Load ?? record.SheetName;
            groups.Add((groups.Any(g => g.Item1 == name) ? $"{name}, {record.ShotDate}" : name, offsets,
                $"{offsets.Count} shots, {record.ShotDate}{(excluded > 0 ? $", {excluded} excluded and left out" : "")}",
                SpeedOf(record.Load)));
            distances.Add(record.DistanceInches);
        }

        compareFooting = null;
        if (distances.Distinct().Count() > 1)
        {
            if (distances.Any(d => d is null))
            {
                ShowComparison([], "One of these sessions has no distance and the others differ, so they cannot be put on one footing. Set the distance on it, accept it again, and compare.");
                return;
            }

            double first = distances[0]!.Value;
            for (int i = 0; i < groups.Count; i++)
            {
                double k = first / distances[i]!.Value;
                groups[i] = (groups[i].Item1, [.. groups[i].Item2.Select(o => new PointD(o.X * k, o.Y * k))], groups[i].Item3 + $", shot at {units.DistanceText(distances[i]!.Value)}", groups[i].Item4);
            }

            compareFooting = $"These were shot at different distances, so they are compared as angles: every group is scaled to {units.DistanceText(first)}, where its figures are given.";
        }

        DiagnosticLog.Info("compare.sessions", ("groups", groups.Count));
        ShowComparison(groups, null);
    }

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

        compareFooting = null;
        DiagnosticLog.Info("compare.subgroups", ("groups", groups.Count));
        ShowComparison(groups, null);
    }

    private static (IReadOnlyList<PointD> Offsets, int Excluded) KeptOffsets(MarkingState state)
    {
        var sighters = state.Bulls.Where(b => !b.Scoring).Select(b => b.Index).ToHashSet();
        var shots = state.Shots.Where(s => s.IsShot && !(s.Bull is { } b && sighters.Contains(b))).ToList();
        var kept = shots.Where(s => s.Exclusion is null).ToList();
        return (GroupAnalysis.CompositeOffsets(state, kept), shots.Count - kept.Count);
    }

    /// <summary>
    /// What a load was chronographed at, for the card, entry 131 section 10: the muzzle velocity and its spread where the record book has
    /// them, and null where it does not. A figure GroupLab worked out from readings says so, because a measured spread and a typed one are
    /// not the same claim (entry 115 section 3).
    /// </summary>
    private string? SpeedOf(string? loadName)
    {
        if (loadName is null || book.FindLoad(loadName) is not { MuzzleVelocityFps: { } fps })
        {
            return null;
        }

        var load = book.FindLoad(loadName)!;
        string speed = units.Speed(fps);
        if (load.MuzzleVelocitySdFps is not { } sd)
        {
            return speed;
        }

        return speed + ", SD " + units.Speed(sd) + (load.MuzzleVelocitySdFrom is { Length: > 0 } from ? " from " + from : "");
    }

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
        compareColumn.Children.Add(Line($"{report.Groups.Count} loads, {report.Groups.Sum(g => g.Shots)} shots, in the order chosen." + (compareFooting is null ? "" : " " + compareFooting)));

        // The groups side by side, each with its plot and its figures with intervals.
        var cards = new Grid { ColumnDefinitions = new ColumnDefinitions(string.Join(",", report.Groups.Select(_ => "*"))) };
        for (int i = 0; i < report.Groups.Count; i++)
        {
            var group = report.Groups[i];
            var card = new StackPanel { Spacing = Tokens.Space4, Margin = new Thickness(0, 0, Tokens.Space12, 0) };
            card.Children.Add(new TextBlock { Text = group.Name, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Section } });
            card.Children.Add(Detail(compareGroups[i].Detail));
            if (compareGroups[i].Speed is { } speed)
            {
                card.Children.Add(Detail(speed));
            }
            var groupPlot = new CompositePlot
            {
                Height = 220,
                Shots = [.. group.Offsets.Select((o, k) => new PlotShot(k, (k + 1).ToString(CultureInfo.InvariantCulture), null, o, false))],
                Centre = group.Centre,
                Cep50Inches = group.Rayleigh.Cep(0.5).Value,
                Cep90Inches = group.Rayleigh.Cep(0.9).Value,
                Length = inches => units.Length(inches),
                ShowKey = false,
            };
            card.Children.Add(groupPlot);
            string Interval(Estimate e) => $"{units.Number(e.Lower)} to {units.Length(e.Upper)}";
            card.Children.Add(Rowed(Readout("Sigma", units.Length(group.Rayleigh.Sigma.Value), Tokens.ValueSize)));
            card.Children.Add(Detail("95% interval " + Interval(group.Rayleigh.Sigma)));
            card.Children.Add(Rowed(Readout("Mean radius", units.Length(group.MeanRadius.Value), Tokens.ValueSize)));
            card.Children.Add(Detail("95% interval " + Interval(group.MeanRadius)));
            card.Children.Add(Rowed(Readout("Extreme spread", units.Length(group.ExtremeSpread), Tokens.ValueSize, subordinate: true)));
            Grid.SetColumn(card, i);
            cards.Children.Add(card);
        }

        compareColumn.Children.Add(cards);
        compareColumn.Children.Add(Note("Each plot: the dots are the shots about their own bulls, excluded ones left out; the cross is the group's centre, the dotted circle CEP 50 and the dashed circle CEP 90."));

        // Entry 131 section 10: the figures with their intervals, drawn. This is the one picture that makes the project's whole argument
        // visible. Two loads reading 0.42 in and 0.51 in look like a winner and a loser in a table; drawn with their intervals, anybody can
        // see in a moment whether these shots can tell them apart at all.
        foreach (var (title, rows) in new (string, List<IntervalRow>)[]
        {
            ("Mean radius", [.. report.Groups.Select(g => new IntervalRow(g.Name, g.MeanRadius.Value, g.MeanRadius.Lower, g.MeanRadius.Upper))]),
            ("Sigma", [.. report.Groups.Select(g => new IntervalRow(g.Name, g.Rayleigh.Sigma.Value, g.Rayleigh.Sigma.Lower, g.Rayleigh.Sigma.Upper))]),
        })
        {
            var chart = new IntervalChart { Rows = rows, Length = inches => units.Length(inches) };
            compareCharts[title] = chart;
            compareColumn.Children.Add(Ruled(title + ", with the range each could really be"));
            compareColumn.Children.Add(chart);
            compareColumn.Children.Add(Note(chart.Description));
        }

        // The verdict, which never ranks by point estimate.
        var verdict = new StackPanel { Spacing = Tokens.Space4 };
        verdict.Children.Add(new TextBlock { Text = report.Headline, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Warn } });
        foreach (string line in report.Explanation)
        {
            verdict.Children.Add(Line(line));
        }

        compareColumn.Children.Add(new Border { Child = verdict, Classes = { AppStyles.JudgementCard } });

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

    /// <summary>Shows or leaves the comparison.</summary>
    internal void ShowCompare(bool on = true) => Go(on ? Destination.Compare : Destination.Analyse);

    internal bool ShowingCompare => destination == Destination.Compare;

    /// <summary>The interval charts by figure, so a headless test can read what each one says.</summary>
    private readonly Dictionary<string, IntervalChart> compareCharts = [];

    /// <summary>What the comparison's interval charts say, for the headless tests.</summary>
    internal IReadOnlyList<string> CompareChartSays => [.. compareCharts.Values.Select(c => c.Description)];

    internal LoadComparisonReport? Comparison => comparison;

    internal IEnumerable<string> CompareText => compareColumn.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");
}
