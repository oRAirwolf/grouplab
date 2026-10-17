using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Cli.Imaging;
using GroupLab.Cli.Library;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 423, Alan, Quack and Jylee. Section 0: a drop onto a tab holding a sheet never replaces it, and the sheet's
/// record is never touched. Sections 1 to 3: several pictures of one set dropped together open in ONE tab, read one after another and pooled
/// with no question, giving the same figures as Session records' Pool the chosen; two sets mixed, a missing sheet added by a later drop, the
/// same sheet twice, and pictures with no codes, with both answers to the one question they ask.
/// </summary>
public class Entry423Tests
{
    private const double Dpi = 300;

    private static string Repository([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    internal static TargetDefinition TwoMoaSet() =>
        GltdJsonReader.ReadFile(Path.Combine(Repository(), "targets", "GL-CF9-T.gltd.json")).Definition!;

    /// <summary>One sheet of a set as a 300 dpi scan with a hole near each scoring bull, the seed telling two pictures of a sheet apart.</summary>
    internal static string Sheet(TargetDefinition set, string folder, int sheet, string? name = null, int seed = 0)
    {
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(set).Pages[sheet], Dpi);
        var random = new Random(423 + sheet + (100 * seed));
        var holes = set.Bulls.Where(b => b.Scoring)
            .Select(b => SyntheticSheet.SampleHole(random, b.X + random.Next(-60, 61), b.Y + random.Next(-60, 61), onInk: false, HoleBacking.ScannerLid, 1.0)).ToList();
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var image = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], random);
        string path = Path.Combine(folder, name ?? $"sheet-{sheet}.png");
        using var mat = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
        Cv2.ImWrite(path, mat);
        return path;
    }

    private static readonly Dictionary<int, AutomaticResult> Detected = [];

    /// <summary>
    /// For the screenshot walk: a sheet of the 2 MOA set drawn as <see cref="Sheet"/> draws it, the same pixels every time, and its detection,
    /// worked out once however many window sizes the walk pictures it at.
    /// </summary>
    internal static (string Path, AutomaticResult Result) DetectedSheet(TargetDefinition set, string folder, int sheet)
    {
        string path = Sheet(set, folder, sheet);
        if (!Detected.TryGetValue(sheet, out var result))
        {
            var (grey, metadata) = ImageLoader.Load(path);
            var (value, _) = ImageLoader.LoadMaxChannel(path);
            result = AutomaticMarking.Run(grey, value, metadata, set, new OpenCvSharpBackend(), calibre: Calibre.Of(0.308));
            Detected[sheet] = result;
        }

        return (path, result);
    }

    /// <summary>A photograph with no codes on it: paper with a few dark holes.</summary>
    private static string Untagged(string folder, int k)
    {
        using var mat = new Mat(600, 800, MatType.CV_8UC1, Scalar.All(225));
        var random = new Random(k);
        for (int i = 0; i < 5; i++)
        {
            Cv2.Circle(mat, new Point(random.Next(100, 700), random.Next(100, 500)), 6, Scalar.All(30), -1);
        }

        string path = Path.Combine(folder, $"photo-{k}.png");
        Cv2.ImWrite(path, mat);
        return path;
    }

    internal static (MainWindow Window, string Folder) Window(bool detect)
    {
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-423-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        var store = new AppSettingsStore(Path.Combine(folder, "settings.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900, DetectOnOpen = detect };
        window.Show();
        return (window, folder);
    }

    /// <summary>Pumps the window until the drop has finished or <paramref name="until"/> holds, three minutes at most.</summary>
    internal static async Task Settle(MainWindow window, Func<bool>? until = null)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (!window.Dropping.IsCompleted && until?.Invoke() != true && clock.Elapsed < TimeSpan.FromMinutes(3))
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(20);
        }

        Dispatcher.UIThread.RunJobs();
        Assert.True(window.Dropping.IsCompleted || until?.Invoke() == true, "the drop did not finish in three minutes");
    }

    [AvaloniaFact]
    public void ANewTabThatCannotBeShownIsNotKept()
    {
        // Section 0: the bug behind "only opens 1 image and overwrites the session". With a question open in the middle of the window the new
        // tab could not be shown, was kept anyway, and the next picture opened over the target showing.
        var (window, folder) = Window(detect: false);
        try
        {
            string path = Untagged(folder, 1);
            window.OpenImage(path);
            window.ShowProblem("A question", "Something to answer first.");
            int tabs = window.Targets.Count;
            Assert.Null(window.AddTarget(choose: false));
            Assert.Equal(tabs, window.Targets.Count);
            Assert.Equal(path, window.Session.State.ImagePath);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task ADropOntoASavedSheetLeavesItAndItsRecord(int dropped)
    {
        var (window, folder) = Window(detect: false);
        try
        {
            var set = TwoMoaSet();
            string first = Sheet(set, folder, 0, "saved.png");
            window.OpenImage(first);
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.Session.Load(window.Session.State with { Shots = [new MarkedShot(1, new PointD(500, 500), ShotProvenance.Manual)], NextId = 2 });
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            long id = Assert.IsType<long>(window.CurrentSession);
            string record = window.Sessions!.Get(id)!.MarkingJson;
            int records = window.Sessions.List().Count;

            var paths = Enumerable.Range(0, dropped).Select(k => Sheet(set, folder, k, $"dropped-{k}.png", seed: 1)).ToList();
            window.OpenDropped(paths);
            await Settle(window);

            Assert.Equal(2, window.Targets.Count);
            Assert.True(window.ShowTarget(0));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(first, window.Session.State.ImagePath);
            Assert.Equal(id, window.CurrentSession);
            Assert.Single(window.Session.State.Shots);
            Assert.Equal(record, window.Sessions.Get(id)!.MarkingJson);
            Assert.Equal(records, window.Sessions.List().Count);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }

    [AvaloniaFact]
    public async Task ThreeSheetsOfOneSetOpenOneTabPooledAsPoolTheChosen()
    {
        var (window, folder) = Window(detect: true);
        try
        {
            var set = TwoMoaSet();
            var paths = new[] { 2, 0, 1 }.Select(k => Sheet(set, folder, k)).ToList();
            window.OpenDropped(paths);
            await Settle(window);

            // One tab, the empty one filled, named for the set; every sheet read, saved as its own record and pooled.
            Assert.Single(window.Targets);
            var shown = Assert.IsType<SetGroup>(window.CurrentSet);
            Assert.Equal(3, shown.Members.Count);
            Assert.Equal($"{set.Name}, 3 sheets", window.CurrentTarget.Name);
            var pool = Assert.IsType<SetPoolResult>(window.SetPoolShown);
            Assert.Empty(pool.Missing);
            Assert.Equal([0, 1, 2], window.Plot.PooledSheets);
            Assert.True(pool.Shots > 0);
            Assert.Equal(SetTabWords.PoolSaved, window.SavedText);
            var records = window.Sessions!.List();
            Assert.Equal(3, records.Count);
            double? cep = window.Plot.Cep50Inches;
            int shots = window.Plot.Shots.Count;

            // Pool the chosen over the same three records gives the same group.
            foreach (var r in records)
            {
                window.ChooseSession(r.Id, true);
            }

            window.PoolChosen();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(2, window.Targets.Count);
            Assert.Null(window.CurrentSet);
            Assert.Equal(shots, window.Plot.Shots.Count);
            Assert.Equal(cep!.Value, window.Plot.Cep50Inches!.Value, 9);
            Assert.Contains(window.PoolText, l => l.StartsWith($"Mean radius {pool.Figures!.MeanRadius!.Value:0.000} in", StringComparison.Ordinal));
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }

    [AvaloniaFact]
    public async Task OneSheetAloneCanBeCheckedAndTheWayBackShowsThePool()
    {
        var (window, folder) = Window(detect: true);
        try
        {
            var set = TwoMoaSet();
            window.OpenDropped([Sheet(set, folder, 0), Sheet(set, folder, 1)]);
            await Settle(window);
            var group = Assert.IsType<SetGroup>(window.CurrentSet);
            int pooled = window.Plot.Shots.Count;

            // Section 2: one sheet alone, with its own picture and record; a shot set to not a shot there leaves the pool when it is shown again.
            window.ShowSetSheet(1);
            Dispatcher.UIThread.RunJobs();
            Assert.Same(group.Members[1], group.Showing);
            Assert.Equal(group.Members[1].Path, window.Session.State.ImagePath);
            Assert.NotNull(window.CurrentSession);

            // Sheet 2 shares its design with sheet 1 and is not a copy of it, so it is not called one.
            Assert.Null(window.CopyOfADesignSeenBefore);
            var shot = window.Session.State.Shots.First(s => s.IsShot);
            window.Session.Load(window.Session.State with { Shots = window.Session.State.Shots.Replace(shot, shot with { NotAShot = true }) });
            window.SaveWaitingNow();
            window.BackToSet();
            Dispatcher.UIThread.RunJobs();
            Assert.Null(group.Showing);
            Assert.Equal(pooled - 1, window.Plot.Shots.Count);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }

    [AvaloniaFact]
    public async Task TwoSetsMixedOpenATabEachAndAPictureAloneItsOwn()
    {
        var (window, folder) = Window(detect: false);
        try
        {
            var twoMoa = TwoMoaSet();
            var made = TargetGenerator.Generate(new GeneratorRequest(100, 4, null, 25, "letter")).Design!.Definition!;
            string single = Path.Combine(folder, "single.png");
            var page = SceneRasterizer.Rasterize(SceneBuilder.Build(GltdJsonReader.ReadFile(Path.Combine(Repository(), "targets", "GL-CF9-LTR.gltd.json")).Definition!).Pages[0], Dpi);
            using (var mat = Mat.FromPixelData(page.Height, page.Width, MatType.CV_8UC1, page.Pixels))
            {
                Cv2.ImWrite(single, mat);
            }

            window.OpenDropped([Sheet(twoMoa, folder, 0, "a0.png"), Sheet(made, folder, 1, "b1.png"), single, Sheet(twoMoa, folder, 2, "a2.png"), Sheet(made, folder, 0, "b0.png")]);
            await Settle(window);

            Assert.Equal(3, window.Targets.Count);
            var sets = window.Targets.Select(t => t.Set).OfType<SetGroup>().ToList();
            Assert.Equal(2, sets.Count);
            Assert.All(sets, s => Assert.Equal(2, s.Members.Count));
            Assert.Contains(sets, s => s.DefinitionId != sets[0].DefinitionId);
            Assert.Contains("single.png: a tab of its own.", window.StatusText, StringComparison.Ordinal);
            Assert.Contains("5 pictures dropped together", window.StatusText, StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }

    [AvaloniaFact]
    public async Task AMissingSheetIsNamedAndADropOntoTheTabAddsIt()
    {
        var (window, folder) = Window(detect: true);
        try
        {
            var set = TwoMoaSet();
            window.OpenDropped([Sheet(set, folder, 0), Sheet(set, folder, 2)]);
            await Settle(window);
            Assert.Equal([1], window.SetPoolShown!.Missing);
            Assert.Contains("2 is still missing", window.StatusText, StringComparison.Ordinal);

            // Section 3: the missing sheet dropped onto the set's tab joins it, and no tab is added.
            window.OpenDropped([Sheet(set, folder, 1)]);
            await Settle(window);
            Assert.Single(window.Targets);
            Assert.Equal(3, window.CurrentSet!.Members.Count);
            Assert.Empty(window.SetPoolShown!.Missing);
            Assert.Equal([0, 1, 2], window.Plot.PooledSheets);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }

    [AvaloniaFact]
    public async Task TheSameSheetInTwoPicturesIsAskedAboutAndAnIdenticalPictureIsOne()
    {
        var (window, folder) = Window(detect: false);
        try
        {
            var set = TwoMoaSet();
            string a = Sheet(set, folder, 0, "first-0.png");
            string b = Sheet(set, folder, 0, "second-0.png", seed: 7);
            string copy = Path.Combine(folder, "copy-of-first-0.png");
            File.Copy(a, copy);
            window.OpenDropped([a, Sheet(set, folder, 1), b, copy]);
            await Settle(window, () => window.ProblemOpen);
            await Settle(window, () => window.ProblemTitle is not null && window.Dropping.IsCompleted);

            var group = Assert.IsType<SetGroup>(window.CurrentSet);
            Assert.Equal(3, group.Members.Count);
            Assert.Equal([0], group.Contested);
            Assert.Equal("Sheet 1 is in two pictures", window.ProblemTitle);
            Assert.Contains("1 was the same picture as another", window.StatusText, StringComparison.Ordinal);
            window.PressProblemChoice("Count second-0.png");
            Dispatcher.UIThread.RunJobs();
            Assert.False(window.ProblemOpen);
            Assert.Empty(group.Contested);
            Assert.True(group.Counts(group.Members.Single(m => m.Name == "second-0.png")));
            Assert.False(group.Counts(group.Members.Single(m => m.Name == "first-0.png")));
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PicturesWithNoCodesAskOnceAndFollowTheAnswer(bool pool)
    {
        var (window, folder) = Window(detect: false);
        try
        {
            window.OpenDropped([Untagged(folder, 1), Untagged(folder, 2), Untagged(folder, 3)]);
            await Settle(window, () => window.ProblemOpen);
            Assert.Equal("Pool these 3 pictures as one group?", window.ProblemTitle);
            Assert.Equal([SetTabWords.PoolQuestionDefault, SetTabWords.PoolQuestionOther], window.ProblemChoices);
            window.PressProblemChoice(pool ? SetTabWords.PoolQuestionDefault : SetTabWords.PoolQuestionOther);
            await Settle(window);

            if (pool)
            {
                Assert.Single(window.Targets);
                var group = Assert.IsType<SetGroup>(window.CurrentSet);
                Assert.Null(group.Definition);
                Assert.Equal(3, group.Members.Count);
                Assert.Equal("3 pictures pooled", window.CurrentTarget.Name);
                Assert.Contains("no scale yet", window.StatusText, StringComparison.Ordinal);
            }
            else
            {
                Assert.Equal(3, window.Targets.Count);
                Assert.All(window.Targets, t => Assert.Null(t.Set));
            }
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }
}
