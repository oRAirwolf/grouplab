using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 298: every split between panes can be dragged, with a grip that can be seen and a least width for each
/// side, and each size is remembered between runs, per screen. A side column that holds a form or a list keeps its width in pixels (the
/// pattern the Targets list set, since a form needs the same room on any monitor); two panes that are peers keep their share, so a wider or
/// narrower window keeps the proportion. Settings' Reset layout puts every one back at once.
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>The splitter's width: wide enough to take the pointer at 150 percent on a 4K monitor, narrow enough to read as a rule.</summary>
    internal const double GripWidth = 6;

    /// <summary>What each split does to go back to its default, for Reset layout.</summary>
    private readonly List<Action> layoutResets = [];

    /// <summary>A splitter between two columns, with its tooltip and the sideways cursor.</summary>
    private static GridSplitter Grip()
    {
        var splitter = new GridSplitter
        {
            Width = GripWidth,
            ResizeDirection = GridResizeDirection.Columns,
            Cursor = new Cursor(StandardCursorType.SizeWestEast),
        };
        ToolTip.SetTip(splitter, "Drag to widen or narrow this column. Settings, Reset layout puts every column back.");
        return splitter;
    }

    /// <summary>
    /// Two peer panes side by side with a grip between, the first taking <paramref name="share"/> of the width until a person drags it, and
    /// the share they leave it at remembered under <paramref name="name"/>.
    /// </summary>
    private Grid PeerPanes(string name, Control first, Control second, double share, double firstLeast, double secondLeast)
    {
        double start = Math.Clamp(settingsStore.LoadPaneShare(name) ?? share, 0.05, 0.95);
        var grid = new Grid();
        void Shares(double s)
        {
            grid.ColumnDefinitions = new ColumnDefinitions
            {
                new ColumnDefinition(s, GridUnitType.Star) { MinWidth = firstLeast },
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(1 - s, GridUnitType.Star) { MinWidth = secondLeast },
            };
        }

        Shares(start);
        var grip = Grip();
        grip.DragCompleted += (_, _) =>
        {
            double a = grid.ColumnDefinitions[0].ActualWidth, b = grid.ColumnDefinitions[2].ActualWidth;
            if (a + b > 0)
            {
                settingsStore.SavePaneShare(name, a / (a + b));
            }
        };
        Grid.SetColumn(first, 0);
        Grid.SetColumn(grip, 1);
        Grid.SetColumn(second, 2);
        grid.Children.Add(first);
        grid.Children.Add(grip);
        grid.Children.Add(second);
        layoutResets.Add(() => Shares(share));
        return grid;
    }

    /// <summary>
    /// A side column's width in pixels, remembered under <paramref name="name"/> once dragged: its column definition, and the grip that goes
    /// beside it, which saves the width it is left at.
    /// </summary>
    private (ColumnDefinition Column, GridSplitter Grip) SideWidth(string name, double width, double least, double most)
    {
        var column = new ColumnDefinition(Math.Clamp(settingsStore.LoadColumnWidth(name) ?? width, least, most), GridUnitType.Pixel) { MinWidth = least, MaxWidth = most };
        var grip = Grip();
        grip.DragCompleted += (_, _) => settingsStore.SaveColumnWidth(name, column.ActualWidth);
        layoutResets.Add(() => column.Width = new GridLength(width));
        return (column, grip);
    }

    /// <summary>Entry 298 section 3: every pane back to its default size at once, and the dragged sizes forgotten.</summary>
    internal void ResetLayout()
    {
        settingsStore.ResetLayout();
        foreach (var reset in layoutResets)
        {
            reset();
        }

        DiagnosticLog.Info("layout.reset", ("splits", layoutResets.Count));
    }

    /// <summary>How many splits Reset layout reaches, for the headless tests.</summary>
    internal int LayoutSplits => layoutResets.Count;
}
