using Avalonia.Automation;
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
using OpenCvSharp;
using Window = Avalonia.Controls.Window;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entries 340 and 341 on the desktop: a recognized store-bought target is named, its bull placed and its printed size
/// set as the scale, with the warning under the scale and on the result and "Check the scale" one click away; a family member the picture
/// cannot tell from its other size asks "Which target is this?" in a small window, the last answer first; a GroupLab sheet never shows the
/// warning.
/// </summary>
public class StoreTargetScreenTests
{
    private const string SixInch = "bc-34550-shoot-n-c-6in-bull";
    private const string EightInch = "bc-34805-shoot-n-c-8in-bull";

    private static MainWindow NewWindow(out string picture)
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        picture = Path.Combine(Path.GetTempPath(), $"grouplab-storetarget-{Guid.NewGuid():N}.png");
        using (var blank = new Mat(1000, 1000, MatType.CV_8UC3, Scalar.All(230)))
        {
            Cv2.ImWrite(picture, blank);
        }

        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        window.OpenImage(picture);
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static StoreTargetCandidate C(string id, int features, double layout, double ppi) =>
        new(StoreTargetLibrary.Find(id)!, features, new Homography([ppi, 0, 100, 0, ppi, 100, 0, 0, 1]), layout);

    private static IEnumerable<string> Words(Control control) => control.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");

