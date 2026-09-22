using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;
using GroupLab.Core.Trace;

namespace GroupLab.App;

/// <summary>
/// The solver on screen, NOTES-FROM-PLANNING.md entry 112 section 4. The Ballistics screen keeps what the solver needs on the rifle and load
/// records, all of it optional, takes the air as an input, and shows a dope table: range, drop and the wind of a 10 mph crosswind, in the
/// person's units and in the scope's clicks, with the sentence that aerodynamic jump is not modelled beside it. The analysis's zero block
/// carries its correction to a second distance from the same records and air, with the uncertainty carried through, and keeps its refusal
/// where the offset cannot be told from zero. Hit probability and distance normalisation wait for their own entry.
/// </summary>
public sealed partial class MainWindow
{
    private readonly ComboBox ballisticRifle = new() { MinWidth = 220 };
    private readonly ComboBox ballisticLoad = new() { MinWidth = 220 };
    private readonly TextBox sightHeight = Field();
    private readonly TextBox zeroDistance = Field();
    private readonly TextBox twist = Field();
    private readonly ComboBox twistDirection = new() { ItemsSource = new[] { "right-hand", "left-hand" }, SelectedIndex = 0, MinWidth = 120 };
    private readonly TextBox muzzleVelocity = Field();
    private readonly TextBox muzzleVelocitySd = Field();
    private readonly TextBox ballisticCoefficient = Field();
    private readonly ComboBox dragModel = new() { ItemsSource = new[] { "not set", "G1", "G7" }, SelectedIndex = 0, MinWidth = 120 };
    private readonly ComboBox bcReference = new() { ItemsSource = new[] { "not set", "ICAO", "Army Standard Metro" }, SelectedIndex = 0, MinWidth = 120 };
    private readonly TextBox bulletWeight = Field();
    private readonly TextBox bulletLength = Field();
    private readonly TextBox bulletDiameter = Field();
    private readonly TextBox airTemperature = Field("59");
    private readonly TextBox airPressure = Field();
    private readonly TextBox airAltitude = Field("0");
    private readonly TextBox airHumidity = Field("50");
    private readonly TextBox dopeTo = Field("600");
    private readonly TextBox dopeStep = Field("100");
    private readonly StackPanel dopeTable = new() { Spacing = 0 };
    private readonly TextBox carryTo = new() { Width = 90 };
    private readonly TextBox projectTo = Field("600");
    private readonly ComboBox targetShape = new() { ItemsSource = new[] { "circle, its diameter", "rectangle, width by height" }, SelectedIndex = 0, MinWidth = 180 };
    private readonly TextBox targetWidth = Field("2");
    private readonly TextBox targetHeight = Field("2");
    private readonly TextBox windSd = Field();
    private readonly StackPanel projectionLines = new() { Spacing = Tokens.Space4 };
    private readonly TextBlock sdFrom = new() { TextWrapping = TextWrapping.Wrap, IsVisible = false, Classes = { AppStyles.Secondary } };
    private readonly Control ballisticsBody;
    private Button railBallistics = null!;
    private double? carryYards;
    private bool fillingBallistics;

    private static TextBox Field(string text = "") => new() { Width = 110, Text = text };

    /// <summary>What a box holds, turned into the imperial value the records and the solver use, or null where it holds nothing usable.</summary>
    private double? Imperial(TextBox box, BallisticMeasure measure) =>
        Number(box) is { } shown ? BallisticMeasures.ToImperial(shown, measure, units) : null;

    private static double? Number(TextBox box) =>
        double.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && value > 0 && double.IsFinite(value) ? value : null;

