using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 417, Quack's 3-sheet .223 set scanned on a flatbed: a hole seen through to the scanner's white lid is as light
/// as the paper, so only its rim is dark, and where the rim is faint the ring has gaps. Detection refused such holes on white paper as "not
/// compact" (four found of nine on this page), and the Impact tool's snap went to the rim's darker side, up to a quarter of a hole off. The
/// page is his second sheet of GL-CF9-T-C, reduced to 300 dpi (samples/PROVENANCE.md), read through the same path as a scan with a printer
/// chosen, with a calibre and without one. The holes are where the 600 dpi original's rims put them, each checked by eye; the ninth touches a
/// printed marker, where no hole is looked for.
/// </summary>
public class LightHoleTests
{
    private const double Dpi = 300;

    private static readonly PointD[] Holes =
    [
        new(1518.7, 313.5), new(460.9, 513.9), new(2088.4, 1049.7), new(860.9, 1079.0), new(2024.3, 1380.8), new(1516.1, 1915.0),
        new(593.0, 2342.4), new(1806.7, 2414.1),
    ];

    private static readonly PointD BesideAMarker = new(1395.8, 2703.5);

    private static string Page => Path.Combine(Repo.PathTo("samples"), "scan-cf9-t-c-tile-2-light-holes-300-dpi.png");

    private static double Inches(PointD a, PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y))) / Dpi;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EveryHoleWithALightMiddleIsFoundWhereItIs(bool withCalibre)
    {
        var (grey, metadata) = ImageLoader.Load(Page);
        var (value, _) = ImageLoader.LoadMaxChannel(Page);
        var printer = new PrinterProfile("Brother", 0.997, 0.997, PrinterMethod.Scan, new DateOnly(2026, 10, 10), 0.001);
        var result = AutomaticMarking.Run(grey, value, metadata, BuiltIns.Load("GL-CF9-T-C.gltd.json"), new OpenCvSharpBackend(),
            calibre: withCalibre ? Calibre.Of(0.224) : null, printer: printer);
        Assert.Null(result.Failure);
        var found = result.Detections.Select(d => d.Image).ToList();
        foreach (var hole in Holes)
        {
            double off = found.Min(f => Inches(f, hole));
            Assert.True(off <= 0.02, $"the hole at {hole} was placed {off:0.000} in off");
        }

        // Nothing else is marked: every mark is one of the holes, the one beside the marker included if it is found.
        Assert.All(found, f => Assert.True(Holes.Append(BesideAMarker).Min(h => Inches(f, h)) <= 0.02, $"a mark at {f} is on no hole"));
    }

    [Fact]
    public void APressAQuarterOfAHoleOffSnapsToTheMiddle()
    {
        // With the sheet read, as the marking screen snaps: the expected artwork tells a printed marker's ink from a hole's rim.
        var (grey, metadata) = ImageLoader.Load(Page);
        var (value, _) = ImageLoader.LoadMaxChannel(Page);
        var artwork = AutomaticMarking.Run(grey, value, metadata, BuiltIns.Load("GL-CF9-T-C.gltd.json"), new OpenCvSharpBackend()).ExpectedArtwork;
        Assert.NotNull(artwork);
        const double Hole = 0.21;
        double radius = (Hole / 2 * Dpi) + 12;
        // The holes on white paper, the case this fixes. Two more sit on the paper but are left out here: the one at the top of bull 1 has part
        // of its rim on a printed marker's ink, which the snap rightly leaves out, and the one beside the marker below. The four on the black
        // diamonds are light holes in black ink, which the dark-weight snap was never for. Detection places every one of them.
        PointD[] onPaper = [new(1518.7, 313.5), new(860.9, 1079.0), new(1516.1, 1915.0)];
        foreach (var centre in onPaper)
        {
            foreach (var (dx, dy) in new[] { (0.0, 1.0), (1.0, 0.0), (-0.7, -0.7) })
            {
                var press = new PointD(centre.X + (dx * Hole / 4 * Dpi), centre.Y + (dy * Hole / 4 * Dpi));
                var snapped = Snapping.ToHole(value, press, radius, artwork).At;
                Assert.True(Inches(snapped, centre) <= 0.02, $"a press at {press} snapped {Inches(snapped, centre):0.000} in from the middle of the hole at {centre}");
            }
        }
    }
}
