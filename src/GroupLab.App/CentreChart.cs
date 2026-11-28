using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Reporting;
using GroupLab.Core.Statistics;

namespace GroupLab.App;

/// <summary>
/// Suggestion 63 (Discord, Alan's yes on 2026-11-28): where each load's center sits from the point of aim, drawn. The aim is a cross at the
/// middle; each load's center is its marker in its color with its letter, and the range it could really be is a bar across and a bar up and
/// down. Every load is on one scale, <see cref="Extent"/> inches from the middle to the edge. The groups' own pictures stay drawn about
/// their own middles (request 123); this is a chart of its own.
/// </summary>
internal sealed class CentreChart : Control
{
    public CentreChart(LoadComparisonReport report, double extent)
    {
        Report = report;
        Extent = extent;
        AutomationProperties.SetName(this, ComparisonPages.CentresHeading);
    }

    public LoadComparisonReport Report { get; }

    /// <summary>Inches from the aim to the nearest edge.</summary>
    public double Extent { get; }

    /// <summary>The grid's step in inches: a round length about a quarter of the half width.</summary>
    public double Step => Steps.LastOrDefault(s => s <= Extent / 2, Steps[0]);

    private static readonly double[] Steps = [0.01, 0.02, 0.05, 0.1, 0.2, 0.25, 0.5, 1, 2, 5, 10, 20];

    /// <summary>Where a point the given inches right and low of the aim is drawn in a square this size.</summary>
    internal Point At(double right, double low, Size size)
    {
        double half = Math.Min(size.Width, size.Height) / 2, perInch = half / Extent;
        double Clamp(double v) => Math.Max(-half, Math.Min(half, v));
        return new Point((size.Width / 2) + Clamp(right * perInch), (size.Height / 2) + Clamp(low * perInch));
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var palette = Tokens.For(ActualThemeVariant);
        var bounds = new Rect(Bounds.Size);
        context.DrawRectangle(new SolidColorBrush(palette.Panel2), new Pen(new SolidColorBrush(palette.Line), 1), bounds, 6, 6);
        var middle = bounds.Center;
        double half = Math.Min(bounds.Width, bounds.Height) / 2, perInch = half / Extent;
        var faint = new Pen(new SolidColorBrush(palette.Line2), 1);
        for (int i = 1; i * Step * perInch < half; i++)
        {
            double d = i * Step * perInch;
            foreach (double at in new[] { middle.X - d, middle.X + d })
            {
                context.DrawLine(faint, new Point(at, 1), new Point(at, bounds.Height - 1));
            }

            foreach (double at in new[] { middle.Y - d, middle.Y + d })
            {
                context.DrawLine(faint, new Point(1, at), new Point(bounds.Width - 1, at));
            }
        }

        // The aim: a cross in the text color, larger than any marker, so it is never taken for a load.
        var aim = new Pen(new SolidColorBrush(palette.Text), 2);
        context.DrawLine(aim, new Point(middle.X - 9, middle.Y), new Point(middle.X + 9, middle.Y));
        context.DrawLine(aim, new Point(middle.X, middle.Y - 9), new Point(middle.X, middle.Y + 9));

        for (int i = 0; i < Report.Groups.Count; i++)
        {
            var (x, y) = Report.Groups[i].CentreRange;
            var brush = LoadGroups.Brush(i, ActualThemeVariant);
            var pen = new Pen(brush, 2);
            var centre = At(x.Value, y.Value, bounds.Size);
            Point left = At(x.Lower, y.Value, bounds.Size), right = At(x.Upper, y.Value, bounds.Size);
            Point up = At(x.Value, y.Lower, bounds.Size), down = At(x.Value, y.Upper, bounds.Size);
            context.DrawLine(pen, left, right);
            context.DrawLine(pen, up, down);
            context.DrawLine(pen, left + new Vector(0, -5), left + new Vector(0, 5));
            context.DrawLine(pen, right + new Vector(0, -5), right + new Vector(0, 5));
            context.DrawLine(pen, up + new Vector(-5, 0), up + new Vector(5, 0));
            context.DrawLine(pen, down + new Vector(-5, 0), down + new Vector(5, 0));
            GroupDots.Marker(context, i, brush, new Pen(brush, 1.5), centre, 5);
            var letter = new FormattedText(LoadComparison.Letter(i), CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(Tokens.Sans, FontStyle.Normal, FontWeight.SemiBold), TextSize.Of(Tokens.SecondarySize), new SolidColorBrush(palette.Text));
            context.DrawText(letter, centre + new Vector(7, -7 - letter.Height));
        }
    }

