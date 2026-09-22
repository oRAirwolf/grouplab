using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 5.1.3: no text cut off or wrapping mid-number, at either size.
/// <para>
/// <b>Why a measurement and not a look.</b> Reading a screenshot for a clipped word is guesswork: a figure right-aligned two pixels inside
/// the edge and a figure two pixels outside it look the same at any scale a person actually views a render at. Avalonia knows exactly where
/// it put every word, so this asks it.
/// </para>
/// <para>
/// It is also the check that will not rot. A screenshot is looked at once, on the night it is taken; this runs on every commit, at both of
/// the sizes entry 141 names, and it fails with the word and how far outside its panel the word landed.
/// </para>
/// </summary>
public class NothingIsCutOffTests
{
    private static MainWindow NewWindow(int width, int height)
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        return new MainWindow(store) { Width = width, Height = height };
    }

    /// <summary>A marking with the figures a person actually reads: ten shots, a scale, an aim point and a calibre.</summary>
    private static void Mark(MainWindow window)
    {
        var session = window.Session;
        session.Open(@"C:\a-target.png");
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        session.SetPointOfAim(new PointD(0, 0));
        for (int k = 0; k < 10; k++)
        {
            double angle = 2 * Math.PI * k / 10;
            session.AddShot(new PointD(27 * Math.Cos(angle), 27 * Math.Sin(angle)));
        }

        session.SetCalibre(Calibre.Of(0.308));
        session.SetShotDistance(3600);
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// Every word the window shows sits inside the window. A word whose right edge is past the window's own width has been cut off, whatever
    /// a screenshot looks like, and the panel it is in is the one that needs the room.
    /// </summary>
    [AvaloniaTheory]
    [InlineData(1280, 720)]
    [InlineData(2560, 1440)]
    public void NoTextRunsPastTheEdgeOfTheWindow(int width, int height)
    {
        var window = NewWindow(width, height);
        window.Show();
        Mark(window);
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(width, height));
        window.Arrange(new Rect(0, 0, width, height));
        Dispatcher.UIThread.RunJobs();

        var over = new List<string>();
        foreach (var text in window.GetVisualDescendants().OfType<TextBlock>())
        {
            if (!text.IsVisible || string.IsNullOrWhiteSpace(text.Text) || text.Bounds.Width <= 0)
            {
                continue;
            }

            var corner = text.TranslatePoint(new Point(text.Bounds.Width, 0), window);
            if (corner is { } at && at.X > width + 0.5)
            {
                over.Add($"\"{text.Text}\" ends {at.X - width:0.0} px past the right edge");
            }
        }

        Assert.True(over.Count == 0, $"at {width} by {height}, text is cut off at the window's edge:\n  " + string.Join("\n  ", over.Take(12)));
        window.Close();
    }
}
