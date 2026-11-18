using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 499 section 2: Snap to holes, on by default, in the marking toolbar and on Shooting defaults. Off, a hole
/// placed by hand goes exactly where the pointer is let go; Alt held as it is let go does the same for one hole with the switch on; the
/// choice survives a restart. The picture is drawn from the measurements <c>InkBesideAHoleTests</c> rebuilds error report 50's frame from
/// (never its pixels): the black band of a diamond 50 to 97 pixels from its middle, and a ring hole beside it, its grey rim 17 to 26 pixels
/// from its centre round a light middle.
/// </summary>
public sealed class Entry499Tests : IDisposable
{
    private static readonly PointD Middle = new(220, 216), BesideTheDiamond = new(261.3, 118.5);

    private readonly string folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-499-{Guid.NewGuid():N}")).FullName;

    public void Dispose() => GroupLab.Tests.Support.Temp.Delete(folder);

    private string Picture()
    {
        string path = Path.Combine(folder, "diamond.png");
        using var image = new Mat(430, 440, MatType.CV_8UC3, new Scalar(255, 255, 255));
        for (int y = 0; y < 430; y++)
        {
            for (int x = 0; x < 440; x++)
            {
                double d = Math.Abs(x - Middle.X) + Math.Abs(y - Middle.Y);
                double r = Math.Sqrt(((x - BesideTheDiamond.X) * (x - BesideTheDiamond.X)) + ((y - BesideTheDiamond.Y) * (y - BesideTheDiamond.Y)));
                int level = d <= 97 && d > 50 ? 15 : r >= 17 && r <= 26 ? (int)(140 + (50 * Math.Sin(5 * Math.Atan2(y - BesideTheDiamond.Y, x - BesideTheDiamond.X)))) : 255;
                if (level < 255)
                {
                    image.Set(y, x, new Vec3b((byte)level, (byte)level, (byte)level));
                }
            }
        }

        Cv2.ImWrite(path, image);
        return path;
    }

    private MainWindow NewWindow(string settings)
    {
        var window = new MainWindow(new AppSettingsStore(settings)) { Width = 1400, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    /// <summary>Lets go of a new hole at an image point, with or without Alt, and returns where it was placed.</summary>
    private static PointD Place(MainWindow window, PointD image, bool alt)
    {
        var canvas = window.Canvas;
        canvas.Tool = MarkingTool.Impact;
        var at = canvas.TranslatePoint(canvas.ToControl(image), window)!.Value;
        var modifiers = alt ? RawInputModifiers.Alt : RawInputModifiers.None;
        window.MouseDown(at, MouseButton.Left, modifiers);
        window.MouseUp(at, MouseButton.Left, modifiers);
        Dispatcher.UIThread.RunJobs();
        return window.Session.State.Shots[^1].Image;
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    [AvaloniaFact]
    public void SnapToHolesIsOnByDefaultAndOffOrAltPlacesAHoleExactlyWhereItIsLetGo()
    {
        string settings = Path.Combine(folder, "settings.json");
        var window = NewWindow(settings);
        try
        {
            Assert.True(window.SnapsToHoles);
            window.OpenImage(Picture());
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            window.Canvas.FitToView();
            Dispatcher.UIThread.RunJobs();

            // A quarter of a hole down and left of the hole beside the diamond, toward the black.
            var click = new PointD(BesideTheDiamond.X - 9, BesideTheDiamond.Y + 9);
            var snapped = Place(window, click, alt: false);
            Assert.True(Distance(snapped, click) > 3, $"with the snap on, a click at {click} stayed at {snapped}");

            var withAlt = Place(window, click, alt: true);
            Assert.True(Distance(withAlt, click) < 0.01, $"with Alt held, a click at {click} went to {withAlt}");

            window.SetSnapToHoles(false);
            var off = Place(window, click, alt: false);
            Assert.True(Distance(off, click) < 0.01, $"with the snap off, a click at {click} went to {off}");
            Assert.False(window.SnapsToHoles);
        }
        finally
        {
            window.Close();
        }

        var again = NewWindow(settings);
        try
        {
            Assert.False(again.SnapsToHoles);
            again.SetSnapToHoles(true);
        }
        finally
        {
            again.Close();
        }

        var third = NewWindow(settings);
        try
        {
            Assert.True(third.SnapsToHoles);
        }
        finally
        {
            third.Close();
        }
    }
}
