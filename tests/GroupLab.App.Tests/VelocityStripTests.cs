using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 5.2.5: the velocities drawn, with the mean and SD marked, and the extreme spread.
/// <para>
/// <b>The caption is what is held here.</b> Both figures a chronograph prints mislead on their own: an SD of 10 ft/s from ten shots is not
/// a rifle that holds 10 ft/s, and the extreme spread of ten shots is expected to be larger than that of five from the same rifle. So the
/// words never give either one bare.
/// </para>
/// </summary>
public class VelocityStripTests
{
    private static readonly double[] TenShots = [2705, 2711, 2698, 2732, 2700, 2718, 2709, 2703, 2715, 2707];

    [Fact]
    public void AnSdNeverAppearsWithoutTheRangeThatManyShotsPinsItTo()
    {
        var strip = new VelocityStrip { VelocitiesFps = TenShots };

        Assert.Contains("10 readings: mean 2710 ft/s, SD ", strip.Description, StringComparison.Ordinal);
        Assert.Matches(@"SD \d+\.\d ft/s \(\d+\.\d ft/s to \d+\.\d ft/s\)", strip.Description);
    }

    [Fact]
    public void AnExtremeSpreadNeverAppearsWithoutWhatItDependsOn()
    {
        var strip = new VelocityStrip { VelocitiesFps = TenShots };

        Assert.Contains("Extreme spread 34.0 ft/s", strip.Description, StringComparison.Ordinal);
        Assert.Contains("grows with the number of shots on its own", strip.Description, StringComparison.Ordinal);
        Assert.Contains("compared with another string of 10", strip.Description, StringComparison.Ordinal);
    }

    /// <summary>One reading is not a spread, and it says that rather than drawing an SD of nothing.</summary>
    [Fact]
    public void OneReadingIsNotASpread()
    {
        var strip = new VelocityStrip { VelocitiesFps = [2705] };

        Assert.Equal("One reading, 2705 ft/s. Two or more are needed for a spread.", strip.Description);
        Assert.Null(strip.Spread);
    }

    [Fact]
    public void WithNoReadingsItSaysSo()
    {
        Assert.Equal("No velocities recorded for these shots.", new VelocityStrip().Description);
    }

    /// <summary>Metric readers get metres a second throughout, including the SD and the extreme spread.</summary>
    [Fact]
    public void TheFiguresFollowTheUnitsInForce()
    {
        var metric = GroupLab.Core.Marking.UnitSettings.Metric;
        var strip = new VelocityStrip { VelocitiesFps = TenShots, Speed = metric.Speed, SpeedDifference = metric.SpeedDifference };

        Assert.Contains("mean 826 m/s", strip.Description, StringComparison.Ordinal);
        Assert.Contains("Extreme spread 10.4 m/s", strip.Description, StringComparison.Ordinal);
    }

    /// <summary>Drawing it must not throw, including where every shot read the same and the spread is nothing.</summary>
    [AvaloniaTheory]
    [InlineData(2705, 2705)]
    [InlineData(2680, 2740)]
    public void ItDrawsWithoutFailing(double first, double last)
    {
        var strip = new VelocityStrip { VelocitiesFps = [first, (first + last) / 2, last] };
        var window = new Avalonia.Controls.Window { Width = 900, Height = 300, Content = strip };
        window.Show();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();

        Assert.NotEqual(0, strip.Bounds.Width);
    }
}
