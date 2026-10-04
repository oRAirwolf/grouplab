using GroupLab.Cli.Imaging;
using GroupLab.Cli.Library;
using GroupLab.Core.Imaging;
using GroupLab.Core.Rendering;
using GroupLab.Core.ScaleMarkers;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>NOTES-FROM-PLANNING.md entry 365: the marker finder on pictures, the pages read back, and a card blanked without exception.</summary>
public class Entry365Tests
{
    [Theory]
    [InlineData(MarkerKind.Bracket, MarkerPaper.Letter, 555, 8)]
    [InlineData(MarkerKind.InchBar, MarkerPaper.Letter, 563, 4)]
    [InlineData(MarkerKind.MetricBar, MarkerPaper.A4, 567, 4)]
    [InlineData(MarkerKind.Sticker, MarkerPaper.A4, 571, 16)]
    public void EveryCodeOnAPrintedPageIsReadBack(MarkerKind kind, MarkerPaper paper, int first, int count)
    {
        var page = Assert.Single(ScaleMarkerPages.Pages(kind, paper));
        var grey = SceneRasterizer.Rasterize(page, 100);
        var ids = ScaleMarkerFinder.Codes(grey).Select(m => m.Id).ToList();
        Assert.Equal(Enumerable.Range(first, count), ids);
    }

    [Fact]
    public void ACardIsFoundByItsRoundedCornersAndBlankedInEveryCopy()
    {
        // A dark card, turned 12 degrees, on white paper: 85.60 by 53.98 mm at 5 px a millimetre, its corners rounded 3.18 mm.
        using var colour = new Mat(1500, 2000, MatType.CV_8UC3, Scalar.White);
        double s = 5, angle = 12 * Math.PI / 180, cx = 1100, cy = 800;
        var card = new List<Point>();
        PointD At(double x, double y) => new(cx + (Math.Cos(angle) * x) - (Math.Sin(angle) * y), cy + (Math.Sin(angle) * x) + (Math.Cos(angle) * y));
        double w = ScaleMarkerLayout.CardWidth * s / 2, h = ScaleMarkerLayout.CardHeight * s / 2, r = ScaleMarkerLayout.CardRadius * s;
        foreach (var (ox, oy, start) in new[] { (w - r, -h + r, -90), (w - r, h - r, 0), (-w + r, h - r, 90), (-w + r, -h + r, 180) })
        {
            for (int a = 0; a <= 90; a += 5)
            {
                double t = (start + a) * Math.PI / 180;
                var p = At(ox + (r * Math.Cos(t)), oy + (r * Math.Sin(t)));
                card.Add(new Point((int)Math.Round(p.X), (int)Math.Round(p.Y)));
            }
        }

        Cv2.FillPoly(colour, [card.ToArray()], new Scalar(120, 60, 20), LineTypes.AntiAlias);
        Cv2.Rectangle(colour, new Rect(200, 200, 300, 300), Scalar.Black, 3);
        using var greyMat = new Mat();
        Cv2.CvtColor(colour, greyMat, ColorConversionCodes.BGR2GRAY);
        var grey = OpenCvSharpBackend.Copy(greyMat);
        var also = OpenCvSharpBackend.Copy(greyMat);
        var trace = new List<string>();
        ScaleMarkerFinder.Card(OpenCvSharpBackend.Copy(greyMat), null, trace.Add);
        var sighting = ScaleMarkerFinder.Find(grey, colour, true, also);
        Assert.True(sighting.Card is not null, string.Join(" | ", trace));

        var found = Assert.IsType<CardSighting>(sighting.Card);
        PointD[] truth = [At(-w, -h), At(w, -h), At(w, h), At(-w, h)];
        Assert.All(found.Corners, c => Assert.True(truth.Min(t => Math.Sqrt(Math.Pow(t.X - c.X, 2) + Math.Pow(t.Y - c.Y, 2))) < 1.5, c.ToString()));

        // The card's middle is now plain grey in the picture and both grey copies: nothing of it is left to keep, show or send.
        Assert.Equal(128, grey[(int)cx, (int)cy]);
        Assert.Equal(128, also[(int)cx, (int)cy]);
        Assert.Equal(new Vec3b(128, 128, 128), colour.At<Vec3b>((int)cy, (int)cx));

        var (finding, _) = ScaleMarkerReading.Read([], found, null, []);
        Assert.Contains(ScaleMarkerWords.CardLeast, finding!.Says(0.01), StringComparison.Ordinal);
        Assert.Equal(1 / s / 25.4 * 25.4, finding.Fit.MillimetresPerPixel(new PointD(cx, cy)), 2);
    }

    /// <summary>
    /// Request 76's first photos (Alan, 2026-10-04, on the kitchen table, square on, no printer check): the size each marker gives the target
    /// through Add a store-bought target. The bars agree with the Rigid crosshair's reference file (11.9 in) once they are painted out of
    /// the corner search; the brackets give the rectangle they enclose, larger by any gap between the paper and the pieces (on the
    /// Eze-Scorer grid Alan left one on purpose). The card photos are not here: a light card lying on white paper is not found yet.
    /// </summary>
    [Theory]
    [InlineData("table-rigid-crosshair-bar.jpg", 11.95, 11.90)]
    [InlineData("table-rigid-crosshair-two-bars-L.jpg", 11.91, 11.91)]
    [InlineData("table-rigid-crosshair-brackets.jpg", 12.05, 12.01)]
    [InlineData("table-eze-scorer-grid-bar.jpg", 11.99, 11.99)]
    [InlineData("table-eze-scorer-grid-two-bars-L.jpg", 11.88, 12.02)]
    [InlineData("table-eze-scorer-grid-brackets.jpg", 12.21, 12.21)]
    public void OnAlansPhotosTheMarkersGiveTheTargetsSize(string file, double width, double height)
    {
        string path = Path.Combine(@"C:\Dev\grouplab-local\commercial-targets\wood-2026-10-04\markers", file);
        if (!File.Exists(path))
        {
            return;
        }

        using var session = new FingerprintSession(() => []);
        Assert.Null(session.Load(path));
        Assert.NotNull(session.Markers);
        Assert.Equal(GroupLab.Core.StoreTargets.ScaleSource.Markers, session.Source);
        Assert.Null(session.Next());
        Assert.Null(session.Next());
        Assert.Null(session.Next());
        var target = session.Target!;
        Assert.True(Math.Abs(target.WidthInches - width) < 0.03 && Math.Abs(target.HeightInches - height) < 0.03, $"{target.WidthInches:0.000} by {target.HeightInches:0.000} in");
    }

    [Fact]
    public void APlainPrintedBoxIsNotTakenForACard()
    {
        using var colour = new Mat(1500, 2000, MatType.CV_8UC3, Scalar.White);
        Cv2.Rectangle(colour, new Rect(600, 500, 428, 270), new Scalar(40, 40, 40), -1);
        using var greyMat = new Mat();
        Cv2.CvtColor(colour, greyMat, ColorConversionCodes.BGR2GRAY);
        Assert.Null(ScaleMarkerFinder.Card(OpenCvSharpBackend.Copy(greyMat)));
    }
}
