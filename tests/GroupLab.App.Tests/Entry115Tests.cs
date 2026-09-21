using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.App.Tests;

/// <summary>NOTES-FROM-PLANNING.md entry 115, driven headlessly against the scanned-sheet fixture of entry 109.</summary>
public class Entry115Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    /// <summary>
    /// Entry 115 section 2, which answers question 27: a load is set on the bulls chosen on the sheet, several at once, from the loads the
    /// records carry. The bulls sharing a load are a subgroup, a bull with no load belongs to none and the panel says how many those are, and
    /// the marking keeps it, so a session reopens with its subgroups and the comparison screen has them.
    /// </summary>
    [AvaloniaFact]
    public void ALoadIsSetOnSeveralBullsAtOnceAndBecomesTheirSubgroup()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Book = RecordBook.Empty.With(new Load("41.5 gr", null)).With(new Load("42.1 gr", null));
            window.Session.SetShotDistance(3600);
            Settle();

            // The field offers the records' loads and nothing else.
            var field = window.GetLogicalDescendants().OfType<ComboBox>().First(c => c.ItemsSource is IEnumerable<string> items && items.Contains("41.5 gr"));
            Assert.Equal(["No load", "41.5 gr", "42.1 gr"], field.ItemsSource!.Cast<string>());
            Assert.Contains(window.BullLoadText, t => t.StartsWith("No bulls chosen.", StringComparison.Ordinal));

            // Clicking a bull chooses it, and shift takes a row of them.
            var canvas = window.Canvas;
            canvas.Tool = MarkingTool.Select;
            canvas.FitToView();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Settle();
            var bulls = window.Session.State.Bulls.Where(b => b.Scoring).Take(5).ToList();
            void Click(PointD image, RawInputModifiers modifiers = RawInputModifiers.None)
            {
                var at = canvas.TranslatePoint(canvas.ToControl(image), window)!.Value;
                window.MouseDown(at, MouseButton.Left, modifiers);
                window.MouseUp(at, MouseButton.Left, modifiers);
                Settle();
            }

            // Every bull on this sheet holds a shot, so a plain click there is the shot; shift and click chooses the bull itself.
            Click(bulls[0].Image);
            Assert.Empty(window.ChosenBulls);
            Assert.Equal(window.Session.State.Shots.First(s => s.Bull == bulls[0].Index).Id, window.Canvas.Selected);
            foreach (var bull in bulls)
            {
                Click(bull.Image, RawInputModifiers.Shift);
            }

            Assert.Equal(bulls.Select(b => b.Index).Order(), window.ChosenBulls.Order());
            Assert.Contains(window.BullLoadText, t => t.StartsWith("5 bulls chosen: ", StringComparison.Ordinal));

            window.PickBullLoad("41.5 gr");
            window.SetLoadOnChosenBulls("41.5 gr");
            Settle();
            var map = window.Session.State.Subgroups!;
            Assert.All(bulls, b => Assert.Equal("41.5 gr", map.For(b.Index)));
            Assert.Contains(window.BullLoadText, t => t == "41.5 gr: 5 bulls");
            int scoring = window.Session.State.Bulls.Count(b => b.Scoring);
            Assert.Contains(window.BullLoadText, t => t == $"{scoring - 5} of {scoring} bulls carry no load, and belong to no subgroup.");

            // A second load on the next five, and the two are subgroups the comparison screen takes.
            var next = window.Session.State.Bulls.Where(b => b.Scoring).Skip(5).Take(5).ToList();
            window.ChooseBulls([.. next.Select(b => b.Index)]);
            window.SetLoadOnChosenBulls("42.1 gr");
            Settle();
            Assert.Equal(["41.5 gr", "42.1 gr"], window.Session.State.Subgroups!.Names);

            // It is kept in the marking, so a saved session reopens with its subgroups.
            window.CalibreAnswered();
            window.Analyse();
            Settle();
            long id = window.CurrentSession!.Value;
            window.OpenSession(id);
            Settle();
            Assert.Equal(["41.5 gr", "42.1 gr"], window.Session.State.Subgroups!.Names);
            window.CompareSubgroups();
            Settle();
            Assert.Equal(["41.5 gr", "42.1 gr"], window.Comparison!.Groups.Select(g => g.Name));

            // Taking the load off leaves those bulls in no subgroup.
            window.ChooseBulls([.. next.Select(b => b.Index)]);
            window.SetLoadOnChosenBulls(null);
            Settle();
            Assert.Equal(["41.5 gr"], window.Session.State.Subgroups!.Names);
            window.Close();
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    /// <summary>
    /// Entry 115 section 3: a string of velocities entered by hand, reconciled with the shots rather than assumed to line up with them. With
    /// the counts equal the in-order pairing is a proposal; a reading that belongs to no shot is marked and the rest pair in order; what is
    /// accepted is kept on the session; and the readings' own spread is set on the load with a note of where it came from.
    /// </summary>
    [AvaloniaFact]
    public void AChronographStringIsReconciledWithTheShotsAndItsSpreadReachesTheLoad()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Book = RecordBook.Empty.With(new Load("H4350 41.5", null));
            window.Session.SetShotDistance(3600);
            window.Session.SetEquipment(null, null, "H4350 41.5");
            window.ShowBallistics();
            Settle();
            Assert.Contains(window.ChronographText, t => t.StartsWith("Accept and analyse a sheet first", StringComparison.Ordinal));

            window.ShowBallistics(false);
            window.CalibreAnswered();
            window.Analyse();
            Settle();
            long id = window.CurrentSession!.Value;
            int shots = window.Session.State.Shots.Count(s => s.IsShot);
            window.ShowBallistics();
            Settle();
            Assert.Contains(window.ChronographText, t => t == "Paste a string of velocities and press Read the list.");

            // A list that is not velocities is refused by what it names.
            window.ReadChronograph("Garmin Xero C1", "2026-09-20", "2705 fps");
            Settle();
            Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(), t => (t.Text ?? "").Contains("is not a velocity", StringComparison.Ordinal));

            // One reading for every shot: the pairing is in order, and it says the counts agree without calling it a fact.
            var readings = Enumerable.Range(0, shots).Select(i => 2700 + (i % 7)).ToList();
            window.ReadChronograph(list: string.Join(", ", readings));
            Settle();
            Assert.Equal(shots, window.ChronographPairing.Count);
            Assert.All(window.ChronographPairing, p => Assert.NotNull(p.Reading));
            Assert.Contains(window.ChronographText, t => t.StartsWith($"{shots} of {shots} readings sit beside a shot. The counts agree", StringComparison.Ordinal));

            // One extra reading, the fouling round fired into the berm: it is marked, and the rest pair in order.
            window.ReadChronograph(list: string.Join(", ", readings.Prepend(2350)));
            Settle();
            Assert.Contains(window.ChronographText, t => t.Contains("1 reading belongs to no shot", StringComparison.Ordinal));
            window.ReadingOfNoShot(0);
            Settle();
            Assert.Contains(window.ChronographText, t => t.StartsWith($"{shots} of {shots + 1} readings sit beside a shot", StringComparison.Ordinal));
            Assert.Equal(2350, window.ChronographPairing.Single(p => p.Shot is null).Reading);

            // A shot the chronograph missed is marked instead, and that shot keeps no reading.
            int first = window.ChronographPairing[0].Shot!.Value;
            window.ShotWithNoReading(first);
            Settle();
            Assert.Null(window.ChronographPairing.Single(p => p.Shot == first).Reading);
            window.ShotWithNoReading(first);
            Settle();

            window.AcceptChronograph();
            Settle();
            var kept = Assert.Single(window.Sessions!.ChronographStrings(id));
            Assert.Equal("Garmin Xero C1", kept.Source);
            Assert.Equal(shots + 1, kept.VelocitiesFps.Count);
            Assert.Equal(shots, window.Sessions.ShotVelocities(id).Count);
            Assert.DoesNotContain(window.Sessions.ShotVelocities(id), v => v.Ordinal == 0);

            // The readings' own spread reaches the load, with where it came from, and the Ballistics screen says so.
            var load = window.Book.FindLoad("H4350 41.5")!;
            Assert.Equal(GroupLab.Core.Records.Chronograph.Spread([.. readings.Select(r => (double)r)])!.SdFps, load.MuzzleVelocitySdFps!.Value, 9);
            Assert.StartsWith($"{shots} readings, Garmin Xero C1, 2026-09-20", load.MuzzleVelocitySdFrom, StringComparison.Ordinal);
            Assert.Contains(window.BallisticsText, t => t.StartsWith("The velocity SD is from ", StringComparison.Ordinal));
            Assert.Contains(window.ChronographText, t => t.StartsWith("Garmin Xero C1, 2026-09-20: ", StringComparison.Ordinal) && t.Contains($"{shots} beside a shot", StringComparison.Ordinal));
            window.Close();
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    /// <summary>
    /// Entry 115 section 4: a sheet whose codes never printed, which is what Alan's sheets may be today. GroupLab cannot name it, so the screen
    /// asks which sheet it is, by name, from the library and the person's own sheets; choosing it registers off the markers and detects as
    /// usual. Marking it by hand stays beside it.
    /// </summary>
    [AvaloniaFact]
    public async Task ASheetWithNoCodesIsOfferedByNameRatherThanRefused()
    {
        var definition = GroupLab.Core.Gltd.Json.GltdJsonReader.ReadFile(Path.Combine(Entry109Tests.Repository(), "targets", "GL-CF25-LTR.gltd.json")).Definition!;
        var page = GroupLab.Core.Rendering.SceneBuilder.Build(definition).Pages[0];
        var codeless = page with { Items = [.. page.Items.Where(i => i.Layer != GroupLab.Core.Rendering.SceneLayer.Codes)] };
        const double dpi = 300;
        var render = GroupLab.Core.Rendering.SceneRasterizer.Rasterize(codeless, dpi);
        var random = new Random(1154);
        var holes = definition.Bulls.Where(b => b.Scoring)
            .Select(b => GroupLab.Core.Detection.SyntheticSheet.SampleHole(random, b.X + random.Next(-40, 41), b.Y + random.Next(-40, 41), onInk: false, GroupLab.Core.Detection.HoleBacking.ScannerLid, 0.871))
            .ToList();
        double s = 254 / dpi;
        var truth = new GroupLab.Core.Registration.HomographyMapping(new GroupLab.Core.Imaging.Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var image = GroupLab.Core.Detection.SyntheticSheet.Compose(render, dpi, truth, render.Width, render.Height, holes, [], random);

        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-nocodes-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "sheet-without-codes.png");
        using (var mat = OpenCvSharp.Mat.FromPixelData(image.Height, image.Width, OpenCvSharp.MatType.CV_8UC1, image.Pixels))
        {
            OpenCvSharp.Cv2.ImWrite(path, mat);
        }

        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900, DetectOnOpen = true };
        try
        {
            window.Show();
            window.OpenImage(path);
            await window.DetectionTask!;
            Settle();

            // It asks which sheet, by name, and says why, rather than stopping with the identity as the reason.
            Assert.True(window.AskingWhichSheet);
            Assert.Equal("GroupLab could not read this sheet's codes. Which sheet is it?", window.StatusText);
            var texts = window.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
            Assert.Contains(texts, t => t == "Which sheet is this?");
            Assert.Contains(texts, t => t.Contains("still registers from its markers", StringComparison.Ordinal));
            var choice = window.GetLogicalDescendants().OfType<ComboBox>().First(c => c.ItemsSource is IEnumerable<string> names && names.Contains(definition.Name));
            Assert.Contains("GroupLab 6x6 Rimfire, Letter", choice.ItemsSource!.Cast<string>());

            // Choosing it registers off the markers and detects as any other sheet does.
            await window.ChooseTheSheet(definition.Name);
            Settle();
            Assert.False(window.AskingWhichSheet);
            Assert.True(window.Session.State.Shots.Count(s => s.IsShot) >= 20, $"{window.Session.State.Shots.Count} marks");
            Assert.NotNull(window.Session.State.Scale);
            Assert.StartsWith("Detected ", window.StatusText, StringComparison.Ordinal);
            window.Close();
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
