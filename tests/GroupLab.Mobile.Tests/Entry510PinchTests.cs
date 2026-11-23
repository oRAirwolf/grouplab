using System.Collections.Immutable;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 510 section 1, error report 53's reply with a screen recording on an iPhone, build 238. Two things on the
/// result's picture: once it had been pinched in and let go, a second pinch could neither enlarge it further nor make it smaller, because
/// every touch on an enlarged picture was kept from the gesture recognizers (so the page does not scroll under a drag, entry 374 section 4),
/// the pinch's own included; and the holes' circles stayed the same size on screen while the picture grew, so they shrank inside the holes.
/// Fix holes had the same pinch fault. These press real touch fingers on the picture, as iOS and Android deliver them.
/// </summary>
public class Entry510PinchTests
{
    private static ulong time;

    private static readonly PointerPointProperties Touching = new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed);

    private static void Press(Control on, Pointer finger, Point at) =>
        on.RaiseEvent(new PointerPressedEventArgs(on, finger, TopLevel.GetTopLevel(on)!, at, ++time, Touching, KeyModifiers.None));

    private static void Move(Control on, Pointer finger, Point at) =>
        on.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, on, finger, TopLevel.GetTopLevel(on)!, at, ++time, Touching, KeyModifiers.None));

    private static void Lift(Control on, Pointer finger, Point at) =>
        on.RaiseEvent(new PointerReleasedEventArgs(on, finger, TopLevel.GetTopLevel(on)!, at, ++time,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));

    /// <summary>Two fingers down about the middle, <paramref name="from"/> apart, moved in steps to <paramref name="to"/> apart, then lifted.</summary>
    private static void Pinch(Control on, double from, double to)
    {
        var middle = new Point(on.Bounds.Width / 2, on.Bounds.Height / 2);
        var one = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        var two = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, false);
        Press(on, one, middle - new Point(from / 2, 0));
        Press(on, two, middle + new Point(from / 2, 0));
        double spread = from;
        for (int step = 1; step <= 10; step++)
        {
            spread = from + ((to - from) * step / 10);
            Move(on, one, middle - new Point(spread / 2, 0));
            Move(on, two, middle + new Point(spread / 2, 0));
        }

        Lift(on, one, middle - new Point(spread / 2, 0));
        Lift(on, two, middle + new Point(spread / 2, 0));
    }

    private static void Drawn(Window window)
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        Assert.NotNull(window.CaptureRenderedFrame());
    }

    private static MarkingState Sheet(ImmutableList<MarkedShot> shots, ImmutableList<BullAim> bulls) =>
        new(null, new LengthReference(new PointD(0, 0), new PointD(1000, 0), 10), null, bulls, shots, shots.Count + 1, Calibre: Calibre.Of(0.284));

    private static (ResultView.SheetPicture Picture, Window Window, MarkingState State) Result()
    {
        var bulls = ImmutableList.Create(new BullAim(1, "1", new PointD(400, 400)));
        var shots = ImmutableList.Create(new MarkedShot(1, new PointD(410, 400), ShotProvenance.Automatic, Bull: 1));
        var state = Sheet(shots, bulls);
        var bitmap = new WriteableBitmap(new PixelSize(800, 800), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        var picture = new ResultView.SheetPicture(bitmap, 0, () => shots, bulls: () => bulls) { Marking = () => state };
        var window = new Window { Width = 400, Height = 400, Content = picture };
        window.Show();
        Drawn(window);
        return (picture, window, state);
    }

    [AvaloniaFact]
    public void TheResultPictureOncePinchedInPinchesFurtherInAndBackOutToFitted()
    {
        var (picture, window, _) = Result();
        try
        {
            picture.ZoomBy(3);
            Assert.Equal(3, picture.Zoom, 3);

            Pinch(picture, 100, 200);
            Assert.Equal(6, picture.Zoom, 1);

            Pinch(picture, 300, 50);
            Assert.Equal(1, picture.Zoom, 6);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ALoneFingerOnTheEnlargedResultPictureStillMovesItAndDoesNotZoom()
    {
        var (picture, window, _) = Result();
        try
        {
            picture.ZoomBy(4);
            var finger = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
            Press(picture, finger, new Point(200, 200));
            Move(picture, finger, new Point(150, 150));
            Lift(picture, finger, new Point(150, 150));
            Assert.Equal(4, picture.Zoom, 3);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheHolesCirclesKeepTheirFittedSizeAndGrowToTheBulletOnceEnlarged()
    {
        var (picture, window, state) = Result();
        try
        {
            var hole = state.Shots[0].Image;
            Assert.Equal(9, picture.HoleRadius(hole));

            picture.ZoomBy(8);
            // The picture is 800 pixels fitted into 400 units, 100 pixels to the inch, so 50 units an inch fitted and 400 at eight times.
            Assert.Equal(0.284 / 2 * 400, picture.HoleRadius(hole), 3);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FixHolesOncePinchedInPinchesBackOut()
    {
        var shots = ImmutableList.Create(new MarkedShot(1, new PointD(410, 400), ShotProvenance.Automatic, Bull: 1));
        var state = Sheet(shots, ImmutableList.Create(new BullAim(1, "1", new PointD(400, 400))));
        var image = new WriteableBitmap(new PixelSize(800, 800), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        var viewer = new MarkingAPage.Viewer(image, new MarkingSession(state), []) { RingInches = 0.284 };
        var window = new Window { Width = 400, Height = 400, Content = viewer };
        window.Show();
        try
        {
            Drawn(window);
            double fitted = viewer.HoleRadius(shots[0].Image);
            viewer.ZoomBy(4);
            Assert.Equal(fitted * 4, viewer.HoleRadius(shots[0].Image), 3);

            Pinch(viewer, 400, 100);
            Assert.Equal(fitted, viewer.HoleRadius(shots[0].Image), 3);
        }
        finally
        {
            window.Close();
        }
    }
}
