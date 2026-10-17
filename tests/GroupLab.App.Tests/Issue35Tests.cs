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
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// Error report issue 35, from Quack on the Discord, nightly 195: on a pooled set, a click on the small sheet at the top left picked shots
/// from another bull with the bull's own, and the second and third sheets' shots never showed on it at all.
/// </summary>
public class Issue35Tests
{
    /// <summary>
    /// A bull's shots are the shots given to it, not the shots that landed nearest it: a shot wide of bull 1, landing on bull 2, is not picked
    /// with bull 2's, and is picked with bull 1's. A shot wide of every bull is picked on its own dot.
    /// </summary>
    [AvaloniaFact]
    public void AClickOnABullPicksTheShotsGivenToItAndNoOther()
    {
        var thumbnail = new SheetThumbnail
        {
            Width = 240,
            Height = 300,
            Bulls = [new ThumbnailBull(0, new PointD(2, 2), 0.5), new ThumbnailBull(1, new PointD(4, 2), 0.5)],
            Shots =
            [
                new ThumbnailShot(1, new PointD(2.05, 2.05), false, Bull: 0),
                new ThumbnailShot(2, new PointD(3.9, 2.1), false, Bull: 0),
                new ThumbnailShot(3, new PointD(4.05, 1.95), false, Bull: 1),
                new ThumbnailShot(4, new PointD(6, 8), false, Bull: 1),
            ],
        };
        var window = new Window { Width = 240, Height = 300, Content = thumbnail };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            Assert.Equal([3, 4], thumbnail.ClickBull(1));
            Assert.Equal([1, 2], thumbnail.ClickBull(0));
            Assert.Equal([4], thumbnail.ClickShot(4));
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Each sheet of a pooled set is drawn in its own color, the colors the plot gives its sheets.</summary>
    [AvaloniaFact]
    public void APooledSetsSheetsAreDrawnInTheirOwnColors()
    {
        const int width = 240, height = 300;
        var thumbnail = new SheetThumbnail
        {
            Width = width,
            Height = height,
            Bulls = [new ThumbnailBull(0, new PointD(2, 2), 0.5)],
            Shots = [new ThumbnailShot(1, new PointD(2, 2), false, 0, Sheet: 0), new ThumbnailShot(2, new PointD(6, 8), false, 0, Sheet: 1)],
        };
        var window = new Window { Width = width, Height = height, Content = thumbnail, RequestedThemeVariant = ThemeVariant.Dark };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            Assert.Equal([0, 1], thumbnail.PooledSheets);
            using var bitmap = new RenderTargetBitmap(new PixelSize(width, height));
            bitmap.Render(thumbnail);
            var bgra = new byte[width * height * 4];
            var handle = GCHandle.Alloc(bgra, GCHandleType.Pinned);
            try
            {
                bitmap.CopyPixels(new PixelRect(0, 0, width, height), handle.AddrOfPinnedObject(), bgra.Length, width * 4);
            }
            finally
            {
                handle.Free();
            }

            var colours = Tokens.Loads(ThemeVariant.Dark);
            foreach (var (shot, sheet) in new[] { (thumbnail.Shots[0], 0), (thumbnail.Shots[1], 1) })
            {
                var at = thumbnail.Drawn(shot.At);
                int i = (((int)at.Y * width) + (int)at.X) * 4;
                // The bitmap's own byte order: BGRA on Windows and Linux, RGBA on macOS.
                var drawn = bitmap.Format == Avalonia.Platform.PixelFormat.Rgba8888
                    ? Color.FromRgb(bgra[i], bgra[i + 1], bgra[i + 2])
                    : Color.FromRgb(bgra[i + 2], bgra[i + 1], bgra[i]);
                Assert.True(Near(drawn, colours[sheet]), $"sheet {sheet + 1}'s shot is drawn {drawn}, not its sheet's {colours[sheet]}");
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static bool Near(Color a, Color b) => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) <= 30;

    /// <summary>
    /// Quack's case, end to end: three sheets of a set dropped together open pooled; the small sheet holds every sheet's shots, and a click on a
    /// bull picks that bull's shot from each sheet, on the plot too, and nothing from any other bull.
    /// </summary>
    [AvaloniaFact]
    public async Task EverySheetOfAPooledSetShowsOnTheSmallSheetAndABullPicksItsOwnShots()
    {
        var (window, folder) = Entry423Tests.Window(detect: true);
        try
        {
            var set = Entry423Tests.TwoMoaSet();
            window.OpenDropped([.. new[] { 0, 1, 2 }.Select(k => Entry423Tests.Sheet(set, folder, k))]);
            await Entry423Tests.Settle(window);
            Assert.IsType<SetGroup>(window.CurrentSet);
            Assert.Equal([0, 1, 2], window.Plot.PooledSheets);

            var thumbnail = window.Thumbnail;
            Assert.True(thumbnail.IsVisible);
            Assert.Equal(window.Plot.Shots.Count, thumbnail.Shots.Count);
            Assert.Equal([0, 1, 2], thumbnail.PooledSheets);

            var state = window.Session.State;
            var bull = state.Shots.Where(s => s.IsShot && s.Bull is not null)
                .GroupBy(s => SetPool.BullOnSheet(s.Bull!.Value))
                .First(g => g.Select(s => SetPool.SheetOf(s.Bull!.Value)).Distinct().Count() == 3);
            var picked = thumbnail.ClickBull(bull.Key);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(bull.Select(s => s.Id).Order(), picked.Order());
            Assert.Equal(bull.Select(s => s.Id).Order(), window.Plot.Selected.Order());
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }
}
