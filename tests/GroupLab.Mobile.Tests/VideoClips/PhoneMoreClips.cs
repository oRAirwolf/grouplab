using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Tests.Support;
using OpenCvSharp;
#if GROUPLAB_PDF_PAGES
using SkiaSharp;
#endif
using Window = Avalonia.Controls.Window;
using static GroupLab.Mobile.Tests.VideoClips.PhoneClips;

namespace GroupLab.Mobile.Tests.VideoClips;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 481 part A: the phone's clips entry 475 left for a later run, made as <see cref="PhoneClips"/> makes the rest:
/// chapter 10's marking by hand under the fixed crosshair and two aim points in their colors (on a store-bought target the phone recognizes),
/// chapter 11's one-page report, and the tablet's result with the sheet beside the numbers.
/// </summary>
public class PhoneMoreClips
{
    private const string EightInch = "bc-34805-shoot-n-c-8in-bull";

    /// <summary>The store-bought 8 in bull, at 100 pixels an inch as a phone's picture might be, with two groups shot on it.</summary>
    private static (string Path, List<PointD> First, PointD Second, List<PointD> SecondShots) StoreTarget(string folder)
    {
        string blank = Path.Combine(@"C:\Dev\grouplab-local\commercial-targets", EightInch, "blank.png");
        Xunit.Assert.SkipWhen(!File.Exists(blank), "the store-bought target's scan is on Alan's computer only");
        using var scan = Cv2.ImRead(blank, ImreadModes.Color);
        using var picture = new Mat();
        Cv2.Resize(scan, picture, new OpenCvSharp.Size(0, 0), 1 / 6.0, 1 / 6.0, InterpolationFlags.Area);
        string plain = Path.Combine(folder, "plain.jpg");
        Cv2.ImWrite(plain, picture);
        var seen = PhoneAnalysis.Run(plain, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None).Recognized
            ?? throw new InvalidOperationException("the plain target was not recognized");
        var bull = seen.Recognition.Named!.Bulls(seen.Width, seen.Height)[0];

        // Five hits on the bull, showing the bright ring the target is sold for, and four on a dot stuck in the white corner.
        const double Ppi = 100;
        var first = new[] { (0.3, -0.2), (-0.4, 0.1), (0.1, 0.5), (-0.2, -0.45), (0.55, 0.25) }.Select(o => new PointD(bull.X + (o.Item1 * Ppi), bull.Y + (o.Item2 * Ppi))).ToList();
        var second = new PointD(Math.Max(90, bull.X - (3.6 * Ppi)), Math.Max(90, bull.Y - (3.6 * Ppi)));
        var secondShots = new[] { (0.2, 0.3), (-0.35, 0.15), (0.1, -0.4), (-0.1, 0.55) }.Select(o => new PointD(second.X + (o.Item1 * Ppi), second.Y + (o.Item2 * Ppi))).ToList();
        Cv2.Circle(picture, new OpenCvSharp.Point((int)second.X, (int)second.Y), 50, Scalar.All(25), -1, LineTypes.AntiAlias);
        foreach (var hit in first)
        {
            var at = new OpenCvSharp.Point((int)hit.X, (int)hit.Y);
            Cv2.Circle(picture, at, 24, new Scalar(60, 235, 200), -1, LineTypes.AntiAlias);
            Cv2.Circle(picture, at, 14, Scalar.All(30), -1, LineTypes.AntiAlias);
        }

        foreach (var hit in secondShots)
        {
            Cv2.Circle(picture, new OpenCvSharp.Point((int)hit.X, (int)hit.Y), 15, hit.X < second.X + 50 && hit.Y < second.Y + 50 && hit.X > second.X - 50 && hit.Y > second.Y - 50
                ? new Scalar(60, 235, 200) : Scalar.All(40), -1, LineTypes.AntiAlias);
        }

        string path = Path.Combine(folder, "store-bought target.jpg");
        Cv2.ImWrite(path, picture);
        return (path, first, second, secondShots);
    }

    /// <summary>Slides the picture under the fixed crosshair until <paramref name="to"/> is under it, eased at both ends.</summary>
    private static void Slide(ScreenClip clip, MarkingAPage.Viewer viewer, PointD to, double seconds = 0.7)
    {
        var from = viewer.Centre;
        int n = Math.Max(1, (int)Math.Round(seconds * ScreenClip.Fps));
        for (int i = 1; i <= n; i++)
        {
            double t = i / (double)n, eased = t * t * (3 - (2 * t));
            viewer.CentreOn(new PointD(from.X + ((to.X - from.X) * eased), from.Y + ((to.Y - from.Y) * eased)));
            ScreenClip.Settle();
            clip.Redraw();
            clip.Hold(1.0 / ScreenClip.Fps);
        }
    }

    private static MarkingAPage.Viewer ViewerOn(Window window) => window.GetVisualDescendants().OfType<MarkingAPage.Viewer>().Single(v => v.IsEffectivelyVisible);

    /// <summary>Marks each of <paramref name="holes"/> under the crosshair, as a person slides the picture and taps Add hole here.</summary>
    private static void MarkHoles(ScreenClip clip, Window window, IEnumerable<PointD> holes)
    {
        var viewer = ViewerOn(window);
        foreach (var hole in holes)
        {
            Slide(clip, viewer, hole);
            clip.Click(Find(window, "Add hole here"), hold: 0.4);
        }
    }

