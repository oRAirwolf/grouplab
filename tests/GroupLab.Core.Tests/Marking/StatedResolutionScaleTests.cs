using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 130 section 4.1, from entry 120: a scan states its own resolution, and on a blank sheet with no printed
/// markers to measure from, that is a scale.
/// <para>
/// <b>The whole point is that it is offered.</b> A scale decides what every figure means: get it wrong and a one inch group reads as two,
/// and nothing on the screen looks unusual. A scanner saying 600 dpi is almost always right, and "almost always" is not a basis for quietly
/// deciding what somebody's measurements mean when they are the one who carries the cost of it being wrong.
/// </para>
/// </summary>
public class StatedResolutionScaleTests
{
    private static ImageMetadata Scan(double? dpiX, double? dpiY = null) =>
        ImageMetadata.ForScan(5100, 6600, dpiX ?? 0) with { DpiX = dpiX, DpiY = dpiY ?? dpiX };

    [Fact]
    public void ASixHundredDpiScanIsOfferedWithItsNumberShown()
    {
        var offer = StatedResolutionScale.Offer(Scan(600), 5100);

        Assert.NotNull(offer);
        Assert.Equal(600, offer.DotsPerInch);

        // The number is in the words, because a person cannot judge an offer they cannot see.
        Assert.Contains("600 dpi, stated by the file", offer.Says, StringComparison.Ordinal);

        // And it is plainly a choice, with the alternative named.
        Assert.Contains("measure a known distance yourself instead", offer.Says, StringComparison.Ordinal);
    }

    /// <summary>The scale it would set is the right one: one inch across, at the resolution stated.</summary>
    [Fact]
    public void TheScaleItOffersIsOneInchAtThatResolution()
    {
        var offer = StatedResolutionScale.Offer(Scan(600), 5100);

        Assert.NotNull(offer);
        Assert.Equal(1, offer.Reference.Inches);
        double pixels = Math.Sqrt(
            Math.Pow(offer.Reference.B.X - offer.Reference.A.X, 2) + Math.Pow(offer.Reference.B.Y - offer.Reference.A.Y, 2));
        Assert.Equal(600, pixels, 3);
    }

    /// <summary>
    /// A photograph's stated resolution describes the file, not the paper. It says nothing whatever about how far the camera was from the
    /// sheet, so offering it would be offering a number that means nothing.
    /// </summary>
    [Fact]
    public void APhotographIsNeverOfferedAScaleFromItsMetadata()
    {
        // A camera is one that recorded a focal length, which is what IsCamera reads. Nothing sets that flag by hand.
        var camera = ImageMetadata.ForScan(4000, 3000, 600) with { DpiX = 600, DpiY = 600, FocalLengthMm = 27 };
        Assert.True(camera.IsCamera);
        Assert.Null(StatedResolutionScale.Offer(camera, 4000));
    }

    /// <summary>72 and 96 are what a file gets when whatever wrote it had nothing to say, so they are not measurements.</summary>
    [Fact]
    public void ADefaultNobodySetIsNotOffered()
    {
        Assert.Null(StatedResolutionScale.Offer(Scan(72), 5100));
        Assert.Null(StatedResolutionScale.Offer(Scan(96), 5100));
        Assert.Null(StatedResolutionScale.Offer(Scan(null), 5100));
        Assert.Null(StatedResolutionScale.Offer(Scan(0), 5100));
    }

    /// <summary>Nothing above this is a scan of a sheet of paper, whatever the file claims.</summary>
    [Fact]
    public void AnAbsurdResolutionIsNotOffered()
    {
        Assert.Null(StatedResolutionScale.Offer(Scan(100000), 5100));
        Assert.NotNull(StatedResolutionScale.Offer(Scan(1200), 5100));
        Assert.NotNull(StatedResolutionScale.Offer(Scan(2400), 5100));
    }

    /// <summary>
    /// A scan stretched one way cannot be described by one number, and saying so is better than offering a scale that is right across and
    /// wrong down. A group measured on such a scan is wrong in one axis only, which is the hardest kind of wrong to notice.
    /// </summary>
    [Fact]
    public void AStretchedScanSaysSoRatherThanOfferingOneNumber()
    {
        string? stretched = StatedResolutionScale.Stretched(Scan(600, 300));

        Assert.NotNull(stretched);
        Assert.Contains("600 dpi across and 300 dpi down", stretched, StringComparison.Ordinal);
        Assert.Contains("stretched", stretched, StringComparison.Ordinal);
        Assert.Contains("measure a known distance in each direction", stretched, StringComparison.Ordinal);
    }

    /// <summary>A square scan says nothing, so this is not warning about every scan there is.</summary>
    [Fact]
    public void ASquareScanIsNotCalledStretched()
    {
        Assert.Null(StatedResolutionScale.Stretched(Scan(600, 600)));
        Assert.Null(StatedResolutionScale.Stretched(Scan(600, 600.5)));
        Assert.Null(StatedResolutionScale.Stretched(Scan(null)));
    }

    /// <summary>
    /// Entry 481 part E, question 108 (a): an accepted offer is recognized as the scan's own resolution, so its pill and its line say so
    /// rather than calling it a length somebody measured; a measured length and imported shot coordinates are never taken for it.
    /// </summary>
    [Fact]
    public void AnAcceptedOfferHasItsOwnPillAndLine()
    {
        var offer = StatedResolutionScale.Offer(Scan(600), 5100)!;
        Assert.Equal(600, StatedResolutionScale.DotsPerInchOf(offer.Reference));
        Assert.Equal(ScaleSaid.FromScanResolution, ScaleSaid.Pill(offer.Reference));
        Assert.False(ScaleSaid.AllChecked([offer.Reference]));
        Assert.Equal("From the scan's own resolution, 600 dpi, as the file states it; not checked against a ruler or a GroupLab sheet.",
            ScaleSaid.How([offer.Reference], UnitSettings.Imperial));
        Assert.Equal("Use the scan's 600 dpi as the scale", StatedResolutionScale.Use(offer));

        var measured = new LengthReference(new PointD(102.4, 340.7), new PointD(702.1, 345.2), 1);
        Assert.Null(StatedResolutionScale.DotsPerInchOf(measured));
        Assert.Equal(ScaleSaid.ByHand, ScaleSaid.Pill(measured));

        var imported = new LengthReference(ShotCsv.Aim, new PointD(ShotCsv.Aim.X + ShotCsv.PixelsPerInch, ShotCsv.Aim.Y), 1);
        Assert.Null(StatedResolutionScale.DotsPerInchOf(imported));
        Assert.Equal(ScaleSaid.FromImport, ScaleSaid.Pill(imported));

        // A pool of two scans, one by its resolution and one by a length, says each.
        Assert.Equal("From a scan's own resolution on 1 sheet; a length you measured on 1 sheet.", ScaleSaid.How([offer.Reference, measured], UnitSettings.Imperial));
    }
}
