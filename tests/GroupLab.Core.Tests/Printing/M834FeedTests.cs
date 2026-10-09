using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Printing.Labels;
using GroupLab.Core.Printing.Thermal;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Printing;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 391: the M834 feeds short, so its pages are drawn longer along the feed and only along it. The check page's
/// crosshairs and rulers are found in the dots GroupLab sends, which is what the caliper and the scan measured on paper.
/// </summary>
public sealed class M834FeedTests
{
    private const double MmPerDot = 25.4 / 300;

    /// <summary>
    /// Across the head nothing changes: the crosshairs are 1772 dots apart, 150.02 mm at the head's 300 dpi (100.01 percent, as the second
    /// print's scan measured), the ruler 2244 dots, 189.99 mm, and the Letter page's 2550 dots lose 11 each side to the 2528-dot head, a
    /// whole number of bytes, so nothing is shifted.
    /// </summary>
    [Fact]
    public void AcrossIsTheHeadsOwnDotPitchStretchedOrNot()
    {
        var page = CheckPage();
        foreach (var head in new[] { M834Print.Profile.Head, M834Print.Head })
        {
            var print = ThermalRaster.Render(page, head);
            Assert.Equal((2550, 11, 2528), (print.PageDots, print.CutLeft, print.Image.Width));
            Assert.Equal(1772, Across(page, head));
            Assert.Equal(1.0, Across(page, head) * MmPerDot / 150.0, 0.0005);
            Assert.Equal(2244, RulerAcross(page, head));
        }
    }

    /// <summary>Along the feed the page is 0.70 percent longer, so the M834's 99.30 percent brings it back to 150.00 and 250.0 mm.</summary>
    [Fact]
    public void AlongTheFeedThePageIsDrawnLongerByWhatTheM834Loses()
    {
        var page = CheckPage();
        var plain = M834Print.Profile.Head;
        var stretched = M834Print.Head;
        Assert.Equal(1.0070, M834Print.FeedStretch, 0.0001);
        Assert.Equal(1.0, plain.FeedStretch);

        Assert.Equal(1772, Down(page, plain));
        Assert.Equal(2953, RulerDown(page, plain));
        Assert.Equal(3300, ThermalRaster.Render(page, plain).Image.Height);

        double down = Down(page, stretched), ruler = RulerDown(page, stretched);
        Assert.Equal(150.0, down * MmPerDot * M834Print.MeasuredFeed, 0.1);
        Assert.Equal(250.0, ruler * MmPerDot * M834Print.MeasuredFeed, 0.1);
        Assert.Equal((int)Math.Round(3300 * M834Print.FeedStretch), ThermalRaster.Render(page, stretched).Image.Height);
    }

    /// <summary>The stretch is the M834's alone: every other thermal head is drawn as before, and a disc stays a disc across.</summary>
    [Fact]
    public void OtherPrintersAndTheBullsAreUnchanged()
    {
        var page = CheckPage();
        Assert.All(PrinterProfiles.All.Where(p => p.Id != M834Print.ProfileId), p => Assert.Equal(1.0, p.Head.FeedStretch));

        var plain = ThermalRaster.Render(page, M834Print.Profile.Head).Image;
        var stretched = ThermalRaster.Render(page, M834Print.Head).Image;
        var (plainWide, plainTall) = Extent(plain);
        var (wide, tall) = Extent(stretched);
        Assert.Equal(plainWide, wide);
        Assert.InRange(tall - (plainTall * M834Print.FeedStretch), -2, 2);
    }

    /// <summary>The encoded page is the stretched one, on the phone and the computer alike, since both call the one encoder.</summary>
    [Fact]
    public void TheEncodedPageCarriesTheStretch()
    {
        var page = CheckPage();
        var profile = M834Print.Profile;
        var dots = ThermalRaster.Render(page, M834Print.Head);
        var job = new LabelJob(dots.Image, page.Width / (10.0 * Scene.UnitsPerDmm), page.Height / (10.0 * Scene.UnitsPerDmm), FeedAfterMm: 0);
        Assert.Equal(PrinterEncoders.For(profile).Encode(job, profile), M834Print.Encode(page, PaperForm.Fanfold));
    }

    private static Scene CheckPage() =>
        Assert.Single(SceneBuilder.Build(BuiltIns.Load("GL-SCALE-LTR-1.gltd.json"), new RenderOptions(PrintNote: SceneBuilder.ActualSizeNote)).Pages);

    private static IReadOnlyList<PointDmm> Crosshairs() => GridStyle4.Crosshairs(BuiltIns.Load("GL-SCALE-LTR-1.gltd.json").Page);

    /// <summary>Between the top two crosshairs' vertical arms, in dots across.</summary>
    private static double Across(Scene page, PrintHead head)
    {
        var c = Crosshairs();
        return (CentreX(page, head, c[1].X, c[1].Y, tall: true) - CentreX(page, head, c[0].X, c[0].Y, tall: true)) / 2.0;
    }

    /// <summary>Between the right two crosshairs' horizontal arms, in rows along the feed.</summary>
    private static double Down(Scene page, PrintHead head)
    {
        var c = Crosshairs();
        return (CentreY(page, head, c[2].X, c[2].Y) - CentreY(page, head, c[1].X, c[1].Y)) / 2.0;
    }

    private static double RulerDown(Scene page, PrintHead head)
    {
        var (x, top, bottom) = GridStyle4.RulerDown(BuiltIns.Load("GL-SCALE-LTR-1.gltd.json").Page);
        return (CentreY(page, head, x, bottom) - CentreY(page, head, x, top)) / 2.0;
    }

    private static double RulerAcross(Scene page, PrintHead head)
    {
        var (y, left, right) = GridStyle4.RulerAcross(BuiltIns.Load("GL-SCALE-LTR-1.gltd.json").Page);
        return (CentreX(page, head, right, y, tall: true) - CentreX(page, head, left, y, tall: true)) / 2.0;
    }

    /// <summary>Twice the dot centre across of the tall rectangle centred on (x, y) dmm, so a half dot stays whole.</summary>
    private static long CentreX(Scene page, PrintHead head, long x, long y, bool tall)
    {
        var (x0, _, x1, _) = ThermalRaster.Dots(Centred(page, x, y, tall), head.DotsPerUnit, head.RowsPerUnit);
        return x0 + x1;
    }

    /// <summary>Twice the row centre along the feed of the wide rectangle centred on (x, y) dmm.</summary>
    private static long CentreY(Scene page, PrintHead head, long x, long y)
    {
        var (_, y0, _, y1) = ThermalRaster.Dots(Centred(page, x, y, tall: false), head.DotsPerUnit, head.RowsPerUnit);
        return y0 + y1;
    }

    private static RectFill Centred(Scene page, long x, long y, bool tall) =>
        page.Items.OfType<RectFill>().First(r => r.Module is null && (2 * r.X) + r.Width == 4 * x && (2 * r.Y) + r.Height == 4 * y
            && (r.Height > r.Width) == tall);

    /// <summary>The width and height of everything inked, in dots.</summary>
    private static (int Wide, int Tall) Extent(DotImage image)
    {
        int left = int.MaxValue, right = -1, top = int.MaxValue, bottom = -1;
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                if (image[x, y])
                {
                    (left, right, top, bottom) = (Math.Min(left, x), Math.Max(right, x), Math.Min(top, y), Math.Max(bottom, y));
                }
            }
        }

        return (right - left, bottom - top);
    }
}
