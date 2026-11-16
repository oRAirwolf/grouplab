using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Cli.Imaging;
using GroupLab.Cli.Library;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
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

            // Error report 46: Back, then Accept and analyze, saved the pool as a session of one sheet holding every sheet's shots. Entry 495
            // (question 113, Alan's (b)): it is saved as a pool record of its own instead, naming its three sheets; no session is added.
            Assert.True(window.ShowingPool);
            Assert.Equal(SetTabWords.PoolNotSavedYet, window.SavedText);
            window.BackToEditor();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(SessionPools.SavedWords(3), window.StatusText);
            Assert.Equal(SetTabWords.PoolRecordSaved, window.SavedText);
            long poolId = Assert.IsType<long>(window.CurrentPool);
            var pool = Assert.Single(window.Sessions.Pools());
            Assert.Equal(poolId, pool.Id);
            Assert.Equal(saved.Select(id => DataExport.Identity(window.Sessions.Get(id)!)).Order(), pool.Sheets.Order());
            window.Session.SetExclusion(window.Session.State.Shots[0].Id, ExclusionReason.ByShooter);
            Assert.False(window.HasUnsavedWork);
            window.SaveWaitingNow();
            Assert.Null(window.CurrentSession);
            Assert.Equal(records, window.Sessions.List().Count);
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            Assert.Single(window.Sessions.Pools());

            // Session records shows it as one row, a pooled set of 3 sheets dated by its newest sheet, and it opens again into the pooled
            // tab with the same shots, worked out again from its sheets.
            var row = Assert.Single(SessionPools.Rows(window.Sessions, UnitSettings.Imperial)).Row;
            Assert.StartsWith(SessionPools.RowName(3), row.SheetName, StringComparison.Ordinal);
            Assert.Equal(SessionPools.RowId(poolId), row.Id);
            Assert.Equal(3 * perSheet, row.ShotCount);
            window.OpenSavedPool(poolId);
            Dispatcher.UIThread.RunJobs();
            Assert.True(window.ShowingPool);
            Assert.Equal(poolId, window.CurrentPool);
            Assert.Equal(3 * perSheet, window.Plot.Shots.Count);
            Assert.Equal([0, 2, 4], window.Plot.PooledSheets);

            // Entry 495 section 4: shared as one file with its sheets, it opens whole on a second GroupLab, the sheets brought in as sessions
            // first; opened again there, nothing is made twice. Export is still the pooled shots' CSV.
            Assert.Equal(["*.csv"], Assert.Single(MainWindow.ExportChoices(pool: true)).Patterns!);
            Assert.Equal(2, MainWindow.ExportChoices(pool: false).Count);
            string file = Path.Combine(folder, "pool" + SessionPackage.Extension);
            window.SharePoolTo(file);
            Assert.Equal(SessionPools.SharedWords(3), window.StatusText);
            string elsewhere = Path.Combine(folder, "elsewhere");
            Directory.CreateDirectory(elsewhere);
            var second = new MainWindow(new AppSettingsStore(Path.Combine(elsewhere, "settings.json"))) { Width = 1400, Height = 900 };
            second.Show();
            try
            {
                second.OpenSessionFile(file);
                Dispatcher.UIThread.RunJobs();
                Assert.True(second.ShowingPool);
                Assert.Equal(3, second.Sessions!.List().Count);
                Assert.Equal(3, Assert.Single(second.Sessions.Pools()).Sheets.Count);
                Assert.Equal(3 * perSheet, second.Plot.Shots.Count);
                Assert.Contains("the pool is saved with them", second.StatusText, StringComparison.Ordinal);
                second.OpenSessionFile(file);
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(3, second.Sessions.List().Count);
                Assert.Single(second.Sessions.Pools());
            }
            finally
            {
                second.Close();
            }

            // The same tab given a sheet of its own saves again.
            window.OpenSession(saved[0]);
            Dispatcher.UIThread.RunJobs();
            Assert.False(window.ShowingPool);
            Assert.Equal(saved[0], window.CurrentSession);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }
}
