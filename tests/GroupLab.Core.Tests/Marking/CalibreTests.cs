using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 24 section 5: the calibre as an optional group property, read from a pick list or free text, giving
/// extreme spread edge to edge, a snap radius of one bullet diameter, and a flag on a hole too large for it, with nothing gated on it.
/// </summary>
public class CalibreTests
{
    /// <summary>
    /// Entry 107 section 1, Alan's decision: the calibre is a diameter, in inches, or in millimetres marked mm, and nothing else. Every diameter
    /// in the pick list reads exactly in both units.
    /// </summary>
    [Fact]
    public void EveryPickListDiameterReadsExactlyInBothUnits()
    {
        Assert.Equal(37, Calibre.Diameters.Distinct().Count());
        Assert.Equal((0.172, 0.510), (Calibre.Diameters.Min(), Calibre.Diameters.Max()));
        Assert.DoesNotContain(0.223, Calibre.Diameters);
        foreach (double inches in Calibre.Diameters)
        {
            string decimalInches = inches.ToString(".000#", System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(inches, Calibre.Parse(decimalInches, out _)!.DiameterInches, 12);
            Assert.Equal(inches, Calibre.Parse("0" + decimalInches, out _)!.DiameterInches, 12);
            Assert.Equal(inches, Calibre.Parse(decimalInches + " in", out _)!.DiameterInches, 12);
            Assert.Equal(inches, Calibre.Parse(Calibre.Shown(inches), out _)!.DiameterInches, 12);
            string mm = (inches * 25.4).ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(inches, Calibre.Parse(mm + " mm", out _)!.DiameterInches, 12);
            Assert.Equal(inches, Calibre.Parse(mm + "mm", out _)!.DiameterInches, 12);
        }
    }

    [Theory]
    [InlineData("0.308", 0.308)]
    [InlineData(".308", 0.308)]
    [InlineData("0.308 in", 0.308)]
    [InlineData("0.308\"", 0.308)]
    [InlineData("7.82 mm", 7.82 / 25.4)]
    [InlineData("7.82mm", 7.82 / 25.4)]
    [InlineData(".223", 0.223)]
    public void ADiameterReadsAsTyped(string text, double inches)
    {
        var calibre = Calibre.Parse(text, out string? problem);
        Assert.Null(problem);
        Assert.Equal(inches, calibre!.DiameterInches, 12);
        Assert.Equal(Calibre.Shown(inches), calibre.Name);
    }

    /// <summary>
    /// Names are refused with the one sentence that says what to type, and so is a bare number of one or more, which the old rule guessed as
    /// millimetres, hundredths or thousandths: "7.62" read as 0.300 in, a diameter no 7.62 bullet has.
    /// </summary>
    [Theory]
    [InlineData("300 Blackout")]
    [InlineData("6.5 Creedmoor")]
    [InlineData("30 Cal.")]
    [InlineData("9mm Luger")]
    [InlineData("7.62")]
    [InlineData("308")]
    [InlineData("22")]
    [InlineData("6.5")]
    public void NamesAndBareNumbersAreRefusedWithWhatToType(string text)
    {
        Assert.Null(Calibre.Parse(text, out string? problem));
        Assert.Equal(Calibre.Refusal, problem);
    }

    /// <summary>
    /// "9mm" is a number marked mm, so under entry 107 section 1's rule it reads as 9 mm, 0.354 in, although the entry's own test list says it
    /// should be refused. No syntax tells a metric calibre name from a diameter in millimetres, so which one wins is question 22 of
    /// docs/QUESTIONS-FOR-PLANNING.md; this pins the rule as written until it is answered.
    /// </summary>
    [Fact]
    public void NineMillimetresMarkedMmIsADiameter() => Assert.Equal(9 / 25.4, Calibre.Parse("9mm", out _)!.DiameterInches, 12);

    /// <summary>A marking saved under a calibre name before entry 107 loads with its diameter, shown in both units; the old name is not displayed.</summary>
    [Fact]
    public void AMarkingSavedUnderANameLoadsWithItsDiameter()
    {
        var session = FiveShots();
        session.SetCalibre(new Calibre(".308, 7.62 mm", 0.308));
        var read = MarkingFile.Read(MarkingFile.Write(session.State)).State.Calibre!;
        Assert.Equal(0.308, read.DiameterInches, 12);
        Assert.Equal(".308 in (7.82 mm)", read.Name);
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

        // NOTES-FROM-PLANNING.md entry 98 section 2: without a calibre the snap is still sized in sheet units, to a nominal .30 hole, so no zoom
        // or layout can move it; before, it fell back to screen pixels.
        var at = new PointD(50, 50);
        Assert.Equal(HoleSize.NominalHoleInches * HoleSize.PixelsPerInch(session.State.Scale!, at), HoleSize.SnapRadiusPixels(session.State, at)!.Value, 9);
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
