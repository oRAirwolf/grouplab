using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 97 section 5, the guard on every interface batch: entry 84's synthetic 25-shot sheet, with its five injected
/// errors, detected and put in the window, and then settled through the window's own keys with the answer truth says is right. The Core
/// rehearsal drives the review queue directly and cannot see the window, so a styling pass that broke a key, or left an item without its
/// shot selected, would pass it; this one presses the keys a person presses.
/// <para>
/// It is a rehearsal of the Phase 3 gate and not the gate: nothing here measures a person reading a card.
/// </para>
/// </summary>
public class RehearsalTests(ITestOutputHelper output)
{
    /// <summary>
    /// The ceiling on key presses. The Core rehearsal settles the 600 DPI sheet in 15; this one runs the same sheet at 300 DPI to keep the suite
    /// quick, where detection leaves 7 items and the window settles them in 10 presses and no taps. A change to the window that raises it has
    /// cost the loop key presses, and entry 97 section 5 says that has not improved anything, however it looks.
    /// </summary>
    private const int MostKeyPresses = 10;

    private const double DmmPerInch = 254;

    [AvaloniaFact]
    public void TheInjectedSheetSettlesFromTheKeyboardAlone()
    {
        const double dpi = 300;
        var definition = BuiltInDefinition();
        var scoring = definition.Bulls.Select((b, i) => (Bull: b, Index: i)).Where(x => x.Bull.Scoring).ToList();
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        double s = DmmPerInch / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var random = new Random(8425);
        var holes = new List<SyntheticHole>();
        var firedAt = new List<int>();
        double Jitter() => 0.09 * DmmPerInch * (random.NextDouble() - 0.5);
        void Place(int bull, double x, double y, double scale = 1)
        {
            // Entry 81: drawn at 0.871, where a synthetic single hole reads what a real .308 hole reads.
            holes.Add(SyntheticSheet.SampleHole(random, x, y, onInk: false, HoleBacking.ScannerLid, scale * 0.871));
            firedAt.Add(bull);
        }

        // Entry 84 section 2's five errors: two contested shots, a merged pair, a stray leaving one bull doubled and one empty, a faint mark.
        for (int k = 0; k < scoring.Count; k++)
        {
            var (bull, index) = scoring[k];
            double x = bull.X + Jitter(), y = bull.Y + Jitter();
            if (k is 6 or 13)
            {
                var next = scoring[(k + 1) % scoring.Count].Bull;
                Place(index, bull.X + (0.55 * (next.X - bull.X)), bull.Y + (0.55 * (next.Y - bull.Y)));
            }
            else if (k == 20)
            {
                Place(index, scoring[19].Bull.X + (0.45 * DmmPerInch), scoring[19].Bull.Y);
            }
            else if (k == 2)
            {
                Place(index, x, y);
                Place(index, x + (0.16 * DmmPerInch), y + (0.05 * DmmPerInch));
            }
            else
            {
                Place(index, x, y, k == 10 ? 0.5 : 1);
            }
        }

        var observed = SyntheticSheet.Compose(render, dpi, truth, render.Width, render.Height, holes, [], random);
        var result = AutomaticMarking.Run(observed, observed, ImageMetadata.ForScan(render.Width, render.Height, dpi), definition, new OpenCvSharpBackend(), calibre: new Calibre(".308", 0.308));
        Assert.Null(result.Failure);

        var window = NewWindow();
        window.Show();
        window.ApplyDetection(result);
        Dispatcher.UIThread.RunJobs();
        var truthImage = holes.Select(h => truth.ToImage(new PointD(h.X, h.Y))).ToList();
        int? Fired(MarkedShot shot)
        {
            int best = Enumerable.Range(0, truthImage.Count).MinBy(t => Distance(shot.Image, truthImage[t]));
            return Distance(shot.Image, truthImage[best]) / dpi <= 0.3 ? firedAt[best] : null;
        }

        bool ReallyTwo(MarkedShot shot) => truthImage.Count(t => Distance(shot.Image, t) / dpi <= 0.2) > 1;
        string Label(int bull) => window.Session.State.Bulls.First(b => b.Index == bull).Label;

        int presses = 0, steps = 0;
        void Press(Key key)
        {
            presses++;
            window.OnReviewKey(window, new KeyEventArgs { Key = key, RoutedEvent = InputElement.KeyDownEvent, Source = window });
            Dispatcher.UIThread.RunJobs();
        }

        void Type(string bull)
        {
            foreach (char c in bull)
            {
                Press(c == 'S' ? Key.S : Key.D0 + (c - '0'));
            }

            Press(Key.Enter);
        }

        int opened = window.ReviewItems.Count(i => !i.Resolved);
        while (window.CurrentReview is { } item && steps++ < 60)
        {
            var shot = item.ShotId is { } id ? window.Session.State.Find(id) : null;
            int? fired = shot is null ? null : Fired(shot);
            string before = item.Key;

            // The window selects the current item's shot; a typed bull goes to the selected shot, so this is the check that it did.
            if (shot is not null)
            {
                Assert.Equal(shot.Id, window.Canvas.Selected);
            }

            if (item.Kind == ReviewKind.Doubled && shot is not null && fired is { } own && shot.Bull != own)
            {
                Type(Label(own));
            }
            else if (item.Kind == ReviewKind.Contested && fired is { } bull && item.Choices[0].Bull != bull && item.Choices.Any(c => c.Bull == bull))
            {
                Type(Label(bull));
            }
            else if (item.Kind == ReviewKind.Oversized && shot is not null && ReallyTwo(shot))
            {
                Press(Key.T);
            }
            else
            {
                Press(Key.Enter);
            }

            Assert.True(window.CurrentReview?.Key != before || window.ReviewItems.First(i => i.Key == before).Resolved, $"the keys did not settle '{item.Sentence}'");
        }

        output.WriteLine($"{opened} items open after detection, settled in {steps} steps and {presses} key presses, no taps");
        Assert.Equal(0, window.ReviewItems.Count(i => !i.Resolved));
        Assert.True(presses <= MostKeyPresses, $"the loop took {presses} key presses where it took at most {MostKeyPresses}");
        window.Close();
    }

    private static GroupLab.Core.Gltd.Model.TargetDefinition BuiltInDefinition([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        GltdJsonReader.ReadFile(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "targets", "GL-CF25-LTR.gltd.json"))).Definition!;

    private static MainWindow NewWindow()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        return new MainWindow(store) { Width = 1400, Height = 900 };
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
