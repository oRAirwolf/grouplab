using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 42 section 7 points 2 and 3: the marking window photographed in dark and light, for planning to set beside
/// <c>docs/figures/screens</c>, and a close-up of impacts on bare paper, on a printed ring and on a printed numeral of the rendered Phase 0
/// sheet, to show the marks read on all three. The frames are written to <c>out/screens</c>, which git ignores, and a person reads them;
/// nothing here asserts what they look like. With <c>GROUPLAB_SCREENSHOT_PHOTO</c> naming a photograph, marks on it are photographed too.
/// </summary>
public class ScreenshotTests
{
    private static string Repository([CallerFilePath] string here = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    /// <summary>The middle of every dark run of the artwork crossed walking from a point along a direction, with where the run starts and how long it is, pixels.</summary>
    private static List<(PointD Middle, double Start, double Length)> DarkRuns(GrayImage artwork, PointD from, double dx, double dy, double reach)
    {
        var runs = new List<(PointD, double, double)>();
        double? start = null;
        for (double t = 0; t <= reach; t++)
        {
            int x = (int)Math.Round(from.X + (dx * t)), y = (int)Math.Round(from.Y + (dy * t));
            bool dark = x >= 0 && y >= 0 && x < artwork.Width && y < artwork.Height && artwork.Pixels[(y * artwork.Width) + x] < 128;
            if (dark && start is null)
            {
                start = t;
            }
            else if (!dark && start is { } s)
            {
                runs.Add((new PointD(from.X + (dx * (s + t) / 2), from.Y + (dy * (s + t) / 2)), s, t - s));
                start = null;
            }
        }

        return runs;
    }

    [AvaloniaFact]
    public void TheMarkingWindowIsPhotographedInDarkAndLightWithImpactsOnPaperARingAndANumeral()
    {
        string repository = Repository();
        string output = Path.Combine(repository, "out", "screens");
        Directory.CreateDirectory(output);
        string image = Path.Combine(repository, "scans", "phase0", "gl-cf25-ltr-1-600-dpi.png");
        var definition = GltdJsonReader.ReadFile(Path.Combine(repository, "targets", "frozen", "phase0", "GL-YCSK-DZZ1-R0VJ-4T5Y.gltd.json")).Definition!;
        var (grey, metadata) = ImageLoader.Load(image);
        var (value, _) = ImageLoader.LoadMaxChannel(image);
        var result = AutomaticMarking.Run(grey, value, metadata, definition, new OpenCvSharpBackend());
        Assert.Null(result.Failure);
        var artwork = result.ExpectedArtwork!;
        var bull = result.Bulls.First(b => b.Label == "13");
        double pixelsPerInch = HoleSize.PixelsPerInch(result.Scale!, bull.Image);

        // A printed ring: the first dark run to the right of the bull's centre that does not start at the centre itself. A printed numeral:
        // the furthest dark run to the left within an inch, which is the label left of the outer disc, since the next bull's rings begin
        // further out than that. Bare paper: past the last dark run on the diagonal, inside the bull's cell.
        var right = DarkRuns(artwork, bull.Image, 1, 0, 0.7 * pixelsPerInch);
        var ring = right.First(r => r.Start > 2).Middle;
        var numeral = DarkRuns(artwork, bull.Image, -1, 0, 1.0 * pixelsPerInch)[^1].Middle;
        double diagonal = Math.Sqrt(0.5);
        var outward = DarkRuns(artwork, bull.Image, diagonal, diagonal, 0.9 * pixelsPerInch);
        double pastInk = outward.Count == 0 ? 0.3 * pixelsPerInch : outward[^1].Start + outward[^1].Length + (0.12 * pixelsPerInch);
        var paper = new PointD(bull.Image.X + (diagonal * pastInk), bull.Image.Y + (diagonal * pastInk));

        string settings = Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json");
        try
        {
            var store = new AppSettingsStore(settings);
            store.SaveUnits(UnitSettings.Imperial);
            var window = new MainWindow(store) { Width = 1400, Height = 900 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.OpenImage(image);
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();

            window.ApplyDetection(result);
            window.Session.SetCalibre(new Calibre(".308", 0.308));
            window.Session.SetPointOfAim(bull.Image);
            foreach (var at in new[] { paper, ring, numeral })
            {
                window.Session.AddShot(at);
            }

            void Capture(string name)
            {
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
                var frame = window.CaptureRenderedFrame();
                Assert.NotNull(frame);
                string file = Path.Combine(output, name);
                frame.Save(file, new PngBitmapEncoderOptions());
                Assert.True(new FileInfo(file).Length > 0, file);
            }

            foreach (var (theme, name) in new[] { (ThemeChoice.Dark, "dark"), (ThemeChoice.Light, "light") })
            {
                window.SetTheme(theme);
                window.Canvas.FitToView();
                Capture($"marking-{name}.png");
                window.Canvas.ShowAt(bull.Image, window.Canvas.Bounds.Height / (2.4 * pixelsPerInch));
                Capture($"marks-closeup-{name}.png");
            }

            if (Environment.GetEnvironmentVariable("GROUPLAB_SCREENSHOT_PHOTO") is { Length: > 0 } photo && File.Exists(photo))
            {
                window.SetTheme(ThemeChoice.Dark);
                window.OpenImage(photo);
                Dispatcher.UIThread.RunJobs();
                var (photoGrey, _) = ImageLoader.Load(photo);
                window.Session.SetScale(new LengthReference(new PointD(0, 0), new PointD(photoGrey.Width / 18.0, 0), 1));
                window.Session.SetCalibre(new Calibre(".308", 0.308));
                foreach (var (fx, fy) in new[] { (0.30, 0.15), (0.50, 0.22), (0.70, 0.12), (0.42, 0.55), (0.58, 0.64) })
                {
                    window.Session.AddShot(new PointD(photoGrey.Width * fx, photoGrey.Height * fy));
                }

                window.Canvas.FitToView();
                Capture("photo-marks-dark.png");
            }

            window.Close();
        }
        finally
        {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
            File.Delete(settings);
        }
    }
}
