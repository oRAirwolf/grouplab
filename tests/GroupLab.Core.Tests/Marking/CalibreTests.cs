using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 24 section 5: the calibre as an optional group property, read from a pick list or free text, giving
/// extreme spread edge to edge, a snap radius of one bullet diameter, and a flag on a hole too large for it, with nothing gated on it.
/// </summary>
public class CalibreTests
{
    [Theory]
    [InlineData(".22 LR", 0.223)]
    [InlineData(".308 Win", 0.308)]
    [InlineData("0.338", 0.338)]
    [InlineData("6.5 Creedmoor", 6.5 / 25.4)]
    [InlineData("7.62 mm", 7.62 / 25.4)]
    [InlineData("9mm", 9 / 25.4)]
    [InlineData("308", 0.308)]
    [InlineData("22", 0.22)]
    [InlineData("30-06", 0.30)]
    [InlineData("17 HMR", 0.17)]
    public void TypedCalibresReadAsBulletDiameters(string text, double inches)
    {
        var calibre = Calibre.Parse(text, out string? problem);
        Assert.Null(problem);
        Assert.Equal(inches, calibre!.DiameterInches, 12);
    }

    [Theory]
    [InlineData("wildcat")]
    [InlineData("12 in")]
    [InlineData("5000")]
    public void TextThatIsNotADiameterSaysSo(string text)
    {
        Assert.Null(Calibre.Parse(text, out string? problem));
        Assert.NotNull(problem);
    }

    [Fact]
    public void NoCalibreIsNeitherAProblemNorARefusal()
    {
        Assert.Null(Calibre.Parse("  ", out string? problem));
        Assert.Null(problem);

        var session = FiveShots();
        var figures = GroupAnalysis.Analyse(session.State).AllShots!;
        Assert.NotNull(figures.MeanRadius);
        Assert.Null(figures.ExtremeSpreadEdgeToEdge);
        Assert.Equal("needs the group's calibre", figures.ExtremeSpreadEdgeToEdgeUnavailable);
        Assert.Null(HoleSize.SnapRadiusPixels(session.State, new PointD(50, 50)));
    }

    [Fact]
    public void WithACalibreExtremeSpreadIsReportedBothWaysAndTheFileKeepsIt()
    {
        var session = FiveShots();
        session.SetCalibre(Calibre.Parse(".308", out _));
        var figures = GroupAnalysis.Analyse(session.State).AllShots!;
        Assert.Equal(figures.ExtremeSpread!.Value + 0.308, figures.ExtremeSpreadEdgeToEdge!.Value, 12);
        Assert.Equal(30.8, HoleSize.SnapRadiusPixels(session.State, new PointD(50, 50))!.Value, 9);
        Assert.Equal(session.State.Calibre, MarkingFile.Read(MarkingFile.Write(session.State)).State.Calibre);
    }

    /// <summary>
    /// The apparent extent of the dark region under a mark, docs/SCAN-MEASUREMENTS.md section 3.5's pooled ratio: one .308 hole reads about the
    /// calibre, two touching holes marked as one read about twice it, and a mark on a large printed disc reads nothing at all, because the
    /// region runs to the edge of the search circle. It is a measurement and judges nothing (NOTES-FROM-PLANNING.md entry 87 section 1).
    /// </summary>
    [Fact]
    public void TheApparentExtentReadsOneHoleTwoHolesAndNothingOnInk()
    {
        var image = Paper();
        Disc(image, 60, 60, 14);
        Disc(image, 150, 47, 14);
        Disc(image, 176, 47, 14);
        Disc(image, 90, 170, 80);

        var session = new MarkingSession();
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        int single = session.AddShot(new PointD(60, 60));
        int pair = session.AddShot(new PointD(163, 47));
        int ink = session.AddShot(new PointD(90, 170));
        session.SetCalibre(new Calibre(".308", 0.308));

        double radius = HoleSize.CheckRadiusInDiameters * 0.308 * 100;
        Assert.InRange(HoleSize.ApparentExtentPixels(image, session.State.Find(single)!.Image, radius)!.Value / 100, 0.28, 0.31);
        Assert.InRange(HoleSize.ApparentExtentPixels(image, session.State.Find(pair)!.Image, radius)!.Value / 100, 0.53, 0.56);
        Assert.Null(HoleSize.ApparentExtentPixels(image, session.State.Find(ink)!.Image, radius));
    }

    private static MarkingSession FiveShots()
    {
        var session = new MarkingSession();
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        foreach (var p in new PointD[] { new(40, 40), new(70, 45), new(55, 80), new(30, 65), new(62, 58) })
        {
            session.AddShot(p);
        }

        return session;
    }

    private static GrayImage Paper() => new(300, 300, Enumerable.Repeat((byte)230, 300 * 300).ToArray());

    private static void Disc(GrayImage image, double cx, double cy, double radius)
    {
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                if (((x - cx) * (x - cx)) + ((y - cy) * (y - cy)) <= radius * radius)
                {
                    image.Pixels[(y * image.Width) + x] = 40;
                }
            }
        }
    }
}
