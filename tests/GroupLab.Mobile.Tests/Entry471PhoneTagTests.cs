using System.Collections.Immutable;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using Window = Avalonia.Controls.Window;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 471 section 2, question 107 (a): on the phone, holes found on a marking with no bulls carry no tag, as on the
/// computer, because a number beside a hole reads as a count (entry 376 section A6). A marking with bulls still tags each hole by its bull.
/// </summary>
public class Entry471PhoneTagTests
{
    [AvaloniaFact]
    public void FoundHolesOnAMarkingWithNoBullsCarryNoTag()
    {
        var shots = Enumerable.Range(1, 3).Select(i => new MarkedShot(i, new PointD(200 + (60 * i), 300), ShotProvenance.Automatic)).ToImmutableList();
        var state = new MarkingState(null, null, null, [], shots, shots.Count + 1);
        Assert.Empty(Drawn(state).Holes);
    }

    [AvaloniaFact]
    public void FoundHolesOnABullStillCarryItsNumber()
    {
        var bull = new BullAim(1, "1", new PointD(300, 300));
        var shots = ImmutableList.Create(new MarkedShot(1, new PointD(310, 300), ShotProvenance.Automatic, Bull: 1));
        var state = new MarkingState(null, null, null, [bull], shots, 2);
        Assert.Single(Drawn(state).Holes);
    }

    private static TagLayout Drawn(MarkingState state)
    {
        var window = new Window { Width = 412, Height = 915 };
        using var image = new WriteableBitmap(new PixelSize(800, 600), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        var viewer = new MarkingAPage.Viewer(image, new MarkingSession(state), []);
        window.Content = viewer;
        window.Show();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        Assert.NotNull(window.CaptureRenderedFrame());
        var (layout, _) = viewer.LastTags ?? throw new InvalidOperationException("the picture drew no tags");
        window.Close();
        return layout;
    }
}
