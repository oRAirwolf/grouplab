using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Marking;
using OpenCvSharp;
using Window = Avalonia.Controls.Window;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 465 section 1A on the phone: Fix holes places its bull numbers and hole tags with the computer's placing, outside
/// the rings the sheet's design gives, with no tag on another tag, a bull's number or a hole, before a hole is picked up and while it is held.
/// </summary>
public class Entry465PhoneMarkingTests
{
    private static readonly TestPhone ThePhone = new();

    [AvaloniaFact]
    public void FixHolesTagsLieOnNothingElseAndAHeldHoleKeepsItsRingClear()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        var window = new Window { Width = 412, Height = 915 };
        window.Show();
        using (var sheet = Cv2.ImRead(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), ImreadModes.Color))
        using (var half = new Mat())
        {
            Cv2.Resize(sheet, half, new OpenCvSharp.Size(0, 0), 0.5, 0.5, InterpolationFlags.Area);
            Cv2.ImWrite(Path.Combine(ThePhone.CacheFolder, "entry465.png"), half);
        }

        var result = PhoneAnalysis.Run(Path.Combine(ThePhone.CacheFolder, "entry465.png"), new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
        Assert.Null(result.Failure);
        var view = new ResultView(result, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { });
        window.Content = view;
        Dispatcher.UIThread.RunJobs();
        var fix = view.GetVisualDescendants().OfType<Button>().Single(b => Avalonia.Automation.AutomationProperties.GetAutomationId(b) == "result-fix-holes");
        fix.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        var page = view.GetVisualDescendants().OfType<FixHolesPage>().Single();
        var viewer = page.Picture;
        Assert.NotEmpty(viewer.BullRings);

        void Clear(string when)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            Assert.NotNull(window.CaptureRenderedFrame());
            var (layout, holes) = viewer.LastTags ?? throw new InvalidOperationException("the picture drew no tags");
            Assert.Equal(page.State.Bulls.Count, layout.Bulls.Count);
            var overlaps = MarkTags.Overlaps(layout, holes);
            Assert.True(overlaps.Count == 0, when + ": " + string.Join("; ", overlaps));
        }

        Clear("as it opens");
        var held = page.State.Shots.First(s => s.IsShot);
        viewer.CentreOn(held.Image);
        viewer.Held = held.Id;
        Clear("with a hole held");
        window.Close();
    }
}
