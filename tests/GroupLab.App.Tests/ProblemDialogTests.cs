using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Registration;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 356: what stops the work or needs a decision is said in the middle of the window. Section 5's calm
/// "Which target is this?" for a picture with no GroupLab marks, and the keyboard of section 3: Enter takes the first choice, Escape
/// dismisses, and every button has a name a screen reader says.
/// </summary>
public class ProblemDialogTests
{
    private static string PlainTarget()
    {
        string path = Path.Combine(Path.GetTempPath(), $"grouplab-plain-{Guid.NewGuid():N}.png");
        using var image = new Mat(600, 800, MatType.CV_8UC3, new Scalar(235, 235, 235));
        Cv2.Circle(image, new Point(400, 300), 120, new Scalar(30, 30, 30), -1);
        Cv2.Circle(image, new Point(380, 320), 9, new Scalar(200, 200, 200), -1);
        Cv2.ImWrite(path, image);
        return path;
    }

    private static MainWindow Opened(string path)
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(GroupLab.Core.Marking.UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900, DetectOnOpen = true };
        window.Show();
        window.OpenImage(path);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (window.DetectionTask is { IsCompleted: false } && clock.Elapsed < TimeSpan.FromSeconds(120))
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }

        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void APictureWithNoGroupLabMarksAsksWhichTargetCalmlyAndEnterMarksItByHand()
    {
        string path = PlainTarget();
        try
        {
            var window = Opened(path);
            Assert.Equal(OpeningOutcome.NotGroupLab, window.LastOpening);
            Assert.True(window.ProblemOpen);
            Assert.Equal(OpeningWords.WhichTitle, window.ProblemTitle);
            Assert.Equal(OpeningWords.WhichTitle, window.StatusText);

            // Every button in the dialog has a name to be read out.
            var buttons = window.ProblemButtons;
            Assert.True(buttons.Count >= 4);
            Assert.All(buttons, b => Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(b))));

            // Enter takes the first choice: marking by hand, with the length tool for the one true length.
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            Assert.False(window.ProblemOpen);
            Assert.Equal(MarkingTool.Length, window.ToolNow);
            Assert.Equal(OpeningWords.ByHandStatus, window.StatusText);
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void EscapeDismissesTheQuestionAndLeavesThePictureAsItWas()
    {
        string path = PlainTarget();
        try
        {
            var window = Opened(path);
            Assert.True(window.ProblemOpen);
            window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            Assert.False(window.ProblemOpen);
            Assert.Null(window.Session.State.Scale);
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>A GroupLab sheet with its codes left out of the print, written as a file: it looks like a sheet and its codes cannot read.</summary>
    private static string CodelessSheet()
    {
        var definition = GroupLab.Core.Gltd.Json.GltdJsonReader.ReadFile(Path.Combine(AppContext.BaseDirectory, "targets", "GL-RF25-LTR.gltd.json")).Definition!;
        var page = GroupLab.Core.Rendering.SceneBuilder.Build(definition).Pages[0];
        var codeless = page with { Items = [.. page.Items.Where(i => i.Layer != GroupLab.Core.Rendering.SceneLayer.Codes)] };
        var render = GroupLab.Core.Rendering.SceneRasterizer.Rasterize(codeless, 200);
        string path = Path.Combine(Path.GetTempPath(), $"grouplab-codeless-{Guid.NewGuid():N}.png");
        using var mat = Mat.FromPixelData(render.Height, render.Width, MatType.CV_8UC1, render.Pixels);
        Cv2.ImWrite(path, mat);
        return path;
    }

    /// <summary>
    /// Section 7: "Not a GroupLab sheet?" is remembered for that picture and counted, nothing sent; the same picture opened again goes
    /// straight to the calm question. Every button of board B has a name a screen reader says.
    /// </summary>
    [AvaloniaFact]
    public void NotAGroupLabSheetIsRememberedForThePictureAndCounted()
    {
        string path = CodelessSheet();
        try
        {
            var window = Opened(path);
            Assert.Equal(OpeningOutcome.LooksLikeGroupLab, window.LastOpening);
            Assert.Equal(OpeningWords.CodesTitle, window.ProblemTitle);
            var buttons = window.ProblemButtons;
            Assert.All(buttons, b => Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(b))));
            Assert.Contains(buttons, b => AutomationProperties.GetName(b) == OpeningWords.ShowWhatWentWrong);

            window.PressProblemChoice(OpeningWords.StoreOrDrawn);
            Assert.Equal(OpeningWords.WhichTitle, window.ProblemTitle);
            Assert.False(window.ProblemBarShown);
            Assert.Equal(1, window.Settings.LoadNotGroupLabCount());

            window.DismissProblem();
            window.OpenImage(path);
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (window.DetectionTask is { IsCompleted: false } && clock.Elapsed < TimeSpan.FromSeconds(120))
            {
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(10);
            }

            Dispatcher.UIThread.RunJobs();
            Assert.Equal(OpeningWords.WhichTitle, window.ProblemTitle);
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void AnyFailureCanBeSaidInTheMiddleWithItsChoices()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        bool retried = false;
        window.ShowProblem("The marking could not be saved", "The folder is not there any more.", ("Choose another folder", () => retried = true), ("Not now", () => { }));
        Assert.Equal(["Choose another folder", "Not now"], window.ProblemChoices);
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.True(retried);
        Assert.False(window.ProblemOpen);
        window.Close();
    }
}
