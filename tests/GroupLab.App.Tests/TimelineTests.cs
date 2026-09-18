using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Trace;

namespace GroupLab.App.Tests;

/// <summary>
/// DESIGN.md section 19 [r3] and NOTES-FROM-PLANNING.md entry 97 section 3: the analysis shows its work. The stages land on a timeline, a
/// stage is scrubbed to, a rejection is clicked and found on the image, and the design's two constraints hold: a failure is still a normal
/// prominent error with the trace behind it, and nothing is computed for the timeline that the stages did not file anyway.
/// </summary>
public class TimelineTests
{
    private static MainWindow NewWindow()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        return new MainWindow(store) { Width = 1400, Height = 900 };
    }

    private static GroupLab.Core.Gltd.Model.TargetDefinition Definition([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        GltdJsonReader.ReadFile(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "targets", "GL-CF25-LTR.gltd.json"))).Definition!;

    /// <summary>A 300 DPI render of the built-in sheet with a hole on each of the first five bulls and a mark too small to be one.</summary>
    private static GrayImage Sheet(GroupLab.Core.Gltd.Model.TargetDefinition definition)
    {
        const double dpi = 300;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        double s = 254 / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var random = new Random(97);
        var holes = definition.Bulls.Where(b => b.Scoring).Take(5)
            .Select(b => SyntheticSheet.SampleHole(random, b.X + 30, b.Y + 20, onInk: false, HoleBacking.ScannerLid, 0.871))
            .Append(SyntheticSheet.SampleHole(random, definition.Bulls[7].X + 40, definition.Bulls[7].Y - 30, onInk: false, HoleBacking.ScannerLid, 0.35))
            .ToList();
        return SyntheticSheet.Compose(render, dpi, truth, render.Width, render.Height, holes, [], random);
    }

    [AvaloniaFact]
    public void AScrubbedStageShowsItsWorkAndARejectionIsFoundOnTheImage()
    {
        var definition = Definition();
        var image = Sheet(definition);
        var trace = new TraceRecorder();
        var result = AutomaticMarking.Run(image, image, ImageMetadata.ForScan(image.Width, image.Height, 300), definition, new OpenCvSharpBackend(), trace);
        Assert.Null(result.Failure);

        var window = NewWindow();
        window.Show();
        window.ApplyDetection(result);
        window.ShowTrace(trace.Records);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(trace.Records.Count, window.Stages.Count);
        int holesStage = window.Stages.ToList().FindIndex(r => r.Rejections.Any(x => x.XInches is not null));
        Assert.True(holesStage >= 0, "no stage rejected anything with a position");
        window.ShowStage(holesStage);
        Dispatcher.UIThread.RunJobs();

        Assert.NotEmpty(window.Canvas.StageRejections);
        Assert.Contains(window.StageText, t => t.Length > 0);
        var find = window.GetLogicalDescendants().OfType<Button>().First(b => (b.Content as string)?.EndsWith(". Find it", StringComparison.Ordinal) == true);
        find.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(window.Canvas.Highlight);
        Assert.Contains(window.Canvas.StageRejections, p => p == window.Canvas.Highlight);
        Assert.StartsWith("Rejected ", window.StatusText, StringComparison.Ordinal);
        window.Close();
    }

    /// <summary>
    /// Section 19's first constraint: the trace is never the only place an error appears. A blank page fails registration; the panel says so in
    /// the normal way, and the failed stage is on the timeline as the detail behind it.
    /// </summary>
    [AvaloniaFact]
    public void AFailureIsAProminentErrorWithTheFailedStageBehindIt()
    {
        var blank = new GrayImage(1275, 1650, Enumerable.Repeat((byte)240, 1275 * 1650).ToArray());
        var trace = new TraceRecorder();
        var result = AutomaticMarking.Run(blank, blank, ImageMetadata.ForScan(1275, 1650, 150), Definition(), new OpenCvSharpBackend(), trace);
        Assert.NotNull(result.Failure);

        var window = NewWindow();
        window.Show();
        window.ApplyDetection(result);
        window.ShowTrace(trace.Records);
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(), t => (t.Text ?? "").StartsWith("Detection failed", StringComparison.Ordinal));
        Assert.Contains(window.Stages, r => r.Status != StageStatus.Ok);
        window.Close();
    }

    /// <summary>A live run: each stage lands on the timeline as it files, and the timeline moves to it.</summary>
    [AvaloniaFact]
    public void StagesLandAsTheyFile()
    {
        var window = NewWindow();
        window.Show();
        var trace = new TraceRecorder();
        trace.Filed += window.AddStage;

        using (var stage = trace.Begin("S1.first"))
        {
            stage.Done(StageStatus.Ok, "first");
        }

        Assert.Single(window.Stages);
        using (var stage = trace.Begin("S2.second"))
        {
            stage.Done(StageStatus.Degraded, "second");
        }

        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, window.Stages.Count);
        Assert.Equal("S2.second", window.Stages[^1].Stage);
        window.Close();
    }
}