    /// <summary>
    /// Chapter 10: a store-bought target the phone names, its bull placed and its printed size the scale; its holes marked by hand under the
    /// fixed crosshair; then "+ Aim point" on the result, a second aim point and its holes, each aim point's holes in its own color.
    /// </summary>
    [AvaloniaFact]
    public void V2Ch10CrosshairAndTwoAimPoints()
    {
        ScreenClip.SkipUnlessAsked();
        var (window, shell) = Phone();
        string folder = Directory.CreateDirectory(Path.Combine(GroupLab.Mobile.Phone.Platform!.FilesFolder, "clip-store-target")).FullName;
        try
        {
            var (path, first, second, secondShots) = StoreTarget(folder);
            var setup = new ShotSetup(Calibre.Of(0.308), 3600);
            var result = PhoneAnalysis.Run(path, setup, UnitSettings.Imperial, null, CancellationToken.None);
            shell.ShowInPage(new ResultView(result, setup, UnitSettings.Imperial, () => { }));
            ScreenClip.Settle();
            HideOldErrorNote(window);
            ViewerOn(window).ZoomBy(2.5);
            ScreenClip.Settle();

            using (var clip = new ScreenClip(window, "v2-ch10-crosshair-marking", "A store-bought target named by the phone, its bull and scale set; each hole put under the fixed crosshair and added, then the result", touches: true))
            {
                clip.Hold(1.5);
                clip.Click(Find(window, "Next: the holes"), hold: 0.6);
                MarkHoles(clip, window, first);
                clip.Click(Find(window, "Done: measure"), hold: 3);
            }

            using (var clip = new ScreenClip(window, "v2-ch10-two-aim-points", "+ Aim point on the result: a second aim point set under the crosshair, its holes added, and each aim point's holes ringed in its own color", touches: true))
            {
                var add = Find(window, "+ Aim point");
                clip.Act(() => add.BringIntoView(), 1);
                clip.Click(add, hold: 0.6);
                var viewer = ViewerOn(window);
                viewer.ZoomBy(2.5);
                Slide(clip, viewer, second, 1.0);
                clip.Click(Find(window, "Add another aim point"), hold: 0.6);
                clip.Click(Find(window, "Next: the holes"), hold: 0.6);
                MarkHoles(clip, window, secondShots);
                clip.Click(Find(window, "Done: measure"), hold: 2);
                var chip = Find(window, "Aim 2");
                clip.Act(() => chip.BringIntoView(new Avalonia.Rect(0, 0, chip.Bounds.Width, chip.Bounds.Height + 400)), 1.5);
                if (clip.Middle(chip).Y > window.Bounds.Height - 40)
                {
                    throw new InvalidOperationException("the second aim point's chip is not on the screen");
                }

                clip.Click(chip, hold: 3);
                if (window.GetLogicalDescendants().OfType<BallisticsPage>().Any())
                {
                    throw new InvalidOperationException("the tap on Aim 2 opened Ballistics");
                }
            }
        }
        finally
        {
            Close(window);
            Temp.Delete(folder);
        }
    }

    /// <summary>Chapter 11: Report under the phone's result, the one dated page itself, with Share, Save on the phone and Print.</summary>
    [AvaloniaFact]
    public void V2Ch11OnePageReport()
    {
        ScreenClip.SkipUnlessAsked();
#if GROUPLAB_PDF_PAGES
        var (window, shell) = Phone();
        var phone = (TestPhone)GroupLab.Mobile.Phone.Platform!;
        phone.DrawPdf = (pdf, width) =>
        {
            using var page = PDFtoImage.Conversion.ToImage(pdf, 0, options: new PDFtoImage.RenderOptions { Width = width * 2, WithAspectRatio = true, BackgroundColor = SKColors.White });
            using var png = page.Encode(SKEncodedImageFormat.Png, 100);
            return [png.ToArray()];
        };
        try
        {
            Read(null, shell, window);
            var report = Find(window, "One dated page");
            report.BringIntoView();
            ScreenClip.Settle();
            using var clip = new ScreenClip(window, "v2-ch11-one-page-report", "Report under the phone's result: the one dated page itself, the picture, the plot, the figures and how sure, with Share, Save and Print", touches: true);
            clip.Click(report, hold: 3);
            var page = window.GetLogicalDescendants().OfType<Control>().FirstOrDefault(c => Avalonia.Automation.AutomationProperties.GetAutomationId(c) == "report-page")
                ?? throw new InvalidOperationException("the report page showed no picture of the page");
            clip.Act(() => page.BringIntoView(), 4);
        }
        finally
        {
            phone.DrawPdf = null;
            Close(window);
        }
#else
        Xunit.Assert.Skip("The clips are made on Windows, where the PDF's page can be drawn.");
#endif
    }

    /// <summary>The tablet held sideways: the result with the sheet on the left and the numbers beside it.</summary>
    [AvaloniaFact]
    public void V2TabletSideBySide()
    {
        ScreenClip.SkipUnlessAsked();
        var (window, shell) = Phone(wide: 1280, tall: 800, scale: 1.5);
        try
        {
            Read(null, shell, window);
            using var clip = new ScreenClip(window, "v2-tablet-side-by-side", "A tablet held sideways: the result with the sheet on the left and the group's figures beside it", touches: true);
            clip.Hold(3);
            if (window.GetLogicalDescendants().OfType<ScrollViewer>().Where(s => s.IsEffectivelyVisible).OrderByDescending(s => s.Extent.Height).FirstOrDefault() is { } scroller)
            {
                double end = Math.Min(scroller.Extent.Height - scroller.Viewport.Height, 800);
                for (int i = 1; i <= 120; i++)
                {
                    double t = i / 120.0;
                    scroller.Offset = new Avalonia.Vector(0, end * t * t * (3 - (2 * t)));
                    clip.Redraw();
                    clip.Hold(1.0 / ScreenClip.Fps);
                }
            }

            clip.Hold(2);
        }
        finally
        {
            Close(window);
        }
    }
}
