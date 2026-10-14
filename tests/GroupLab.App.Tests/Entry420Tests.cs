using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Cli.Imaging;
using GroupLab.Cli.Library;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 420, from Quack's question on macOS. Section 3: the same picture opened afresh and accepted again made a second
/// record of the same sheet; it now updates the first. Section 2's drop of several pictures, a tab each, was replaced by entry 423: they open
/// in one tab, pooled (Entry423Tests).
/// </summary>
public class Entry420Tests
{
    private const double Dpi = 300;

    private static (string Path, AutomaticResult Result) Sheet(TargetDefinition set, string folder, int sheet)
    {
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(set).Pages[sheet], Dpi);
        var random = new Random(420 + sheet);
        var holes = set.Bulls.Where(b => b.Scoring)
            .Select(b => SyntheticSheet.SampleHole(random, b.X + random.Next(-60, 61), b.Y + random.Next(-60, 61), onInk: false, HoleBacking.ScannerLid, 1.0)).ToList();
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var image = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], random);
        string path = Path.Combine(folder, $"sheet-{sheet}.png");
        using (var mat = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels))
        {
            Cv2.ImWrite(path, mat);
        }

        var (grey, metadata) = ImageLoader.Load(path);
        var (value, _) = ImageLoader.LoadMaxChannel(path);
        var result = AutomaticMarking.Run(grey, value, metadata, set, new OpenCvSharpBackend(), calibre: Calibre.Of(0.308));
        Assert.Null(result.Failure);
        return (path, result);
    }

    private static void Accept(MainWindow window, AutomaticResult result)
    {
        window.ApplyDetection(result);
        window.Session.SetCalibre(Calibre.Of(0.308));
        window.Session.SetShotDistance(3600);
        window.CalibreAnswered();
        window.Analyse();
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void TheSamePictureAcceptedAgainUpdatesItsRecord()
    {
        var set = TargetGenerator.Generate(new GeneratorRequest(100, 4, null, 25, "letter")).Design!.Definition!;
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-420-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        var store = new AppSettingsStore(Path.Combine(folder, "settings.json"));
        var window = new MainWindow(store) { Width = 1400, Height = 900, DetectOnOpen = false };
        window.Show();
        try
        {
            var (path, result) = Sheet(set, folder, 2);
            window.OpenImage(path);
            Accept(window, result);
            long first = Assert.IsType<long>(window.CurrentSession);
            int records = window.Sessions!.List().Count;

            window.OpenImage(path);
            Assert.Null(window.CurrentSession);
            Accept(window, result);
            Assert.Equal(first, window.CurrentSession);
            Assert.Equal(records, window.Sessions.List().Count);
            Assert.Contains("brought up to date rather than made twice", window.StatusText, StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }
}
