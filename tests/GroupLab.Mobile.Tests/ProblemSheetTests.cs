using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using Mat = OpenCvSharp.Mat;
using MatType = OpenCvSharp.MatType;
using Scalar = OpenCvSharp.Scalar;
using Cv2 = OpenCvSharp.Cv2;
using Point = OpenCvSharp.Point;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 356 on the phone: a picture with no GroupLab marks asks "Which target is this?" as a sheet in the middle of
/// the screen, calmly, marking it by hand first; dismissing it leaves the choices that were behind it.
/// </summary>
public class ProblemSheetTests
{
    private static readonly TestPhone ThePhone = new();

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    [AvaloniaFact]
    public void APictureWithNoGroupLabMarksAsksWhichTargetItIs()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        string copy = Path.Combine(ThePhone.CacheFolder, "plain-356.png");
        using (var image = new Mat(1200, 900, MatType.CV_8UC3, new Scalar(235, 235, 235)))
        {
            Cv2.Circle(image, new Point(450, 600), 300, new Scalar(30, 30, 30), -1);
            Cv2.ImWrite(copy, image);
        }

        var setup = new ShotSetup(Calibre.Of(0.308), 3600);
        var result = PhoneAnalysis.Run(copy, setup, UnitSettings.Imperial, null, CancellationToken.None);
        Assert.Equal(OpeningOutcome.NotGroupLab, result.Opening);
        Assert.Equal(OpeningWords.WhichSays, result.Failure);

        var window = new Avalonia.Controls.Window { Width = 390, Height = 844, FontSize = 16 };
        var view = new ResultView(result, setup, UnitSettings.Imperial, () => { });
        window.Content = view;
        window.Show();
        Settle();
        var texts = view.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        Assert.Contains(OpeningWords.WhichTitle, texts);
        var named = view.GetLogicalDescendants().OfType<Button>().Select(b => Avalonia.Automation.AutomationProperties.GetAutomationId(b)).ToList();
        Assert.Contains("which-target-by-hand", named);
        Assert.Contains("which-target-store", named);
        Assert.Contains("which-target-grouplab", named);

        // Dismissed, the choices behind it are there: mark by hand and the sheets by name.
        var dismiss = view.GetLogicalDescendants().OfType<Button>().First(b => Avalonia.Automation.AutomationProperties.GetName(b) == OpeningWords.Dismiss);
        dismiss.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle();
        Assert.DoesNotContain(OpeningWords.WhichTitle, view.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text));
        Assert.Contains(view.GetLogicalDescendants().OfType<Button>(), b => Avalonia.Automation.AutomationProperties.GetAutomationId(b) == "result-mark-by-hand");
        window.Close();
    }
}
