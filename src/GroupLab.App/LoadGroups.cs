using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;
using GroupLab.Core.Statistics;

namespace GroupLab.App;

/// <summary>One load's group for the Compare card: its name, its shots as offsets in inches (+y low), its center and its mean radius.</summary>
internal sealed record LoadGroup(string Name, IReadOnlyList<PointD> Offsets, PointD Centre, double MeanRadius);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 309 section 2, Alan's Compare A with a tap that stacks: the card "Each load's group". Side by side, one small
/// plot a load at the same scale, each load in its own color (<see cref="Tokens.Loads"/>), the shots as dots, the mean radius as a dashed
/// ring, each group centered on its own middle, a shared scale bar, and the shots and mean radius beneath. A tap on any group stacks them:
/// one larger plot with every group on the same center, the first load filled dots, the second rings, and so on, with a button a load to hide
/// or show it; a tap on the stacked plot puts them side by side again. The choice holds while the page is open. The desktop and the phone
/// draw the same card; the Android and iOS applications compile this file as it is.
/// <para>
/// Entry 465 item 7B: with two loads the pictures keep their size and the rest of the card holds "What the difference means", beside them
/// where it fits and under them where it does not: each load's main figures side by side and the comparison's own sentences on whether these
/// shot counts can tell the two apart and how many shots would. With three or more loads the card is as it was.
/// </para>
/// </summary>
internal sealed class LoadGroups : UserControl
{
    private readonly IReadOnlyList<LoadGroup> groups;
    private readonly Func<double, string> size;
    private readonly double least;
    private readonly double touch;
    private readonly bool fill;
    private double cell;
    private readonly HashSet<int> hidden = [];
    private readonly LoadComparisonReport? report;

    /// <summary>Whether "What the difference means" sat beside the pictures when the card was last built.</summary>
    private bool besideLast;

    /// <param name="groups">The loads, in the order chosen.</param>
    /// <param name="size">A length in inches as the person reads it: an angle where there is a distance, or a length.</param>
    /// <param name="cell">The side of one small plot; the stacked plot is twice it.</param>
    /// <param name="touch">The least height of a button, a finger's on the phone.</param>
    /// <param name="fill">Entry 312 section 1: on a wide screen the plots grow to share the card's width; the cell is then the least.</param>
    /// <param name="report">Entry 465 item 7B: the comparison, whose figures and sentences fill the card's free space when it has two loads.</param>
    public LoadGroups(IReadOnlyList<LoadGroup> groups, Func<double, string> size, double cell = 150, double touch = 32, bool fill = false, LoadComparisonReport? report = null)
    {
        this.groups = groups;
        this.report = report is { Groups.Count: 2 } && groups.Count == 2 ? report : null;
        this.size = size;
        this.cell = cell;
        least = cell;
        this.touch = touch;
        this.fill = fill;
        ActualThemeVariantChanged += (_, _) => Build();
        SizeChanged += (_, e) =>
        {
            if ((this.fill && Math.Abs(Fit(this.groups.Count, e.NewSize.Width, least) - this.cell) >= 1)
                || (this.report is not null && Beside(e.NewSize.Width) != besideLast))
            {
                Build();
            }
        };
        Build();
    }

    /// <summary>The narrowest card the plots grow in; narrower, a phone held upright, they stay at their least side.</summary>
    internal const double Wide = 600;

    /// <summary>The side a plot grows to at most, so two loads on a wide tablet stay on the screen at once.</summary>
    internal const double Largest = 480;

    /// <summary>
    /// Entry 312 section 1: on an iPad, an Android tablet or the open Fold, two plots of 150 points sat in the left half of the card. On a
    /// card at least <see cref="Wide"/> across they share its width: two or three loads in a row, four in a row where each can be 240
    /// across or else two by two, and more as many as fit; still square and at one scale.
    /// </summary>
    internal static double Fit(int loads, double width, double least)
    {
        if (!double.IsFinite(width) || width < Wide || loads < 1)
        {
            return least;
        }

        double gap = Tokens.Space12;
        int columns = loads switch
        {
            <= 3 => loads,
            4 => (width / 4) - gap >= 240 ? 4 : 2,
            _ => Math.Max(1, Math.Min(loads, (int)(width / (least + gap)))),
        };
        return Math.Clamp(Math.Floor((width / columns) - gap - 1), least, Largest);
    }

