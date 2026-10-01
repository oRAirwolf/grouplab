using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 331 section 1: the printed shapes the store-bought blanks showed as false marks, refused, and a hole of the
/// same size kept, whichever way round its tones are.
/// </summary>
public class PrintedShapeTests
{
    private const int Size = 160;

    private static GrayImage Draw(byte background, byte ink, Func<double, double, bool> inside)
    {
        var pixels = new byte[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                pixels[(y * Size) + x] = inside(x - (Size / 2.0), y - (Size / 2.0)) ? ink : background;
            }
        }

        return new GrayImage(Size, Size, pixels);
    }

    [Fact]
    public void ASolidPrintedDiamondIsPrinting()
    {
        var diamond = Draw(230, 20, (x, y) => Math.Abs(x) + Math.Abs(y) <= 28);
        Assert.StartsWith("a solid shape with straight sides", PrintedShape.Why(diamond, Size / 2, Size / 2, 56, dark: true));
    }

    [Fact]
    public void ARoundHoleIsNotPrintingEitherWayRound()
    {
        var dark = Draw(230, 20, (x, y) => (x * x) + (y * y) <= 28 * 28);
        var light = Draw(20, 230, (x, y) => (x * x) + (y * y) <= 28 * 28);
        Assert.Null(PrintedShape.Why(dark, Size / 2, Size / 2, 56, dark: true));
        Assert.Null(PrintedShape.Why(light, Size / 2, Size / 2, 56, dark: false));
    }

    /// <summary>A white "7" on black, strokes 6 px wide: what the EZE scorer's ring numbers looked like.</summary>
    [Fact]
    public void AWhiteNumberInPrintIsPrinting()
    {
        var seven = Draw(20, 230, (x, y) => (y >= -24 && y <= -18 && x >= -16 && x <= 16) || (Math.Abs(x - (16 - ((y + 18) * 0.4))) <= 3 && y >= -18 && y <= 24));
        Assert.StartsWith("thin strokes", PrintedShape.Why(seven, Size / 2, Size / 2, 50, dark: false));
    }

    /// <summary>A dark mark is never judged by its strokes: a torn hole's dark rim round a light centre is a thin ring.</summary>
    [Fact]
    public void ADarkRimRoundALightCentreIsKept()
    {
        var rim = Draw(230, 20, (x, y) => (x * x) + (y * y) <= 28 * 28 && (x * x) + (y * y) >= 23 * 23);
        Assert.Null(PrintedShape.Why(rim, Size / 2, Size / 2, 56, dark: true));
    }
}
