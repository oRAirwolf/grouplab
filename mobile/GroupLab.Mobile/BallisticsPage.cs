using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Statistics;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 259 screen 5, "its own tab, A" (Alan's choice): a large title, three summary chips (Rifle, Load, Air) each
/// opening its form, then three tabs. <b>Dope</b>: To and Every, and a table of range, elevation and its clicks, and the wind for 10 mph and
/// its clicks, the last row highlighted, with drop, velocity and energy on a phone turned sideways. <b>Trajectory</b>: the desktop's graph.
/// <b>Hit chance</b>: target chips (10 in plate, 2 MOA, Other), range, wind and a confidence preset, and the group from the open result;
/// the answer a large amber percentage with one sentence, the chance by range with the chosen range marked, and "What costs the most" as
/// bars. Everything is the desktop's shared engine (<see cref="SolverUse"/>, <see cref="HitProbability"/>, <see cref="HitFromGroup"/>),
/// and the rifles and loads are the same record book the sessions use.
/// </summary>
internal sealed class BallisticsPage : UserControl
{
    private readonly SessionStore store = PhoneAnalysis.Store();
    private UnitSettings units = Phone.Settings.LoadUnits();
    private readonly StackPanel column = new() { Spacing = 12 };
    private readonly ContentControl form = new();
    private readonly ContentControl body = new();
    private readonly MarkingState? carried;
    private RecordBook book;
    private string? rifleName;
    private string? loadName;
    private AirInput air = new();
    private string open = "";
    private string tab = "Dope";
    private readonly TextBox to = Field("1000");
    private readonly TextBox every = Field("100");
    private readonly TextBox range = Field("600");
    private readonly TextBox wind = Field("10");
    private readonly TextBox otherSize = Field("");
    private string target = "10 in plate";
    private int preset = 1;

    /// <summary>
    /// Entry 280 section 2, board ZeroFrom: a group's zero offset, as angles, carried into the dope. A zero that is off by an angle at the
    /// distance shot is off by that angle at every range, so each range's hold gains it; null when no group's offset was handed in.
    /// </summary>
    private readonly (double UpMoa, double LeftMoa, string Words)? zeroOffset;

    /// <param name="carried">The result's group, carried in from a result's section list, for the hit chance; null from the tab.</param>
    /// <param name="useZeroOffset">Carry the group's offset from its aim point into the dope as the zero offset.</param>
    public BallisticsPage(MarkingState? carried = null, bool useZeroOffset = false)
    {
        // Entry 280 section 2: the offset and its sentence are the desktop's own (ResultWords.ZeroOffsetFor).
        if (useZeroOffset && carried is not null && ResultWords.ZeroOffsetFor(carried, units) is { } offset)
        {
            zeroOffset = (offset.UpMoa, offset.LeftMoa, offset.Words);
        }

        // Entry 273: a tap on any number switches units everywhere; this page shows them again.
        void Follow() => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            this.units = Phone.Settings.LoadUnits();
            Build();
        });
        AttachedToVisualTree += (_, _) => Shell.UnitsChanged += Follow;
        DetachedFromVisualTree += (_, _) => Shell.UnitsChanged -= Follow;

        this.carried = carried;
        book = store.LoadBook();
        rifleName = carried?.Rifle?.Name ?? book.Rifles.FirstOrDefault()?.Name;
        loadName = carried?.Load ?? book.Loads.FirstOrDefault()?.Name;
        if (carried is not null)
        {
            tab = "Hit chance";
            if (carried.ShotDistanceInches is { } d)
            {
                range.Text = (d / 36).ToString("0", CultureInfo.InvariantCulture);
            }
        }

        Content = Screens.Page(column);
        Build();
    }

    private Rifle? RifleChosen => book.FindRifle(rifleName);

    /// <summary>Entry 294 section 1: the angle this page aims in, the chosen rifle's scope unit, or Settings' where there is none.</summary>
    private bool Mil => units.Aiming(RifleChosen).Angular == AngularUnit.Mrad;

    /// <summary>The hit chance's angular target, in the scope's unit: half a mil, or two minutes.</summary>
    private string AngleTarget => Mil ? "0.5 mil" : "2 MOA";

    private Load? LoadChosen => book.FindLoad(loadName);

    private void Build()
    {
        column.Children.Clear();
        column.Children.Add(Screens.Title("Ballistics"));
        var chips = new WrapPanel
        {
            Children =
            {
                Chip("Rifle: " + (rifleName ?? "none"), open == "rifle", () => Toggle("rifle")),
                Chip("Load: " + (loadName ?? "none"), open == "load", () => Toggle("load")),
                Chip(string.Create(CultureInfo.CurrentCulture, $"Air: {air.TemperatureF:0} °F, {air.AltitudeFt:0} ft"), open == "air", () => Toggle("air")),
            },
        };
        column.Children.Add(chips);
        form.Content = open switch { "rifle" => RifleForm(), "load" => LoadForm(), "air" => AirForm(), _ => null };
        column.Children.Add(form);
        var tabs = new WrapPanel();
        foreach (string name in new[] { "Dope", "Trajectory", "Hit chance" })
        {
            tabs.Children.Add(Chip(name, tab == name, () =>
            {
                tab = name;
                Build();
            }));
        }

        column.Children.Add(tabs);
        column.Children.Add(body);
        var missing = SolverUse.Missing(RifleChosen, LoadChosen);
        if (missing.Count > 0)
        {
            body.Content = Screens.Card(Screens.Line("The solver needs " + string.Join(", ", missing) + ". Open Rifle or Load above to enter them."));
            return;
        }

        var input = SolverUse.Input(RifleChosen, LoadChosen, air)!;
        body.Content = tab switch
        {
            "Trajectory" => Trajectory(input),
            "Hit chance" => Hit(input),
            _ => Dope(input),
        };
    }

    private void Toggle(string which)
    {
        open = open == which ? "" : which;
        Build();
    }

    private Control Dope(BallisticInput input)
    {
        double max = Number(to) ?? 1000, step = Number(every) ?? 100;
        max = Math.Clamp(max, 100, 3000);
        step = Math.Clamp(step, 10, 500);
        bool mil = Mil;
        double click = RifleChosen?.ClickValue is > 0 and var c ? c : mil ? 0.1 : 0.25;
        var points = SolverUse.Dope(input with { CrosswindMph = 10 }, max, step).Points.Where(p => p.RangeYards > 0).ToList();
        bool wide = Bounds.Width > Bounds.Height && Bounds.Width > 600;
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions(wide ? "*,*,*,*,*,*,*,*" : "*,*,*,*,*"), RowSpacing = 6 };
        string unit = mil ? "mil" : "MOA";
        var head = new List<string> { "Range", "Up, " + unit, "Clicks", "Wind 10 mph", "Clicks" };
        if (wide)
        {
            head.AddRange(["Drop", "Velocity", "Energy"]);
        }

        void Cell(string text, int row, int col, bool strong)
        {
            var block = row == 0 ? Screens.Dim(text) : Screens.Line(text);
            if (strong)
            {
                block.FontWeight = FontWeight.SemiBold;
                block.Foreground = new SolidColorBrush(Color.FromRgb(232, 150, 46));
            }

            Grid.SetRow(block, row);
            Grid.SetColumn(block, col);
            grid.Children.Add(block);
        }

        grid.RowDefinitions = new RowDefinitions(string.Join(",", Enumerable.Repeat("Auto", points.Count + 1)));
        for (int k = 0; k < head.Count; k++)
        {
            Cell(head[k], 0, k, false);
        }

        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            bool last = i == points.Count - 1;
            double up = -(mil ? p.DropMil : p.DropMoa), across = mil ? p.WindMil : p.WindMoa;
            // Entry 280 section 2: the carried group's zero offset, the same angle at every range, in the table's unit.
            if (zeroOffset is { } offset)
            {
                up += mil ? offset.UpMoa / 3.43774677 : offset.UpMoa;
            }

            var cells = new List<string>
            {
                units.DistanceText(p.RangeYards * 36),
                up.ToString("0.0", CultureInfo.CurrentCulture),
                Math.Round(up / click).ToString("0", CultureInfo.CurrentCulture),
                across.ToString("0.0", CultureInfo.CurrentCulture),
                Math.Round(across / click).ToString("0", CultureInfo.CurrentCulture),
            };
            if (wide)
            {
                cells.AddRange([units.Length(-p.DropInches), p.VelocityFps.ToString("0", CultureInfo.CurrentCulture) + " ft/s", p.EnergyFtLb.ToString("0", CultureInfo.CurrentCulture) + " ft lb"]);
            }

            for (int k = 0; k < cells.Count; k++)
            {
                Cell(cells[k], i + 1, k, last);
            }
        }

        var fields = new WrapPanel { Children = { Labeled("To, yd", to), Labeled("Every, yd", every), Screens.Choice("Work it out", Build) } };
        var column = new StackPanel { Spacing = 10, Children = { Screens.Card(fields) } };
        if (zeroOffset is { } carriedZero)
        {
            column.Children.Add(Screens.Card(Screens.Line(carriedZero.Words), Screens.Dim("The Up column includes it; hold or dial the across part at every range too.")));
        }

        column.Children.Add(Screens.Card(grid));
        return column;
    }

    private Control Trajectory(BallisticInput input)
    {
        double max = Math.Clamp(Number(to) ?? 1000, 100, 3000);
        var graph = new TrajectoryGraph { Points = SolverUse.Dope(input, max, Math.Max(10, max / 50)).Points, ZeroYards = RifleChosen?.ZeroDistanceYards, Height = 260, Distance = y => units.DistanceText(y * 36) };
        return Screens.Card(Screens.Dim("The bullet's path against the line of sight, to " + units.DistanceText(max * 36) + "."), graph);
    }

    private Control Hit(BallisticInput input)
    {
        var panel = new StackPanel { Spacing = 10 };
        var targets = new WrapPanel();
        if (target is "2 MOA" or "0.5 mil")
        {
            target = AngleTarget;
        }

        foreach (string name in new[] { "10 in plate", AngleTarget, "Other" })
        {
            targets.Children.Add(Chip(name, target == name, () =>
            {
                target = name;
                Build();
            }));
        }

        var presets = new WrapPanel();
        for (int i = 0; i < HitPresets.All.Count; i++)
        {
            int which = i;
            presets.Children.Add(Chip(HitPresets.All[i].Name, preset == i, () =>
            {
                preset = which;
                Build();
            }));
        }

        var setupCard = new StackPanel { Spacing = 8, Children = { Screens.Dim("Target"), targets } };
        if (target == "Other")
        {
            setupCard.Children.Add(Labeled("Diameter, in", otherSize));
        }

        setupCard.Children.Add(new WrapPanel { Children = { Labeled("Range, yd", range), Labeled("Crosswind, mph", wind) } });
        setupCard.Children.Add(Screens.Dim("How sure you are of everything else"));
        setupCard.Children.Add(presets);
        panel.Children.Add(Screens.Card(setupCard));

        var (group, refusal) = carried is { } state ? HitFromGroup.Of(state) : (null, "Open a result and choose Ballistics from it to carry its group in; the hit chance needs your own measured precision.");
        panel.Children.Add(Screens.Dim(group is { } g
            ? string.Create(CultureInfo.CurrentCulture, $"Your group: sigma {g.Precision.SigmaMrad * (Mil ? 1 : 3.4377):0.00} {(Mil ? "mil" : "MOA")} per axis, {g.Shots} shots.")
            : refusal ?? ""));
        var answer = new StackPanel { Spacing = 10 };
        panel.Children.Add(answer);
        panel.Children.Add(Screens.Primary("Work out the chance", () => Work(input, group, answer)));
        return panel;
    }

    private void Work(BallisticInput input, GroupPrecision? group, StackPanel answer)
    {
        answer.Children.Clear();
        double? yards = Number(range);
        double? size = target switch
        {
            "10 in plate" => 10,
            "2 MOA" when yards is { } y => 2 * 1.047 * y / 100,
            "0.5 mil" when yards is { } y => 0.5 * 3.6 * y / 100,
            "Other" => Number(otherSize),
            _ => null,
        };
        if (group is null || yards is null || yards <= 0 || size is null || size <= 0 || Number(wind) is not { } mph)
        {
            answer.Children.Add(Screens.Line(group is null ? "The hit chance needs a group carried in from a result." : "Enter the range, the crosswind and the target's size."));
            return;
        }

        var errors = new Dictionary<HitSource, HitUncertainty>(HitPresets.All[preset].Errors(yards.Value))
        {
            [HitSource.Velocity] = new HitUncertainty(LoadChosen?.MuzzleVelocitySdFps ?? 0),
            [HitSource.Zero] = new HitUncertainty(group.ZeroMrad),
        };
        var setup = new HitSetup(input with { CrosswindMph = mph }, yards.Value, HitTarget.Circle(size.Value), group.Precision, errors, 1, 10_000, 20260924);
        var bar = new ProgressBar { IsIndeterminate = true, MinHeight = 6 };
        answer.Children.Add(Screens.Card(Screens.Line("Working it out."), bar));
        var clock = System.Diagnostics.Stopwatch.StartNew();
        _ = Task.Run(() =>
        {
            var result = HitProbability.Work(setup);
            var curve = result.Refusal is null ? HitProbability.Curve(setup, [.. Enumerable.Range(1, 8).Select(k => yards.Value * k / 4)], 1000) : [];
            return (result, curve);
        }).ContinueWith(done => Dispatcher.UIThread.Post(() =>
        {
            answer.Children.Clear();
            if (!done.IsCompletedSuccessfully)
            {
                answer.Children.Add(Screens.Line("The chance could not be worked out."));
                return;
            }

            var (result, curve) = done.Result;
            DiagnosticLog.Info("phone.hit", ("ms", clock.ElapsedMilliseconds), ("refused", result.Refusal is not null));
            if (result.Refusal is { } refused)
            {
                answer.Children.Add(Screens.Line(refused));
                return;
            }

            var (value, lower, upper) = HitProbability.Percents(result.FirstRound);
            var big = new TextBlock { Text = value, FontSize = 44, // one line on purpose: a percentage
                FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(232, 150, 46)) };
            answer.Children.Add(Screens.Card(big, Screens.Line($"First round on a {units.Length(size.Value)} circle at {units.DistanceText(yards.Value * 36)}: {value}, {lower} to {upper}.")));
            answer.Children.Add(Screens.Card(Screens.Dim("The chance by range, the chosen range marked"),
                new HitCurve { Points = curve, MarkYards = yards, Height = 200, Distance = y => units.DistanceText(y * 36) }));
            var costs = new StackPanel { Spacing = 6, Children = { Screens.Heading("What costs the most") } };
            double most = result.Costs.Count == 0 ? 1 : Math.Max(1e-9, result.Costs.Max(c => c.Cost));
            foreach (var cost in result.Costs.OrderByDescending(c => c.Cost).Take(6))
            {
                costs.Children.Add(Screens.Dim(string.Create(CultureInfo.CurrentCulture, $"{cost.Source}: {100 * cost.Cost:0.#} points")));
                costs.Children.Add(new Border
                {
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = Math.Max(4, 280 * cost.Cost / most),
                    Background = new SolidColorBrush(Color.FromRgb(232, 150, 46)),
                });
            }

            answer.Children.Add(Screens.Card(costs));
        }), TaskScheduler.Default);
    }

    private Control RifleForm()
    {
        var r = RifleChosen;
        var name = Field(r?.Name ?? "My rifle");
        var sight = Field(r?.SightHeightInches?.ToString("0.##", CultureInfo.InvariantCulture) ?? "1.75");
        var zero = Field(r?.ZeroDistanceYards?.ToString("0", CultureInfo.InvariantCulture) ?? "100");
        // Entry 294 section 1: the scope's unit and click as chips, 0.1 mil, 0.05 mil, 1/4 MOA, 1/8 MOA, or any other typed in mil or MOA;
        // a new rifle starts in the scope unit Settings has.
        var usual = r is null ? ScopeClicks.Usual(units.Angular) : (r.ClickValue, r.ClickUnit);
        var click = Field(usual.Item1.ToString("0.###", CultureInfo.InvariantCulture));
        bool mil = usual.Item2 == AngularUnit.Mrad;
        var unitChips = new WrapPanel();
        var typedUnit = new WrapPanel();
        void ShowClicks()
        {
            unitChips.Children.Clear();
            double? now = Number(click);
            foreach (var (words, value, u) in ScopeClicks.Common)
            {
                unitChips.Children.Add(Chip(words, now is { } v && Math.Abs(v - value) < 1e-9 && mil == (u == AngularUnit.Mrad), () =>
                {
                    click.Text = value.ToString("0.###", CultureInfo.InvariantCulture);
                    mil = u == AngularUnit.Mrad;
                    ShowClicks();
                }));
            }

            typedUnit.Children.Clear();
            typedUnit.Children.Add(Chip("mil", mil, () => { mil = true; ShowClicks(); }));
            typedUnit.Children.Add(Chip("MOA", !mil, () => { mil = false; ShowClicks(); }));
        }

        ShowClicks();
        return Screens.Card(Screens.Heading("The rifle"), Picker(book.Rifles.Select(x => x.Name), n => { rifleName = n; Build(); }),
            Labeled("Name", name), Labeled("Sight height, in", sight), Labeled("Zero distance, yd", zero), Screens.Dim("Scope unit and one click"), unitChips,
            Labeled("One click, or any other", click), typedUnit,
            Screens.Primary("Keep this rifle", () =>
            {
                var rifle = (r ?? new Rifle(name.Text ?? "My rifle", usual.Item1, usual.Item2)) with
                {
                    Name = name.Text is { Length: > 0 } n ? n : "My rifle",
                    ClickValue = Number(click) ?? ScopeClicks.Usual(mil ? AngularUnit.Mrad : AngularUnit.Moa).Value,
                    ClickUnit = mil ? AngularUnit.Mrad : AngularUnit.Moa,
                    SightHeightInches = Number(sight),
                    ZeroDistanceYards = Number(zero),
                };
                Keep(book.With(rifle));
                rifleName = rifle.Name;
                open = "";
                Build();
            }));
    }

    private Control LoadForm()
    {
        var l = LoadChosen;
        var name = Field(l?.Name ?? "My load");
        var speed = Field(l?.MuzzleVelocityFps?.ToString("0", CultureInfo.InvariantCulture) ?? "");
        var sd = Field(l?.MuzzleVelocitySdFps?.ToString("0.#", CultureInfo.InvariantCulture) ?? "");
        var bc = Field(l?.BallisticCoefficient?.ToString("0.###", CultureInfo.InvariantCulture) ?? "");
        var weight = Field(l?.BulletWeightGrains?.ToString("0.#", CultureInfo.InvariantCulture) ?? "");
        var g7 = new CheckBox { Content = "G7 (off: G1)", IsChecked = l?.DragModel == DragModel.G7, MinHeight = Screens.Touch };
        return Screens.Card(Screens.Heading("The load"), Picker(book.Loads.Select(x => x.Name), n => { loadName = n; Build(); }),
            Labeled("Name", name), Labeled("Muzzle velocity, ft/s", speed), Labeled("Velocity SD, ft/s", sd), Labeled("Ballistic coefficient", bc), g7,
            Labeled("Bullet weight, gr", weight),
            Screens.Primary("Keep this load", () =>
            {
                var load = (l ?? new Load(name.Text ?? "My load", null)) with
                {
                    Name = name.Text is { Length: > 0 } n ? n : "My load",
                    MuzzleVelocityFps = Number(speed),
                    MuzzleVelocitySdFps = Number(sd),
                    BallisticCoefficient = Number(bc),
                    DragModel = g7.IsChecked == true ? DragModel.G7 : DragModel.G1,
                    BulletWeightGrains = Number(weight),
                };
                Keep(book.With(load));
                loadName = load.Name;
                open = "";
                Build();
            }));
    }

    private Control AirForm()
    {
        var temperature = Field(air.TemperatureF.ToString("0", CultureInfo.InvariantCulture));
        var altitude = Field(air.AltitudeFt.ToString("0", CultureInfo.InvariantCulture));
        var humidity = Field(air.HumidityPct.ToString("0", CultureInfo.InvariantCulture));
        return Screens.Card(Screens.Heading("The air"), Labeled("Temperature, °F", temperature), Labeled("Altitude, ft", altitude), Labeled("Humidity, percent", humidity),
            Screens.Primary("Use this air", () =>
            {
                air = new AirInput(Number(temperature) ?? 59, null, Number(altitude) ?? 0, Number(humidity) ?? 50);
                open = "";
                Build();
            }));
    }

    private void Keep(RecordBook next)
    {
        book = next;
        store.SaveBook(book);
        DiagnosticLog.Info("ballistics.keep", ("rifles", book.Rifles.Count), ("loads", book.Loads.Count));
    }

    private static Control Picker(IEnumerable<string> names, Action<string> chosen)
    {
        var row = new WrapPanel();
        foreach (string name in names)
        {
            row.Children.Add(Chip(name, false, () => chosen(name)));
        }

        return row;
    }

    private static TextBox Field(string text) => new() { Text = text, MinHeight = Screens.Touch, MinWidth = 110 };

    private static Control Labeled(string label, Control field) => new StackPanel { Spacing = 2, Margin = new Thickness(0, 0, 10, 6), Children = { Screens.Dim(label), field } };

    private static double? Number(TextBox box) => double.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : null;

    private static Button Chip(string words, bool on, Action chosen)
    {
        var chip = new Button { Content = words, MinHeight = Screens.Touch, Margin = new Thickness(0, 0, 6, 6) };
        if (on)
        {
            chip.Classes.Add(GroupLab.App.Theme.AppStyles.Chosen);
        }

        chip.Click += (_, _) => chosen();
        return chip;
    }
}