    /// <summary>Whether the groups are stacked on one center now.</summary>
    public bool Stacked { get; private set; }

    /// <summary>The loads hidden on the stacked plot, by their place in the order.</summary>
    public IReadOnlyCollection<int> Hidden => hidden;

    /// <summary>The plots showing now, for the tests: one a load side by side, or one stacked.</summary>
    internal IReadOnlyList<GroupDots> Plots { get; private set; } = [];

    /// <summary>A tap on any plot: side by side becomes stacked and back.</summary>
    public void Toggle()
    {
        Stacked = !Stacked;
        Build();
    }

    /// <summary>Hides or shows one load on the stacked plot.</summary>
    public void SetHidden(int index, bool hide)
    {
        if (hide)
        {
            hidden.Add(index);
        }
        else
        {
            hidden.Remove(index);
        }

        Build();
    }

    /// <summary>The load's color in the theme showing, the one the range chart and the verdict use for it too.</summary>
    public static IBrush Brush(int index, Avalonia.Styling.ThemeVariant? theme)
    {
        var colours = Tokens.Loads(theme);
        return new SolidColorBrush(colours[index % colours.Count]);
    }

    /// <summary>Entry 309 section 2.3: each load's name in its color, heading the verdict, so the verdict reads in the plots' colors.</summary>
    public static WrapPanel Key(IReadOnlyList<string> names)
    {
        var key = new WrapPanel();
        for (int i = 0; i < names.Count; i++)
        {
            key.Children.Add(Named(i, names[i], FontWeight.SemiBold));
        }

        return key;
    }

