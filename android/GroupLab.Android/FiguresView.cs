using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Reporting;
using GroupLab.Core.Statistics;

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 259 screen 1, "full figures, A" (Alan's choice): look B's four tiles at the top with a units switch (in, MOA,
/// mil) at the top right; the plot card with chips under it (CEP 50, 90, 95, 99 and Sheet) turning the circles and the sheet on and off;
/// then the sections as cards that open: "All figures" open, "Advanced" with a CEP of your own percent, "Bull by bull", "Shots Needed to
/// Zero", and "Full CEP table and the fitted ellipse". Every figure's name is dotted-underlined, and a tap opens the explanation sheet: the
/// name, the value, the plain explanation, and an amber box headed "What N shots can say". The numbers are <see cref="ResultFigures"/>',
/// the desktop's own shared computation.
/// </summary>
internal sealed class FiguresView : UserControl
{
    private readonly StackPanel column = new() { Spacing = 12 };
    private readonly Border sheet = new() { IsVisible = false, VerticalAlignment = VerticalAlignment.Bottom, Classes = { PhoneStyles.Card } };
    private readonly CompositePlot plot;
    private readonly Action? shotsToZero;
    private MarkingState state;
    private UnitSettings units;
    private AngularUnit? angle;

    public FiguresView(MarkingState state, UnitSettings units, CompositePlot plot, Action? shotsToZero)
    {
        this.state = state;
        this.units = units;
        this.plot = plot;
        this.shotsToZero = shotsToZero;
        Content = column;
    }

    /// <summary>The explanation sheet, for the page to lay over everything at its bottom edge.</summary>
    public Control Sheet => sheet;

    public void Show(MarkingState now, UnitSettings withUnits)
    {
        state = now;
        units = withUnits;
        // Entry 273: an angle chosen here follows the setting a tap on a number changes.
        if (angle is not null)
        {
            angle = withUnits.Angular;
        }

        Build();
    }

    private void Build()
    {
        var (tiles, sections) = ResultFigures.Build(state, units, angle);
        column.Children.Clear();
        if (tiles.Count == 0)
        {
            column.Children.Add(Screens.Dim(ResultView.Figures(null, units, state.ShotDistanceInches)));
            return;
        }

        // The units switch, top right: inches, or an angle where the distance is known.
        var switcher = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var (words, unit) in new (string, AngularUnit?)[] { ("in", null), ("MOA", AngularUnit.Moa), ("mil", AngularUnit.Mrad) })
        {
            var chip = Chip(words, angle == unit, () =>
            {
                angle = unit;
                // Entry 273: MOA and mil here are the one angle setting that a tap on any number switches too.
                if (unit is { } chosen && chosen != units.Angular)
                {
                    UnitTap.Apply?.Invoke(units with { Angular = chosen }, GroupLab.Core.Marking.UnitKind.Angle);
                    return;
                }

                Build();
            });
            chip.IsEnabled = unit is null || state.ShotDistanceInches is not null;
            switcher.Children.Add(chip);
        }

        column.Children.Add(switcher);
        if (Printed() is { } pill)
        {
            column.Children.Add(pill);
        }
        column.Children.Add(Screens.Tiles(tiles.Select(t => (t.Label, t.Value, t.Beneath ?? "", t.Headline))));
        if (state.ShotDistanceInches is null)
        {
            column.Children.Add(Screens.Dim("Enter the distance on Capture to see the angles."));
        }

        // The plot and its chips: each circle drawn as on the desktop, and the sheet's own bulls behind the shots.
        var chips = new WrapPanel();
        var marks = plot.Shown;
        void Toggle(string words, bool on, Func<PlotMarks, bool, PlotMarks> set) =>
            chips.Children.Add(Chip(words, on, () =>
            {
                plot.Shown = set(plot.Shown, !on);
                plot.InvalidateVisual();
                Build();
            }));
        Toggle("CEP 50", marks.Cep50, (m, v) => m with { Cep50 = v });
        Toggle("CEP 90", marks.Cep90, (m, v) => m with { Cep90 = v });
        Toggle("CEP 95", marks.Cep95, (m, v) => m with { Cep95 = v });
        Toggle("CEP 99", marks.Cep99, (m, v) => m with { Cep99 = v });
        chips.Children.Add(Chip("Sheet", plot.WholeTarget, () =>
        {
            plot.WholeTarget = !plot.WholeTarget;
            plot.InvalidateVisual();
            Build();
        }));
        column.Children.Add(Screens.Card(Detach(plot), chips));

        foreach (var section in sections)
        {
            var rows = new StackPanel { Spacing = 10 };
            foreach (var figure in section.Figures)
            {
                rows.Children.Add(Row(figure));
            }

            if (section.Key == "advanced")
            {
                rows.Children.Add(OwnPercent());
            }

            if (section.Note is { } note)
            {
                rows.Children.Add(Screens.Dim(note));
            }

            column.Children.Add(Section(section.Heading, section.Open, rows));
            if (section.Key == "bulls" && shotsToZero is not null)
            {
                column.Children.Add(Screens.Row("Shots Needed to Zero", "How many shots a zeroing group needs, from this group. Suggested by Jylee.", shotsToZero));
            }
        }
    }

    /// <summary>The sheet's definition, for the scan pill's markers; the result sets it.</summary>
    public GroupLab.Core.Gltd.Model.TargetDefinition? Definition { get; set; }

    private bool scaleOpen;

    /// <summary>
    /// Entry 259 screen 7, approved: on a result from a scan, a teal pill saying how the sheet was printed and that every size is corrected,
    /// which opens a card with the plain sentence, the markers' distance as drawn and as printed, the correction in amber, and why a
    /// photograph cannot do this. Null on a photograph or where the scan's scale was not believed (<see cref="SheetReference.PrintScale"/>).
    /// </summary>
    private Control? Printed()
    {
        if (state.Scale is not SheetReference { PrintScale: { } k })
        {
            return null;
        }

        double off = Math.Abs(1 - k) * 100;
        string words = Math.Abs(k - 1) <= DetectionAdvice.ScaleWorthSaying
            ? "Printed at its true size, checked"
            : string.Create(CultureInfo.CurrentCulture, $"Printed {off:0.0}% {(k < 1 ? "small" : "large")}, every size corrected");
        var teal = Color.FromRgb(42, 157, 143);
        var pill = new Button
        {
            Content = words,
            MinHeight = Screens.Touch,
            HorizontalAlignment = HorizontalAlignment.Left,
            Foreground = new SolidColorBrush(teal),
            BorderBrush = new SolidColorBrush(teal),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20),
        };
        pill.Click += (_, _) =>
        {
            scaleOpen = !scaleOpen;
            Build();
        };
        var holder = new StackPanel { Spacing = 8, Children = { pill } };
        if (scaleOpen)
        {
            var card = new StackPanel { Spacing = 6 };
            card.Children.Add(Screens.Line(string.Create(CultureInfo.CurrentCulture,
                $"This sheet was printed at {k * 100:0.0} percent of its intended size. The scan measured that, so every size here is corrected to real inches.")));
            if (Definition?.Fiducials?.Markers is { Count: >= 2 } markers)
            {
                var (a, b) = markers.SelectMany(m => markers.Select(n => (m, n))).MaxBy(p => ((p.m.X - p.n.X) * (p.m.X - p.n.X)) + ((p.m.Y - p.n.Y) * (p.m.Y - p.n.Y)));
                double drawn = Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y))) / 254;
                card.Children.Add(Screens.Dim($"The farthest two markers are {units.Length(drawn)} apart as drawn, and {units.Length(drawn * k)} as printed."));
            }

            card.Children.Add(new TextBlock
            {
                Text = string.Create(CultureInfo.CurrentCulture, $"Every size times {k:0.000}"),
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(232, 150, 46)),
            });
            card.Children.Add(Screens.Dim("A phone photograph cannot measure this, because it has no absolute ruler: its figures stay in the sheet's own inches. A scan states its resolution, which is one."));
            holder.Children.Add(Screens.Card(card));
        }

        return holder;
    }

    /// <summary>The plot moved into the new card: a control has one parent, and the page is rebuilt on every change.</summary>
    private static Control Detach(Control control)
    {
        switch (control.Parent)
        {
            case Panel panel:
                panel.Children.Remove(control);
                break;
            case ContentControl holder:
                holder.Content = null;
                break;
            case Decorator decorator:
                decorator.Child = null;
                break;
        }

        return control;
    }

    /// <summary>A CEP for any percent from 1 to 99.9, drawn on the plot and given here.</summary>
    private Control OwnPercent()
    {
        var box = new TextBox { PlaceholderText = "e.g. 97.5", MinHeight = Screens.Touch, Width = 110, Text = plot.Shown.CustomPercent?.ToString("0.#", CultureInfo.CurrentCulture) ?? "" };
        var said = Screens.Dim(plot.Shown.CustomPercent is null ? "A circle for any percent you type, on the plot." : "It is on the plot.");
        box.LostFocus += (_, _) => Apply();
        box.KeyUp += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Apply();
            }
        };
        void Apply()
        {
            double? percent = double.TryParse(box.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double p) && p >= PlotMarks.LeastPercent && p <= PlotMarks.MostPercent ? p : null;
            if (percent is null && !string.IsNullOrWhiteSpace(box.Text))
            {
                said.Text = "Type a percent from 1 to 99.9.";
                return;
            }

            plot.Shown = plot.Shown with { CustomPercent = percent };
            plot.InvalidateVisual();
            said.Text = percent is null ? "A circle for any percent you type, on the plot." : "It is on the plot.";
        }

        return new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { Label("CEP of your own percent", "cep"), box } },
                said,
            },
        };
    }

    private Control Row(ResultFigure figure)
    {
        var value = UnitTap.Attach(new TextBlock { Text = figure.Value, TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Right, TextAlignment = TextAlignment.Right });
        var right = new StackPanel { Spacing = 2, Children = { value } };
        if ((figure.Range ?? figure.Beneath) is { } under)
        {
            right.Children.Add(UnitTap.Attach(Screens.Dim(under)));
        }

        var label = Label(figure.Label, figure.Key, figure);
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12, Children = { label, right } };
        Grid.SetColumn(right, 1);
        return grid;
    }

    /// <summary>A figure's name, dotted-underlined, that opens the explanation sheet when tapped.</summary>
    private TextBlock Label(string words, string key, ResultFigure? figure = null)
    {
        var label = new TextBlock
        {
            Text = words,
            TextWrapping = TextWrapping.Wrap,
            TextDecorations = [new TextDecoration { Location = TextDecorationLocation.Underline, StrokeDashArray = [1, 2], StrokeThickness = 1 }],
        };
        label.Tapped += (_, _) => Explain(figure ?? new ResultFigure(key, words, "", Explanation: ResultFigures.Explain(key)));
        return label;
    }

    /// <summary>The explanation sheet: the name, the value, the plain explanation, what the shots can say, and Close.</summary>
    private void Explain(ResultFigure figure)
    {
        var inside = new StackPanel { Spacing = 10, Children = { Screens.Heading(figure.Label) } };
        if (figure.Value.Length > 0)
        {
            inside.Children.Add(Screens.Line(figure.Value + (figure.Range is { } r ? ", " + r : "")));
        }

        inside.Children.Add(Screens.Line(figure.Explanation ?? ResultFigures.Explain(figure.Key) ?? "No explanation is written for this one yet."));
        if (figure.ShotsCanSay is { } can)
        {
            int shots = GroupAnalysis.Analyse(state).AllShots?.Shots ?? 0;
            inside.Children.Add(new Border
            {
                Padding = new Thickness(12),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(40, 232, 150, 46)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(232, 150, 46)),
                BorderThickness = new Thickness(1),
                Child = new StackPanel { Spacing = 4, Children = { Screens.Heading($"What {shots} shots can say"), Screens.Line(can) } },
            });
        }

        inside.Children.Add(Screens.Choice("Close", () => sheet.IsVisible = false));
        sheet.Child = inside;
        sheet.IsVisible = true;
    }

    private static Control Section(string heading, bool open, Control body) => new Expander
    {
        Header = new TextBlock { Text = heading, Classes = { PhoneStyles.Heading } },
        Content = body,
        IsExpanded = open,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Classes = { PhoneStyles.Card },
    };

    private static Button Chip(string words, bool on, Action chosen)
    {
        var chip = new Button { Content = words, MinHeight = Screens.Touch, MinWidth = Screens.Touch, Margin = new Thickness(0, 0, 6, 6) };
        if (on)
        {
            chip.Classes.Add(GroupLab.App.Theme.AppStyles.Chosen);
        }

        chip.Click += (_, _) => chosen();
        return chip;
    }
}
