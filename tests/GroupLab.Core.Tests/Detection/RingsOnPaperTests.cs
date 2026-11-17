using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// Error report 50, part two (the tester's answer, 2026-11-17): build 225 on Windows 11, a 600 dpi flatbed scan with the lid closed of a
/// target they made themselves, four boxes each with a black diamond round an orange one, and "Find holes didn't work well". Every hole on
/// a flatbed with its lid closed is a ring: a grey torn rim about 0.055 in wide round a middle as light as the paper, 0.26 in across in all.
/// Find holes proposed one hole of about fifteen, because its dark pass opens away anything narrower than 0.064 in before it looks, and the
/// rims went with it. The page here is one of the scan's boxes rebuilt at 300 dpi from the scan's measurements, not its pixels, since the
/// tester has not consented to public test data (samples/PROVENANCE.md): the box 2.1 in square with its frame and middle lines, the black
/// diamond 1.0 in tip to tip round an orange one 0.54 in (as light as the paper in the brightest channel) with a printed cross in its
/// white centre, and the holes the scan has: one on clean paper, one touching the black diamond, one touching the frame line, one across a
/// printed line, and two touching each other. A printed thin ring the size of a hole (a letter's stroke) is there too, and is not a hole.
/// On the scan itself every ring is now found, centred by eye within a few pixels at 600 dpi, and the five store-bought blanks and the
/// any-target scoreboard's false marks are as they were.
/// </summary>
public class RingsOnPaperTests
{
    private const double Dpi = 300;
    private const int Size = 700;
    private const double Outer = 39, Inner = 22.5; // 0.26 in and 0.15 in across at 300 dpi

    private static readonly PointD OnPaper = new(520, 170);
    private static readonly PointD BesideTheDiamond = new(445, 265);
    private static readonly PointD BesideTheFrame = new(628, 450);
    private static readonly PointD AcrossALine = new(580, 350);
    private static readonly PointD[] Touching = [new(200, 560), new(200, 600)];
    private static readonly PointD PrintedRing = new(130, 130);

    private static GrayImage Page()
    {
        var pixels = new byte[Size * Size];
        Array.Fill(pixels, (byte)252);
        PointD[] holes = [OnPaper, BesideTheDiamond, BesideTheFrame, AcrossALine, .. Touching];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                int dx = x - 350, dy = y - 350, d = Math.Abs(dx) + Math.Abs(dy);
                int level = 252;
                if (d > 81 && d <= 150)
                {
                    level = y % 6 == 0 ? 236 : 12 + (((x * 7) + (y * 3)) % 9);
                }

                if ((Math.Abs(dx) <= 12 && Math.Abs(dy) <= 12 && (dx is 0 or 1 || dy is 0 or 1))
                    || x is 35 or 36 or 664 or 665 || y is 35 or 36 or 664 or 665
                    || (dx is 0 or 1 && (y < 200 || y > 500)) || (dy is 0 or 1 && (x < 200 || x > 500)))
                {
                    level = 30;
                }

                double printed = Math.Sqrt(((x - PrintedRing.X) * (x - PrintedRing.X)) + ((y - PrintedRing.Y) * (y - PrintedRing.Y)));
                if (Math.Abs(printed - 30) <= 1.5)
                {
                    level = 30;
                }

                // A hole: the paper is gone in its middle, whatever was printed there; round it a torn rim, clumps of grey with small gaps.
                double nearest = holes.Min(h => Math.Sqrt(((x - h.X) * (x - h.X)) + ((y - h.Y) * (y - h.Y))));
                var hole = holes.First(h => Math.Sqrt(((x - h.X) * (x - h.X)) + ((y - h.Y) * (y - h.Y))) == nearest);
                if (nearest < Inner)
                {
                    level = 250;
                }
                else if (nearest <= Outer)
                {
                    double angle = Math.Atan2(y - hole.Y, x - hole.X);
                    level = Math.Min(level, Math.Sin(7 * angle) > 0.9 ? 235 : (int)(140 + (50 * Math.Sin(5 * angle))));
                }

                pixels[(y * Size) + x] = (byte)level;
            }
        }

        return new GrayImage(Size, Size, pixels);
    }

    private static double Off(PointD a, PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y))) / Dpi;

    [Fact]
    public void EveryRingOnTheTestersBoxIsProposedOnItsCentre()
    {
        var found = AnyTargetHoleFinder.Find(Page(), Dpi, new OpenCvSharpBackend());

        foreach (var hole in new[] { OnPaper, BesideTheDiamond, BesideTheFrame, AcrossALine })
        {
            var nearest = found.Holes.MinBy(h => Off(h.Image, hole));
            Assert.True(nearest is not null && Off(nearest.Image, hole) < 0.02,
                $"the ring at {hole.X},{hole.Y} was {(nearest is null ? "not found" : $"proposed {Off(nearest.Image, hole):0.000} in off")}");
            Assert.InRange(nearest!.DiameterInches, 0.2, 0.32);
        }

        // Two touching holes are two marks, both sent to be checked.
        foreach (var hole in Touching)
        {
            var nearest = found.Holes.MinBy(h => Off(h.Image, hole))!;
            Assert.True(Off(nearest.Image, hole) < 0.04, $"the touching hole at {hole.X},{hole.Y} was proposed {Off(nearest.Image, hole):0.000} in off");
            Assert.NotNull(nearest.Doubt);
        }

        Assert.NotEqual(found.Holes.MinBy(h => Off(h.Image, Touching[0])), found.Holes.MinBy(h => Off(h.Image, Touching[1])));
    }

    [Fact]
    public void PrintingOnTheBoxIsNotProposed()
    {
        var found = AnyTargetHoleFinder.Find(Page(), Dpi, new OpenCvSharpBackend());
        PointD[] holes = [OnPaper, BesideTheDiamond, BesideTheFrame, AcrossALine, .. Touching];

        // Nothing but the six holes: not the thin printed ring, not the cross in the diamond's white centre, not the orange diamond.
        var stray = found.Holes.Where(h => holes.All(hole => Off(h.Image, hole) > 0.1)).ToList();
        Assert.True(stray.Count == 0, "proposed where nothing was shot: " + string.Join("; ", stray.Select(h => $"{h.Image.X:0},{h.Image.Y:0}")));
        Assert.Equal(6, found.Holes.Count);
    }
}