    /// <summary>
    /// A load's name after its marker in its color. The words stay in the text color, which every theme holds to its contrast, and the color
    /// is carried by the marker beside them.
    /// </summary>
    public static StackPanel Named(int index, string name, FontWeight weight = FontWeight.Normal, double maxWidth = double.PositiveInfinity) => new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = Tokens.Space4,
        Margin = new Thickness(0, 0, Tokens.Space12, 0),
        Children =
        {
            new LoadSwatch(index) { VerticalAlignment = VerticalAlignment.Center },
            new TextBlock { Text = name, FontWeight = weight, TextWrapping = TextWrapping.Wrap, MaxWidth = maxWidth, VerticalAlignment = VerticalAlignment.Center },
        },
    };

    /// <summary>The inches from its own center that every group fits inside, so every plot is at one scale.</summary>
    internal double Extent => Math.Max(0.05, groups.Max(g => Math.Max(g.MeanRadius, g.Offsets.Count == 0 ? 0 : g.Offsets.Max(o => Distance(o, g.Centre))))) * 1.15;

    private static double Distance(PointD a, PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    private void Build()
    {
        if (fill)
        {
            cell = Fit(groups.Count, Bounds.Width, least);
        }

        var column = new StackPanel { Spacing = Tokens.Space8 };
        column.Children.Add(new TextBlock { Text = "Each load's group", FontSize = TextSize.Of(Tokens.HeadingSize), FontWeight = FontWeight.SemiBold });
        double extent = Extent;
        if (!Stacked)
        {
            var row = new WrapPanel();
            var plots = new List<GroupDots>();
            for (int i = 0; i < groups.Count; i++)
            {
                var plot = new GroupDots(extent) { Width = cell, Height = cell };
                plot.Show.Add((i, groups[i]));
                plot.Tapped += (_, _) => Toggle();
                plots.Add(plot);
                var name = Named(i, groups[i].Name, FontWeight.SemiBold, cell - 16);
                var figures = new TextBlock
                {
                    Text = string.Create(CultureInfo.CurrentCulture, $"{groups[i].Offsets.Count} shots, mean radius {size(groups[i].MeanRadius)}"),
                    FontSize = TextSize.Of(Tokens.SecondarySize),
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = cell,
                    Classes = { AppStyles.Secondary },
                };
                row.Children.Add(new StackPanel { Spacing = Tokens.Space4, Margin = new Thickness(0, 0, Tokens.Space12, Tokens.Space8), Children = { plot, name, figures } });
            }

            Plots = plots;
            column.Children.Add(WithMeaning(row));
            column.Children.Add(Bar(extent, cell));
            column.Children.Add(new TextBlock { Text = "Each group about its own center, at one scale. Tap a group to stack them on one center.", TextWrapping = TextWrapping.Wrap, FontSize = TextSize.Of(Tokens.SecondarySize), Classes = { AppStyles.Secondary } });
        }
        else
        {
            double side = fill && Bounds.Width >= Wide ? Math.Max(least * 2, Math.Min(cell * 2, Math.Min(Bounds.Width, Largest * 1.5))) : cell * 2;
            var plot = new GroupDots(extent) { Width = side, Height = side, HorizontalAlignment = HorizontalAlignment.Left };
            for (int i = 0; i < groups.Count; i++)
            {
                if (!hidden.Contains(i))
                {
                    plot.Show.Add((i, groups[i]));
                }
            }

            plot.Tapped += (_, _) => Toggle();
            Plots = [plot];
            column.Children.Add(WithMeaning(plot));
            column.Children.Add(Bar(extent, side));
            var buttons = new WrapPanel();
            for (int i = 0; i < groups.Count; i++)
            {
                int at = i;
                bool shown = !hidden.Contains(i);
                var button = new ToggleButton { Content = Named(i, groups[i].Name), IsChecked = shown, MinHeight = touch, Margin = new Thickness(0, 0, Tokens.Space8, Tokens.Space8) };
                ToolTip.SetTip(button, shown ? "Hide this load" : "Show this load");
                button.IsCheckedChanged += (_, _) => SetHidden(at, button.IsChecked != true);
                buttons.Children.Add(button);
            }

            column.Children.Add(buttons);
            column.Children.Add(new TextBlock { Text = "Every group on one center. Tap the plot to put them side by side again.", TextWrapping = TextWrapping.Wrap, FontSize = TextSize.Of(Tokens.SecondarySize), Classes = { AppStyles.Secondary } });
        }

        Content = column;
    }

    /// <summary>The least width "What the difference means" is given beside the pictures; narrower, it goes under them.</summary>
    internal const double MeaningLeast = 300;

    /// <summary>Whether a card this wide leaves room for "What the difference means" beside the pictures.</summary>
    private bool Beside(double width) => report is not null && double.IsFinite(width) && width - PicturesWidth(width) >= MeaningLeast;

    /// <summary>The width the pictures take in a row at a card's width, side by side or stacked, as <see cref="Build"/> lays them out.</summary>
    private double PicturesWidth(double width)
    {
        double side = fill ? Fit(groups.Count, width, least) : cell;
        return Stacked
            ? (fill && width >= Wide ? Math.Max(least * 2, Math.Min(side * 2, Math.Min(width, Largest * 1.5))) : side * 2) + Tokens.Space12
            : groups.Count * (side + Tokens.Space12);
    }

    /// <summary>Entry 465 item 7B: "What the difference means" as last built, for the tests; null with three or more loads.</summary>
    internal StackPanel? Meaning { get; private set; }

    /// <summary>Whether it sits beside the pictures, for the tests.</summary>
    internal bool MeaningBeside => besideLast;

    /// <summary>The pictures with "What the difference means" beside them where it fits and under them where not; the pictures alone otherwise.</summary>
    private Control WithMeaning(Control pictures)
    {
        if (report is null)
        {
            Meaning = null;
            besideLast = false;
            return pictures;
        }

        Meaning = Means(report, size);
        besideLast = Beside(Bounds.Width);
        if (!besideLast)
        {
            Meaning.Margin = new Thickness(0, Tokens.Space4, 0, Tokens.Space8);
            return new StackPanel { Spacing = Tokens.Space8, Children = { pictures, Meaning } };
        }

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        pictures.VerticalAlignment = VerticalAlignment.Top;
        Meaning.Margin = new Thickness(Tokens.Space12, 0, 0, Tokens.Space8);
        Grid.SetColumn(Meaning, 1);
        grid.Children.Add(pictures);
        grid.Children.Add(Meaning);
        return grid;
    }

    /// <summary>The heading of the card's part that says what the difference between two loads means.</summary>
    internal const string MeaningHeading = "What the difference means";

    /// <summary>
    /// Entry 465 item 7B, "What the difference means": the two loads' main figures side by side, the shots and then the figures the phone's
    /// chart offers (mean radius, extreme spread and CEP 90), then the comparison's own headline, its sentence on how far apart the spreads
    /// could be, and how many shots each would take to tell a 10 percent difference apart. Nothing here is worked out afresh.
    /// </summary>
    internal static StackPanel Means(LoadComparisonReport report, Func<double, string> size)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(size);
        var panel = new StackPanel { Spacing = Tokens.Space8 };
        panel.Children.Add(new TextBlock { Text = MeaningHeading, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
        var table = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto"), HorizontalAlignment = HorizontalAlignment.Left };
        var rows = new List<(string Label, Func<ComparedGroup, string> Value)>
        {
            ("Shots", g => g.Shots.ToString(CultureInfo.CurrentCulture)),
            ("Mean radius", g => size(g.MeanRadius.Value)),
            ("Extreme spread", g => size(g.ExtremeSpread)),
            ("CEP 90", g => size(g.Rayleigh.Cep(0.9).Value)),
        };
        for (int r = 0; r <= rows.Count; r++)
        {
            table.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        for (int i = 0; i < report.Groups.Count; i++)
        {
            var name = Named(i, report.Groups[i].Name, FontWeight.SemiBold, 200);
            name.Margin = new Thickness(Tokens.Space16, 0, 0, Tokens.Space4);
            name.HorizontalAlignment = HorizontalAlignment.Right;
            Grid.SetColumn(name, i + 1);
            table.Children.Add(name);
        }

        for (int r = 0; r < rows.Count; r++)
        {
            var label = new TextBlock { Text = rows[r].Label, FontSize = TextSize.Of(Tokens.SecondarySize), Margin = new Thickness(0, 2), VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Secondary } };
            Grid.SetRow(label, r + 1);
            table.Children.Add(label);
            for (int i = 0; i < report.Groups.Count; i++)
            {
                var value = new TextBlock
                {
                    Text = rows[r].Value(report.Groups[i]),
                    FontSize = TextSize.Of(Tokens.SecondarySize),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    TextAlignment = TextAlignment.Right,
                    Margin = new Thickness(Tokens.Space16, 2, 0, 2),
                };
                Grid.SetRow(value, r + 1);
                Grid.SetColumn(value, i + 1);
                table.Children.Add(value);
            }
        }

        panel.Children.Add(table);
        panel.Children.Add(new TextBlock { Text = report.Headline, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Warn } });
        foreach (string? line in new[] { report.Apart, report.ShotsNeeded })
        {
            if (line is { Length: > 0 })
            {
                panel.Children.Add(new TextBlock { Text = line, TextWrapping = TextWrapping.Wrap, FontSize = TextSize.Of(Tokens.SecondarySize), Classes = { AppStyles.Secondary } });
            }
        }

        return panel;
    }

    /// <summary>The scale bar under the plots: a round length about a third of a plot's half width, drawn to the plots' scale.</summary>
    private Control Bar(double extent, double side)
    {
        double perInch = side / 2 / extent;
        double[] steps = [0.01, 0.02, 0.05, 0.1, 0.2, 0.25, 0.5, 1, 2, 5, 10, 20];
        double inches = steps.LastOrDefault(s => s * perInch <= side / 3, steps[0]);
        var line = new Border { Width = inches * perInch, Height = 3, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Divider } };
        var words = new TextBlock { Text = size(inches), FontSize = TextSize.Of(Tokens.SecondarySize), VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Secondary } };
        return new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space8, Children = { line, words } };
    }
}

