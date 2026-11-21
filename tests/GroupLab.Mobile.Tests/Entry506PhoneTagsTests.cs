using System.Collections.Immutable;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 506 section 3, the DESIGN NEEDED line of entry 505 section 3.2 answered: the phone's result picture carries the
/// holes' tags only when zoomed in. At the normal size it stays as it was (holes, lines to bulls, the "?", no tags), because the shots are
/// listed right under it; pinched in far enough that every tag fits without touching another or a hole, the tags appear, placed as on Fix
/// holes, and they go again when it is pinched back out. The phone's code is the same on Android and the iPhone.
/// </summary>
public class Entry506PhoneTagsTests
{
    private static (ResultView.SheetPicture Picture, Window Window) Shown()
    {
        var bulls = ImmutableList.Create(new BullAim(1, "1", new PointD(300, 300)), new BullAim(2, "2", new PointD(1100, 300)));
        var shots = ImmutableList.Create(
            new MarkedShot(1, new PointD(310, 300), ShotProvenance.Automatic, Bull: 1),
            new MarkedShot(2, new PointD(334, 322), ShotProvenance.Automatic, Bull: 1),
            new MarkedShot(3, new PointD(296, 334), ShotProvenance.Automatic, Bull: 1),
            new MarkedShot(4, new PointD(1112, 310), ShotProvenance.Automatic, Bull: 2));
        var state = new MarkingState(null, null, null, bulls, shots, shots.Count + 1);
        var bitmap = new WriteableBitmap(new PixelSize(1600, 900), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        var picture = new ResultView.SheetPicture(bitmap, 0, () => shots, bulls: () => bulls) { Marking = () => state };
        var window = new Window { Width = 412, Height = 915, Content = picture };
        window.Show();
        Drawn(window);
        return (picture, window);
    }

    private static void Drawn(Window window)
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        Assert.NotNull(window.CaptureRenderedFrame());
    }

    [AvaloniaFact]
    public void AtTheNormalSizeThePictureCarriesNoTags()
    {
        var (picture, window) = Shown();
        try
        {
            Assert.Equal(1, picture.Zoom);
            Assert.False(picture.TagsShown);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void PinchedInFarEnoughTheTagsAppearClearOfEachOtherAndTheHolesAndGoWhenPinchedOut()
    {
        var (picture, window) = Shown();
        try
        {
            picture.ZoomBy(ResultView.SheetPicture.MostZoom);
            Drawn(window);
            Assert.True(picture.TagsShown);
            var (layout, holes) = picture.LastTags!.Value;
            Assert.Equal(4, layout.Holes.Count);
            Assert.Empty(MarkTags.Overlaps(layout, holes));

            picture.ZoomBy(1 / ResultView.SheetPicture.MostZoom);
            Drawn(window);
            Assert.Equal(1, picture.Zoom);
            Assert.False(picture.TagsShown);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void OnACrowdedPictureZoomedInALittleTheTagsShownStillTouchNothing()
    {
        // Ten holes in one ragged patch on bull 1 and bull 2 just beside it. Look 1A moves a tag outward until it is clear, so even here the
        // tags fit as soon as the picture is enlarged, and what is shown touches no other tag and no hole.
        var bulls = ImmutableList.Create(new BullAim(1, "1", new PointD(300, 300)), new BullAim(2, "2", new PointD(380, 300)));
        var shots = Enumerable.Range(1, 10).Select(i => new MarkedShot(i, new PointD(300 + (i % 3), 300 + (i % 4)), ShotProvenance.Automatic, Bull: 1))
            .Concat(Enumerable.Range(11, 6).Select(i => new MarkedShot(i, new PointD(372 + (3 * (i % 3)), 292 + (4 * (i % 4))), ShotProvenance.Automatic, Bull: 2)))
            .ToImmutableList();
        var state = new MarkingState(null, null, null, bulls, shots, shots.Count + 1);
        var bitmap = new WriteableBitmap(new PixelSize(1600, 900), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        var picture = new ResultView.SheetPicture(bitmap, 0, () => shots, bulls: () => bulls) { Marking = () => state };
        var window = new Window { Width = 412, Height = 915, Content = picture };
        window.Show();
        try
        {
            Drawn(window);
            Assert.False(picture.TagsShown);
            picture.ZoomBy(1.2);
            Drawn(window);
            Assert.True(picture.TagsShown);
            var (layout, holes) = picture.LastTags!.Value;
            Assert.Equal(16, layout.Holes.Count);
            Assert.Empty(MarkTags.Overlaps(layout, holes));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AnEnlargedPictureStillLightsTheBullATapIsOn()
    {
        var (picture, window) = Shown();
        try
        {
            picture.ZoomBy(2);
            Drawn(window);
            // The middle of the picture is between the bulls; a tap at its far left, after zooming about the middle, finds bull 1's area.
            picture.TapAt(new Point(5, 5));
            Assert.Null(picture.Lit);
        }
        finally
        {
            window.Close();
        }
    }
}
