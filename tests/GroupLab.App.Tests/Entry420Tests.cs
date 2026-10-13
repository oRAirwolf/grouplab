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
/// NOTES-FROM-PLANNING.md entry 420, from Quack's question on macOS. Section 2: three scans of one set dropped together opened only the first;
/// each now opens in a tab of its own, and once they are accepted the analysis screen offers to read them as one set, which opens the pooled
/// tab. Section 3: the same picture opened afresh and accepted again made a second record of the same sheet; it now updates the first.
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
    public async Task SheetsDroppedTogetherOpenOneTabEachAndReadAsOneSet()
    {
        var set = TargetGenerator.Generate(new GeneratorRequest(100, 4, null, 25, "letter")).Design!.Definition!;
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-420-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        var store = new AppSettingsStore(Path.Combine(folder, "settings.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900, DetectOnOpen = false };
        window.Show();
        try
        {
            var sheets = new[] { 1, 0, 3 }.Select(k => Sheet(set, folder, k)).ToList();
            int tabs = window.Targets.Count;
            window.OpenDropped([.. sheets.Select(x => x.Path)]);
            for (int i = 0; i < 400 && window.Targets.Count < tabs + 2; i++)
            {
                Dispatcher.UIThread.RunJobs();
                await Task.Delay(25);
            }

            await window.Opening;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(tabs + 2, window.Targets.Count);

            // Each tab holds its own picture; accepted in turn, the third offers to read them as one set.
            for (int k = 0; k < 3; k++)
            {
                window.ShowTarget(window.Targets.Count - 3 + k);
                Dispatcher.UIThread.RunJobs();
                Assert.EndsWith($"sheet-{new[] { 1, 0, 3 }[k]}.png", window.Session.State.ImagePath, StringComparison.Ordinal);
                Accept(window, sheets[k].Result);
            }

            Assert.Equal(3, window.DroppedSetSessions().Count);
            int before = window.Targets.Count;
            window.ReadDroppedAsOneSet();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(before + 1, window.Targets.Count);
            Assert.Equal([0, 1, 3], window.Plot.PooledSheets);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
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
