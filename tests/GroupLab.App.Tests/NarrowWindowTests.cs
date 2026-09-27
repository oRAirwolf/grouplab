using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 243 section 1.2, answering question 58 with option A: the analysis screen needed about 1060 units of width,
/// and a 1920 screen at 200 percent gives 960 and a 1366 laptop 683, so the right column ran past the window's edge. Now the side columns
/// narrow toward their minimums, and below them the right column moves under the image. At the default size nothing changes.
/// </summary>
public class NarrowWindowTests
{
    private static void Settle(Window window)
    {
        for (int i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    /// <summary>Every visible line of text whose right edge is past the window's.</summary>
    private static List<string> PastTheEdge(Window window) =>
        [.. window.GetVisualDescendants().OfType<TextBlock>()
            .Where(b => b.IsEffectivelyVisible && !string.IsNullOrEmpty(b.Text) && b.Bounds.Width > 0)
            .Select(b => (Block: b, Corner: b.TranslatePoint(new Point(b.Bounds.Width, 0), window)))
            .Where(x => x.Corner is { } p && p.X > window.Width + 1)
            .Select(x => $"'{x.Block.Text![..Math.Min(40, x.Block.Text!.Length)]}' reaches {x.Corner!.Value.X:0}")];

    [AvaloniaTheory]
    [InlineData(1400)]
    [InlineData(960)]
    [InlineData(683)]
    public void TheAnalysisFitsTheWindow(double width)
    {
        var (window, path, _) = Entry109Tests.Sheet((int)width, 900);
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.Session.SetShotDistance(3600);
            window.CalibreAnswered();
            window.Analyse();
            Settle(window);
            var past = PastTheEdge(window);
            Assert.True(past.Count == 0, $"at {width}: " + string.Join("; ", past.Take(8)));

            var mean = window.GetVisualDescendants().OfType<TextBlock>().First(b => b.IsEffectivelyVisible && b.Text == "Mean radius");
            var at = mean.TranslatePoint(default, window)!.Value;
            if (width >= 1400)
            {
                // The look at the default size is unchanged: the figures sit in a right column beside the image.
                Assert.True(at.X > width - Tokens.RightColumnWidth - 20, $"at {width} the figures start at {at.X:0}");
            }
            else if (width < 893)
            {
                // Below the minimums the right column is under the image, in the image's column.
                Assert.True(at.Y > 300 && at.X < width - 260, $"at {width} the figures start at ({at.X:0}, {at.Y:0})");
            }
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
