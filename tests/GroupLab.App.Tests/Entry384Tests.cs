using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 384 section 1, from Alan's screenshot on nightly 175: with the key beside the analysis drawing, everything
/// right of the key's left edge was blank for the whole height, every circle and line cut off level with it. Entry 465 item 2A moved the key
/// to a box of its own under the plot: the drawing now spans the plot's whole width above the key, and nothing of it runs under the key.
/// </summary>
public sealed class Entry384Tests
{
    [AvaloniaTheory]
    [InlineData("light", 1000, 420)]
    [InlineData("dark", 1000, 420)]
    [InlineData("light", 1500, 700)]
    [InlineData("dark", 700, 420)]
    public void TheDrawingSpansThePlotAndStopsAboveTheKey(string theme, int width, int height)
    {
        var plot = new CompositePlot
        {
            Shots = [new PlotShot(1, "1", null, new PointD(0.4, 0.3), false), new PlotShot(2, "2", null, new PointD(-0.5, 0.2), false), new PlotShot(3, "3", null, new PointD(0.1, -0.6), false)],
            Centre = new PointD(0, 0),
            Cep50Inches = 0.5,
            Cep99Inches = 2.4,
        };
        var window = new Window { Width = width, Height = height, Content = plot, RequestedThemeVariant = theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try
        {
            var (data, key, collapsed) = plot.KeyLayout();
            Assert.False(collapsed, $"{width} by {height}: the key folded into a button, so this size tests nothing");
            Assert.True(key.Top >= data.Bottom, $"{width} by {height}: the key {key} is not under the plot {data}");
            Assert.Equal(plot.Bounds.Width, data.Width);
            var size = new PixelSize((int)plot.Bounds.Width, (int)plot.Bounds.Height);
            using var bitmap = new RenderTargetBitmap(size);
            bitmap.Render(plot);
            var paper = Tokens.Plot(plot.ActualThemeVariant).Paper;
            bool Inked(int x, int y)
            {
                var c = At(bitmap, x, y);
                return Math.Abs(c.R - paper.R) + Math.Abs(c.G - paper.G) + Math.Abs(c.B - paper.B) > 30;
            }

            // The plot's right part, past where a key beside it once began, is drawn on: the lines and the circles reach the edge.
            int drawn = 0;
            for (int y = (int)data.Top + 2; y < (int)data.Bottom - 2; y += 2)
            {
                for (int x = (int)(data.Right * 0.8); x < (int)data.Right - 2; x += 2)
                {
                    drawn += Inked(x, y) ? 1 : 0;
                }
            }

            Assert.True(drawn > 0, $"{width} by {height}, {theme}: the right of the plot is blank paper");

            // Under the plot, level with the key and beside its box, nothing of the drawing: it stopped at the plot's edge.
            int under = 0, looked = 0;
            var around = new Rect(key.X - 2, key.Y - 2, key.Width + 4, key.Height + 4);
            for (int y = (int)data.Bottom + 1; y < (int)key.Bottom - 1; y += 2)
            {
                for (int x = 1; x < (int)plot.Bounds.Width - 1; x += 2)
                {
                    if (around.Contains(new Point(x, y)))
                    {
                        continue;
                    }

                    looked++;
                    under += Inked(x, y) ? 1 : 0;
                }
            }

            Assert.True(looked > 0, $"{width} by {height}: nothing beside the key to look at");
            Assert.True(under == 0, $"{width} by {height}, {theme}: {under} of {looked} pixels beside the key are drawn on");
        }
        finally
        {
            window.Close();
        }
    }

    private static Color At(RenderTargetBitmap bitmap, int x, int y)
    {
        var buffer = Marshal.AllocHGlobal(4);
        try
        {
            bitmap.CopyPixels(new PixelRect(x, y, 1, 1), buffer, 4, 4);
            byte[] px = new byte[4];
            Marshal.Copy(buffer, px, 0, 4);
            // The bitmap's own byte order: BGRA on Windows and Linux, RGBA on macOS.
            return bitmap.Format == Avalonia.Platform.PixelFormat.Rgba8888
                ? Color.FromArgb(px[3], px[0], px[1], px[2])
                : Color.FromArgb(px[3], px[2], px[1], px[0]);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
