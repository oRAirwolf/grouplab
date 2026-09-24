using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Rendering;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 187. Section 6, question 50's option 2 quietly: where a sheet has more bulls than shots and nobody has said
/// which were fired at, a hint sits beside the control, holds nothing back, claims nothing, and goes for good once answered or put away.
/// Section 2 item 3: white paper on a white board cannot be found, says so, and the corners placed by hand still give the scale, because the
/// manual path is the one that must never fail.
/// </summary>
public class Entry187Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    [AvaloniaFact]
    public void TheAimHintIsQuietAndOnceIsEnough()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        var store = window.SettingsStore;
        MainWindow? again = null;
        try
        {
            window.Session.SetExpectedShots(10);
            Settle();
            string hint = "This sheet has 25 bulls and 10 shots. If you fired at only some of the bulls, choose them and press These ones.";
            Assert.Contains(hint, window.AimedAtText);
            Assert.DoesNotContain(window.AimedAtText, t => t.Contains("would move", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(window.StillNeeded(), n => n.Contains("bull", StringComparison.OrdinalIgnoreCase));

            window.PutAwayAimHintNow();
            Settle();
            Assert.DoesNotContain(hint, window.AimedAtText);

            // Put away is final for the target, in a window opened later too.
            again = new MainWindow(store) { Width = 1400, Height = 900 };
            again.Show();
            again.OpenImage(path);
            again.Session.SetExpectedShots(10);
            Settle();
            Assert.DoesNotContain(hint, again.AimedAtText);
        }
        finally
        {
            again?.Close();
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    [AvaloniaFact]
    public void AnsweringTheHintIsFinalForTheTarget()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetExpectedShots(10);
            Settle();
            Assert.Contains(window.AimedAtText, t => t.StartsWith("This sheet has 25 bulls", StringComparison.Ordinal));
            window.SetAimedAtChosenBulls(true);
            Settle();
            window.Session.SetAssignmentRule(null);
            Settle();
            Assert.DoesNotContain(window.AimedAtText, t => t.StartsWith("This sheet has 25 bulls", StringComparison.Ordinal));
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    private static string Repository([System.Runtime.CompilerServices.CallerFilePath] string here = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    /// <summary>A Letter sheet on a white board, lit more on one side than the other, as Alan's range photographs are; with its corners.</summary>
    private static (string Path, PointD[] Corners) WhiteOnWhite()
    {
        const int w = 1200, h = 1000;
        var definition = GltdJsonReader.ReadFile(Path.Combine(Repository(), "targets", "GL-CF25-LTR.gltd.json")).Definition!;
        var page = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], 60);
        double angle = 3 * Math.PI / 180, cos = Math.Cos(angle), sin = Math.Sin(angle);
        double cx = w / 2.0, cy = h / 2.0, pw = page.Width, ph = page.Height;
        PointD Place(double x, double y) => new(cx + ((x - (pw / 2)) * cos) - ((y - (ph / 2)) * sin), cy + ((x - (pw / 2)) * sin) + ((y - (ph / 2)) * cos));
        PointD[] corners = [Place(-0.5, -0.5), Place(pw - 0.5, -0.5), Place(pw - 0.5, ph - 0.5), Place(-0.5, ph - 0.5)];
        var pixels = new byte[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                double light = 0.72 + (0.28 * x / w);
                double px = ((x - cx) * cos) + ((y - cy) * sin) + (pw / 2), py = (-(x - cx) * sin) + ((y - cy) * cos) + (ph / 2);
                int ix = (int)Math.Round(px), iy = (int)Math.Round(py);
                double surface = ix >= 0 && iy >= 0 && ix < page.Width && iy < page.Height ? page[ix, iy] * 235.0 / 255 : 240;
                pixels[(y * w) + x] = (byte)Math.Round(surface * light);
            }
        }

        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-entry187-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "white-on-white.png");
        using (var mat = Mat.FromPixelData(h, w, MatType.CV_8UC1, pixels))
        {
            Cv2.ImWrite(path, mat);
        }

        return (path, corners);
    }

    [AvaloniaFact]
    public void OnAWhiteBoardTheCornersPlacedByHandStillGiveTheScale()
    {
        var (path, corners) = WhiteOnWhite();
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        try
        {
            window.Show();
            window.OpenImage(path);
            Settle();
            window.FindPaper();
            Settle();
            Assert.Empty(window.Canvas.AwaitingTaps);
            Assert.StartsWith("GroupLab could not find the paper's edges", window.ProblemText, StringComparison.Ordinal);
            Assert.Contains("A darker background behind the sheet lets it; otherwise tap the four corners.", window.ProblemText, StringComparison.Ordinal);

            window.Canvas.PlaceRectangle(corners);
            window.AskRectangle();
            Settle();
            var use = window.GetLogicalDescendants().OfType<Button>().First(b => b.Content as string == "Use this rectangle");
            var boxes = ((Panel)use.Parent!).Children.OfType<TextBox>().ToList();
            Assert.Equal(2, boxes.Count);
            boxes[0].Text = "8.5";
            boxes[1].Text = "11";
            use.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.IsType<RectangleReference>(window.Session.State.Scale);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
