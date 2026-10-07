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
/// right of the key's left edge was blank for the whole height, every circle and line cut off level with it. The key now covers only its
/// own box; the drawing runs on below it.
/// </summary>
public sealed class Entry384Tests
{
    [AvaloniaTheory]
    [InlineData("light", 1000, 420)]
    [InlineData("dark", 1000, 420)]
    [InlineData("light", 1500, 700)]
    [InlineData("dark", 700, 420)]
    public void BelowTheKeyTheDrawingGoesOn(string theme, int width, int height)
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
            // Beside the drawing, look below the key; below the drawing, look right of it: either way the drawing must go on there.
            bool beside = key.X >= data.Right - 1;
            var (x0, x1, y0, y1) = beside
                ? ((int)key.X + 2, (int)plot.Bounds.Width - 2, (int)key.Bottom + 6, (int)plot.Bounds.Height - 2)
                : ((int)key.Right + 6, (int)plot.Bounds.Width - 2, (int)key.Y + 2, (int)key.Bottom - 2);
            var size = new PixelSize((int)plot.Bounds.Width, (int)plot.Bounds.Height);
            using var bitmap = new RenderTargetBitmap(size);
            bitmap.Render(plot);
            var paper = Tokens.Plot(plot.ActualThemeVariant).Paper;
            int drawn = 0, looked = 0;
            for (int y = y0; y < y1; y += 2)
            {
                for (int x = x0; x < x1; x += 2)
                {
                    looked++;
                    var c = At(bitmap, x, y);
                    if (Math.Abs(c.R - paper.R) + Math.Abs(c.G - paper.G) + Math.Abs(c.B - paper.B) > 30)
                    {
                        drawn++;
                    }
                }
            }

            Assert.True(looked > 0, $"{width} by {height}: nothing below the key to look at");
            Assert.True(drawn > 0, $"{width} by {height}, {theme}: {(beside ? "below" : "beside")} the key, all {looked} pixels looked at are blank paper");
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