    private Control BuildBallistics()
    {
        var column = new StackPanel { Margin = new Thickness(Tokens.Space24, Tokens.Space20), Spacing = Tokens.Space8, MaxWidth = 1100, HorizontalAlignment = HorizontalAlignment.Left };
        column.Children.Add(new TextBlock { Text = "Ballistics", Classes = { AppStyles.Title } });
        column.Children.Add(Line("The solver's trajectory for a rifle and load. What it needs is kept on the records, and all of it is optional: a record without it cannot use the solver, and this says which field is missing."));
        // Entry 131 section 8: the imperial and metric toggle, at the top where Alan's own calculator has it. It moves the whole
        // application's units, because a page in one system and a panel in another is how somebody reads a number as the wrong thing.
        var toggle = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space4 };
        toggle.Children.Add(new TextBlock { Text = "Units", VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Label } });
        foreach (var (name, chosen) in new[] { ("Imperial", UnitSettings.Imperial), ("Metric", UnitSettings.Metric) })
        {
            var button = new Button { Content = name, Name = "BallisticUnits" + name };
            button.Click += (_, _) => UseUnits(chosen);
            unitButtons[name] = button;
            toggle.Children.Add(button);
        }

        column.Children.Add(toggle);
        column.Children.Add(Row(FieldLabel("Rifle"), ballisticRifle, FieldLabel("Load"), ballisticLoad));
        foreach (var combo in new[] { ballisticRifle, ballisticLoad })
        {
            combo.SelectionChanged += (_, _) =>
            {
                if (!fillingBallistics)
                {
                    ShowBallisticRecords();
                }
            };
        }

        column.Children.Add(Heading("The rifle"));
        column.Children.Add(Row(Measured("Sight height", BallisticMeasure.SmallLength), sightHeight, Distanced("Zero distance"), zeroDistance));
        column.Children.Add(Row(Measured("Twist", BallisticMeasure.SmallLength, " per turn"), twist, twistDirection));
        column.Children.Add(Heading("The load"));
        column.Children.Add(Row(Measured("Muzzle velocity", BallisticMeasure.Speed), muzzleVelocity, Measured("Its standard deviation", BallisticMeasure.Speed), muzzleVelocitySd));
        column.Children.Add(sdFrom);
        column.Children.Add(Row(FieldLabel("BC"), ballisticCoefficient, FieldLabel("Drag model"), dragModel, FieldLabel("Its reference atmosphere"), bcReference));
        // Grains stay grains on both sides of the toggle: a reloader weighs in grains whatever else they measure in.
        column.Children.Add(Row(FieldLabel("Bullet weight, gr"), bulletWeight, Measured("Length", BallisticMeasure.SmallLength), bulletLength, Measured("Diameter", BallisticMeasure.SmallLength), bulletDiameter));
        column.Children.Add(Row(Button("Keep these on the records", KeepBallistics)));
        column.Children.Add(Heading("The air"));
        column.Children.Add(Row(Measured("Temperature", BallisticMeasure.Temperature), airTemperature, Measured("Station pressure", BallisticMeasure.Pressure), airPressure,
            Measured("Altitude", BallisticMeasure.Altitude), airAltitude, FieldLabel("Humidity, %"), airHumidity));
        column.Children.Add(Line("Leave the pressure empty to take it from the altitude. The zero correction on the analysis carries in this air too."));
        column.Children.Add(Heading("Dope table"));
        column.Children.Add(Row(Distanced("To"), dopeTo, FieldLabel("Every"), dopeStep, Button("Work out the table", FillDope)));

        // Entry 131 section 8: the curve beside the table. A table answers "what do I dial at 600" exactly and cannot show shape; the curve
        // shows where the drop runs away and how far the velocity holds, which is what a person reads a trajectory for.
        var series = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space4 };
        foreach (var which in new[] { TrajectorySeries.Drop, TrajectorySeries.Wind, TrajectorySeries.Velocity, TrajectorySeries.Energy })
        {
            var button = new Button { Content = TrajectoryGraph.Title(which).Split(',')[0], Name = "TrajectorySeries" + which };
            button.Click += (_, _) =>
            {
                trajectory.Series = which;
                trajectory.InvalidateVisual();
                ShowTrajectoryTitle();
            };
            series.Children.Add(button);
        }

        column.Children.Add(series);
        column.Children.Add(trajectoryTitle);
        column.Children.Add(trajectory);
        column.Children.Add(dopeTable);

        // Entry 113 section 3: the analysed group carried to another distance, its hit probability there and its predicted size.
        column.Children.Add(Heading("The analysed group at another distance"));
        column.Children.Add(Line("A prediction from the group open in the analysis, the rifle and load it names, and the air above: its sigma carried through the solver, with the load's velocity SD and the crosswind's uncertainty added where they are given. It is never a measurement."));
        column.Children.Add(Row(Distanced("At"), projectTo, Measured("Crosswind uncertainty", BallisticMeasure.WindSpeed), windSd));
        column.Children.Add(Row(FieldLabel("Target"), targetShape, Lengthed("Size"), targetWidth, targetHeight, Button("Work it out", FillProjection)));
        column.Children.Add(projectionLines);
        BuildChronograph(column);
        unitBoxes.AddRange(new (TextBox, BallisticMeasure)[]
        {
            (sightHeight, BallisticMeasure.SmallLength),
            (twist, BallisticMeasure.SmallLength),
            (muzzleVelocity, BallisticMeasure.Speed),
            (muzzleVelocitySd, BallisticMeasure.Speed),
            (bulletLength, BallisticMeasure.SmallLength),
            (bulletDiameter, BallisticMeasure.SmallLength),
            (airTemperature, BallisticMeasure.Temperature),
            (airPressure, BallisticMeasure.Pressure),
            (airAltitude, BallisticMeasure.Altitude),
            (windSd, BallisticMeasure.WindSpeed),
        });
        RelabelBallistics();
        airTemperature.Text = BallisticMeasures.Text(59, BallisticMeasure.Temperature, units);
        return new ScrollViewer { Content = column, IsVisible = false };
    }

    private Rifle? ChosenRifle => ballisticRifle.SelectedIndex > 0 && ballisticRifle.SelectedIndex <= book.Rifles.Count ? book.Rifles[ballisticRifle.SelectedIndex - 1] : null;

    private Load? ChosenLoad => ballisticLoad.SelectedIndex > 0 && ballisticLoad.SelectedIndex <= book.Loads.Count ? book.Loads[ballisticLoad.SelectedIndex - 1] : null;

    /// <summary>Fills the choices from the book, the marking's own rifle and load chosen where it names them, and the fields from the records.</summary>
    private void FillBallistics()
    {
        fillingBallistics = true;
        string? rifle = ChosenRifle?.Name ?? session.State.Rifle?.Name, load = ChosenLoad?.Name ?? session.State.Load;
        ballisticRifle.ItemsSource = new[] { "No rifle" }.Concat(book.Rifles.Select(r => r.Name)).ToList();
        ballisticLoad.ItemsSource = new[] { "No load" }.Concat(book.Loads.Select(l => l.Name)).ToList();
        ballisticRifle.SelectedIndex = book.Rifles.FindIndex(r => string.Equals(r.Name, rifle, StringComparison.OrdinalIgnoreCase)) + 1;
        ballisticLoad.SelectedIndex = book.Loads.FindIndex(l => string.Equals(l.Name, load, StringComparison.OrdinalIgnoreCase)) + 1;
        fillingBallistics = false;
        ShowBallisticRecords();
        FillProjection();
        FillChronograph();
    }

    private static string Text(double? value) => value is { } v ? v.ToString("0.####", CultureInfo.InvariantCulture) : "";

    /// <summary>The page's unit buttons, so the one in force can be shown as chosen.</summary>
    private readonly Dictionary<string, Button> unitButtons = [];

    /// <summary>Every label that names a unit, with how to write it. They are rewritten when the toggle moves rather than built once.</summary>
    private readonly List<(TextBlock Label, Func<string> Words)> unitLabels = [];

    /// <summary>Every box holding a value in the person's units, with what it measures, so the toggle can rewrite what is in it.</summary>
    private readonly List<(TextBox Box, BallisticMeasure Measure)> unitBoxes = [];

    private TextBlock Measured(string name, BallisticMeasure measure, string after = "")
    {
        var label = FieldLabel("");
        unitLabels.Add((label, () => name + ", " + BallisticMeasures.Symbol(measure, units) + after));
        return label;
    }

    private TextBlock Distanced(string name)
    {
        var label = FieldLabel("");
        unitLabels.Add((label, () => name + ", " + UnitSettings.Symbol(units.Distance)));
        return label;
    }

    private TextBlock Lengthed(string name)
    {
        var label = FieldLabel("");
        unitLabels.Add((label, () => name + ", " + UnitSettings.Symbol(units.Linear)));
        return label;
    }

    /// <summary>Writes every unit-bearing label on this page as the units in force say it.</summary>
    private void RelabelBallistics()
    {
        foreach (var (label, words) in unitLabels)
        {
            label.Text = words();
        }

        foreach (var (name, button) in unitButtons)
        {
            button.Classes.Set(AppStyles.Primary, name == (BallisticMeasures.IsMetric(units) ? "Metric" : "Imperial"));
        }
    }

    /// <summary>
    /// Moves the whole application to these units, entry 131 section 8. What is typed on this page is rewritten in the new units first, from
    /// the imperial values everything is stored in, so a number never silently changes meaning under somebody.
    /// </summary>
    internal void UseUnits(UnitSettings chosen)
    {
        ArgumentNullException.ThrowIfNull(chosen);
        var imperial = unitBoxes
            .Select(b => (b.Box, b.Measure, Value: Number(b.Box) is { } v ? BallisticMeasures.ToImperial(v, b.Measure, units) : (double?)null))
            .ToList();
        var distances = new[] { zeroDistance, dopeTo, dopeStep, projectTo }
            .Select(box => (Box: box, Value: Number(box) is { } v ? UnitSettings.DistanceToInches(v, units.Distance) : (double?)null))
            .ToList();
        var lengths = new[] { targetWidth, targetHeight }
            .Select(box => (Box: box, Value: Number(box) is { } v ? UnitSettings.ToInches(v, units.Linear) : (double?)null))
            .ToList();

        SetUnits(chosen);
        foreach (var (box, measure, value) in imperial)
        {
            box.Text = BallisticMeasures.Text(value, measure, units);
        }

        foreach (var (box, value) in distances)
        {
            box.Text = Text(value is { } v ? UnitSettings.DistanceFromInches(v, units.Distance) : null);
        }

        foreach (var (box, value) in lengths)
        {
            box.Text = Text(value is { } v ? UnitSettings.FromInches(v, units.Linear) : null);
        }

        DiagnosticLog.Info("units.chosen", ("linear", units.Linear.ToString()), ("distance", units.Distance.ToString()), ("where", "ballistics"));
        FillDope();
    }

    private void ShowBallisticRecords()
    {
        var rifle = ChosenRifle;
        var load = ChosenLoad;
        // Entry 115 section 3: a velocity SD GroupLab worked out says where it came from, beside the field it is in.
        sdFrom.Text = load?.MuzzleVelocitySdFrom is { } from ? "The velocity SD is from " + from + "." : "";
        sdFrom.IsVisible = load?.MuzzleVelocitySdFrom is not null;
        sightHeight.Text = BallisticMeasures.Text(rifle?.SightHeightInches, BallisticMeasure.SmallLength, units);
        zeroDistance.Text = Text(rifle?.ZeroDistanceYards is { } yards ? UnitSettings.DistanceFromInches(yards * 36, units.Distance) : null);
        twist.Text = BallisticMeasures.Text(rifle?.TwistInches, BallisticMeasure.SmallLength, units);
        twistDirection.SelectedIndex = rifle?.TwistDirection == -1 ? 1 : 0;
        muzzleVelocity.Text = BallisticMeasures.Text(load?.MuzzleVelocityFps, BallisticMeasure.Speed, units);
        muzzleVelocitySd.Text = BallisticMeasures.Text(load?.MuzzleVelocitySdFps, BallisticMeasure.Speed, units);
        ballisticCoefficient.Text = Text(load?.BallisticCoefficient);
        dragModel.SelectedIndex = load?.DragModel switch { DragModel.G1 => 1, DragModel.G7 => 2, _ => 0 };
        bcReference.SelectedIndex = load?.BcReference switch { ReferenceAtmosphere.Icao => 1, ReferenceAtmosphere.ArmyStandardMetro => 2, _ => 0 };
        bulletWeight.Text = Text(load?.BulletWeightGrains);
        bulletLength.Text = BallisticMeasures.Text(load?.BulletLengthInches, BallisticMeasure.SmallLength, units);
        bulletDiameter.Text = BallisticMeasures.Text(load?.BulletDiameterInches, BallisticMeasure.SmallLength, units);
        FillDope();
    }

    /// <summary>Keeps the fields on the chosen rifle and load; an empty field clears its value, which the solver then says is missing.</summary>
    internal void KeepBallistics()
    {
        var rifle = ChosenRifle;
        var load = ChosenLoad;
        if (rifle is null && load is null)
        {
            status.Text = "Choose a rifle or a load to keep these on.";
            return;
        }

        if (rifle is not null)
        {
            book = book.With(rifle with
            {
                SightHeightInches = Imperial(sightHeight, BallisticMeasure.SmallLength),
                ZeroDistanceYards = Number(zeroDistance) is { } zero ? UnitSettings.DistanceToInches(zero, units.Distance) / 36 : null,
                TwistInches = Imperial(twist, BallisticMeasure.SmallLength),
                TwistDirection = Number(twist) is null ? null : twistDirection.SelectedIndex == 1 ? -1 : 1,
            });
        }

        if (load is not null)
        {
            book = book.With(load with
            {
                MuzzleVelocityFps = Imperial(muzzleVelocity, BallisticMeasure.Speed),
                MuzzleVelocitySdFps = Imperial(muzzleVelocitySd, BallisticMeasure.Speed),
                BallisticCoefficient = Number(ballisticCoefficient),
                DragModel = dragModel.SelectedIndex switch { 1 => DragModel.G1, 2 => DragModel.G7, _ => null },
                BcReference = bcReference.SelectedIndex switch { 1 => ReferenceAtmosphere.Icao, 2 => ReferenceAtmosphere.ArmyStandardMetro, _ => null },
                BulletWeightGrains = Number(bulletWeight),
                BulletLengthInches = Imperial(bulletLength, BallisticMeasure.SmallLength),
                BulletDiameterInches = Imperial(bulletDiameter, BallisticMeasure.SmallLength),
            });
        }

        SaveBook();
        DiagnosticLog.Info("records.ballistics", ("rifle", rifle is not null), ("load", load is not null));
        status.Text = $"Kept on {string.Join(" and ", new[] { rifle?.Name, load?.Name }.OfType<string>())}.";
        string? keepRifle = rifle?.Name, keepLoad = load?.Name;
        fillingBallistics = true;
        ballisticRifle.ItemsSource = new[] { "No rifle" }.Concat(book.Rifles.Select(r => r.Name)).ToList();
        ballisticLoad.ItemsSource = new[] { "No load" }.Concat(book.Loads.Select(l => l.Name)).ToList();
        ballisticRifle.SelectedIndex = book.Rifles.FindIndex(r => r.Name == keepRifle) + 1;
        ballisticLoad.SelectedIndex = book.Loads.FindIndex(l => l.Name == keepLoad) + 1;
        fillingBallistics = false;
        ShowBallisticRecords();
        Refresh();
    }

    /// <summary>The air as the Ballistics screen states it, the standard atmosphere where a field is empty or not a number.</summary>
    private AirInput Air() => new(
        double.TryParse(airTemperature.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double t)
            ? BallisticMeasures.ToImperial(t, BallisticMeasure.Temperature, units) : 59,
        Imperial(airPressure, BallisticMeasure.Pressure),
        double.TryParse(airAltitude.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double a)
            ? BallisticMeasures.ToImperial(a, BallisticMeasure.Altitude, units) : 0,
        double.TryParse(airHumidity.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double h) ? Math.Clamp(h, 0, 100) : 50);

    private string AirWords(AirInput air)
    {
        // The air is stated in whichever system the toggle is on, entry 131 section 8. It is held in imperial, as everything here is.
        string Shown(double imperial, BallisticMeasure measure, string format) => string.Create(CultureInfo.InvariantCulture,
            $"{BallisticMeasures.FromImperial(imperial, measure, units).ToString(format, CultureInfo.InvariantCulture)} {BallisticMeasures.Symbol(measure, units)}");

        return string.Create(CultureInfo.InvariantCulture,
            $"{Shown(air.TemperatureF, BallisticMeasure.Temperature, "0.#")}, "
            + $"{(air.PressureInHg is { } p ? Shown(p, BallisticMeasure.Pressure, "0.00") : Shown(air.AltitudeFt, BallisticMeasure.Altitude, "0") + " of altitude")}, "
            + $"{air.HumidityPct:0} percent humidity");
    }

    private const string DopeColumns = "90,*,*,*,*,*,*";

    /// <summary>The dope table for the chosen rifle and load in the stated air, or which fields it still needs.</summary>
    private readonly TrajectoryGraph trajectory = new();

    private readonly TextBlock trajectoryTitle = new() { Classes = { AppStyles.Label } };

    /// <summary>The graph's caption, which names the series and its unit so the axis figures mean something.</summary>
    private void ShowTrajectoryTitle() => trajectoryTitle.Text = TrajectoryGraph.Title(trajectory.Series);

    /// <summary>What the trajectory graph is showing, for the headless tests.</summary>
    internal string TrajectorySays => trajectory.Description;

    /// <summary>Chooses the graph's series, for the headless tests.</summary>
    internal void ShowTrajectorySeries(TrajectorySeries which)
    {
        trajectory.Series = which;
        trajectory.InvalidateVisual();
        ShowTrajectoryTitle();
    }

    internal void FillDope()
    {
        dopeTable.Children.Clear();
        trajectory.Points = [];
        ShowTrajectoryTitle();
        var rifle = ChosenRifle;
        var load = ChosenLoad;
        var missing = SolverUse.Missing(rifle, load);
        if (missing.Count > 0)
        {
            dopeTable.Children.Add(Line("The solver needs " + Joined(missing) + "."));
            return;
        }

        double? to = Number(dopeTo), step = Number(dopeStep);
        if (to is null || step is null)
        {
            dopeTable.Children.Add(Line("Enter how far the table goes and how often it has a row, in " + UnitSettings.Symbol(units.Distance) + "."));
            return;
        }

        double toYards = UnitSettings.DistanceToInches(to.Value, units.Distance) / 36, stepYards = UnitSettings.DistanceToInches(step.Value, units.Distance) / 36;
        if (toYards / stepYards > 200 || toYards > 3000)
        {
            dopeTable.Children.Add(Line("That is more than 200 rows or past 3000 yd; choose a shorter table or a longer step."));
            return;
        }

        var air = Air();
        var input = SolverUse.Input(rifle, load, air)!;
        var table = SolverUse.Dope(input, toYards, stepYards);
        trajectory.Points = table.Points;
        trajectory.ZeroYards = rifle!.ZeroDistanceYards;
        trajectory.Distance = yards => units.DistanceText(yards * 36);
        trajectory.InvalidateVisual();
        Grid Cells(IEnumerable<string> texts, bool heading)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions(DopeColumns) };
            int c = 0;
            foreach (string text in texts)
            {
                var cell = new TextBlock
                {
                    Text = text,
                    FontSize = Tokens.DetailSize,
                    FontFamily = heading ? Tokens.Sans : Mono,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 0, Tokens.Space8, 0),
                    Classes = { heading ? AppStyles.Dim : AppStyles.Secondary },
                };
                Grid.SetColumn(cell, c++);
                grid.Children.Add(cell);
            }

            return grid;
        }

        string length = UnitSettings.Symbol(units.Linear), angle = UnitSettings.Symbol(units.Angular);
        var head = Cells([$"range, {UnitSettings.Symbol(units.Distance)}", $"drop, {length}", $"elevation, {angle}", "clicks", $"10 mph wind, {length}", $"windage, {angle}", "clicks"], heading: true);
        head.Margin = new Thickness(Tokens.Space4, Tokens.Space4, Tokens.Space4, Tokens.Space4);
        dopeTable.Children.Add(head);
        int index = 0;
        foreach (var point in table.Points.Where(p => p.RangeYards > 0))
        {
            double range = point.RangeYards * 36;
            string Angle(double inches) => units.Angle(Math.Abs(inches), range) is { } a ? a.ToString("0.00", CultureInfo.InvariantCulture) : "";
            // Anything under the display's resolution is zero, with no direction to dial.
            string ClickText(double inches, string direction) => Math.Abs(inches) < 5e-4 ? "0" : Clicks.For(inches, range, rifle!, direction).Describe();
            var row = new Border
            {
                Child = Cells(
                [
                    UnitSettings.DistanceFromInches(range, units.Distance).ToString("0", CultureInfo.InvariantCulture),
                    units.Number(point.DropInches),
                    Angle(point.DropInches) + (point.DropInches <= -5e-4 ? " up" : point.DropInches >= 5e-4 ? " down" : ""),
                    ClickText(point.DropInches, point.DropInches < 0 ? "up" : "down"),
                    units.Number(Math.Abs(point.WindInches)),
                    Angle(point.WindInches),
                    ClickText(point.WindInches, "into the wind"),
                ], heading: false),
                Padding = new Thickness(Tokens.Space4, 2),
                Classes = { AppStyles.TableRow },
            };
            if (index++ % 2 == 1)
            {
                row.Classes.Add(AppStyles.Shaded);
            }

            dopeTable.Children.Add(row);
        }

        dopeTable.Children.Add(Line($"Zeroed at {units.DistanceText(input.ZeroRangeYards * 36)}, sight height {units.Length(input.SightHeightInches)}, in {AirWords(air)}. Drop is below the line of sight when negative; the clicks are {rifle!.Name}'s, {rifle.DescribeClick()}."));
        dopeTable.Children.Add(Line(table.Stability is { } sg
            ? string.Create(CultureInfo.InvariantCulture, $"Gyroscopic stability {sg:0.00} by Miller's rule. Spin drift is not in the wind column.")
            : "No spin drift or stability: they need the twist, and the bullet's length and diameter."));
        foreach (string sentence in table.NotModelled)
        {
            dopeTable.Children.Add(new TextBlock { Text = sentence, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
        }
    }

    private static string Joined(IReadOnlyList<string> items) => items.Count switch
    {
        1 => items[0],
        _ => string.Join(", ", items.Take(items.Count - 1)) + " and " + items[^1],
    };

    /// <summary>
    /// The zero correction carried to a second distance, beneath the zero block's verdict: an input for the distance and, once it is set, each
    /// axis there with its uncertainty and clicks, or the refusal that nothing indistinguishable from zero can be carried.
    /// </summary>
    private void ShowCarry(MarkingState state, StackPanel zeroPanel)
    {
        if (Zeroing.For(state) is not { } zero)
        {
            return;
        }

        zeroPanel.Children.Add(FieldLabel("Carried to another distance"));
        if (state.ShotDistanceInches is not { } shot)
        {
            zeroPanel.Children.Add(Line("Set the shot distance to carry this correction to another distance."));
            return;
        }

        var rifle = book.FindRifle(state.Rifle?.Name);
        var load = book.FindLoad(state.Load);
        var missing = SolverUse.Missing(rifle, load);
        if (missing.Count > 0)
        {
            zeroPanel.Children.Add(Line("Carrying it needs " + Joined(missing) + ": optional fields of the records, on the Ballistics screen."));
            return;
        }

        if (carryTo.Parent is Panel old)
        {
            old.Children.Remove(carryTo);
        }

        zeroPanel.Children.Add(Row(carryTo, new TextBlock { Text = UnitSettings.Symbol(units.Distance), VerticalAlignment = VerticalAlignment.Center }, Button("Carry", CarryFromBox)));
        if (carryYards is not { } to)
        {
            return;
        }

        var air = Air();
        var carried = SolverUse.Carry(SolverUse.Input(rifle, load, air)!, zero, shot / 36, to, rifle);
        double toInches = to * 36;
        string Both(double inches) => units.AngleText(Math.Abs(inches), toInches) is { } angle ? $"{units.Length(Math.Abs(inches))}  {angle}" : units.Length(Math.Abs(inches));
        string at = units.DistanceText(toInches);
        foreach (var (name, axis) in new[] { ("Windage", carried.Windage), ("Elevation", carried.Elevation) })
        {
            zeroPanel.Children.Add(Line(axis.Distinguishable
                ? $"{name} at {at}: {Both(axis.OffsetInches)} {axis.Dial}, give or take {Both(axis.HalfWidthInches)}" + (axis.Clicks is { } clicks
                    ? string.Create(CultureInfo.InvariantCulture, $"; {clicks.Describe()}, leaving {Math.Abs(clicks.ResidualAngle):0.00} {(clicks.Unit == AngularUnit.Mrad ? "mil" : "MOA")}.")
                    : ".")
                : $"{name}: not distinguishable from zero at {zero.Shots} shots, so there is nothing to carry to {at}."));
        }

        zeroPanel.Children.Add(Note(string.Create(CultureInfo.InvariantCulture,
            $"Windage carries in proportion to range. Elevation carries along the solver's path, by {carried.ElevationTransfer:0.000} from {units.DistanceText(shot)} to {at} against {carried.WindageTransfer:0.000} for a straight line, in {AirWords(air)}. The uncertainty carries by the same factors. ")
            + string.Join(" ", BallisticSolver.NotModelled)));
    }

    private void CarryFromBox()
    {
        if (Number(carryTo) is { } value)
        {
            carryYards = UnitSettings.DistanceToInches(value, units.Distance) / 36;
            Refresh();
        }
        else
        {
            problem.Text = "Enter the distance to carry the correction to as a number of " + UnitSettings.Symbol(units.Distance) + ".";
        }
    }

    /// <summary>
    /// The analysed group at another distance, entry 113 section 3: its per-axis sigma there at the estimate and at both ends of its interval,
    /// labelled a prediction; whether that is angular scaling and nothing more; and the chance of a hit on the target, as a range.
    /// </summary>
    internal void FillProjection()
    {
        projectionLines.Children.Clear();
        var state = session.State;
        var rifle = book.FindRifle(state.Rifle?.Name);
        var load = book.FindLoad(state.Load);
        var figures = GroupAnalysis.Analyse(state);
        var group = figures.Excluded > 0 ? figures.WithoutExclusions : figures.AllShots;
        var missing = SolverUse.Missing(rifle, load);
        string? refusal = state.ShotDistanceInches is null ? "The analysed group has no shot distance; set it in the marking."
            : group?.Sigma is null ? "The analysis has no group with a sigma to carry; mark at least " + GroupAnalysis.MinimumShotsForDispersion.ToString(CultureInfo.InvariantCulture) + " shots and accept them."
            : missing.Count > 0 ? "The solver needs " + Joined(missing) + ", for the rifle and load the analysis names."
            : Number(projectTo) is null ? "Enter the distance to carry the group to."
            : Number(targetWidth) is null || (targetShape.SelectedIndex == 1 && Number(targetHeight) is null) ? "Enter the target's size."
            : null;
        if (refusal is not null)
        {
            projectionLines.Children.Add(Line(refusal));
            return;
        }

        var air = Air();
        var input = SolverUse.Input(rifle, load, air)!;
        var sigma = group!.Sigma!;
        var estimate = new Estimate(sigma.Value, sigma.Lower ?? sigma.Value, sigma.Upper ?? sigma.Value);
        double from = state.ShotDistanceInches!.Value / 36, to = UnitSettings.DistanceToInches(Number(projectTo)!.Value, units.Distance) / 36;
        var (projection, failed) = Projection.Project(input, estimate, from, to, load!.MuzzleVelocitySdFps, Imperial(windSd, BallisticMeasure.WindSpeed));
        if (projection is null)
        {
            projectionLines.Children.Add(Line(failed!));
            return;
        }

        string at = units.DistanceText(to * 36);
        string Axis(Func<ProjectedSigma, double> pick) => $"{units.Length(pick(projection.Point))} ({units.Number(pick(projection.Lower))} to {units.Number(pick(projection.Upper))})";
        projectionLines.Children.Add(new TextBlock { Text = $"Predicted at {at}, not measured", FontWeight = FontWeight.SemiBold, Classes = { AppStyles.Warn } });
        projectionLines.Children.Add(Line($"Sigma across {Axis(p => p.AcrossInches)}, up and down {Axis(p => p.UpDownInches)}, the brackets from the ends of the sigma interval measured at {units.DistanceText(from * 36)}."));
        double cep = GroupStatistics.CepCorrNormal(projection.Point.AcrossInches * projection.Point.AcrossInches, 0, projection.Point.UpDownInches * projection.Point.UpDownInches, 0.5);
        projectionLines.Children.Add(Line($"CEP 50 about {units.Length(cep)} there, about the group's own centre."));
        projectionLines.Children.Add(Line(projection.AngularOnly
            ? "No velocity SD or crosswind uncertainty is given, so this is the group scaled by angle and nothing more."
            : $"Of that, the load's velocity SD of {units.Speed(load.MuzzleVelocitySdFps ?? 0)} gives {units.Length(projection.VelocityAtToInches)} up and down at {at}, having been taken out of the group measured, where it gave {units.Length(projection.VelocityAtFromInches)}; the crosswind's uncertainty gives {units.Length(projection.WindAtToInches)} across."));
        if (projection.Lower.LowerEndAllVelocity)
        {
            projectionLines.Children.Add(Note("At the lower end of the sigma interval the velocity SD accounts for all of the vertical measured, so that end's vertical is the velocity's alone."));
        }

        // The group's centre carried to the distance, where the zero correction has one; otherwise the aim.
        double muX = 0, muY = 0;
        string centred = "about the aim";
        if (Zeroing.For(state) is { } zero)
        {
            var carried = SolverUse.Carry(input, zero, from, to, rifle);
            muX = carried.Windage.OffsetInches;
            muY = carried.Elevation.OffsetInches;
            centred = $"about the group's centre carried there, {units.Length(Math.Abs(muX))} {(muX >= 0 ? "right" : "left")} and {units.Length(Math.Abs(muY))} {(muY >= 0 ? "low" : "high")} of the aim";
        }

        double width = UnitSettings.ToInches(Number(targetWidth)!.Value, units.Linear);
        double height = targetShape.SelectedIndex == 1 ? UnitSettings.ToInches(Number(targetHeight)!.Value, units.Linear) : width;
        var hit = targetShape.SelectedIndex == 1
            ? Projection.Hit(projection, muX, muY, (x, y, sx, sy) => Projection.HitRectangle(x, y, sx, sy, width, height))
            : Projection.Hit(projection, muX, muY, (x, y, sx, sy) => Projection.HitCircle(x, y, sx, sy, width / 2));
        string target = targetShape.SelectedIndex == 1 ? $"a {units.Number(width)} by {units.Length(height)} rectangle" : $"a {units.Length(width)} circle";
        string Percent(double p) => (100 * p).ToString("0", CultureInfo.InvariantCulture);
        projectionLines.Children.Add(new TextBlock
        {
            Text = $"Chance of a hit on {target} at {at}: {Percent(hit.AtPoint)} percent, between {Percent(hit.AtUpperSigma)} and {Percent(hit.AtLowerSigma)} percent across the sigma interval, {centred}.",
            TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeight.SemiBold,
        });
        foreach (string sentence in BallisticSolver.NotModelled)
        {
            projectionLines.Children.Add(Note(sentence));
        }
    }

    /// <summary>Carries the analysed group to a distance and a target, as the screen's fields do, for the headless tests.</summary>
    internal void ProjectGroup(string distance, int shape, string width, string height, string wind)
    {
        projectTo.Text = distance;
        targetShape.SelectedIndex = shape;
        targetWidth.Text = width;
        targetHeight.Text = height;
        windSd.Text = wind;
        FillProjection();
    }

    internal IEnumerable<string> ProjectionText => projectionLines.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");

    /// <summary>Carries the zero correction to a distance in the display unit, as the Carry button does, for the headless tests.</summary>
    internal void CarryTo(string distance)
    {
        carryTo.Text = distance;
        CarryFromBox();
    }

    /// <summary>Shows or leaves the Ballistics screen.</summary>
    internal void ShowBallistics(bool on = true) => Go(on ? Destination.Ballistics : Destination.Analyse);

    internal bool ShowingBallistics => destination == Destination.Ballistics;

    /// <summary>Sets the Ballistics screen's fields for the chosen rifle and load, as a person would, for the headless tests.</summary>
    internal void SetBallisticFields(string rifle, string load, string sight, string zero, string velocity, string bc, int model, int reference, string weight)
    {
        ballisticRifle.SelectedIndex = book.Rifles.FindIndex(r => r.Name == rifle) + 1;
        ballisticLoad.SelectedIndex = book.Loads.FindIndex(l => l.Name == load) + 1;
        sightHeight.Text = sight;
        zeroDistance.Text = zero;
        muzzleVelocity.Text = velocity;
        ballisticCoefficient.Text = bc;
        dragModel.SelectedIndex = model;
        bcReference.SelectedIndex = reference;
        bulletWeight.Text = weight;
    }

    /// <summary>Chooses a load on the ballistics page by name, for the headless tests.</summary>
    internal void ChooseBallisticLoad(string name)
    {
        ballisticLoad.ItemsSource = new[] { "No load" }.Concat(book.Loads.Select(l => l.Name)).ToList();
        ballisticLoad.SelectedIndex = book.Loads.FindIndex(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase)) + 1;
    }

    /// <summary>The record book, kept as it is set, for the headless tests.</summary>
    internal RecordBook Book
    {
        get => book;
        set
        {
            book = value;
            SaveBook();
            Refresh();
        }
    }

    /// <summary>The dope table's rows as text, for the headless tests.</summary>
    internal IReadOnlyList<string> DopeRows =>
        [.. dopeTable.Children.Select(c => c switch
        {
            Border { Child: Grid grid } => string.Join(" | ", grid.Children.OfType<TextBlock>().Select(t => t.Text)),
            Grid grid => string.Join(" | ", grid.Children.OfType<TextBlock>().Select(t => t.Text)),
            TextBlock text => text.Text ?? "",
            _ => "",
        })];

    internal IEnumerable<string> BallisticsText => ballisticsBody.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");
}
