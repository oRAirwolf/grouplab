using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 499 section 3: the snap on a target with no design known should not move a mark somewhere worse than the
/// click. Error report 50's tester (build 231, the same self-made sheet of four boxes, each a black diamond round an orange one, a 600 dpi
/// flatbed scan with the lid closed) found two cases. A hole torn where the orange meets the black has dark print on one side and, in the
/// brightest channel, the orange as light as the paper on the other, so its outline is not a hole's and the dark weight pulls the mark onto
/// the black. Two touching rings have a joint outline that is not round. The pages here are rebuilt at the scale of <see
/// cref="InkBesideAHoleTests"/> (a hole 52 pixels across, its grey rim 17 to 26 pixels from its centre round a light middle) from the
/// scan's measurements, never its pixels, since the poster has not consented to public test data: the black band 47 pixels wide between
/// the orange and the paper, the torn hole's centre 12 pixels inside the band from the orange, its rim darker than the black only where
/// it crosses the orange (the brown of a burn), and the touching pair 41 pixels apart, centre to centre (0.205 in apart against 0.26 in
/// across on the scan).
/// </summary>
public class SnapStaysPutTests(ITestOutputHelper output)
{
    private const int Width = 440, Height = 430;
    private const double Radius = 52;

    // The diamond's middle, and the torn hole on its lower right edge where orange meets black.
    private static readonly PointD Middle = new(220, 216);
    private static readonly PointD TornThrough = new(220 + 44, 216 + 18);
    private static readonly PointD[] Touching = [new(90, 330), new(90, 371)];

    internal static GrayImage Page()
    {
        var pixels = new byte[Width * Height];
        Array.Fill(pixels, (byte)255);
        void Set(int x, int y, int level)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                pixels[(y * Width) + x] = (byte)Math.Min(pixels[(y * Width) + x], level);
            }
        }

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                // The black band of the diamond, 50 to 97 from the middle; the orange inside it is as light as the paper in this channel.
                int d = (int)(Math.Abs(x - Middle.X) + Math.Abs(y - Middle.Y));
                if (d <= 97 && d > 50)
                {
                    Set(x, y, y % 6 == 0 ? 236 : 12 + (((x * 7) + (y * 3)) % 9));
                }
            }
        }

        // The hole torn through: its middle as light as the paper wherever it is, and its torn rim grey where it crosses the orange (on the
        // black it is as dark as the black, so it shows there only as the black's own edge).
        for (int y = (int)TornThrough.Y - 30; y <= TornThrough.Y + 30; y++)
        {
            for (int x = (int)TornThrough.X - 30; x <= TornThrough.X + 30; x++)
            {
                double r = Math.Sqrt(((x - TornThrough.X) * (x - TornThrough.X)) + ((y - TornThrough.Y) * (y - TornThrough.Y)));
                double angle = Math.Atan2(y - TornThrough.Y, x - TornThrough.X);
                if (r < 17)
                {
                    pixels[(y * Width) + x] = 250;
                }
                else if (r <= 26)
                {
                    Set(x, y, Math.Sin(7 * angle) > 0.9 ? 235 : (int)(110 + (40 * Math.Sin(5 * angle))));
                }
            }
        }

        // Two rings touching on the paper, each as InkBesideAHoleTests draws one.
        foreach (var hole in Touching)
        {
            for (int y = (int)hole.Y - 30; y <= hole.Y + 30; y++)
            {
                for (int x = (int)hole.X - 30; x <= hole.X + 30; x++)
                {
                    double r = Math.Sqrt(((x - hole.X) * (x - hole.X)) + ((y - hole.Y) * (y - hole.Y)));
                    double angle = Math.Atan2(y - hole.Y, x - hole.X);
                    if (r >= 17 && r <= 26)
                    {
                        Set(x, y, Math.Sin(7 * angle) > 0.9 ? 235 : (int)(140 + (50 * Math.Sin(5 * angle))));
                    }
                }
            }
        }

        foreach (var hole in Touching)
        {
            for (int y = (int)hole.Y - 17; y <= hole.Y + 17; y++)
            {
                for (int x = (int)hole.X - 17; x <= hole.X + 17; x++)
                {
                    if (((x - hole.X) * (x - hole.X)) + ((y - hole.Y) * (y - hole.Y)) < 17 * 17)
                    {
                        pixels[(y * Width) + x] = 250;
                    }
                }
            }
        }

        return new GrayImage(Width, Height, pixels);
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    /// <summary>Clicks at each hole's centre and a quarter of a hole off it in six directions.</summary>
    private static IEnumerable<PointD> Clicks(PointD hole)
    {
        yield return hole;
        for (int k = 0; k < 6; k++)
        {
            double a = 2 * Math.PI * k / 6;
            yield return new PointD(hole.X + (13 * Math.Cos(a)), hole.Y + (13 * Math.Sin(a)));
        }
    }

    /// <summary>
    /// A click at either touching ring's centre, or a quarter of a hole off it in any direction, lands on that ring's own centre. Before
    /// entry 499 a click on the lower ring's centre went 41 pixels to the upper one, whose outline scored better.
    /// </summary>
    [Fact]
    public void EachOfTwoTouchingRingsKeepsItsOwnClicks()
    {
        var page = Page();
        foreach (var hole in Touching)
        {
            foreach (var click in Clicks(hole))
            {
                var snap = Snapping.ToHole(page, click, Radius, null);
                Assert.True(Distance(snap.At, hole) < 1, $"a click at {click} on the ring at {hole} went to {snap.At}");
                Assert.Null(snap.NotSnapped);
            }
        }
    }

    /// <summary>
    /// A click on the black band near its edge, with no hole within reach: no hole's outline is there, and the dark weight would pull the mark
    /// into the band, so it stays where it was placed and the notice line says so.
    /// </summary>
    [Fact]
    public void AClickOnPrintWithNoHoleNearStaysWhereItWasPlacedAndSaysSo()
    {
        var page = Page();
        var click = new PointD(Middle.X - 60, Middle.Y - 32);
        var snap = Snapping.ToHole(page, click, Radius, null);
        output.WriteLine($"{snap.At} {snap.NotSnapped}");
        Assert.Equal(click, snap.At);
        Assert.Equal(Snapping.StayedPut, snap.NotSnapped);
    }

    /// <summary>A hole on plain paper is found by its outline as before, and says nothing.</summary>
    [Fact]
    public void AHoleOnPaperStillSnapsToItsCentre()
    {
        var page = Page();
        var lone = new PointD(380, 80);
        var pixels = (byte[])page.Pixels.Clone();
        for (int y = 50; y <= 110; y++)
        {
            for (int x = 350; x <= 410; x++)
            {
                double r = Math.Sqrt(((x - lone.X) * (x - lone.X)) + ((y - lone.Y) * (y - lone.Y)));
                if (r >= 17 && r <= 26)
                {
                    pixels[(y * Width) + x] = (byte)(140 + (50 * Math.Sin(5 * Math.Atan2(y - lone.Y, x - lone.X))));
                }
            }
        }

        var withHole = new GrayImage(Width, Height, pixels);
        foreach (var click in Clicks(lone))
        {
            var snap = Snapping.ToHole(withHole, click, Radius, null);
            Assert.True(Distance(snap.At, lone) < 1.5, $"a click at {click} went to {snap.At}");
            Assert.Null(snap.NotSnapped);
        }
    }
}
