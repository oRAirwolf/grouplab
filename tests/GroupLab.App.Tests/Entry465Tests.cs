using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 465, Alan's choices from the mockups drawn for entry 462 section 3. Item 2A: on Analysis, each heading of
/// "Shots, from their own bull" names the unit its numbers are in, following the unit chosen, and the plot's key sits in a compact box
/// directly under the plot, two columns where it fits, instead of over it.
/// </summary>
public sealed class Entry465Tests
{
    [Theory]
    [InlineData(LinearUnit.Inch, "in")]
    [InlineData(LinearUnit.Centimetre, "cm")]
    [InlineData(LinearUnit.Millimetre, "mm")]
    public void EachLengthHeadingNamesTheUnitChosen(LinearUnit linear, string symbol)
    {
        var units = UnitSettings.Imperial with { Linear = linear };
        Assert.Equal(["shot", $"across, {symbol}", $"up/down, {symbol}", $"radius, {symbol}"], MainWindow.OffsetHeadings(units, bullColumn: false));
        Assert.Equal(["shot", "bull", $"across, {symbol}", $"up/down, {symbol}", $"radius, {symbol}"], MainWindow.OffsetHeadings(units, bullColumn: true));
    }

    [AvaloniaFact]
    public void TheShotsTableHeadingsFollowTheUnitChosen()
    {
        var (window, path, _) = Entry109Tests.Sheet(1280, 720);
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            foreach (var (units, symbol) in new[] { (UnitSettings.Imperial, "in"), (UnitSettings.Metric, "cm"), (UnitSettings.Imperial with { Linear = LinearUnit.Millimetre }, "mm") })
            {
                window.SetUnits(units);
                Dispatcher.UIThread.RunJobs();
                var heads = window.OffsetHeads;
                Assert.Contains($"across, {symbol}", heads);
                Assert.Contains($"up/down, {symbol}", heads);
                Assert.Contains($"radius, {symbol}", heads);
            }
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>The key's box is below the plot's area, inside the control, and in two columns at the sizes the analysis screen is shot at.</summary>
    [AvaloniaTheory]
    [InlineData(1280, 720)]
    [InlineData(1400, 900)]
    public void TheKeySitsInABoxUnderThePlotInTwoColumns(int width, int height)
    {
        var (window, path, _) = Entry109Tests.Sheet(width, height);
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            var plot = window.Plot;
            var (data, key, collapsed) = plot.KeyLayout();
            Assert.False(collapsed, $"{width} by {height}: the key folded into its button");
            Assert.True(key.Top >= data.Bottom, $"{width} by {height}: the key {key} is not under the plot's area {data}");
            Assert.False(key.Intersects(data), $"{width} by {height}: the key {key} overlaps the plot's area {data}");
            Assert.True(new Rect(plot.Bounds.Size).Contains(key), $"{width} by {height}: the key {key} runs out of the control {plot.Bounds}");
            Assert.Equal(key, plot.KeyDrawn);
            Assert.Equal(2, plot.KeyColumns);
            Assert.True(key.Width < plot.Bounds.Width, "the key's box is not compact");
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
