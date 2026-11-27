using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// Error report 60, the .338 sets of 2026-11-26: sheets shot over a white paper backer and scanned at 150 dpi read 0 to 3 of about 10 holes a
/// page, because a hole there is the backer's white with torn fibers scattered round it, every speck too small to be a hole and no outline
/// closing. These synthetic sheets draw such holes on bare paper as gray specks in a ring of a .338 hole, beside a cluster of red pen
/// writing and a dashed gray line: on a scan with the caliber named the holes are found from their specks and nothing else is.
/// </summary>
public class WhiteBackerScanTests
{
    private const double Dpi = 150;
    private const double HoleInches = 0.32;
    private const string Target = "GL-CF9-LTR-E.gltd.json";

    private static (GrayImage Value, GrayImage Luminance, IPageMapping Truth, GrayImage Render, List<PointD> Holes) Sheet()
    {
        var definition = BuiltIns.Load(Target);
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var observed = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, [], [], new Random(6026));
        var luminance = (byte[])observed.Pixels.Clone();
        var value = (byte[])observed.Pixels.Clone();
        int width = observed.Width, height = observed.Height;
        var bulls = definition.Bulls.Select(b => truth.ToImage(new PointD(b.X, b.Y))).ToList();
        var random = new Random(338);

        // Somewhere beside a bull where a disc of a hole and a margin is bare paper.
        PointD? BarePaper(PointD bull, double from)
        {
            for (double angle = from; angle < from + (2 * Math.PI); angle += 0.15)
            {
                for (double reach = 0.5; reach <= 1.6; reach += 0.05)
                {
                    var at = new PointD(bull.X + (reach * Dpi * Math.Cos(angle)), bull.Y + (reach * Dpi * Math.Sin(angle)));
                    bool bare = true;
                    for (int y = (int)(at.Y - (0.22 * Dpi)); bare && y <= at.Y + (0.22 * Dpi); y++)
                    {
                        for (int x = (int)(at.X - (0.22 * Dpi)); bare && x <= at.X + (0.22 * Dpi); x++)
                        {
                            bare = x >= 0 && y >= 0 && x < width && y < height && render.Pixels[(y * width) + x] > 230;
                        }
                    }

                    if (bare)
                    {
                        return at;
                    }
                }
            }

            return null;
        }

        // A speck of torn paper: a few pixels of gray, or of a red pen, bright in its own color and dark in luminance.
        void Speck(double cx, double cy, bool red)
        {
            int size = random.Next(1, 3);
            for (int y = (int)cy; y <= (int)cy + size; y++)
            {
                for (int x = (int)cx; x <= (int)cx + size; x++)
                {
                    luminance[(y * width) + x] = red ? (byte)90 : (byte)140;
                    value[(y * width) + x] = red ? (byte)195 : (byte)140;
                }
            }
        }

        // Three .338 holes: twenty two specks in a ring round each, nothing in the middle, which is the white backer.
        var holes = new List<PointD>();
        for (int k = 0; k < 3; k++)
        {
            var at = BarePaper(bulls[k], 0.3 + (k * 1.1)) ?? throw new InvalidOperationException("no bare paper beside bull " + k);
            holes.Add(truth.ToPage(at));
            for (int n = 0; n < 22; n++)
            {
                double angle = ((n + random.NextDouble()) / 22) * 2 * Math.PI, reach = (0.09 + (0.07 * random.NextDouble())) * Dpi;
                Speck(at.X + (reach * Math.Cos(angle)), at.Y + (reach * Math.Sin(angle)), red: false);
            }
        }

        // Red pen writing, as round and as many specks as a hole, beside a fourth bull, and a dashed gray line beside a fifth.
        var writing = BarePaper(bulls[3], 2.0) ?? throw new InvalidOperationException("no bare paper beside bull 3");
        for (int n = 0; n < 22; n++)
        {
            double angle = random.NextDouble() * 2 * Math.PI, reach = random.NextDouble() * 0.15 * Dpi;
            Speck(writing.X + (reach * Math.Cos(angle)), writing.Y + (reach * Math.Sin(angle)), red: true);
        }

        var line = BarePaper(bulls[4], 4.0) ?? throw new InvalidOperationException("no bare paper beside bull 4");
        for (int n = -12; n <= 12; n++)
        {
            Speck(line.X + (n * 0.025 * Dpi), line.Y + (n * 0.008 * Dpi), red: false);
        }

        return (new GrayImage(width, height, value), new GrayImage(width, height, luminance), truth, render, holes);
    }

    private static double Inches(IPageMapping truth, double x, double y, PointD page)
    {
        var p = truth.ToPage(new PointD(x, y));
        return Math.Sqrt(Math.Pow(p.X - page.X, 2) + Math.Pow(p.Y - page.Y, 2)) / 254;
    }

    private static RenderDifferenceResult Detect(GrayImage value, GrayImage luminance, IPageMapping truth, GrayImage render, bool scan, double? calibre) =>
        RenderDifferenceHoleDetector.Detect(value, BuiltIns.Load(Target), 0, truth, Dpi, new OpenCvSharpBackend(),
            new RenderDifferenceOptions(Scan: scan, CalibreInches: calibre), render, luminance);

    [Fact]
    public void OnAScanWithTheCaliberTheHolesAreFoundFromTheirTornPaper()
    {
        var (value, luminance, truth, render, holes) = Sheet();
        var result = Detect(value, luminance, truth, render, scan: true, HoleInches);

        foreach (var hole in holes)
        {
            Assert.Contains(result.Holes, h => h.Fibres && Inches(truth, h.X, h.Y, hole) <= 0.05);
        }

        // The red writing and the dashed line are not holes.
        Assert.Equal(holes.Count, result.Holes.Count);
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, HoleInches)]
    public void WithoutAScanAndACaliberTheSpecksAreLeftAsTheyWere(bool scan, double? calibre)
    {
        // The specks say where a hole is only at a caliber's size, and only a flat scan's paper is white enough to read them on.
        var (value, luminance, truth, render, _) = Sheet();
        Assert.DoesNotContain(Detect(value, luminance, truth, render, scan, calibre).Holes, h => h.Fibres);
    }

    [Fact]
    public void ASpeckClusterAlongALineIsNotAHole()
    {
        // Short dashes along one line fill a disc as well as a hole's specks do, and lie thirty times longer than wide.
        int width = 200, height = 200;
        var over = new byte[width * height];
        for (int y = 98; y <= 101; y++)
        {
            for (int x = 70; x <= 130; x += 3)
            {
                over[(y * width) + x] = 1;
                over[(y * width) + x + 1] = 1;
            }
        }

        Assert.NotEmpty(RenderDifferenceHoleDetector.SpeckClusters(over, null, width, height, Dpi, 24, (_, _) => false));

        var clusters = RenderDifferenceHoleDetector.SpeckClusters(over, null, width, height, Dpi, 24, (_, _) => false);
        Assert.DoesNotContain(clusters, RenderDifferenceHoleDetector.IsFibresHole);
    }
}
