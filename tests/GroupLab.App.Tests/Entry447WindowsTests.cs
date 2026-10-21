using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.StoreTargets;
using Window = Avalonia.Controls.Window;

using GroupLab.Tests.Support;

namespace GroupLab.App.Tests;

/// <summary>
/// What entry 447 section 2 left: the six windows its tests did not open, each tabbed all the way round headless as the screens were: the
/// focus never lost, something in it taking the focus, and every stop named as a screen reader reads it.
/// </summary>
public class Entry447WindowsTests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static MainWindow NewWindow()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        return window;
    }

    private static void RoundsWithEveryStopNamed(Window? dialog, string what)
    {
        Assert.True(dialog is not null, what + " did not open");
        var (stops, unnamed, lost) = Entry447Tests.TabRound(dialog!);
        Assert.False(lost, what + ": the focus was lost");
        Assert.True(stops.Count > 0, what + ": nothing takes the focus");
        Assert.True(unnamed.Count == 0, what + ": " + string.Join("; ", unnamed));
        var found = ScreenReaderAudit.Findings(dialog!, stops);
        Assert.True(found.Count == 0, what + ": " + string.Join("; ", found));
        var laid = LayoutAudit.Findings(dialog!);
        Assert.True(laid.Count == 0, what + ": " + string.Join("; ", laid));
        dialog!.Close();
        Settle();
    }

    private static string Rows(int n) => "x (in),y (in)\n" + string.Join("\n", Enumerable.Range(0, n)
        .Select(i => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{0.3 * Math.Sin(i * 1.7):0.000},{0.3 * Math.Cos(i * 2.3):0.000}")));

    [AvaloniaFact]
    public void TheCsvMappingWindowTabsRound()
    {
        var window = NewWindow();
        _ = window.ShowMapping(ShotCsv.Read(Rows(5)), "shots.csv");
        Settle();
        RoundsWithEveryStopNamed(window.OwnedWindows.LastOrDefault(), "Import shot coordinates");
        window.Close();
    }

    [AvaloniaFact]
    public void TheStoreTargetQuestionTabsRound()
    {
        var window = NewWindow();
        string picture = Path.Combine(Path.GetTempPath(), $"grouplab-storetarget-{Guid.NewGuid():N}.png");
        using (var blank = new OpenCvSharp.Mat(1000, 1000, OpenCvSharp.MatType.CV_8UC3, OpenCvSharp.Scalar.All(230)))
        {
            OpenCvSharp.Cv2.ImWrite(picture, blank);
        }

        try
        {
            window.OpenImage(picture);
            Settle();
            StoreTargetCandidate C(string id, int features, double layout, double ppi) =>
                new(StoreTargetLibrary.Find(id)!, features, new Homography([ppi, 0, 100, 0, ppi, 100, 0, 0, 1]), layout);
            var seen = StoreTargetRecognizer.Decide([C("bc-34805-shoot-n-c-8in-bull", 200, 0.988, 60), C("bc-34550-shoot-n-c-6in-bull", 72, 0.928, 80)]);
            _ = window.ApplyRecognition(seen);
            Settle();
            // Entry 465 3B: the question is a bar in the window now, not a window of its own, so the window's own round reaches its
            // answers, each named, and the screen is laid out whole with it open.
            var bar = window.WhichTargetBar;
            Assert.True(bar is not null, "Which target is this? did not open");
            var (stops, unnamed, lost) = Entry447Tests.TabRound(window);
            Assert.False(lost, "Which target is this?: the focus was lost");
            Assert.True(unnamed.Count == 0, "Which target is this?: " + string.Join("; ", unnamed));
            Assert.Equal(3, stops.Count(s => s.GetLogicalAncestors().Contains(bar)));
            var found = ScreenReaderAudit.Findings(window, stops);
            Assert.True(found.Count == 0, string.Join("; ", found));
            var laid = LayoutAudit.Findings(window);
            Assert.True(laid.Count == 0, string.Join("; ", laid));
        }
        finally
        {
            window.Close();
            File.Delete(picture);
        }
    }

    [AvaloniaFact]
    public void FuddBusterTabsRound()
    {
        var window = NewWindow();
        Assert.Null(window.ImportShots(ShotCsv.Read(Rows(25)), 0, 1, CoordinateUnit.Inch, upIsPositive: true, 3600));
        Settle();
        _ = window.OpenFuddBuster(window.Session.State);
        Settle();
        RoundsWithEveryStopNamed(window.FuddBusterWindow, "Fudd buster");
        window.Close();
    }

    [AvaloniaFact]
    public void TheGroupToolsTabRound()
    {
        var window = NewWindow();
        Assert.Null(window.ImportShots(ShotCsv.Read(Rows(10)), 0, 1, CoordinateUnit.Inch, upIsPositive: true, 3600));
        Settle();
        foreach (var (name, make) in new (string, Func<Window>)[] { ("Shots and clicks", window.ShotsWindow), ("Zero from this group", window.ZeroFromWindow), ("Share a picture", window.ShareWindow) })
        {
            var tool = make();
            tool.Show(window);
            Settle();
            RoundsWithEveryStopNamed(tool, name);
        }

        window.Close();
    }

    [AvaloniaFact]
    public void ThePrintConfirmationTabsRound()
    {
        var panel = TargetsScreen.Open(1200, 800);
        Settle();
        panel.Select("GL-CF25-LTR.gltd.json");
        Settle();
        panel.GetLogicalDescendants().OfType<Button>().Single(b => b.Content as string == "Open to print").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle();
        RoundsWithEveryStopNamed(panel.Confirmation, "the print confirmation");
        panel.Close();
    }

    [AvaloniaFact]
    public void TheFiguresWindowTabsRound()
    {
        var window = NewWindow();
        Assert.Null(window.ImportShots(ShotCsv.Read(Rows(10)), 0, 1, CoordinateUnit.Inch, upIsPositive: true, 3600));
        Settle();
        window.PopOutFigures();
        Settle();
        Assert.True(window.FiguresPoppedOut);
        RoundsWithEveryStopNamed(window.OwnedWindows.SingleOrDefault(w => w.Title == "GroupLab figures"), "GroupLab figures");
        Assert.False(window.FiguresPoppedOut);
        window.Close();
    }
}
