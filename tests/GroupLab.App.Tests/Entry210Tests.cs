using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 210: the composite plot's second pass. The rings about five times entry 204's width, in page units, and a
/// solid mid grey told apart from the thin half strength outlines; a Group and a Whole target framing, remembered; and free zoom.
/// </summary>
public class Entry210Tests
{
    private static double Luminance(Color c)
    {
        static double Linear(byte v) => v / 255.0 <= 0.04045 ? v / 255.0 / 12.92 : Math.Pow(((v / 255.0) + 0.055) / 1.055, 2.4);
        return (0.2126 * Linear(c.R)) + (0.7152 * Linear(c.G)) + (0.0722 * Linear(c.B));
    }

    private static double Contrast(Color a, Color b)
    {
        double x = Luminance(a), y = Luminance(b);
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
    }

    /// <summary>What a shot's outline looks like on the paper: the ink at half strength over it.</summary>
    private static Color Outline(PlotInks inks) => Color.FromRgb(
        (byte)((inks.Ink.R + inks.Paper.R) / 2), (byte)((inks.Ink.G + inks.Paper.G) / 2), (byte)((inks.Ink.B + inks.Paper.B) / 2));

    /// <summary>
    /// Section 1.2 as entry 214 revised it: the rings clearly behind the outlines in both themes, fainter against the paper than the
    /// half strength outlines and apart from them in tone, and still visible; the dark theme's at half entry 210's lightness.
    /// </summary>
    [Fact]
    public void TheRingsSitBehindTheOutlinesAndStillShow()
    {
        Assert.Equal(Color.Parse("#282828"), Tokens.Plot(ThemeVariant.Dark).Bull);
        foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            var inks = Tokens.Plot(variant);
            var outline = Outline(inks);
            Assert.True(Contrast(inks.Bull, inks.Paper) < Contrast(outline, inks.Paper), $"{variant}: the rings are not behind the outlines");
            Assert.True(Contrast(inks.Bull, inks.Paper) >= 1.2, $"{variant}: the rings have vanished into the paper");
            double apart = Math.Abs(inks.Bull.R - outline.R) + Math.Abs(inks.Bull.G - outline.G) + Math.Abs(inks.Bull.B - outline.B);
            Assert.True(apart >= 45, $"{variant}: the rings' grey is only {apart} from the outlines'");
        }
    }

    /// <summary>Sections 1.1 and 2: the rings' width, and the two framings, remembered, with zoom and its reset.</summary>
    [AvaloniaFact]
    public void TheRingsAreWideAndTheWholeTargetFitsEveryRing()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            var plot = window.Plot;
            var bounds = plot.DataRect;
            Assert.False(plot.WholeTarget);
            Assert.True(window.PlotFraming.Group.IsChecked == true && window.PlotFraming.Whole.IsChecked != true);

            // The group framing: every shot inside, as entry 204 framed it.
            Assert.All(plot.Shots, s => Assert.True(bounds.Contains(plot.ToScreen(s.Offset)), $"shot {s.Label} is outside the group view"));
            double groupRing = plot.RingStrokeNow;
            Assert.True(groupRing >= 3 * CompositePlot.BullStroke, $"the rings are {groupRing:0.0} wide at the group view, not about five times {CompositePlot.BullStroke}");
            Assert.True(groupRing > 4 * 1.5, "the rings are not clearly wider than a shot's outline");

            // The whole target: every ring's edge inside the plot, the group with it.
            window.PlotFraming.Whole.IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            Assert.True(plot.WholeTarget);
            Assert.True(window.SettingsStore.LoadPlotWholeTarget());
            double outer = plot.Discs.Max(d => d.DiameterInches) / 2;
            foreach (var edge in new[] { new PointD(outer, 0), new PointD(-outer, 0), new PointD(0, outer), new PointD(0, -outer) })
            {
                Assert.True(bounds.Contains(plot.ToScreen(edge)), $"the ring's edge at {edge} is outside the whole target view");
            }

            Assert.All(plot.Shots, s => Assert.True(bounds.Contains(plot.ToScreen(s.Offset)), $"shot {s.Label} is outside the whole target view"));

            // Free zoom about a point keeps that point still, and the reset returns to the fitted framing.
            var aim = plot.ToScreen(new PointD(0, 0));
            double across = plot.ToScreen(new PointD(1, 0)).X - aim.X;
            plot.ZoomAbout(aim, 2);
            Assert.Equal(aim.X, plot.ToScreen(new PointD(0, 0)).X, 3);
            Assert.Equal(2 * across, plot.ToScreen(new PointD(1, 0)).X - plot.ToScreen(new PointD(0, 0)).X, 3);
            plot.ResetView();
            Assert.Equal(1, plot.Zoom);
            Assert.Equal(across, plot.ToScreen(new PointD(1, 0)).X - plot.ToScreen(new PointD(0, 0)).X, 3);

            window.PlotFraming.Group.IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            Assert.False(window.SettingsStore.LoadPlotWholeTarget());
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
