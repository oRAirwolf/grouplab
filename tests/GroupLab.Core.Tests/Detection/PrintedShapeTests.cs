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

    /// <summary>
    /// Entry 352 item 2: a bold dark 6 on paper, strokes 10 px wide round a small counter, as the Eze-Scorer's printed 6s measured (stroke-like,
    /// 0.06 of it enclosed, its strokes one width all along).
    /// </summary>
    [Fact]
    public void ABoldDarkSixIsPrinting()
    {
        var six = Draw(230, 30, (x, y) =>
        {
            double d2 = (x * x) + ((y - 10) * (y - 10));
            return (d2 <= 15 * 15 && d2 >= 5 * 5) || (x >= -15 && x <= -5 && y >= -34 && y <= 10) || (y >= -34 && y <= -24 && x >= -15 && x <= 10);
        });
        Assert.StartsWith("strokes of one width round a small counter", PrintedShape.Why(six, Size / 2, (Size / 2) + 4, 42, dark: true));
    }

    /// <summary>
    /// Entry 352 item 2: a white 7 larger than the mark the finder saw in it, which runs off the first crop, is looked at whole and refused for
    /// its even strokes.
    /// </summary>
    [Fact]
    public void ALargeWhiteSevenIsPrintingThoughItRunsOffTheMark()
    {
        var seven = Draw(30, 230, (x, y) => (y >= -40 && y <= -30 && x >= -26 && x <= 26) || (Math.Abs(x - (26 - ((y + 40) * 0.45))) <= 5 && y >= -40 && y <= 40));
        Assert.StartsWith("strokes of one width all along", PrintedShape.Why(seven, (Size / 2) + 8, Size / 2, 40, dark: false));
    }

    /// <summary>
    /// A hole in a scan is often a dark crescent of rim, as even as a stroke: with no small closed counter it is kept (on the fifteen commercial
    /// scans, 44 real holes were refused by even strokes alone). A light hole with long spikes, as the scoreboards draw a torn one, is kept too.
    /// </summary>
    [Fact]
    public void AnEvenCrescentOfRimAndASpikyHoleAreKept()
    {
        var crescent = Draw(230, 30, (x, y) => (x * x) + (y * y) <= 22 * 22 && (x * x) + (y * y) >= 14 * 14 && x < 10);
        Assert.Null(PrintedShape.Why(crescent, Size / 2, Size / 2, 44, dark: true));
        var spiky = Draw(30, 230, (x, y) => Math.Sqrt((x * x) + (y * y)) <= 14 + (16 * Math.Pow(Math.Max(0, Math.Cos(5 * Math.Atan2(y, x))), 12)));
        Assert.Null(PrintedShape.Why(spiky, Size / 2, Size / 2, 36, dark: false));
    }

    /// <summary>A dark mark is never judged by its strokes: a torn hole's dark rim round a light centre is a thin ring.</summary>
    [Fact]
    public void ADarkRimRoundALightCentreIsKept()
    {
        var rim = Draw(230, 20, (x, y) => (x * x) + (y * y) <= 28 * 28 && (x * x) + (y * y) >= 23 * 23);
        Assert.Null(PrintedShape.Why(rim, Size / 2, Size / 2, 56, dark: true));
    }
}