    /// <summary>
    /// The whole part: the heading, the chart, what it shows, and the table of each center from the aim and each pair's shift with its
    /// ranges. On a wide screen the table is a grid beside the chart; on a narrow one (<paramref name="compact"/>) each row is a short block
    /// under it, so nothing is cut.
    /// </summary>
    internal static StackPanel Part(LoadComparisonReport report, Func<double, string> size, bool compact, double side = 200)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(size);
        var chart = new CentreChart(report, LoadComparison.CentreReach(report)) { Width = side, Height = side, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var part = new StackPanel { Spacing = Tokens.Space8 };
        part.Children.Add(new TextBlock { Text = ComparisonPages.CentresHeading, FontSize = TextSize.Of(Tokens.HeadingSize), FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
        var key = new WrapPanel();
        for (int i = 0; i < report.Groups.Count; i++)
        {
            key.Children.Add(LoadGroups.Named(i, report.Groups[i].Name, maxWidth: compact ? 280 : 360));
        }

        part.Children.Add(key);
        Control table = compact ? Blocks(report) : Table(report);
        if (compact)
        {
            part.Children.Add(chart);
            part.Children.Add(table);
        }
        else
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
            table.Margin = new Thickness(Tokens.Space16, 0, 0, 0);
            Grid.SetColumn(table, 1);
            row.Children.Add(chart);
            row.Children.Add(table);
            part.Children.Add(row);
        }

        part.Children.Add(new TextBlock { Text = ComparisonPages.CentresSay(size(chart.Step)), TextWrapping = TextWrapping.Wrap, FontSize = TextSize.Of(Tokens.SecondarySize), Classes = { AppStyles.Secondary } });
        return part;
    }

    private static TextBlock Cell(string text, bool quiet = false, bool strong = false)
    {
        var cell = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            FontSize = TextSize.Of(Tokens.SecondarySize),
            FontWeight = strong ? FontWeight.SemiBold : FontWeight.Normal,
        };
        if (quiet)
        {
            cell.Classes.Add(AppStyles.Secondary);
        }

        return cell;
    }

    /// <summary>The rows as a grid: what each is from, the distance, and each direction over its range.</summary>
    private static Grid Table(LoadComparisonReport report)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto"), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        string[] titles = ["", "Distance", "Across, and its range", "Up or down, and its range"];
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (int c = 0; c < titles.Length; c++)
        {
            var title = Cell(titles[c], quiet: true);
            title.Margin = new Thickness(c == 0 ? 0 : Tokens.Space16, 0, 0, Tokens.Space4);
            Grid.SetColumn(title, c);
            grid.Children.Add(title);
        }

        for (int r = 0; r < report.CentreRows.Count; r++)
        {
            var row = report.CentreRows[r];
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Control[] cells =
            [
                Label(report, r, row.Label),
                Cell(row.Distance),
                Stack(row.Across, row.AcrossRange),
                Stack(row.Up, row.UpRange),
            ];
            for (int c = 0; c < cells.Length; c++)
            {
                cells[c].Margin = new Thickness(c == 0 ? 0 : Tokens.Space16, 2, 0, Tokens.Space4);
                Grid.SetRow(cells[c], r + 1);
                Grid.SetColumn(cells[c], c);
                grid.Children.Add(cells[c]);
            }
        }

        return grid;
    }

    /// <summary>The rows as short blocks for a narrow screen: what each is from and the distance, then each direction with its range.</summary>
    private static StackPanel Blocks(LoadComparisonReport report)
    {
        var list = new StackPanel { Spacing = Tokens.Space8 };
        for (int r = 0; r < report.CentreRows.Count; r++)
        {
            var row = report.CentreRows[r];
            var block = new StackPanel { Spacing = Tokens.Space4 };
            var head = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space8, Children = { Label(report, r, row.Label), Cell(row.Distance) } };
            block.Children.Add(head);
            block.Children.Add(Cell($"Across {row.Across}, could be {row.AcrossRange}", quiet: true));
            block.Children.Add(Cell($"Up or down {row.Up}, could be {row.UpRange}", quiet: true));
            list.Children.Add(block);
        }

        return list;
    }

    /// <summary>
    /// A row's label: a load's own row is its marker, which carries its letter, then "from the aim"; a shift between two loads is plain text.
    /// </summary>
    private static Control Label(LoadComparisonReport report, int row, string words) =>
        row < report.Groups.Count
            ? new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space4, Children = { new LoadSwatch(row) { VerticalAlignment = VerticalAlignment.Center }, Centred(Cell(words[LoadComparison.Letter(row).Length..].TrimStart(), strong: true)) } }
            : Cell(words, strong: true);

    private static TextBlock Centred(TextBlock block)
    {
        block.VerticalAlignment = VerticalAlignment.Center;
        return block;
    }

    private static StackPanel Stack(string value, string range) => new() { Spacing = 0, Children = { Cell(value), Cell(range, quiet: true) } };
}
