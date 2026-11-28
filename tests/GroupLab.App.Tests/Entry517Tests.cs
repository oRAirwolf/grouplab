using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 517, two problems a user told Alan about. Section 1: after "Mark it by hand" the scale was asked for only in
/// the status line, and the right column's Scale part sat out of view under the setup, empty until two points were tapped; now "Set the
/// scale" leads the right column until a scale is set. Section 2: "Which target is this?" is the same size the first time it opens as after.
/// </summary>
public class Entry517Tests
{
    private static string PlainTarget()
    {
        string path = Path.Combine(Path.GetTempPath(), $"grouplab-plain-517-{Guid.NewGuid():N}.png");
        using var image = new Mat(600, 800, MatType.CV_8UC3, new Scalar(235, 235, 235));
        Cv2.Circle(image, new OpenCvSharp.Point(400, 300), 120, new Scalar(30, 30, 30), -1);
        Cv2.ImWrite(path, image);
        return path;
    }

    private static void Settle(MainWindow window)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (window.DetectionTask is { IsCompleted: false } && clock.Elapsed < TimeSpan.FromSeconds(120))
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }

        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private static MainWindow Window(int width, int height)
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-517-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = width, Height = height, DetectOnOpen = true };
        window.Show();
        return window;
    }

    private static List<string> Texts(Control control) => [.. control.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "")];

    [AvaloniaFact]
    public void MarkingByHandAsksForTheScaleAtTheTopOfTheRightColumnUntilItIsSet()
    {
        string path = PlainTarget();
        var window = Window(1280, 720);
        try
        {
            window.OpenImage(path);
            Settle(window);
            Assert.Equal(OpeningWords.WhichTitle, window.ProblemTitle);
            Assert.False(window.ScaleAskShown);
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Settle(window);

            Assert.True(window.ScaleAskShown, "marking by hand did not ask for the scale where the person is looking");
            var card = window.ScaleAskCard;
            var said = Texts(card);
            Assert.Contains(OpeningWords.ScaleAskTitle, said);
            Assert.Contains(OpeningWords.ScaleAskSays, said);
            var named = card.GetLogicalDescendants().OfType<Button>().Select(AutomationProperties.GetName).ToList();
            Assert.Contains(OpeningWords.ScaleAskLength, named);
            Assert.Contains(OpeningWords.ScaleAskCorners, named);
            Assert.Contains(OpeningWords.ScaleAskPaper, named);

            // The tool's own inputs, where the length or size is typed, are inside the card while it shows.
            Assert.Contains(window.ScaleInputs, card.GetLogicalDescendants());

            // At 1280 by 720 the card is in view at the top of the right column, not under the setup.
            var column = card.FindAncestorOfType<ScrollViewer>()!;
            var top = Avalonia.VisualExtensions.TranslatePoint(card, new Avalonia.Point(0, 0), column)!.Value;
            Assert.InRange(top.Y, 0, column.Bounds.Height - 40);

            // The corners choice takes the rectangle tool and says how inside the card.
            card.GetLogicalDescendants().OfType<Button>().First(b => AutomationProperties.GetName(b) == OpeningWords.ScaleAskCorners)
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            Assert.Equal(MarkingTool.Rectangle, window.ToolNow);
            Assert.Contains(Texts(card), t => t.StartsWith("Tap four corners of a known rectangle", StringComparison.Ordinal));

            // A scale set: the card goes, and the inputs go back under the Scale heading.
            window.Session.LoadDetections(new GroupLab.Core.Marking.LengthReference(new PointD(100, 100), new PointD(300, 100), 2), [], [], "test");
            Settle(window);
            Assert.False(window.ScaleAskShown);
            Assert.DoesNotContain(window.ScaleInputs, card.GetLogicalDescendants());
            Assert.NotNull(window.ScaleInputs.Parent);
        }
        finally
        {
            window.Close();
            File.Delete(path);
        }
    }

    /// <summary>Section 2, on the computer: the question is the same size each time it opens, at the sizes the sweep checks and 1280 by 720.</summary>
    [AvaloniaTheory]
    [InlineData(1280, 720)]
    [InlineData(1060, 720)]
    [InlineData(1920, 1080)]
    public void WhichTargetIsTheSameSizeTheFirstTimeAsAfter(int width, int height)
    {
        string path = PlainTarget();
        var window = Window(width, height);
        try
        {
            var sizes = new List<Avalonia.Rect>();
            for (int round = 0; round < 2; round++)
            {
                window.OpenImage(path);
                Settle(window);
                var card = window.GetVisualDescendants().OfType<Border>().Single(b => AutomationProperties.GetName(b) == OpeningWords.WhichTitle);
                sizes.Add(card.Bounds);
                window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
                Settle(window);
            }

            Assert.Equal(sizes[0].Size, sizes[1].Size);
            Assert.True(sizes[0].Width >= 480 && sizes[0].Height >= 300, $"the question opened at {sizes[0]}");
        }
        finally
        {
            window.Close();
            File.Delete(path);
        }
    }
}
