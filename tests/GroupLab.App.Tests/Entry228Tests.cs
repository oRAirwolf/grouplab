using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 228, suggested by Unholy (also TNA): a target GroupLab did not print, with bulls placed by hand, a scale drawn
/// at each, the angle of the photograph said in the figures, each bull on its own under Advanced, a bull taken away with Delete, and the
/// bulls kept as a template and placed again from the first two.
/// </summary>
public class Entry228Tests
{
    private static MainWindow Opened()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-entry228-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        window.OpenImage(Path.Combine(Entry109Tests.Repository(), "samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"));
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    /// <summary>Two bulls 2 in apart, an angled view: the right one looks 6 percent smaller across than up and down.</summary>
    private static void Mark(MainWindow window)
    {
        var s = window.Session;
        int left = s.AddBull(new PointD(1000, 1000)), right = s.AddBull(new PointD(2200, 1000));
        foreach (var (x, y) in new[] { (1050.0, 960.0), (980, 1060), (1020, 1010), (2240, 980), (2170, 1030), (2210, 1050) })
        {
            s.AddShot(new PointD(x, y));
        }

        window.AddBullLength(left, new DrawnLength(new PointD(850, 1000), new PointD(1150, 1000), 0.5));
        window.AddBullLength(left, new DrawnLength(new PointD(1000, 850), new PointD(1000, 1150), 0.5));
        window.AddBullLength(right, new DrawnLength(new PointD(2059, 1000), new PointD(2341, 1000), 0.5));
        window.AddBullLength(right, new DrawnLength(new PointD(2200, 850), new PointD(2200, 1150), 0.5));
    }

    [AvaloniaFact]
    public void BullsPlacedByHandAreMeasuredEachWithItsOwnScaleAndTheAngleIsSaid()
    {
        var window = Opened();
        string store = window.SettingsStore.Path;
        try
        {
            Mark(window);
            Assert.IsType<PerBullReference>(window.Session.State.Scale);
            Assert.All(window.Session.State.Shots, sh => Assert.NotNull(sh.Bull));
            Assert.Equal([0, 0, 0, 1, 1, 1], window.Session.State.Shots.Select(sh => sh.Bull!.Value));
            Assert.True(MarkingCanvas.PlacedByHand(window.Session.State));

            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            var said = window.FigureNames;
            Assert.Contains(said, n => n.Contains("taken at an angle", StringComparison.Ordinal));
            Assert.Contains(said, n => n.StartsWith("The scale is uncertain by about", StringComparison.Ordinal));
            Assert.Contains("Bull by bull", said);
            Assert.Contains(said, n => n.StartsWith("Bull 2: 3 shots", StringComparison.Ordinal));
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.DeleteFile(store);
        }
    }

    [AvaloniaFact]
    public void DeleteTakesTheChosenBullAwayAndATemplatePlacesItAgain()
    {
        var window = Opened();
        string store = window.SettingsStore.Path;
        try
        {
            Mark(window);
            var s = window.Session;
            s.AddBull(new PointD(1000, 2200));
            var template = BullTemplate.From("Commercial", s.State)!;
            window.SettingsStore.SaveBullTemplate(template);

            window.Canvas.Tool = MarkingTool.Bulls;
            window.Canvas.ChosenBull = s.State.Bulls[2].Index;
            window.Canvas.Focus();
            window.Canvas.RaiseEvent(new Avalonia.Input.KeyEventArgs { RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent, Key = Avalonia.Input.Key.Delete });
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(2, s.State.Bulls.Count);

            window.PlaceTemplate(Assert.Single(window.SettingsStore.LoadBullTemplates()));
            Assert.Equal(3, s.State.Bulls.Count);
            Assert.InRange(s.State.Bulls[2].Image.X, 990, 1010);
            Assert.InRange(s.State.Bulls[2].Image.Y, 2150, 2250);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.DeleteFile(store);
        }
    }
}
