using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 247: the Ballistics screen laid out as Alan's concept B, like the analysis screen. A bar across the top with
/// the rifle and load, the units and the one amber action; settings on the left as labelled rows in sections that fold and remember it;
/// the work in the middle, the trajectory or the chance of a hit; and on the right the answer at one range, elevation in amber first.
/// </summary>
public partial class MainWindow
{
    /// <summary>Which of the two things the middle shows: the trajectory, or the chance of a hit.</summary>
    internal enum BallisticView
    {
        Trajectory,
        Hit,
    }

    private BallisticView ballisticView = BallisticView.Trajectory;
    private readonly Button ballisticPrimary = new() { Classes = { AppStyles.Primary } };
    private readonly Dictionary<BallisticView, Button> viewButtons = [];
    private readonly StackPanel trajectoryLeft = new() { Spacing = Tokens.Space12 };
    private readonly StackPanel hitLeft = new() { Spacing = Tokens.Space12, IsVisible = false };
    private readonly StackPanel trajectoryMiddle = new() { Spacing = Tokens.Space12 };
    private readonly StackPanel hitMiddle = new() { Spacing = Tokens.Space8, IsVisible = false };
    private readonly TextBox atRange = new() { Width = 80, Text = "300", HorizontalContentAlignment = HorizontalAlignment.Right };
    private readonly StackPanel atRangeLines = new() { Spacing = 0 };
    private readonly TextBlock atRangeUnit = new() { VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Label } };
    private readonly Dictionary<Control, TextBlock> neededWords = [];
    private readonly List<BallisticSection> ballisticSections = [];
    private readonly List<Button> seriesButtons = [];
    private readonly StackPanel sharedLeft = new() { Spacing = Tokens.Space12 };
    private Grid ballisticBody = null!;
    private Border ballisticLeftPane = null!;
    private Border ballisticRightPane = null!;
    private Control ballisticMiddlePane = null!;
    private bool? ballisticNarrow;

    /// <summary>The columns' widths and their minimums, and the width below which the right column moves under the middle, as question 58's rule.</summary>
    internal const double BallisticLeft = 300, BallisticLeftMost = 240, BallisticRight = 330, BallisticRightMost = 260, BallisticMiddleLeast = 420;

    /// <summary>
    /// A section of settings that folds: its title as a button, a one-line summary while it is folded, its rows while it is open, and
    /// whether it was open remembered with the rest of the screen's disclosures.
    /// </summary>
    internal sealed class BallisticSection : StackPanel
    {
        private readonly TextBlock marker = new() { Width = 14, Classes = { AppStyles.Dim } };
        private readonly TextBlock summary = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(18, 0, 0, 0), Classes = { AppStyles.Secondary } };
        private readonly StackPanel rows = new() { Spacing = Tokens.Space4 };
        private readonly Func<string> say;
        private readonly Action<bool> remember;
        private bool chosen;

        public BallisticSection(string title, bool open, Func<string> say, Action<bool> remember, params Control[] children)
        {
            this.say = say;
            this.remember = remember;
            Spacing = Tokens.Space4;
            Title = title;
            var header = new Button
            {
                Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { marker, new TextBlock { Text = title, Classes = { AppStyles.Section } } } },
                Classes = { AppStyles.Link },
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            header.Click += (_, _) => Open(!IsOpen);
            Children.Add(header);
            Children.Add(summary);
            foreach (var child in children)
            {
                rows.Children.Add(child);
            }

            Children.Add(rows);
            chosen = open;
            Open(open, save: false);
        }

        public string Title { get; }

        public bool IsOpen => rows.IsVisible;

        /// <summary>Folded for the other view without forgetting how the shooter left it, or opened again as it was.</summary>
        public void Fold(bool folded) => Open(!folded && chosen, save: false);

        public void Open(bool open, bool save = true)
        {
            if (save)
            {
                chosen = open;
            }

            rows.IsVisible = open;
            marker.Text = open ? "▾" : "▸";
            Refresh();
            if (save)
            {
                remember(open);
            }
        }

        /// <summary>The folded summary again, from the fields as they are now.</summary>
        public void Refresh()
        {
            summary.Text = say();
            summary.IsVisible = !rows.IsVisible && summary.Text.Length > 0;
        }
    }

    private BallisticSection Section(string key, string title, bool openAtFirst, Func<string> summary, params Control[] rows)
    {
        string item = "ballistics." + key;
        bool open = settingsStore.LoadWhyOpen(item) || (openAtFirst && !settingsStore.LoadWhyOpen(item + ".closed"));
        var section = new BallisticSection(title, open, summary, isOpen =>
        {
            settingsStore.SaveWhyOpen(item, isOpen);
            settingsStore.SaveWhyOpen(item + ".closed", !isOpen);
        }, rows);
        ballisticSections.Add(section);
        return section;
    }

    /// <summary>
    /// One setting as concept B draws it: the label dim on the left, the field on the right; a wide field goes under its label instead. The
    /// word "needed" beside it shows when the solver needs this field and it is empty (entry 163 section 4's convention).
    /// </summary>
    private Control SettingRow(TextBlock label, params Control[] fields)
    {
        label.VerticalAlignment = VerticalAlignment.Center;
        label.TextWrapping = TextWrapping.Wrap;
        var needed = new TextBlock { Text = "needed", IsVisible = false, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Alert } };
        foreach (var field in fields)
        {
            neededWords[field] = needed;
        }

        // A wide choice alone takes the column's width under its label rather than its own, so it never runs under the scroll bar.
        if (fields is [ComboBox { MinWidth: > 150 } choice])
        {
            choice.MinWidth = 0;
            choice.HorizontalAlignment = HorizontalAlignment.Stretch;
            return new StackPanel { Spacing = Tokens.Space4, Children = { label, choice } };
        }

        var right = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space4, HorizontalAlignment = HorizontalAlignment.Right, Children = { needed } };
        foreach (var field in fields)
        {
            right.Children.Add(field);
        }

        bool wide = fields.Length > 1 || fields.Any(f => f is ComboBox { MinWidth: > 150 });
        if (wide)
        {
            right.HorizontalAlignment = HorizontalAlignment.Left;
            return new StackPanel { Spacing = Tokens.Space4, Children = { label, right } };
        }

        Grid.SetColumn(right, 1);
        return new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = Tokens.Space8, MinHeight = 30, Children = { label, right } };
    }

    /// <summary>Marks the fields the solver needs and opens their sections, or clears the marks when nothing is missing.</summary>
    private void MarkNeeded(IReadOnlyList<string> missing)
    {
        var fields = new (string Words, Control Field)[]
        {
            ("sight height", sightHeight), ("zero distance", zeroDistance), ("muzzle velocity", muzzleVelocity), ("the load's BC", ballisticCoefficient),
            ("drag model", dragModel), ("reference atmosphere", bcReference), ("weight", bulletWeight),
        };
        foreach (var (words, field) in fields)
        {
            bool isNeeded = missing.Any(m => m.Contains(words, StringComparison.Ordinal));
            field.Classes.Set(AppStyles.Needed, isNeeded);
            if (neededWords.TryGetValue(field, out var word))
            {
                word.IsVisible = isNeeded;
            }

            if (isNeeded && ballisticSections.FirstOrDefault(s => s.GetLogicalDescendants().Contains(field)) is { IsOpen: false } section)
            {
                section.Open(true, save: false);
            }
        }
    }

    private string Symbol(BallisticMeasure measure) => BallisticMeasures.Symbol(measure, units);

    private static string Or(TextBox box, string fallback = "not set") => string.IsNullOrWhiteSpace(box.Text) ? fallback : box.Text.Trim();

    private string RifleSummary() =>
        $"{Or(sightHeight)} {Symbol(BallisticMeasure.SmallLength)} sight · zero {Or(zeroDistance)} {UnitSettings.Symbol(units.Distance)}"
        + (string.IsNullOrWhiteSpace(twist.Text) ? "" : $" · 1 in {twist.Text.Trim()}");

    private string LoadSummary() =>
        $"{Or(bulletWeight, "?")} gr · {Or(muzzleVelocity, "?")} {Symbol(BallisticMeasure.Speed)}"
        + (string.IsNullOrWhiteSpace(muzzleVelocitySd.Text) ? "" : $", SD {muzzleVelocitySd.Text.Trim()}")
        + $" · {(dragModel.SelectedIndex > 0 ? dragModel.SelectedItem : "no drag model")} {Or(ballisticCoefficient, "")}".TrimEnd();

    private string AirSummary() =>
        $"{Or(airTemperature, "?")} {Symbol(BallisticMeasure.Temperature)} · {Or(airAltitude, "0")} {Symbol(BallisticMeasure.Altitude)} · {Or(airHumidity, "?")} %"
        + (string.IsNullOrWhiteSpace(airPressure.Text) ? " · pressure from altitude" : $" · {airPressure.Text.Trim()} {Symbol(BallisticMeasure.Pressure)}");

    private string TableSummary() => $"to {Or(dopeTo)} every {Or(dopeStep)} {UnitSettings.Symbol(units.Distance)}";

    /// <summary>The top bar: the screen's name, the rifle and load, the units, and the one amber action, which follows the view.</summary>
    private Control BallisticBar(Control units)
    {
        var spacer = new Border();
        var bar = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,*,Auto,Auto"), ColumnSpacing = Tokens.Space8, Classes = { AppStyles.Bar } };
        var title = new TextBlock { Text = "Ballistics", VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Title } };
        ballisticRifle.MinWidth = ballisticLoad.MinWidth = 160;
        Control[] cells = [title, ballisticRifle, ballisticLoad, spacer, units, ballisticPrimary];
        for (int i = 0; i < cells.Length; i++)
        {
            Grid.SetColumn(cells[i], i);
            cells[i].VerticalAlignment = VerticalAlignment.Center;
            bar.Children.Add(cells[i]);
        }

        ballisticPrimary.Click += (_, _) =>
        {
            if (ballisticView == BallisticView.Trajectory)
            {
                FillDope();
            }
            else
            {
                FillHit();
            }
        };
        return new Border { Child = bar, Padding = new Thickness(Tokens.Space16, Tokens.Space8), Classes = { AppStyles.Bar } };
    }

    /// <summary>The Trajectory and Hit probability switch at the top of the middle.</summary>
    private Control ViewSwitch()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0 };
        foreach (var (view, words) in new[] { (BallisticView.Trajectory, "Trajectory"), (BallisticView.Hit, "Hit probability") })
        {
            var button = new Button { Content = words, Name = "BallisticView" + view };
            button.Click += (_, _) => ShowBallisticView(view);
            viewButtons[view] = button;
            row.Children.Add(button);
        }

        return row;
    }

    /// <summary>Shows the trajectory or the chance of a hit: the middle, the settings that belong to it, and what the amber action does.</summary>
    internal void ShowBallisticView(BallisticView view)
    {
        ballisticView = view;
        bool trajectoryShown = view == BallisticView.Trajectory;
        trajectoryLeft.IsVisible = trajectoryMiddle.IsVisible = trajectoryShown;
        hitLeft.IsVisible = hitMiddle.IsVisible = !trajectoryShown;

        // The addition's item 1: in the hit view the rifle, the load and the air fold to their one-line summaries, and back in the trajectory
        // view they open as the shooter last left them.
        foreach (var section in sharedLeft.Children.OfType<BallisticSection>())
        {
            section.Fold(!trajectoryShown);
        }

        ballisticPrimary.Content = trajectoryShown ? "Work out the table" : "Work out the chance";
        foreach (var (each, button) in viewButtons)
        {
            button.Classes.Set(AppStyles.Chosen, each == view);
        }

        settingsStore.SaveWhyOpen("ballistics.view.hit", !trajectoryShown);
    }

    internal BallisticView BallisticViewShown => ballisticView;

    /// <summary>The primary action's fill and the chosen unit's, as styled, for the headless tests: only the first is solid amber.</summary>
    internal (Avalonia.Media.IBrush? Primary, Avalonia.Media.IBrush? Chosen) BallisticButtonFills =>
        (ballisticPrimary.Background, unitButtons.Values.FirstOrDefault(b => b.Classes.Contains(AppStyles.Chosen))?.Background);

    /// <summary>The sections open in the view shown, by title, for the headless tests.</summary>
    internal IEnumerable<string> BallisticSectionsOpen => ballisticSections.Where(s => s.IsOpen && s.Parent is Control { IsVisible: true }).Select(s => s.Title);

    /// <summary>The right column: the answer at one range, then the analyzed group carried there.</summary>
    private Control AtOneRange()
    {
        var head = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = Tokens.Space4 };
        var title = new TextBlock { Text = "At one range", VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Section } };
        Grid.SetColumn(atRange, 1);
        Grid.SetColumn(atRangeUnit, 2);
        head.Children.Add(title);
        head.Children.Add(atRange);
        head.Children.Add(atRangeUnit);
        atRange.TextChanged += (_, _) => ChooseRange(null);

        // The addition's item 4: the target's distance and this range are one, so the hold shown and the chance are for the same shot.
        hitDistance.TextChanged += (_, _) =>
        {
            if (!string.Equals(hitDistance.Text, atRange.Text, StringComparison.Ordinal))
            {
                atRange.Text = hitDistance.Text;
            }
        };
        var column = new StackPanel { Spacing = Tokens.Space8 };
        column.Children.Add(head);
        column.Children.Add(atRangeLines);
        column.Children.Add(new TextBlock { Text = "The analyzed group there", Margin = new Thickness(0, Tokens.Space12, 0, 0), Classes = { AppStyles.Section } });
        column.Children.Add(Line("A prediction from the group open in the analysis, the rifle and load it names, and the air on the left: its sigma carried through the solver, with the load's velocity SD and the crosswind's uncertainty added where they are given. It is never a measurement."));
        column.Children.Add(SettingRow(Measured("Crosswind uncertainty", BallisticMeasure.WindSpeed), windSd));
        column.Children.Add(Row(Button("Work it out", FillProjection)));
        column.Children.Add(projectionLines);
        return column;
    }

    /// <summary>
    /// A range chosen by the field, a table row or a point on the curve: the field says it, the right column works it out, the curve and the
    /// table mark it, and the analyzed group's distance and the hit's target distance follow it, so every figure on the screen is for one shot.
    /// </summary>
    internal void ChooseRange(double? yards)
    {
        if (yards is { } chosen)
        {
            fillingBallistics = true;
            atRange.Text = UnitSettings.DistanceFromInches(chosen * 36, units.Distance).ToString("0", CultureInfo.InvariantCulture);
            fillingBallistics = false;
        }

        projectTo.Text = atRange.Text;
        hitDistance.Text = atRange.Text;
        FillAtRange();
    }

    /// <summary>The chosen range worked out: elevation first, in amber, then the clicks and drop, wind, velocity, energy, time and stability.</summary>
    internal void FillAtRange()
    {
        atRangeLines.Children.Clear();
        atRangeUnit.Text = UnitSettings.Symbol(units.Distance);
        var rifle = ChosenRifle;
        var load = ChosenLoad;
        var missing = SolverUse.Missing(rifle, load);
        double? range = Number(atRange);
        if (missing.Count > 0 || range is not > 0)
        {
            atRangeLines.Children.Add(Line(missing.Count > 0 ? "The solver needs " + Joined(missing) + "." : "Enter a range."));
            trajectory.ChosenYards = null;
            trajectory.InvalidateVisual();
            return;
        }

        double yards = UnitSettings.DistanceToInches(range.Value, units.Distance) / 36;
        if (yards > 3000)
        {
            atRangeLines.Children.Add(Line("That is past 3000 yd."));
            return;
        }

        var input = SolverUse.Input(rifle, load, Air())!;
        var solved = SolverUse.Dope(input, yards, yards);
        var point = solved.Points.LastOrDefault(p => p.RangeYards > 0);
        if (point is null)
        {
            atRangeLines.Children.Add(Line("The solver gave nothing at that range."));
            return;
        }

        double inches = yards * 36;
        var aim = units.Aiming(rifle); // entry 294 section 1: the rifle's scope unit
        string Angle(double value) => aim.Angle(Math.Abs(value), inches) is { } a ? $"{a.ToString("0.00", CultureInfo.InvariantCulture)} {UnitSettings.Symbol(aim.Angular)}" : units.Length(Math.Abs(value));
        string ClickText(double value, string direction) => Math.Abs(value) < 5e-4 ? "no clicks" : Clicks.For(value, inches, rifle!, direction).Describe();

        // The one figure the screen is for, in amber at the lead size, as the analysis screen's mean radius is.
        var lead = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, Tokens.Space8, 0, 2) };
        var leadLabel = new TextBlock { Text = "Elevation", VerticalAlignment = VerticalAlignment.Bottom, Classes = { AppStyles.Label } };
        var leadValue = new TextBlock { Text = Angle(point.DropInches) + (point.DropInches <= -5e-4 ? " up" : point.DropInches >= 5e-4 ? " down" : ""), Classes = { AppStyles.HeadlineFigure } };
        leadValue.FontSize = Tokens.LeadValueSize;
        leadValue.FontFamily = Mono;
        UnitTap.Attach(leadValue, "Elevation");
        Grid.SetColumn(leadValue, 1);
        lead.Children.Add(leadLabel);
        lead.Children.Add(leadValue);
        atRangeLines.Children.Add(lead);
        atRangeLines.Children.Add(new TextBlock
        {
            Text = $"{ClickText(point.DropInches, point.DropInches < 0 ? "up" : "down")} · drop {units.Length(point.DropInches)}",
            HorizontalAlignment = HorizontalAlignment.Right,
            FontFamily = Mono,
            Classes = { AppStyles.Secondary },
        });
        atRangeLines.Children.Add(FigureRow($"Wind, {SolverUse.DopeWindMph:0} mph full value", $"{Angle(point.WindInches)} · {units.Length(Math.Abs(point.WindInches))}"));
        atRangeLines.Children.Add(FigureRow("Velocity", Speed(point.VelocityFps) + " " + Symbol(BallisticMeasure.Speed)));
        atRangeLines.Children.Add(FigureRow("Energy", Energy(point.EnergyFtLb)));
        atRangeLines.Children.Add(FigureRow("Time of flight", point.TimeOfFlight.ToString("0.000", CultureInfo.InvariantCulture) + " s"));
        atRangeLines.Children.Add(FigureRow("Stability", solved.Stability is { } sg ? sg.ToString("0.00", CultureInfo.InvariantCulture) + ", Miller's rule" : "needs the twist, and the bullet's length and diameter"));

        trajectory.ChosenYards = point.RangeYards;
        trajectory.InvalidateVisual();
        MarkChosenRow(point.RangeYards);
    }

    /// <summary>A velocity to the foot or meter a second, which is all a chronograph resolves.</summary>
    private string Speed(double fps) => double.TryParse(BallisticMeasures.Text(fps, BallisticMeasure.Speed, units), NumberStyles.Float, CultureInfo.InvariantCulture, out double v)
        ? v.ToString("0", CultureInfo.InvariantCulture)
        : BallisticMeasures.Text(fps, BallisticMeasure.Speed, units);

    private string Energy(double footPounds) => BallisticMeasures.IsMetric(units)
        ? (footPounds * 1.3558179483).ToString("0", CultureInfo.InvariantCulture) + " J"
        : footPounds.ToString("0", CultureInfo.InvariantCulture) + " ft lb";

    /// <summary>A figure row as the analysis screen's right column draws one: a dim label, the value in mono on the right, a hairline under.</summary>
    private static Control FigureRow(string label, string value)
    {
        // A sentence, such as what stability needs, reads under its name in words rather than as a figure squeezed to the right.
        if (value.Length > 24 && value.Count(char.IsLetter) > value.Length / 2)
        {
            return new Border
            {
                Child = new StackPanel
                {
                    Spacing = Tokens.Space4,
                    Children =
                    {
                        new TextBlock { Text = label, Classes = { AppStyles.Label } },
                        new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Label } },
                    },
                },
                Padding = new Thickness(0, Tokens.Space8),
                Classes = { AppStyles.Ruled },
            };
        }

        var name = new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Label } };
        var figure = UnitTap.Attach(new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Right, FontFamily = Mono, VerticalAlignment = VerticalAlignment.Center }, label);
        Grid.SetColumn(figure, 1);
        return new Border
        {
            Child = new Grid { ColumnDefinitions = new ColumnDefinitions("4*,5*"), ColumnSpacing = Tokens.Space8, Children = { name, figure } },
            Padding = new Thickness(0, Tokens.Space8),
            Classes = { AppStyles.Ruled },
        };
    }

    /// <summary>The table's row for the chosen range, highlighted; every other row as it was.</summary>
    private void MarkChosenRow(double yards)
    {
        foreach (var row in dopeTable.Children.OfType<Border>())
        {
            row.Classes.Set(AppStyles.Warn, row.Tag is double range && Math.Abs(range - yards) < 1e-6);
        }
    }

    /// <summary>
    /// Question 58's rule on this screen too: the side columns shrink toward their minimums below the width they are drawn at, and below the
    /// least all three need, the right column moves under the middle.
    /// </summary>
    private void ArrangeBallistics(double width)
    {
        bool narrow = width < BallisticLeftMost + BallisticMiddleLeast + BallisticRightMost;
        if (ballisticNarrow == narrow)
        {
            return;
        }

        ballisticNarrow = narrow;
        ballisticBody.ColumnDefinitions = narrow
            ? new ColumnDefinitions($"{BallisticLeft}*,{BallisticMiddleLeast * 2}*")
            : new ColumnDefinitions($"{BallisticLeft}*,{BallisticMiddleLeast * 2}*,{BallisticRight}*");
        ballisticBody.ColumnDefinitions[0].MinWidth = BallisticLeftMost;
        ballisticBody.ColumnDefinitions[0].MaxWidth = BallisticLeft;
        ballisticBody.ColumnDefinitions[1].MinWidth = narrow ? 0 : BallisticMiddleLeast;
        if (!narrow)
        {
            ballisticBody.ColumnDefinitions[2].MinWidth = BallisticRightMost;
            ballisticBody.ColumnDefinitions[2].MaxWidth = BallisticRight;
        }

        if (ballisticRightPane.Parent is Panel from)
        {
            from.Children.Remove(ballisticRightPane);
        }

        if (narrow)
        {
            ((StackPanel)((ScrollViewer)ballisticMiddlePane).Content!).Children.Add(ballisticRightPane);
        }
        else
        {
            Grid.SetColumn(ballisticRightPane, 2);
            ballisticBody.Children.Add(ballisticRightPane);
        }
    }

    /// <summary>Whether the right column sits under the middle now, for the headless tests.</summary>
    internal bool BallisticRightUnderMiddle => ballisticNarrow == true;

    /// <summary>The three columns' widths as laid out, for the headless tests.</summary>
    internal (double Left, double Middle, double Right) BallisticColumnWidths =>
        (ballisticLeftPane.Bounds.Width, ballisticMiddlePane.Bounds.Width, ballisticRightPane.Bounds.Width);

    /// <summary>How many of this screen's fields are marked needed now, for the headless tests.</summary>
    internal int BallisticFieldsNeeded => neededWords.Values.Distinct().Count(w => w.IsVisible);

    /// <summary>The text of the right column's answer at one range, for the headless tests.</summary>
    internal IEnumerable<string> AtRangeText => atRangeLines.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");
}
