using System.Globalization;
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
        // 38 since entry 153 section 4 added 0.222, the rimfire 22, which was missing: 0.2215 is 5.45x39 and 0.224 is the
        // centrefire 22 of 5.56x45 and 22 ARC, so the most commonly shot cartridge in the world had nothing to pick.
        Assert.Equal(38, Calibre.Diameters.Distinct().Count());
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
    /// NOTES-FROM-PLANNING.md entry 163 section 3, reversing entries 107 and 108: a name the cartridge table knows is read as its family's
    /// bullet diameter. These were all refused before, and a real user shooting a 6.5 Creedmoor was offered .257 for typing 6.5.
    /// </summary>
    [Theory]
    [InlineData("300 Blackout", 0.308)]
    [InlineData("6.5 Creedmoor", 0.264)]
    [InlineData("30 Cal.", 0.308)]
    [InlineData("9mm Luger", 0.355)]
    [InlineData("308", 0.308)]
    [InlineData("6.5", 0.264)]
    public void NamesAreReadAsTheirFamilysBullet(string text, double inches)
    {
        Assert.Equal(inches, Calibre.Parse(text, out _)!.DiameterInches, 6);
    }

    /// <summary>
    /// A bare number that names nothing is still refused with the one sentence that says what to type, rather than guessed as millimetres,
    /// hundredths or thousandths: "7.62" read as 0.300 in, a diameter no 7.62 bullet has, and "22" could be 0.222 or 0.224.
    /// </summary>
    [Theory]
    [InlineData("7.62")]
    [InlineData("22")]
    public void ABareNumberThatNamesNothingIsRefusedWithWhatToType(string text)
    {
        Assert.Null(Calibre.Parse(text, out string? problem));
        Assert.Equal(Calibre.Refusal, problem);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 108 section 2, answering question 22: a calibre designation is refused in either unit, with a sentence
    /// that names the problem and guesses no bullet. Every value on both lists is tried as typed, with and without its leading zero, with more
    /// digits, and with its unit mark.
    /// </summary>
    [Fact]
    public void EveryDesignationIsRefusedInBothUnits()
    {
        var typed = new List<(string Text, string Shown)>();
        foreach (decimal v in Calibre.InchDesignations)
        {
            string two = v.ToString(".00", CultureInfo.InvariantCulture), three = v.ToString(".000", CultureInfo.InvariantCulture);
            typed.Add((two, two + " in"));
            typed.Add(("0" + three, "0" + three + " in"));
            typed.Add((three + " in", three + " in"));
            typed.Add((two + "\"", two + " in"));
        }

        foreach (decimal v in Calibre.MillimetreDesignations)
        {
            string shown = v.ToString("0.##", CultureInfo.InvariantCulture);
            typed.Add((shown + "mm", shown + " mm"));
            typed.Add((shown + " mm", shown + " mm"));
            typed.Add((v.ToString("0.00", CultureInfo.InvariantCulture) + " mm", v.ToString("0.00", CultureInfo.InvariantCulture) + " mm"));
        }

        // Entry 163 section 3: a designation that names a family in the cartridge table is read as that family's bullet, which is what the
        // refusal was protecting against getting wrong. One that names no family is still refused, and says why.
        foreach (var (text, shown) in typed)
        {
            var read = Calibre.Parse(text, out string? problem);
            if (CartridgeTable.Named(text) is { } family)
            {
                Assert.Equal(family.Diameter, read!.DiameterInches, 6);
                continue;
            }

            Assert.True(read is null, $"{text} was read");
            Assert.Equal(Calibre.DesignationRefusal(shown), problem);
        }

        foreach (string text in new[] { "7.62mm", ".22", ".45", "5.56 mm" })
        {
            Assert.Null(Calibre.Parse(text, out string? problem));
            Assert.Contains("is a calibre's name, not the bullet's diameter. Enter the bullet's diameter, such as 7.82 mm or 0.308.", problem, StringComparison.Ordinal);
        }

        // And the ones that name a family now read as it: .38 is 0.357 in, not 0.380, which is the case entry 108 existed for.
        Assert.Equal(0.357, Calibre.Parse(".38", out _)!.DiameterInches, 6);
        Assert.Equal(0.355, Calibre.Parse("9mm", out _)!.DiameterInches, 6);
        Assert.Equal(0.277, Calibre.Parse(".270", out _)!.DiameterInches, 6);

        Assert.Equal("7.62 mm is a calibre's name, not the bullet's diameter. Enter the bullet's diameter, such as 7.82 mm or 0.308.", Calibre.DesignationRefusal("7.62 mm"));
    }

    /// <summary>Entry 108 section 2: the real diameters beside the designations still read, including those deliberately left off the lists.</summary>
    [Theory]
    [InlineData(".357", 0.357)]
    [InlineData(".452", 0.452)]
    [InlineData("0.308", 0.308)]
    [InlineData("7.82 mm", 7.82 / 25.4)]
    [InlineData("9.3 mm", 9.3 / 25.4)]
    [InlineData("12.7 mm", 12.7 / 25.4)]
    [InlineData(".40", 0.40)]
    [InlineData(".41", 0.41)]
    [InlineData(".50", 0.50)]
    [InlineData(".338", 0.338)]
    [InlineData(".375", 0.375)]
    [InlineData(".416", 0.416)]
    public void TheDiametersBesideTheDesignationsStillRead(string text, double inches) => Assert.Equal(inches, Calibre.Parse(text, out _)!.DiameterInches, 12);

    /// <summary>
    /// Entry 108 section 2: the refused lists and the pick list never overlap, in either unit, at the precision each is typed: a designation
    /// against a diameter in inches exactly, and a millimetre designation against each diameter's millimetres to the hundredth, the pick
    /// list's own display. A diameter added later that collides with a designation fails here rather than being silently refused.
    /// </summary>
    [Fact]
    public void NoRefusedValueIsAPickListDiameter()
    {
        foreach (double d in Calibre.Diameters)
        {
            decimal inches = (decimal)d, millimetres = Math.Round((decimal)d * 25.4m, 2);
            Assert.DoesNotContain(inches, Calibre.InchDesignations);
            Assert.DoesNotContain(millimetres, Calibre.MillimetreDesignations);
            Assert.NotNull(Calibre.Parse(Calibre.Shown(d), out _));
            Assert.NotNull(Calibre.Parse(d.ToString("0.000#", CultureInfo.InvariantCulture), out _));
            Assert.NotNull(Calibre.Parse(millimetres.ToString("0.00", CultureInfo.InvariantCulture) + " mm", out _));
        }

        Assert.Equal(38, Calibre.Diameters.Count);
    }

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
