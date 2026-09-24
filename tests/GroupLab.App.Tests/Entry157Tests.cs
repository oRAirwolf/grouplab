using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Rendering;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 157 section 4 on the desktop: Find the paper's edges, beside the rectangle scale tool, places the paper's four
/// corners as the reference rectangle when the sheet stands out from what is behind it, and says why when it cannot.
/// </summary>
public class Entry157Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static string Repository([System.Runtime.CompilerServices.CallerFilePath] string here = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    /// <summary>A sheet rendered at 60 dpi, turned 4 degrees, on a dark board, written as a PNG; with the corners it was placed at.</summary>
    private static (string Path, PointD[] Corners) Photograph(bool blank = false)
    {
        const int w = 1200, h = 1000;
        var pixels = Enumerable.Repeat((byte)60, w * h).ToArray();
        PointD[] corners = [];
        if (!blank)
        {
            var definition = GltdJsonReader.ReadFile(Path.Combine(Repository(), "targets", "GL-CF25-LTR.gltd.json")).Definition!;
            const double dpi = 60;
            var page = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
            double angle = 4 * Math.PI / 180, cos = Math.Cos(angle), sin = Math.Sin(angle);
            double cx = w / 2.0, cy = h / 2.0, pw = page.Width, ph = page.Height;
            PointD Place(double x, double y) => new(cx + ((x - (pw / 2)) * cos) - ((y - (ph / 2)) * sin), cy + ((x - (pw / 2)) * sin) + ((y - (ph / 2)) * cos));
            corners = [Place(-0.5, -0.5), Place(pw - 0.5, -0.5), Place(pw - 0.5, ph - 0.5), Place(-0.5, ph - 0.5)];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    double px = ((x - cx) * cos) + ((y - cy) * sin) + (pw / 2), py = (-(x - cx) * sin) + ((y - cy) * cos) + (ph / 2);
                    int ix = (int)Math.Round(px), iy = (int)Math.Round(py);
                    if (ix >= 0 && iy >= 0 && ix < page.Width && iy < page.Height)
                    {
                        pixels[(y * w) + x] = (byte)(page[ix, iy] * 235 / 255);
                    }
                }
            }
        }

        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-entry157-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, blank ? "blank.png" : "sheet-on-board.png");
        using (var mat = Mat.FromPixelData(h, w, MatType.CV_8UC1, pixels))
        {
            Cv2.ImWrite(path, mat);
        }

        return (path, corners);
    }

    private static MainWindow Open(string path)
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        window.OpenImage(path);
        Settle();
        return window;
    }

    [AvaloniaFact]
    public void FindingThePapersEdgesPlacesItsCorners()
    {
        var (path, corners) = Photograph();
        var window = Open(path);
        try
        {
            window.FindPaper();
            Settle();
            var placed = window.Canvas.AwaitingTaps;
            Assert.Equal(4, placed.Count);
            foreach (var (found, expected) in placed.Zip(corners))
            {
                Assert.InRange(Math.Sqrt(Math.Pow(found.X - expected.X, 2) + Math.Pow(found.Y - expected.Y, 2)), 0, 2);
            }

            Assert.StartsWith("The paper's four corners are placed, 0 degrees off square.", window.StatusText, StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    [AvaloniaFact]
    public void WhereThereIsNoPaperItSaysWhy()
    {
        var (path, _) = Photograph(blank: true);
        var window = Open(path);
        try
        {
            window.FindPaper();
            Settle();
            Assert.Empty(window.Canvas.AwaitingTaps);
            Assert.Equal("GroupLab could not find the paper's edges: no sheet stands out from what is behind it. A darker background behind the sheet lets it; otherwise tap the four corners.", window.ProblemText);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
