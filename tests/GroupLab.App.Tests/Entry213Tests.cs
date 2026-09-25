using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 213 section 2: the composite plot's key never covers the data. Beside the plot where the width allows,
/// below it where the height does, and otherwise a small Key button in a strip of its own; the plot is drawn and clipped in what is left.
/// </summary>
public class Entry213Tests
{
    private static void Clear(CompositePlot plot, string size)
    {
        var (data, key, _) = plot.KeyLayout();
        var bounds = new Rect(plot.Bounds.Size);
        Assert.False(data.Intersects(key), $"{size}: the key {key} overlaps the plot's area {data}");
        Assert.True(bounds.Contains(key), $"{size}: the key {key} is outside the control {bounds}");
        Assert.All(plot.Shots, s => Assert.True(data.Contains(plot.ToScreen(s.Offset)), $"{size}: shot {s.Label} is outside the plot's area"));
        if (plot.Centre is { } centre)
        {
            Assert.True(data.Contains(plot.ToScreen(centre)), $"{size}: the group center is outside the plot's area");
        }
    }

    /// <summary>On the analysis screen, at a large window and the narrowest the analysis screen fits (question 58).</summary>
    [AvaloniaFact]
    public void TheKeyNeverCoversTheDataOnTheAnalysisScreen()
    {
        foreach (var (width, height) in new[] { (1400, 900), (1060, 700) })
        {
            var (window, path, _) = Entry109Tests.Sheet(width, height);
            try
            {
                window.Session.SetCalibre(Calibre.Of(0.264));
                window.CalibreAnswered();
                window.Analyse();
                Dispatcher.UIThread.RunJobs();
                Clear(window.Plot, $"{width} by {height}");
            }
            finally
            {
                window.Close();
                GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
            }
        }
    }

    /// <summary>Too small for the key beside or below: a Key button in its own strip, which opens the key only when pressed.</summary>
    [AvaloniaFact]
    public void ASmallPlotCollapsesTheKeyToAButton()
    {
        var plot = new CompositePlot
        {
            Shots = [new PlotShot(1, "1", null, new PointD(0.1, 0.1), false), new PlotShot(2, "2", null, new PointD(-0.1, 0.2), false)],
            Centre = new PointD(0, 0.15),
        };
        var window = new Window { Width = 320, Height = 300, Content = plot };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try
        {
            var (data, key, collapsed) = plot.KeyLayout();
            Assert.True(collapsed, $"a 320 by 300 plot kept its whole key, at {key}");
            Assert.True(key.Height <= 28 && key.Width <= 60, $"the collapsed key is {key}, not a small button");
            Clear(plot, "320 by 300");
            Assert.Equal(0, data.X);
        }
        finally
        {
            window.Close();
        }
    }
}