/// <summary>
/// One plot of the Compare card: each group shown about its own center, <see cref="Extent"/> inches from the middle to the edge, its shots
/// in the load's color and its mean radius a dashed ring. Where several are shown they share the center, each with its own marker.
/// </summary>
internal sealed class GroupDots : Control
{
    public GroupDots(double extent)
    {
        Extent = extent;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    /// <summary>Inches from the middle to the nearest edge.</summary>
    public double Extent { get; }

    /// <summary>The groups drawn, each with its place in the order, which picks its color and marker.</summary>
    public List<(int Index, LoadGroup Group)> Show { get; } = [];

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var palette = Tokens.For(ActualThemeVariant);
        var bounds = new Rect(Bounds.Size);
        context.DrawRectangle(new SolidColorBrush(palette.Panel2), new Pen(new SolidColorBrush(palette.Line), 1), bounds, 6, 6);
        var middle = bounds.Center;
        double perInch = Math.Min(bounds.Width, bounds.Height) / 2 / Extent;
        var faint = new Pen(new SolidColorBrush(palette.Line2), 1);
        context.DrawLine(faint, new Point(middle.X - 6, middle.Y), new Point(middle.X + 6, middle.Y));
        context.DrawLine(faint, new Point(middle.X, middle.Y - 6), new Point(middle.X, middle.Y + 6));
        foreach (var (index, group) in Show)
        {
            var brush = LoadGroups.Brush(index, ActualThemeVariant);
            var pen = new Pen(brush, 1.5);
            context.DrawEllipse(null, new Pen(brush, 1.5, Marks.Dashed), middle, group.MeanRadius * perInch, group.MeanRadius * perInch);
            foreach (var o in group.Offsets)
            {
                var at = new Point(middle.X + ((o.X - group.Centre.X) * perInch), middle.Y + ((o.Y - group.Centre.Y) * perInch));
                Marker(context, index, brush, pen, at);
            }
        }
    }