    private static Button Named(Control control, string name) =>
        control.GetLogicalDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == name);

    [AvaloniaFact]
    public void ARecognizedTargetIsPlacedAndScaledWithTheWarningAndTheScaleCheck()
    {
        var window = NewWindow(out _);
        var seen = StoreTargetRecognizer.Decide([C(EightInch, 300, 0.99, 100)]);
        window.ApplyRecognition(seen).GetAwaiter().GetResult();
        Dispatcher.UIThread.RunJobs();
        Assert.Null(window.WhichTargetWindow);
        var scale = Assert.IsType<RectangleReference>(window.Session.State.Scale);
        Assert.Equal(EightInch, scale.PrintedTarget);
        Assert.Single(window.Session.State.Bulls);
        Assert.StartsWith("Recognized the Shoot-N-C 8 in bullseye", window.StatusText, StringComparison.Ordinal);

        // The warning under the scale, and the scale check one click away: the length tool.
        Assert.Contains(StoreTargetMatch.Warning, Words(window.ScaleInputs));
        Assert.Contains("Scale from the printed size of the Shoot-N-C 8 in bullseye", Words(window.ScaleInputs));
        Named(window.ScaleInputs, StoreTargetMatch.CheckScale).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(MarkingTool.Length, window.ToolNow);

        // On the result too, before the figures, and the pill says where the scale came from.
        window.Session.AddShot(new PointD(500, 500));
        window.Session.AddShot(new PointD(520, 510));
        window.RefreshForTests();
        Assert.Contains(StoreTargetMatch.Warning, window.StatisticsText);
        Assert.Contains("Scale from the printed size", window.ScalePillText, StringComparison.Ordinal);
        window.Close();
    }

    [AvaloniaFact]
    public void AFamilyThePictureCannotTellApartAsksWhichAndRemembersTheAnswer()
    {
        var window = NewWindow(out _);
        var seen = StoreTargetRecognizer.Decide([C(EightInch, 200, 0.988, 60), C(SixInch, 72, 0.928, 80)]);
        Assert.True(seen.AsksWhichSize);

        var asked = window.ApplyRecognition(seen);
        Dispatcher.UIThread.RunJobs();
        var dialog = Assert.IsType<Window>(window.WhichTargetWindow);
        Assert.Equal(FamilyQuestion.Title, dialog.Title);
        var answers = dialog.GetLogicalDescendants().OfType<Button>().Select(b => AutomationProperties.GetName(b)).ToList();
        Assert.Equal(["Shoot-N-C 6 in bullseye", "Shoot-N-C 8 in bullseye", FamilyQuestion.NotSure], answers);
        Assert.Equal(2, dialog.GetLogicalDescendants().OfType<TargetOutline>().Count());
        Assert.Contains(Words(dialog), w => w.StartsWith("Printed area about 6 by 6 in", StringComparison.Ordinal));

        Named(dialog, "Shoot-N-C 8 in bullseye").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        asked.GetAwaiter().GetResult();
        Assert.Null(window.WhichTargetWindow);
        var scale = Assert.IsType<RectangleReference>(window.Session.State.Scale);
        Assert.Equal(EightInch, scale.PrintedTarget);
        Assert.Equal(EightInch, window.SettingsStore.LoadFamilyAnswer(StoreTargetLibrary.ShootNCBullseye));

        // Next time the 8 in is offered first, and "Not sure" sets no scale but places the bull.
        window.Session.SetScale(null);
        var again = window.ApplyRecognition(seen);
        Dispatcher.UIThread.RunJobs();
        dialog = window.WhichTargetWindow!;
        Assert.Equal("Shoot-N-C 8 in bullseye", AutomationProperties.GetName(dialog.GetLogicalDescendants().OfType<Button>().First()));
        int bulls = window.Session.State.Bulls.Count;
        Named(dialog, FamilyQuestion.NotSure).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        again.GetAwaiter().GetResult();
        Assert.Null(window.Session.State.Scale);
        Assert.Equal(bulls + 1, window.Session.State.Bulls.Count);
        Assert.Equal(FamilyQuestion.NotSureSaid, window.StatusText);
        window.Close();
    }

    /// <summary>
    /// The whole way, where the blanks are on this computer (never on CI): a picture of the 8 in bullseye opened, its codes not found, and
    /// the target recognized, placed and scaled with no question, since the whole sheet tells the sizes apart.
    /// </summary>
    [AvaloniaFact]
    public void OpeningAPictureOfAStoreBoughtTargetRecognizesIt()
    {
        string blank = Path.Combine(@"C:\Dev\grouplab-local\commercial-targets", EightInch, "blank.png");
        if (!File.Exists(blank))
        {
            return;
        }

        string picture = Path.Combine(Path.GetTempPath(), $"grouplab-storetarget-{Guid.NewGuid():N}.jpg");
        using (var scan = Cv2.ImRead(blank, ImreadModes.Color))
        using (var small = new Mat())
        {
            Cv2.Resize(scan, small, new OpenCvSharp.Size(0, 0), 1 / 6.0, 1 / 6.0, InterpolationFlags.Area);
            Cv2.ImWrite(picture, small);
        }

        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        var window = new MainWindow(store) { Width = 1400, Height = 900, DetectOnOpen = true };
        window.Show();
        try
        {
            window.OpenImage(picture);
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (window.DetectionTask is { IsCompleted: false } && clock.Elapsed < TimeSpan.FromSeconds(120))
            {
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(10);
            }

            Dispatcher.UIThread.RunJobs();
            Assert.Null(window.WhichTargetWindow);
            Assert.Equal(EightInch, Assert.IsType<RectangleReference>(window.Session.State.Scale).PrintedTarget);
            Assert.Single(window.Session.State.Bulls);
            Assert.Contains(StoreTargetMatch.Warning, Words(window.ScaleInputs));
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.DeleteFile(picture);
        }
    }

    [AvaloniaFact]
    public void AGroupLabSheetNeverShowsTheWarning()
    {
        var window = NewWindow(out _);
        window.Session.SetScale(new SheetReference(new HomographyMapping(Homography.Identity), "test"));
        window.Session.AddShot(new PointD(500, 500));
        window.Session.AddShot(new PointD(520, 510));
        window.RefreshForTests();
        Assert.DoesNotContain(StoreTargetMatch.Warning, Words(window.ScaleInputs));
        Assert.DoesNotContain(StoreTargetMatch.Warning, window.StatisticsText);
        Assert.DoesNotContain(window.ScaleInputs.GetLogicalDescendants().OfType<Button>(), b => AutomationProperties.GetName(b) == StoreTargetMatch.CheckScale);
        window.Close();
    }
}
