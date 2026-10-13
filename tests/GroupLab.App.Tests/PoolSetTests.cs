using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Cli.Imaging;
using GroupLab.Cli.Library;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 243 section 3.1 on the desktop: three sheets of a seven sheet set from "Made for your optic", each opened,
/// detected and analyzed as a person would, then ticked in Session records and pooled into one group that says which sheets are missing.
/// </summary>
public class PoolSetTests
{
    private const double Dpi = 300;

    [AvaloniaFact]
    public void TheChosenSheetsOfASetPoolIntoOneGroup()
    {
        var made = TargetGenerator.Generate(new GeneratorRequest(100, 4, null, 25, "letter"));
        var set = made.Design!.Definition!;
        var scene = SceneBuilder.Build(set);
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-pool-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        var store = new AppSettingsStore(Path.Combine(folder, "settings.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        try
        {
            var saved = new List<long>();
            foreach (int sheet in new[] { 4, 0, 2 })
            {
                var render = SceneRasterizer.Rasterize(scene.Pages[sheet], Dpi);
                var random = new Random(243 + sheet);
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
                window.OpenImage(path);
                window.ApplyDetection(result);
                window.Session.SetCalibre(Calibre.Of(0.308));
                window.Session.SetShotDistance(3600);
                window.CalibreAnswered();
                window.Analyse();
                Dispatcher.UIThread.RunJobs();
                saved.Add(Assert.IsType<long>(window.CurrentSession));
                Assert.Equal(sheet, window.Session.State.SetSheet);
            }

            window.ShowSessions();
            Dispatcher.UIThread.RunJobs();
            foreach (long id in saved)
            {
                window.ChooseSession(id, true);
            }

            int tabs = window.Targets.Count;
            window.PoolChosen();
            string said = string.Join(" | ", window.PoolText);
            Assert.Contains($"Sheets 1, 3 and 5 of {made.Sheets} are here", said, StringComparison.Ordinal);
            Assert.Contains("still missing", said, StringComparison.Ordinal);
            Assert.Contains("Mean radius 0.", said, StringComparison.Ordinal);

            // Entry 420 section 1: the pooled set opens on the analysis screen in a tab of its own, every shot about its own bull, each
            // sheet in its own shade with a key, and it is not saved as a session of its own.
            int perSheet = set.Bulls.Count(b => b.Scoring), records = window.Sessions!.List().Count;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(tabs + 1, window.Targets.Count);
            Assert.Same(window.Targets[^1], window.CurrentTarget);
            Assert.Equal(3 * perSheet, window.Plot.Shots.Count);
            Assert.Equal([0, 2, 4], window.Plot.PooledSheets);
            Assert.Contains(window.Plot.Legend, l => l.StartsWith($"Sheet 5: {perSheet} shots", StringComparison.Ordinal));
            Assert.All(window.Plot.Shots, s => Assert.Matches(@"^\d\.", s.Bull ?? ""));
            window.Plot.OnlySheet = 2;
            Assert.Contains(window.Plot.Legend, l => l.StartsWith($"Sheet 3: {perSheet} shots, shown alone", StringComparison.Ordinal));
            Assert.Null(window.CurrentSession);
            Assert.Equal(records, window.Sessions.List().Count);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }
}
