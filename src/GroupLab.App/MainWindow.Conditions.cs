using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 416, Alan's concept A on the desktop: the conditions on the day entered where the speeds are, inside Velocity and
/// the vertical, once it has readings. Closed, one line says what is in use and where it came from; open, the four fields in the person's own
/// units, the station pressure link, Fill from Ballistics and Standard day, the rifle's line with a link to its page, Remember for my next
/// sheet, and the line comparing velocity alone with a standard day. A value changes the block at once.
/// </summary>
public partial class MainWindow
{
    private bool conditionsOpen;
    private bool conditionsByPressure;

    /// <summary>Opens or closes the part, as its heading does, for the headless tests.</summary>
    internal void OpenConditions(bool open)
    {
        conditionsOpen = open;
        Refresh();
    }

    /// <summary>The conditions part as last built, for the headless tests.</summary>
    internal ConditionsPart? Conditions { get; private set; }

    /// <summary>The part under the block: closed, its line; open, the form.</summary>
    private Control BuildConditionsPart(MarkingState state, VelocityConditions? ballistics, VelocityConditions? used, string? comparison)
    {
        var part = new ConditionsPart(this, state, ballistics, used, comparison);
        Conditions = part;
        return part.View;
    }

    /// <summary>Entry 416 section 4: what the person entered, kept for their next sheet of the day where Remember is ticked.</summary>
    private void RememberConditions(SessionConditions? conditions, bool remember) =>
        settingsStore.SaveConditionsMemory(remember, DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), conditions);

    /// <summary>The form itself: its fields and buttons, and what each does to the session.</summary>
    internal sealed class ConditionsPart
    {
        private readonly MainWindow window;
        private readonly SessionConditions own;

        internal ConditionsPart(MainWindow window, MarkingState state, VelocityConditions? ballistics, VelocityConditions? used, string? comparison)
        {
            this.window = window;
            own = state.Conditions ?? new SessionConditions();
            var units = window.units;
            Summary = ConditionsWords.Summary(used, units);
            Comparison = comparison;
            FillReason = ballistics is null ? "Ballistics has no conditions for this rifle and load: choose them there, with this sheet's rifle and load, first." : null;

            var column = new StackPanel { Spacing = Tokens.Space8, Margin = new Thickness(0, Tokens.Space12, 0, 0) };
            var header = new Button { Content = window.conditionsOpen ? "Conditions on the day  ▴" : "Conditions on the day  ▾", Classes = { AppStyles.Link } };
            AutomationProperties.SetName(header, "Conditions on the day");
            AutomationProperties.SetAutomationId(header, "conditions-open");
            header.Click += (_, _) =>
            {
                window.conditionsOpen = !window.conditionsOpen;
                window.Refresh();
            };
            column.Children.Add(header);
            column.Children.Add(new TextBlock { Text = Summary, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } });
            if (!window.conditionsOpen)
            {
                View = column;
                return;
            }

            bool pressure = window.conditionsByPressure || (own.PressureInHg is not null && own.AltitudeFt is null);
            Temperature = Field("Temperature", "conditions-temperature", own.TemperatureF, BallisticMeasure.Temperature, units);
            Where = pressure
                ? Field("Station pressure", "conditions-pressure", own.PressureInHg, BallisticMeasure.Pressure, units)
                : Field("Altitude", "conditions-altitude", own.AltitudeFt, BallisticMeasure.Altitude, units);
            Humidity = Field("Humidity", "conditions-humidity", own.HumidityPct, null, units, "%");
            Angle = Field("Shot angle", "conditions-angle", own.AngleDegrees, null, units, "degrees");
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), RowDefinitions = new RowDefinitions("Auto,Auto"), ColumnSpacing = Tokens.Space12, RowSpacing = Tokens.Space8 };
            foreach (var (box, i) in new[] { Temperature, Where, Humidity, Angle }.Select((b, i) => (b, i)))
            {
                Grid.SetColumn(box.Row, i % 2);
                Grid.SetRow(box.Row, i / 2);
                grid.Children.Add(box.Row);
            }

            column.Children.Add(grid);
            var swap = new Button { Content = pressure ? "Use altitude instead of station pressure" : "Use station pressure instead of altitude", Classes = { AppStyles.Link } };
            AutomationProperties.SetAutomationId(swap, "conditions-swap");
            swap.Click += (_, _) =>
            {
                window.conditionsByPressure = !pressure;
                window.Refresh();
            };
            column.Children.Add(swap);

            var fill = new Button { Content = "Fill from Ballistics", IsEnabled = ballistics is not null, HorizontalAlignment = HorizontalAlignment.Stretch };
            AutomationProperties.SetAutomationId(fill, "conditions-fill");
            ToolTip.SetTip(fill, FillReason ?? "Copy the Ballistics screen's conditions for this rifle and load into this sheet");
            fill.Click += (_, _) => Commit(ballistics is { } b ? new SessionConditions(b.Air.TemperatureF, b.Air.PressureInHg is null ? b.Air.AltitudeFt : null, b.Air.PressureInHg, b.Air.HumidityPct, b.AngleDegrees) : own);
            var standard = new Button { Content = "Standard day", HorizontalAlignment = HorizontalAlignment.Stretch };
            AutomationProperties.SetAutomationId(standard, "conditions-standard");
            ToolTip.SetTip(standard, "Clear the four, so a standard day is used");
            standard.Click += (_, _) => Commit(null);
            var buttons = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = Tokens.Space8 };
            Grid.SetColumn(standard, 1);
            buttons.Children.Add(fill);
            buttons.Children.Add(standard);
            column.Children.Add(buttons);
            if (FillReason is not null)
            {
                column.Children.Add(new TextBlock { Text = FillReason, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Dim } });
            }

            // Section 1.3: the sight height and the zero stay on the rifle, with a link to its page.
            var rifle = state.Rifle is { } r ? window.book.Rifles.FirstOrDefault(x => string.Equals(x.Name, r.Name, StringComparison.OrdinalIgnoreCase)) : null;
            var rifleLine = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space4 };
            rifleLine.Children.Add(new TextBlock
            {
                Text = rifle is null ? "Sight height and zero come from the rifle; this sheet has none chosen." : RifleWords(rifle, units),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Classes = { AppStyles.Secondary },
            });
            if (rifle is not null)
            {
                var change = new Button { Content = "Change them on its page.", Classes = { AppStyles.Link } };
                AutomationProperties.SetAutomationId(change, "conditions-rifle");
                change.Click += (_, _) => window.EditRifle(rifle.Name);
                rifleLine.Children.Add(change);
            }

            column.Children.Add(rifleLine);
            Remember = new CheckBox { Content = "Remember for my next sheet", IsChecked = window.settingsStore.LoadConditionsMemory().Remember };
            AutomationProperties.SetAutomationId(Remember, "conditions-remember");
            Remember.IsCheckedChanged += (_, _) => window.RememberConditions(state.Conditions, Remember.IsChecked == true);
            column.Children.Add(Remember);
            if (Comparison is not null)
            {
                column.Children.Add(new TextBlock { Text = Comparison, TextWrapping = TextWrapping.Wrap });
            }

            View = column;
        }

        internal Control View { get; }

        internal string Summary { get; }

        internal string? Comparison { get; }

        internal string? FillReason { get; }

        internal Box? Temperature { get; }

        internal Box? Where { get; }

        internal Box? Humidity { get; }

        internal Box? Angle { get; }

        internal CheckBox? Remember { get; }

        /// <summary>One labelled field with its unit; a value is committed when the field is left or Enter is pressed.</summary>
        internal sealed record Box(Control Row, TextBox Text, BallisticMeasure? Measure);

        private Box Field(string label, string id, double? imperial, BallisticMeasure? measure, UnitSettings units, string? unit = null)
        {
            string shown = imperial is { } v ? (measure is { } m ? BallisticMeasures.FromImperial(v, m, units) : v).ToString(measure == BallisticMeasure.Pressure && !BallisticMeasures.IsMetric(units) ? "0.00" : "0.#", CultureInfo.InvariantCulture) : "";
            var text = new TextBox { Text = shown, Width = 80, PlaceholderText = "", HorizontalContentAlignment = HorizontalAlignment.Right };
            AutomationProperties.SetName(text, label);
            AutomationProperties.SetAutomationId(text, id);
            text.LostFocus += (_, _) => FromBoxes();
            text.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    FromBoxes();
                }
            };
            var row = new StackPanel { Spacing = Tokens.Space4 };
            row.Children.Add(new TextBlock { Text = label, Classes = { AppStyles.Secondary } });
            row.Children.Add(new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = Tokens.Space4,
                Children = { text, new TextBlock { Text = unit ?? BallisticMeasures.Symbol(measure!.Value, units).TrimStart('°'), VerticalAlignment = VerticalAlignment.Center } },
            });
            return new Box(row, text, measure);
        }

        /// <summary>What the four fields say now, in imperial, each blank one null.</summary>
        internal SessionConditions Read()
        {
            var units = window.units;
            double? Value(Box? box)
            {
                if (box is null || !double.TryParse((box.Text.Text ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                {
                    return null;
                }

                return box.Measure is { } m ? BallisticMeasures.ToImperial(v, m, units) : v;
            }

            bool pressure = Where?.Measure == BallisticMeasure.Pressure;
            double? humidity = Value(Humidity) is { } h ? Math.Clamp(h, 0, 100) : null;
            return new SessionConditions(Value(Temperature), pressure ? null : Value(Where), pressure ? Value(Where) : null, humidity, Value(Angle));
        }

        /// <summary>The fields into the session, where they differ from what it holds.</summary>
        internal void FromBoxes()
        {
            var now = Read();
            if (now != own with { FromLastSheet = false })
            {
                Commit(now);
            }
        }

        private void Commit(SessionConditions? conditions)
        {
            window.session.SetConditions(conditions);
            if (Remember?.IsChecked != false)
            {
                window.RememberConditions(conditions, Remember?.IsChecked ?? window.settingsStore.LoadConditionsMemory().Remember);
            }

            window.Refresh();
        }

        private static string RifleWords(GroupLab.Core.Marking.Rifle rifle, UnitSettings units)
        {
            var inv = CultureInfo.InvariantCulture;
            string height = rifle.SightHeightInches is { } h ? units.Length(h) : "not recorded";
            string zero = rifle.ZeroDistanceYards is { } z ? string.Create(inv, $"{z:0} yd") : "not recorded";
            return $"Sight height {height} and zero {zero} come from the rifle.";
        }
    }
}
