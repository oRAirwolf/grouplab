using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 298: every split between panes can be dragged and is remembered between runs, and Settings' Reset layout
/// puts every one back. Checked at Alan's 3840 by 2160, and at 2560 by 1440, which is what that monitor gives at 150 percent.
/// </summary>
public class Entry298Tests
{
    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private static (MainWindow Window, AppSettingsStore Store) Open(double width, double height, Action<AppSettingsStore>? before = null)
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        before?.Invoke(store);
        var window = new MainWindow(store) { Width = width, Height = height };
        window.Show();
        Settle();
        return (window, store);
    }

    [Fact]
    public void AShareAndAWidthAreKeptAndResetLayoutForgetsBoth()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveColumnWidth("targets.sheet", 512.4);
        store.SavePaneShare("equipment", 0.3456789);
        Assert.Equal(512, store.LoadColumnWidth("targets.sheet"));
        Assert.Equal(0.3457, store.LoadPaneShare("equipment"));
        store.ResetLayout();
        Assert.Null(store.LoadColumnWidth("targets.sheet"));
        Assert.Null(store.LoadPaneShare("equipment"));
    }

    [AvaloniaFact]
    public void EverySplitHasAGripAndResetLayoutReachesEachOne()
    {
        var (window, _) = Open(1920, 1080);
        try
        {
            // The marking screen's right column, the analysis's two, the Targets list, the sheet beside its preview, Equipment, and
            // Ballistics' two.
            var grips = window.GetLogicalDescendants().OfType<GridSplitter>().ToList();
            Assert.True(grips.Count >= 8, $"{grips.Count} grips");
            Assert.True(window.LayoutSplits >= 6, $"{window.LayoutSplits} splits reset");
            Assert.All(grips, g => Assert.True(g.Width >= MainWindow.GripWidth));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(3840, 2160)]
    [InlineData(2560, 1440)]
    public void OnAFourKMonitorBallisticsKeepsADraggedWidthUntilResetLayout(int width, int height)
    {
        var (window, store) = Open(width, height, s => s.SaveColumnWidth("ballistics.left", 520));
        try
        {
            window.ShowBallistics();
            Settle();
            Assert.False(window.BallisticRightUnderMiddle);
            var (left, middle, right) = window.BallisticColumnWidths;
            Assert.InRange(left, 519, 521);
            Assert.InRange(right, MainWindow.BallisticRightMost - 1, MainWindow.BallisticRight + 1);
            Assert.True(middle >= MainWindow.BallisticMiddleLeast);

            window.ResetLayout();
            Settle();
            (left, middle, _) = window.BallisticColumnWidths;
            Assert.InRange(left, MainWindow.BallisticLeftMost - 1, MainWindow.BallisticLeft + 1);
            Assert.True(middle >= MainWindow.BallisticMiddleLeast);
            Assert.Null(store.LoadColumnWidth("ballistics.left"));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ADraggedWidthWiderThanTheWindowAllowsGivesWayRatherThanSqueezingTheMiddle()
    {
        var (window, _) = Open(1280, 720, s =>
        {
            s.SaveColumnWidth("ballistics.left", 700);
            s.SaveColumnWidth("ballistics.right", 700);
        });
        try
        {
            window.ShowBallistics();
            Settle();
            var (left, middle, right) = window.BallisticColumnWidths;
            Assert.True(middle >= MainWindow.BallisticMiddleLeast - 1, $"the middle is {middle:0} wide");
            Assert.True(left >= MainWindow.BallisticLeftMost - 1 && right >= MainWindow.BallisticRightMost - 1);
        }
        finally
        {
            window.Close();
        }
    }
}