    /// <summary>A load's marker: filled dots, rings, squares, triangles, then crosses.</summary>
    internal static void Marker(DrawingContext context, int index, IBrush brush, Pen pen, Point at, double r = 3.5)
    {
        switch (index % 5)
        {
            case 0:
                context.DrawEllipse(brush, null, at, r, r);
                break;
            case 1:
                context.DrawEllipse(null, pen, at, r, r);
                break;
            case 2:
                context.DrawRectangle(null, pen, new Rect(at.X - r, at.Y - r, 2 * r, 2 * r));
                break;
            case 3:
                context.DrawLine(pen, new Point(at.X, at.Y - r), new Point(at.X + r, at.Y + r));
                context.DrawLine(pen, new Point(at.X + r, at.Y + r), new Point(at.X - r, at.Y + r));
                context.DrawLine(pen, new Point(at.X - r, at.Y + r), new Point(at.X, at.Y - r));
                break;
            default:
                context.DrawLine(pen, new Point(at.X - r, at.Y - r), new Point(at.X + r, at.Y + r));
                context.DrawLine(pen, new Point(at.X - r, at.Y + r), new Point(at.X + r, at.Y - r));
                break;
        }
    }
}

/// <summary>A load's marker in its color, beside its name wherever the load is named: on the card, the buttons and the verdict.</summary>
internal sealed class LoadSwatch : Control
{
    public LoadSwatch(int index)
    {
        Index = index;
        Width = 12;
        Height = 12;
    }

    public int Index { get; }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var brush = LoadGroups.Brush(Index, ActualThemeVariant);
        GroupDots.Marker(context, Index, brush, new Pen(brush, 1.5), new Rect(Bounds.Size).Center, 4.5);
    }
}
