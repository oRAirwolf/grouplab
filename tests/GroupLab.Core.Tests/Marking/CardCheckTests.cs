using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Measurement;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Marking;

/// <summary>NOTES-FROM-PLANNING.md entries 272 and 273: a card laid on the check page's outline measures the printer, across and down.</summary>
public class CardCheckTests
{
    private const double Dpi = 200;

    /// <summary>
    /// The check page printed at <paramref name="printed"/>, straight on, with an ID-1 card of <paramref name="grey"/> laid on its outline,
    /// shifted by (<paramref name="dxMm"/>, <paramref name="dyMm"/>) and turned by <paramref name="degrees"/>, rounded corners and all.
    /// </summary>
    private static GrayImage WithCard(double printed, byte grey, double dxMm, double dyMm, double degrees)
    {
        var definition = BuiltIns.Load("GL-SCALE-LTR-1.gltd.json");
        var page = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi * printed);
        var pixels = (byte[])page.Pixels.Clone();
        double pxPerMm = Dpi / 25.4;
        var card = GridStyle4.Card(definition.Page);
        // The card's center on the printed page, where the outline's center printed, plus the shift.
        double cx = ((card.X + (GridStyle4.CardWidthDmm / 2.0)) / 10.0 * printed * pxPerMm) + (dxMm * pxPerMm);
        double cy = ((card.Y + (GridStyle4.CardHeightDmm / 2.0)) / 10.0 * printed * pxPerMm) + (dyMm * pxPerMm);
        double hw = GridStyle4.CardWidthMm / 2 * pxPerMm, hh = GridStyle4.CardHeightMm / 2 * pxPerMm, r = 3.18 * pxPerMm;
        double c = Math.Cos(degrees * Math.PI / 180), s = Math.Sin(degrees * Math.PI / 180);
        for (int y = (int)(cy - hw - 10); y < cy + hw + 10; y++)
        {
            for (int x = (int)(cx - hw - 10); x < cx + hw + 10; x++)
            {
                // Four samples a pixel, so the card's edge is antialiased as a camera would blur it.
                int inside = 0;
                foreach (var (ox, oy) in new[] { (0.25, 0.25), (0.75, 0.25), (0.25, 0.75), (0.75, 0.75) })
                {
                    double u = ((x + ox - cx) * c) + ((y + oy - cy) * s), v = (-(x + ox - cx) * s) + ((y + oy - cy) * c);
                    double qx = Math.Max(Math.Abs(u) - (hw - r), 0), qy = Math.Max(Math.Abs(v) - (hh - r), 0);
                    if (Math.Abs(u) <= hw && Math.Abs(v) <= hh && (qx * qx) + (qy * qy) <= r * r)
                    {
                        inside++;
                    }
                }

                int i = (y * page.Width) + x;
                pixels[i] = (byte)Math.Round(((pixels[i] * (4 - inside)) + (grey * inside)) / 4.0);
            }
        }

        return new GrayImage(page.Width, page.Height, pixels);
    }

    private static CardMeasure? Measure(GrayImage image)
    {
        var definition = BuiltIns.Load("GL-SCALE-LTR-1.gltd.json");
        var metadata = new ImageMetadata("JPEG", image.Width, image.Height, null, null, "Test", "Phone", 1, 6.25, 24);
        var measured = SheetMeasurer.Measure(image, metadata, definition, new MeasureOptions(), new OpenCvSharpBackend());
        Assert.Null(measured.Failure);
        return CardCheck.Measure(image, measured.Registration!.Mapping, definition, measured.Scale!.PixelsPerDmmArea * 10, focalPixels: 0);
    }

    [Theory]
    [InlineData(60, 0.0, 0.0, 0.0)]
    [InlineData(40, 0.6, -0.4, 1.2)]
    [InlineData(170, -0.5, 0.3, -0.8)]
    public void ACardMeasuresThePrinter(byte grey, double dx, double dy, double degrees)
    {
        var card = Measure(WithCard(0.992, grey, dx, dy, degrees));
        Assert.NotNull(card);
        Assert.InRange(card.Across, 0.992 - 0.0015, 0.992 + 0.0015);
        Assert.InRange(card.Down, 0.992 - 0.002, 0.992 + 0.002);
    }

    [Fact]
    public void NoCardIsNoMeasurement()
    {
        var definition = BuiltIns.Load("GL-SCALE-LTR-1.gltd.json");
        var page = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        Assert.Null(Measure(page));
    }

    /// <summary>The card's face is nearer the camera than the page, by its thickness, and looks that much larger.</summary>
    [Fact]
    public void TheCardsThicknessIsTakenOut()
    {
        var image = WithCard(1.0, 60, 0, 0, 0);
        var definition = BuiltIns.Load("GL-SCALE-LTR-1.gltd.json");
        var metadata = new ImageMetadata("JPEG", image.Width, image.Height, null, null, "Test", "Phone", 1, 6.25, 24);
        var measured = SheetMeasurer.Measure(image, metadata, definition, new MeasureOptions(), new OpenCvSharpBackend());
        double perMm = measured.Scale!.PixelsPerDmmArea * 10;
        // A focal length that puts the camera 400 mm away: the face 0.76 mm nearer looks 0.19 percent larger, and that is multiplied back.
        var card = CardCheck.Measure(image, measured.Registration!.Mapping, definition, perMm, focalPixels: 400 * perMm);
        Assert.NotNull(card);
        Assert.Equal(400 / (400 - 0.76), card.Lift, 9);
        Assert.InRange(card.Across, (1.0 * card.Lift) - 0.0015, (1.0 * card.Lift) + 0.0015);
    }
}
