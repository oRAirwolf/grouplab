using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using Mat = OpenCvSharp.Mat;
using MatType = OpenCvSharp.MatType;
using Scalar = OpenCvSharp.Scalar;
using Cv2 = OpenCvSharp.Cv2;
using Point = OpenCvSharp.Point;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 517 section 2 on the phone: "Which target is this?" opened as a card over the list behind it, and Back from
/// a store-bought target or marking by hand came back to that list, full size, so the question looked too small the first time. It now
/// fills the screen when it opens and Back comes back to it at that size. Its first row's title, "Mark it by hand", was drawn in the color
/// meant for amber on a clear row and did not show; it is in the text color now.
/// </summary>
public class Entry517PhoneTests
{
    private static readonly TestPhone ThePhone = new();

    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private static Border? Card(Control view) =>
        view.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => AutomationProperties.GetName(b) == OpeningWords.WhichTitle);

    private static Button Named(Control view, string id) =>
        view.GetLogicalDescendants().OfType<Button>().First(b => AutomationProperties.GetAutomationId(b) == id);

    public static TheoryData<int, int> Sizes => new() { { 390, 844 }, { 320, 568 }, { 1024, 1366 } };

    [AvaloniaTheory]
    [MemberData(nameof(Sizes))]
    public void WhichTargetFillsTheScreenAndBackComesBackToItAtThatSize(int width, int height)
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        string copy = Path.Combine(ThePhone.CacheFolder, $"plain-517-{width}.png");
        using (var image = new Mat(1200, 900, MatType.CV_8UC3, new Scalar(235, 235, 235)))
        {
            Cv2.Circle(image, new Point(450, 600), 300, new Scalar(30, 30, 30), -1);
            Cv2.ImWrite(copy, image);
        }

        var setup = new ShotSetup(Calibre.Of(0.308), 3600);
        var result = PhoneAnalysis.Run(copy, setup, UnitSettings.Imperial, null, CancellationToken.None);
        var window = new Window { Width = width, Height = height, FontSize = 16 };
        var view = new ResultView(result, setup, UnitSettings.Imperial, () => { });
        window.Content = view;
        window.Show();
        try
        {
            Settle();
            var first = Card(view)!.Bounds;
            Assert.True(first.Height >= height - 40, $"at {width} by {height} the question opened {first.Height:0} tall");
            Assert.True(first.Width >= width - 40, $"at {width} by {height} the question opened {first.Width:0} wide");

            // The first row's title shows: its words are in the text color, not the color meant for amber.
            var byHand = Named(view, "which-target-by-hand");
            var title = byHand.GetLogicalDescendants().OfType<TextBlock>().First(t => t.Text == OpeningWords.ByHand.Label);
            var palette = GroupLab.App.Theme.Tokens.For(view.ActualThemeVariant);
            Assert.Equal(palette.Text, Assert.IsAssignableFrom<ISolidColorBrush>(title.Foreground).Color);

            // A store-bought target, then Back: the same question, the same size.
            Named(view, "which-target-store").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.Null(Card(view));
            view.GetLogicalDescendants().OfType<Button>().First(b => (b.Content as string ?? AutomationProperties.GetName(b) ?? "").StartsWith("Back", StringComparison.Ordinal))
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.Equal(first.Size, Card(view)!.Bounds.Size);

            // Dismissed, the list behind it is the screen, as before.
            view.GetLogicalDescendants().OfType<Button>().First(b => AutomationProperties.GetName(b) == OpeningWords.Dismiss)
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.Null(Card(view));
            Assert.Contains(view.GetLogicalDescendants().OfType<Button>(), b => AutomationProperties.GetAutomationId(b) == "result-mark-by-hand");
        }
        finally
        {
            window.Close();
        }
    }
}
