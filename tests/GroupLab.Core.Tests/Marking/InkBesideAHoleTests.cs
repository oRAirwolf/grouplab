using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// Error report 50 (a tester's Discord video, 2026-11-17): on a store-bought diamond target with a hand-set scale, marking by hand, a click
/// on a ring hole touching the black diamond snapped down and left onto the diamond's edge, and the dashed preview sat away from the pointer.
/// With no artwork to say which dark pixels were printed, the snap weighed every dark pixel within one hole of the click, and the diamond's
/// ink is far darker and larger than a ring hole's grey rim. The page here is bull 3 of the video's sheet at 0.5 s, rebuilt from that frame's
/// measurements rather than its pixels, because the poster has not consented to public test data (samples/PROVENANCE.md): the frame is
/// 440 by 430 pixels around the bull, paper 255 in the brightest channel, the black band of the diamond 10 to 20 with the light scan lines
/// the frame shows, the orange middle as light as the paper in that channel, the bull's grid lines, and three holes, each a grey rim 17 to
/// 26 pixels from its centre round a light middle, as a flatbed with a white lid shows them. The centres are the frame's: the rim's circle
/// fitted on each hole. Against the frame itself, pressed at its centre and a quarter of a hole off in six directions, the old snap went 38
/// to 48 pixels off for the hole beside the diamond and this one 0.4 to 0.9; the hole below the diamond 0.0 to 0.2, and the one touching
/// the bull's frame line 1.3 to 2.2 (the old snap 12 there, its centre read by eye to about a pixel).
/// </summary>
public class InkBesideAHoleTests
{
    private const int Width = 440, Height = 430;
    private const double Radius = 52; // one hole across, as the marking screen sizes the snap

    private static readonly PointD BesideTheDiamond = new(261.3, 118.5);
    private static readonly PointD BelowTheDiamond = new(289.2, 312.7);
    private static readonly PointD BesideTheFrame = new(404.0, 281.5);

    private static GrayImage Page()
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
                // The diamond: a black band between the orange middle (light in the brightest channel) and the paper, with scan lines.
                int d = Math.Abs(x - 220) + Math.Abs(y - 216);
                if (d <= 97 && d > 50)
                {
                    Set(x, y, y % 6 == 0 ? 236 : 12 + (((x * 7) + (y * 3)) % 9));
                }

                // The bull's frame and its middle lines, broken where the diamond is.
                if (x is 5 or 6 or 432 or 433 || y is 6 or 7 or 423 or 424 || (x is 220 or 221 && (y < 105 || y > 327)) || (y is 216 && (x < 115 || x > 325)))
                {
                    Set(x, y, 40);
                }
            }
        }

        foreach (var hole in new[] { BesideTheDiamond, BelowTheDiamond, BesideTheFrame })
        {
            for (int y = (int)hole.Y - 30; y <= hole.Y + 30; y++)
            {
                for (int x = (int)hole.X - 30; x <= hole.X + 30; x++)
                {
                    double r = Math.Sqrt(((x - hole.X) * (x - hole.X)) + ((y - hole.Y) * (y - hole.Y)));
                    double angle = Math.Atan2(y - hole.Y, x - hole.X);
                    if (r >= 17 && r <= 26)
                    {
                        // A torn rim: clumps of grey from 90 to 190, with a few gaps as light as the paper.
                        Set(x, y, Math.Sin(7 * angle) > 0.9 ? 235 : (int)(140 + (50 * Math.Sin(5 * angle))));
                    }
                    else if (r < 17)
                    {
                        pixels[(y * Width) + x] = 250;
                    }
                }
            }
        }

        return new GrayImage(Width, Height, pixels);
    }

    private static double Off(PointD a, PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    public static TheoryData<string, double, double> Presses()
    {
        var data = new TheoryData<string, double, double>();
        foreach (var hole in new[] { nameof(BesideTheDiamond), nameof(BelowTheDiamond), nameof(BesideTheFrame) })
        {
            foreach (var (dx, dy) in new[] { (0.0, 0.0), (0.0, -13.0), (13.0, 0.0), (-9.0, 9.0), (9.0, -9.0), (-13.0, 0.0), (0.0, 13.0) })
            {
                data.Add(hole, dx, dy);
            }
        }

        return data;
    }

    private static PointD Hole(string name) => name switch
    {
        nameof(BesideTheDiamond) => BesideTheDiamond,
        nameof(BelowTheDiamond) => BelowTheDiamond,
        _ => BesideTheFrame,
    };

    [Theory]
    [MemberData(nameof(Presses))]
    public void APressOnAHoleBesideDarkPrintLandsOnThatHole(string hole, double dx, double dy)
    {
        var centre = Hole(hole);
        var press = new PointD(centre.X + dx, centre.Y + dy);
        var snapped = Snapping.ToHole(Page(), press, Radius, null).At;
        // Within a twentieth of a hole (the hole is about 52 pixels across); the old snap put the first of them on the diamond's edge.
        Assert.True(Off(snapped, centre) <= 2.6, $"a press at {press} on the hole at {centre} snapped to {snapped}, {Off(snapped, centre):0.0} pixels off");
    }

    [Fact]
    public void TheHoleIsFoundByItsOutlineAndNotByTheDiamond()
    {
        var found = Snapping.Outline(Page(), BesideTheDiamond, Radius);
        Assert.NotNull(found);
        Assert.True(Off(found.Value, BesideTheDiamond) <= 1.5, $"the outline was found at {found.Value}");
    }

    [Fact]
    public void APressOnPlainPrintWithNoHoleNearKeepsTheOldSnap()
    {
        // The diamond's bottom corner, far from every hole: no outline is found there, and the snap is the dark weight as it always was.
        var page = Page();
        var press = new PointD(220, 300);
        Assert.Null(Snapping.Outline(page, press, 20));
    }
}
