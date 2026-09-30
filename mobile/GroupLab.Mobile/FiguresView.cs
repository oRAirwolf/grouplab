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

namespace GroupLab.Mobile;

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
        this.units = units.Aiming(state.Rifle); // entry 294 section 1: angles in the rifle's scope unit where one is named
        this.plot = plot;
        this.shotsToZero = shotsToZero;
        Content = column;
    }

    /// <summary>The explanation sheet, for the page to lay over everything at its bottom edge.</summary>
    public Control Sheet => sheet;

    public void Show(MarkingState now, UnitSettings withUnits)
    {
        state = now;
        withUnits = withUnits.Aiming(now.Rifle); // entry 294 section 1
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
        // Entry 294 section 1: the scope's own unit comes first after inches, so a mil shooter reads mil before MOA.
        var angles = units.Angular == AngularUnit.Moa ? new[] { AngularUnit.Moa, AngularUnit.Mrad } : new[] { AngularUnit.Mrad, AngularUnit.Moa };
        foreach (var (words, unit) in new (string, AngularUnit?)[] { ("in", null), (UnitSettings.Symbol(angles[0]), angles[0]), (UnitSettings.Symbol(angles[1]), angles[1]) })
        {
            var chip = Chip(words, angle == unit, () =>
            {
                angle = unit;
                // Entry 273: MOA and mil here are the one angle setting that a tap on any number switches too.
                // Entry 294: a session with a rifle keeps its scope unit, so a chip here only changes what this page shows.
                if (unit is { } chosen && chosen != units.Angular && state.Rifle is null)
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
        column.Children.Add(Screens.Card(Screens.Detach(plot), chips));

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
    /// photograph cannot do this. On a photograph corrected by a printer check (entry 271), the card names the check instead (entry 291
    /// section 5.2). Null where nothing measured the scale (<see cref="SheetReference.PrintScale"/>).
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
            // Entry 291 section 5.2: on a photograph the figure is the printer check's, not this picture's, and the card says which check.
            string? from = (state.Scale as SheetReference)?.ScaleFrom;
            card.Children.Add(Screens.Line(from is null
                ? string.Create(CultureInfo.CurrentCulture, $"This sheet was printed at {k * 100:0.0} percent of its intended size. The scan measured that, so every size here is corrected to real inches.")
                : string.Create(CultureInfo.CurrentCulture, $"{from}. That check measured this printer's sheets at {k * 100:0.0} percent of their intended size, so every size here is corrected by it.")));
            if (Definition?.Fiducials?.Markers is { Count: >= 2 } markers)
            {
                var (a, b) = markers.SelectMany(m => markers.Select(n => (m, n))).MaxBy(p => ((p.m.X - p.n.X) * (p.m.X - p.n.X)) + ((p.m.Y - p.n.Y) * (p.m.Y - p.n.Y)));
                double drawn = Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y))) / 254;
                card.Children.Add(Screens.Dim($"The farthest two markers are {units.Length(drawn)} apart as drawn, and {units.Length(drawn * k)} as printed."));
            }

            card.Children.Add(new TextBlock
            {
                Text = string.Create(CultureInfo.CurrentCulture, $"Every size times {k:0.000}"),
                TextWrapping = TextWrapping.Wrap,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(232, 150, 46)),
            });
            card.Children.Add(Screens.Dim(from is null
                ? "A phone photograph cannot measure this, because it has no absolute ruler: its figures stay in the sheet's own inches. A scan states its resolution, which is one."
                : "A photograph has no absolute ruler, so this figure comes from the printer check, not from this picture. It holds for sheets printed before the printer was calibrated, serviced or set differently; after that, check the printer again."));
            holder.Children.Add(Screens.Card(card));
        }

        return holder;
    }

    /// <summary>A CEP for any percent from 1 to 99.9, drawn on the plot and given here.</summary>
    private Control OwnPercent()
    {
        var box = Screens.Numeric(new TextBox { PlaceholderText = "e.g. 97.5", MinHeight = Screens.Touch, Width = 110, Text = plot.Shown.CustomPercent?.ToString("0.#", CultureInfo.CurrentCulture) ?? "" });
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
        var value = UnitTap.Attach(new TextBlock { Text = figure.Value, TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Right, TextAlignment = TextAlignment.Right }, figure.Label);
        var right = new StackPanel { Spacing = 2, Children = { value } };
        if ((figure.Range ?? figure.Beneath) is { } under)
        {
            right.Children.Add(UnitTap.Attach(Screens.Dim(under), figure.Label));
        }

        var label = Label(figure.Label, figure.Key, figure);
        return new FigureRow { Children = { label, right } };
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
            int shots = GroupAnalysis.Analyse(state).Counted?.Shots ?? 0;
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
        Header = new TextBlock { Text = heading, TextWrapping = TextWrapping.Wrap, Classes = { PhoneStyles.Heading } },
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

/// <summary>
/// A figure's name with its value and note, NOTES-FROM-PLANNING.md entry 295 section 2: beside each other where both fit on one line, and
/// otherwise the value and its note on the line under the name, across the full width. A grid gave the value its whole width first, so a
/// long note ("too small to dial yet; about 44 shots would settle it") squeezed "Zero, elevation" to a letter a line. The name is never
/// given less than the whole width when it does not fit beside the value, so it wraps only between words.
/// </summary>
internal sealed class FigureRow : Panel
{
    private const double Gap = 12;

    /// <summary>Whether the value went under the name at the last layout, for the headless tests.</summary>
    public bool Stacked { get; private set; }

    private Control? LabelPart => Children.Count > 0 ? Children[0] : null;

    private Control? ValuePart => Children.Count > 1 ? Children[1] : null;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (LabelPart is not { } label || ValuePart is not { } value)
        {
            return default;
        }

        double width = availableSize.Width;
        label.Measure(Size.Infinity);
        value.Measure(Size.Infinity);
        double labelWidth = label.DesiredSize.Width, valueWidth = value.DesiredSize.Width;
        Stacked = double.IsFinite(width) && Math.Ceiling(labelWidth) + Gap + Math.Ceiling(valueWidth) > width;
        Align(value, Stacked ? HorizontalAlignment.Left : HorizontalAlignment.Right);
        if (Stacked)
        {
            label.Measure(new Size(width, double.PositiveInfinity));
            value.Measure(new Size(width, double.PositiveInfinity));
            return new Size(width, label.DesiredSize.Height + 2 + value.DesiredSize.Height);
        }

        // Both fit on one line: the name keeps its own width and the value has all the rest, so rounding a width down never wraps either.
        labelSpace = Math.Ceiling(labelWidth);
        double rest = double.IsFinite(width) ? Math.Max(valueWidth, width - labelSpace - Gap) : valueWidth;
        label.Measure(new Size(labelSpace, double.PositiveInfinity));
        value.Measure(new Size(rest, double.PositiveInfinity));
        return new Size(double.IsFinite(width) ? width : labelSpace + Gap + rest, Math.Max(label.DesiredSize.Height, value.DesiredSize.Height));
    }

    private double labelSpace;

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (LabelPart is not { } label || ValuePart is not { } value)
        {
            return finalSize;
        }

        if (Stacked)
        {
            label.Arrange(new Rect(0, 0, finalSize.Width, label.DesiredSize.Height));
            value.Arrange(new Rect(0, label.DesiredSize.Height + 2, finalSize.Width, value.DesiredSize.Height));
            return finalSize;
        }

        double left = Math.Min(labelSpace, finalSize.Width);
        label.Arrange(new Rect(0, 0, left, label.DesiredSize.Height));
        double from = Math.Min(finalSize.Width, left + Gap);
        value.Arrange(new Rect(from, 0, finalSize.Width - from, value.DesiredSize.Height));
        return finalSize;
    }

    /// <summary>The value's lines to the right beside the name, or to the left under it, where they read on from the name.</summary>
    private static void Align(Control value, HorizontalAlignment side)
    {
        var alignment = side == HorizontalAlignment.Left ? TextAlignment.Left : TextAlignment.Right;
        foreach (var text in (value is Panel panel ? panel.Children : [value]).OfType<TextBlock>())
        {
            if (text.HorizontalAlignment != side)
            {
                text.HorizontalAlignment = side;
            }

            if (text.TextAlignment != alignment)
            {
                text.TextAlignment = alignment;
            }
        }
    }
}
