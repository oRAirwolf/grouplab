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
    /// Entry 105 section 7 changed four of these, each from a name read as if it were a diameter to the bullet it fires: "6.5 Creedmoor" was
    /// 0.2559 and is .264, "22" was 0.220 and is .224, "17 HMR" was 0.170 and is .172. "9mm", "7.62 mm" and "30-06" moved to the ambiguous
    /// names below, because each is fired as more than one diameter. A wildcat with no name in the table still reads by its leading number.
    /// </summary>
    [Theory]
    [InlineData(".22 LR", 0.223)]
    [InlineData(".308 Win", 0.308)]
    [InlineData("0.338", 0.338)]
    [InlineData("6.5 Creedmoor", 0.264)]
    [InlineData("308", 0.308)]
    [InlineData("22", 0.224)]
    [InlineData("17 HMR", 0.172)]
    [InlineData("300 Win Mag", 0.300)]
    [InlineData("6.5 mm", 0.264)]
    [InlineData("6.5MM", 0.264)]
    [InlineData("8mm", 0.323)]
    [InlineData("10mm", 0.400)]
    [InlineData("9.3mm", 0.366)]
    [InlineData("5.45", 0.2215)]
    [InlineData("270", 0.277)]
    [InlineData(".270", 0.270)]
    [InlineData("270 cal", 0.277)]
    [InlineData("35 Cal. .357", 0.357)]
    public void TypedCalibresReadAsBulletDiameters(string text, double inches)
    {
        var calibre = Calibre.Parse(text, out string? problem);
        Assert.Null(problem);
        Assert.Equal(inches, calibre!.DiameterInches, 12);
    }

    /// <summary>Entry 105 section 7's table, all 44 rows, each once by its name and once by its diameter.</summary>
    public static TheoryData<string, double> Rows() => new()
    {
        { "17 Cal.", 0.172 }, { "20 Cal.", 0.204 }, { "5.45 Cal.", 0.2215 }, { "22 Cal.", 0.224 }, { "6mm", 0.243 }, { "25 Cal.", 0.257 },
        { "6.5mm", 0.264 }, { "270 Cal.", 0.277 }, { "7mm", 0.284 }, { "30 Cal.", 0.308 }, { "7.62mm", 0.310 }, { "303 Cal.", 0.3105 },
        { "303 Cal.", 0.312 }, { "32 Cal.", 0.321 }, { "8mm", 0.323 }, { "338 Cal.", 0.338 }, { "35 Cal.", 0.355 }, { "35 Cal.", 0.357 },
        { "35 Cal.", 0.358 }, { "9.3mm", 0.366 }, { "375 Cal.", 0.375 }, { "400 Cal.", 0.410 }, { "405 Cal.", 0.411 }, { "416 Cal.", 0.416 },
        { "423 Cal.", 0.423 }, { "44 Cal.", 0.430 }, { "45 Cal.", 0.452 }, { "45 Cal.", 0.458 }, { "470 Cal.", 0.474 }, { "505 Cal.", 0.505 },
        { "50 Cal.", 0.510 },
        { "30 Cal.", 0.309 }, { "32 Cal.", 0.312 }, { "9mm", 0.355 }, { "9mm", 0.356 }, { "38 Cal.", 0.357 }, { "38 Cal.", 0.358 },
        { "10mm", 0.400 }, { "41 Cal.", 0.410 }, { "44 Cal.", 0.430 }, { "45 Cal.", 0.451 }, { "45 Cal.", 0.452 }, { "45 Cal.", 0.454 },
        { "50 Cal.", 0.500 },
    };

    /// <summary>The nine names fired as more than one diameter: eight from the table, and 7.62, which the old pick list put at .308.</summary>
    private static readonly HashSet<string> Ambiguous = ["30 Cal.", "303 Cal.", "32 Cal.", "35 Cal.", "38 Cal.", "45 Cal.", "50 Cal.", "9mm", "7.62mm"];

    [Fact]
    public void TheTableHasEveryRowOfTheTwoListsAndNineAmbiguousNames()
    {
        Assert.Equal(44, Rows().Count);
        Assert.Equal(42, Rows().Select(r => ((string)r[0], (double)r[1])).Distinct().Count());
        var byName = Calibre.Table.GroupBy(c => c.Name).Where(g => g.Select(c => c.DiameterInches).Distinct().Count() > 1).Select(g => g.Key).ToHashSet();
        Assert.Equal(8, byName.Count);
        Assert.Subset(Ambiguous, byName);
    }

    /// <summary>Typed as its diameter, every row reads exactly that diameter, and it always did.</summary>
    [Theory]
    [MemberData(nameof(Rows))]
    public void EveryRowTypedAsItsDiameterReadsIt(string name, double inches)
    {
        _ = name;
        string typed = inches.ToString(".000#", System.Globalization.CultureInfo.InvariantCulture);
        var reading = Calibre.Read(typed);
        Assert.Equal(inches, reading.Calibre!.DiameterInches, 12);
        Assert.Empty(reading.Candidates);
        Assert.Equal(inches, Calibre.Read("0" + typed).Calibre!.DiameterInches, 12);
    }

    /// <summary>
    /// Typed as its name, a row that means one diameter reads it, and a row whose name means several never picks one: it returns the
    /// candidates, and its own diameter is among them.
    /// </summary>
    [Theory]
    [MemberData(nameof(Rows))]
    public void EveryRowTypedAsItsNameReadsItsDiameterOrOffersItAsACandidate(string name, double inches)
    {
        var reading = Calibre.Read(name);
        if (Ambiguous.Contains(name))
        {
            Assert.Null(reading.Calibre);
            Assert.True(reading.Candidates.Count > 1, name);
            Assert.Contains(reading.Candidates, c => Math.Abs(c.DiameterInches - inches) < 1e-12);
            Assert.All(reading.Candidates, c => Assert.Contains(c.DiameterInches.ToString(".000#", System.Globalization.CultureInfo.InvariantCulture), c.Name, StringComparison.Ordinal));
            Assert.Null(Calibre.Parse(name, out string? problem));
            Assert.Contains("more than one bullet diameter", problem, StringComparison.Ordinal);
        }
        else
        {
            Assert.Equal(inches, reading.Calibre!.DiameterInches, 12);
        }
    }

    /// <summary>The ambiguous names in the forms a shooter types them, each returning candidates and never a calibre.</summary>
    [Theory]
    [InlineData("30", new[] { 0.308, 0.309 })]
    [InlineData("303", new[] { 0.3105, 0.312 })]
    [InlineData("32 cal", new[] { 0.312, 0.321 })]
    [InlineData("35", new[] { 0.355, 0.357, 0.358 })]
    [InlineData("38 Special", new[] { 0.357, 0.358 })]
    [InlineData("45 Cal", new[] { 0.451, 0.452, 0.454, 0.458 })]
    [InlineData("50", new[] { 0.500, 0.510 })]
    [InlineData("9mm", new[] { 0.355, 0.356 })]
    [InlineData("7.62 mm", new[] { 0.308, 0.310 })]
    [InlineData("30-06", new[] { 0.308, 0.309 })]
    public void AnAmbiguousNameOffersItsCandidatesAndChoosesNone(string typed, double[] diameters)
    {
        var reading = Calibre.Read(typed);
        Assert.Null(reading.Calibre);
        Assert.Equal(diameters, reading.Candidates.Select(c => c.DiameterInches));

        // A candidate chosen from the list reads as itself.
        foreach (var candidate in reading.Candidates)
        {
            Assert.Equal(candidate.DiameterInches, Calibre.Read(candidate.Name).Calibre!.DiameterInches, 12);
        }
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
