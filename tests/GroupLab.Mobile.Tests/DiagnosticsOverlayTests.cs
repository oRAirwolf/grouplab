using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using GroupLab.App;
using GroupLab.Core.Capture;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 315 section 4: "Show diagnostics on the camera", a plain setting off until turned on, and the overlay's lines
/// built from one moment's values.
/// </summary>
public class DiagnosticsOverlayTests
{
    private static TestPhone Started()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        return (TestPhone)Phone.Platform!;
    }

    private static CaptureQuality Quality(double degrees = 5, double focus = 1, double resolution = 1) =>
        new(90, "good", 0.001, focus, 0, 200, 1, degrees, 1, 200, resolution, 34, 34, 1);

    [Fact]
    public void TheCameraLinesSayFramesGuidanceTiltTorchAndHealthInThatOrder()
    {
        var lines = DiagnosticsOverlay.Lines(new OverlayState(12.46, 83, Instruction.LessAngle, "angle 41.0 deg", 2.5, 2, 5,
            MemoryMb: 412, HeapMb: 96, Heat: "fair"));
        Assert.Equal(
        [
            "frames 12.5/s, 83 ms each",
            "guidance LessAngle, held by angle 41.0 deg",
            "tilt 2.5 deg, level",
            "torch 2 of 5",
            "memory 412 MB, heap 96 MB, heat fair",
        ], lines);
    }

    [Fact]
    public void TheReadingLinesSayTheStageAndTheTimeAndNothingTheScreenLacks()
    {
        var lines = DiagnosticsOverlay.Lines(new OverlayState(Stage: "S0.identify", StageTook: TimeSpan.FromSeconds(3.21), Elapsed: TimeSpan.FromSeconds(12.04), HeapMb: 80));
        Assert.Equal(["stage S0.identify, 3.2 s", "reading 12.0 s", "heap 80 MB"], lines);
        Assert.Empty(DiagnosticsOverlay.Lines(new OverlayState()));
        Assert.Equal("torch none", DiagnosticsOverlay.Text(new OverlayState(TorchOf: 0)));
        Assert.Equal("tilt 7.0 deg", DiagnosticsOverlay.Text(new OverlayState(TiltDegrees: 7)));
    }

    [Fact]
    public void TheFailingCheckIsTheFirstThatHoldsTheFrameInTheGuidancesOrder()
    {
        Assert.Null(DiagnosticsOverlay.Failing(CaptureGuidance.Judge(null, null, Quality(), true)));
        Assert.Equal("angle 41.0 deg", DiagnosticsOverlay.Failing(CaptureGuidance.Judge(null, null, Quality(degrees: 41), true)));
        Assert.Equal("focus 0.20", DiagnosticsOverlay.Failing(CaptureGuidance.Judge(null, null, Quality(focus: 0.2), true)));
        Assert.Equal("resolution 0.10", DiagnosticsOverlay.Failing(CaptureGuidance.Judge(null, null, Quality(degrees: 41, resolution: 0.1), true)));
        Assert.Equal("out of frame", DiagnosticsOverlay.Failing(CaptureGuidance.Judge(null, null, Quality(), false)));
        Assert.Equal("no sheet", DiagnosticsOverlay.Failing(new FrameVerdict(Instruction.FindTheSheet, "", false, false, false, false, false, false)));

        // Every check holds and the words still wait: the steadier held them.
        var held = CaptureGuidance.Judge(null, null, Quality(), true) with { Say = Instruction.HoldSteadier };
        Assert.Equal("steadiness", DiagnosticsOverlay.Failing(held));
    }

    [Fact]
    public void TheFrameRateIsCountedOverASecond()
    {
        var rate = new FrameRate();
        double? last = null;
        for (int i = 0; i <= 10; i++)
        {
            last = rate.Next(i * 100);
            if (i < 10)
            {
                Assert.Null(last);
            }
        }

        Assert.Equal(10.0, last!.Value, 3);
    }

    [Fact]
    public void TheStageAndTheHealthAreWhatTheLogAndTheOverlayRead()
    {
        ReadStage.Began("S2.detect");
        Assert.Equal("S2.detect", ReadStage.Now.Stage);
        var before = DeviceHealth.Heat;
        try
        {
            DeviceHealth.Heat = () => "serious";
            Assert.Equal(["memMb", "heapMb", "heat"], DeviceHealth.Fields().Select(f => f.Key));
            Assert.Equal("serious", DeviceHealth.Fields()[2].Value);
            Assert.Contains("stage S2.detect", DiagnosticsOverlay.Reading(TimeSpan.FromSeconds(1)), StringComparison.Ordinal);

            // A phone that cannot say how hot it is costs the line nothing.
            DeviceHealth.Heat = () => throw new InvalidOperationException("no thermal service");
            Assert.Null(DeviceHealth.HeatNow());
        }
        finally
        {
            DeviceHealth.Heat = before;
        }
    }

    [AvaloniaFact]
    public void TheSettingIsOffUntilTurnedOnAndPutsTheOverlayOverTheReadingScreen()
    {
        var phone = Started();
        var fresh = new AppSettingsStore(Path.Combine(phone.CacheFolder, "overlay-settings.json"));
        Assert.False(fresh.LoadShowDiagnostics());
        try
        {
            var view = new SettingsView(Phone.Settings);
            var window = new Window { Content = view };
            window.Show();
            var box = view.GetLogicalDescendants().OfType<CheckBox>().Single(c => AutomationProperties.GetAutomationId(c) == "settings-show-diagnostics");
            Assert.Equal("Show diagnostics on the camera", box.Content);
            box.IsChecked = false;
            Assert.False(DiagnosticsOverlay.On);
            var page = new TextBlock { Text = "Reading the sheet" };
            Assert.Same(page, DiagnosticsOverlay.Over(page, () => TimeSpan.Zero));

            box.IsChecked = true;
            Assert.True(DiagnosticsOverlay.On);
            var over = DiagnosticsOverlay.Over(new TextBlock(), () => TimeSpan.FromSeconds(4.5));
            var overlay = over.GetLogicalDescendants().OfType<Border>().Single(b => AutomationProperties.GetAutomationId(b) == "diagnostics-overlay");
            Assert.Contains("reading 4.5 s", ((TextBlock)overlay.Child!).Text, StringComparison.Ordinal);
            window.Close();
        }
        finally
        {
            Phone.Settings.SaveShowDiagnostics(false);
        }
    }
}
