using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Rendering;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 300: the Targets preview is the sheet's own scene drawn as vectors, not a compressed picture, so it is exactly
/// what prints and sharp at any zoom; it follows every setting at once; and Open as PDF opens the real PDF.
/// </summary>
public class Entry300Tests
{
    private static Scene Page(string file, BullColour colour = BullColour.Black)
    {
        var definition = GltdJsonReader.ReadFile(Path.Combine(Entry109Tests.Repository(), "targets", file)).Definition!;
        return SceneBuilder.Build(definition, new RenderOptions(PrintNote: SceneBuilder.ActualSizeNote, BullColour: colour)).Pages[0];
    }

    /// <summary>The view drawn at a size, as grey levels by luminance.</summary>
    private static byte[] Drawn(Scene scene, int width, int height)
    {
        var view = new SheetView { Scene = scene, Width = width, Height = height };
        var window = new Window { Width = width, Height = height, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        using var bitmap = new RenderTargetBitmap(new PixelSize(width, height));
        bitmap.Render(view);
        window.Close();
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

        var grey = new byte[width * height];
        for (int i = 0; i < grey.Length; i++)
        {
            grey[i] = (byte)Math.Round((0.114 * bgra[4 * i]) + (0.587 * bgra[(4 * i) + 1]) + (0.299 * bgra[(4 * i) + 2]));
        }

        return grey;
    }

    [AvaloniaTheory]
    [InlineData("GL-CF25-LTR.gltd.json")]
    [InlineData("GL-CF25-LTR-C.gltd.json")]
    [InlineData("GL-ZERO-MOA-100Y.gltd.json")]
    public void ThePreviewIsThePrintedPage(string file)
    {
        // The live drawing against the independent rasterizer the PDF is held to (conformance test 39), words and all, at 60 dpi.
        var scene = Page(file);
        const double dpi = 60;
        var reference = SceneRasterizer.Rasterize(scene, dpi, words: true);
        var drawn = Drawn(scene, reference.Width, reference.Height);
        double difference = 0;
        int far = 0;
        for (int i = 0; i < drawn.Length; i++)
        {
            int d = Math.Abs(drawn[i] - reference.Pixels[i]);
            difference += d;
            far += d > 128 ? 1 : 0;
        }

        double mean = difference / drawn.Length;
        Assert.True(mean < 4, $"{file}: the drawing differs from the render by {mean:0.00} levels on average");
        Assert.True(far < drawn.Length / 200, $"{file}: {far} pixels differ by more than half the range");
    }

    [AvaloniaFact]
    public void APageIsDrawnInAFewGeometriesAndGrowsWithoutBlur()
    {
        var scene = Page("GL-CF25-LTR.gltd.json", BullColour.Red);
        var shapes = SheetView.Build(scene);
        Assert.InRange(shapes.Count, 2, scene.Items.Count / 10);

        // Twice the size is twice the detail, not a picture stretched: a module of a code keeps a hard edge.
        var small = Drawn(scene, 300, 388);
        var large = Drawn(scene, 1200, 1553);
        static double Edges(byte[] grey) => grey.Count(v => v is > 40 and < 215) / (double)grey.Length;
        Assert.True(Edges(large) < Edges(small), "the larger drawing has as much soft edge as the smaller");
    }

    [AvaloniaFact]
    public void OpenAsPdfOpensTheRealPdf()
    {
        var recorder = new GroupLab.Core.Updates.RecordedOutsideWorld();
        var before = GroupLab.Core.Updates.TheOutsideWorld.Current;
        GroupLab.Core.Updates.TheOutsideWorld.Current = recorder;
        var panel = TargetsScreen.Open();
        try
        {
            panel.Select("GL-CF25-LTR.gltd.json");
            Dispatcher.UIThread.RunJobs();
            panel.OpenAsPdf();
            string opened = Assert.Single(recorder.Asked, a => a.What == "file").Target;
            Assert.EndsWith(".pdf", opened, StringComparison.Ordinal);
            Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(opened), 0, 4), StringComparison.Ordinal);
        }
        finally
        {
            GroupLab.Core.Updates.TheOutsideWorld.Current = before;
            panel.Close();
        }
    }
}
