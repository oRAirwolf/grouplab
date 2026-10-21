using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 465 4B: the line under the scale's pill says how the scale was found, from what each scale records and
/// nothing more, for each way of finding it and for a set's sheets alike or mixed.
/// </summary>
public class ScaleSaidTests
{
    private static readonly UnitSettings Units = UnitSettings.Imperial;

    private static SheetReference Sheet(int? found = null, int? expected = null) =>
        new(new HomographyMapping(Homography.Identity), "test") { MarkersFound = found, MarkersExpected = expected };

    private static SheetReference PrinterChecked() =>
        Sheet().CorrectedBy(new PrinterProfile("Office", 0.99, 0.99, PrinterMethod.Card, new DateOnly(2026, 10, 1), 0.003));

    private static SheetReference ScanMeasured() => Sheet() with { PrintScale = 0.992 };

    [Fact]
    public void EachWayOfFindingOneSheetsScaleIsSaidAsItWas()
    {
        Assert.Equal("From 38 of 38 printed markers on the sheet, against your printer check.",
            ScaleSaid.How([PrinterChecked() with { MarkersFound = 38, MarkersExpected = 38 }], Units));
        Assert.Equal("From the printed markers on the sheet, against the scan's resolution.", ScaleSaid.How([ScanMeasured()], Units));
        Assert.Equal("From the printed markers on the sheet; with no printer check, the figures are in the sheet's own inches.", ScaleSaid.How([Sheet()], Units));
        Assert.Equal($"From a {Units.Length(2)} length you measured on the picture.", ScaleSaid.How([new LengthReference(new PointD(0, 0), new PointD(200, 0), 2)], Units));
        Assert.Equal("From the shot coordinates you imported, in the unit you gave them.",
            ScaleSaid.How([new LengthReference(ShotCsv.Aim, new PointD(ShotCsv.Aim.X + ShotCsv.PixelsPerInch, ShotCsv.Aim.Y), 1)], Units));
        PointD[] corners = [new(0, 0), new(800, 0), new(800, 1000), new(0, 1000)];
        Assert.Equal($"From a {Units.Number(8)} by {Units.Length(10)} rectangle you measured on the picture.", ScaleSaid.How([new RectangleReference(corners, 8, 10)], Units));
        Assert.Equal("From the printed size of the Shoot-N-C 8 in bullseye, which GroupLab recognized; not checked against a ruler or a GroupLab sheet.",
            ScaleSaid.How([new RectangleReference(corners, 8, 8) { PrintedTarget = "bc-34805-shoot-n-c-8in-bull" }], Units));
        Assert.Equal("From corner brackets in the photo: good to about 0.4 percent.",
            ScaleSaid.How([new MarkerReference(Homography.Identity, "Scale from corner brackets in the photo: good to about 0.4 percent. No printer check yet.")], Units));
        var drawn = new DrawnLength(new PointD(0, 0), new PointD(100, 0), 1);
        Assert.Equal("From lengths you measured at 2 bulls.", ScaleSaid.How([new PerBullReference([new BullScale(1, drawn, null), new BullScale(2, drawn, null)],
            new Dictionary<int, PointD> { [1] = new(0, 0), [2] = new(300, 0) })], Units));
        Assert.Equal("Nothing has set the scale yet: measure a known length on the picture.", ScaleSaid.How([null], Units));
    }

    [Fact]
    public void ThePillIsCheckedOnlyWhenEverySheetsOwnMarkersSetTheScale()
    {
        Assert.Equal(ScaleSaid.Checked, ScaleSaid.Pill([PrinterChecked(), ScanMeasured(), Sheet()]));
        Assert.True(ScaleSaid.AllChecked([PrinterChecked(), Sheet()]));
        Assert.False(ScaleSaid.AllChecked([PrinterChecked(), new LengthReference(new PointD(0, 0), new PointD(100, 0), 1)]));
        Assert.Equal(ScaleSaid.MoreThanOneKind, ScaleSaid.Pill([PrinterChecked(), new LengthReference(new PointD(0, 0), new PointD(100, 0), 1)]));
        Assert.Equal(ScaleSaid.ByHand, ScaleSaid.Pill([new LengthReference(new PointD(0, 0), new PointD(100, 0), 1), null]));
        Assert.Equal(ScaleSaid.FromMarkers, ScaleSaid.Pill(new MarkerReference(Homography.Identity, "Scale from a bank card in the photo.")));
        Assert.Equal(ScaleSaid.NoScale, ScaleSaid.Pill([null, null]));
    }

    [Fact]
    public void ASetsSheetsAreSaidTogetherWhenAlikeAndCountedWhenNot()
    {
        Assert.Equal("From the printed markers on all 3 sheets, against your printer check.",
            ScaleSaid.How([PrinterChecked(), PrinterChecked(), PrinterChecked()], Units));
        Assert.Equal("From the printed markers on both sheets, against each scan's resolution.", ScaleSaid.How([ScanMeasured(), ScanMeasured()], Units));
        Assert.Equal("From the printed markers on all 3 sheets; with no printer check, the figures are in the sheets' own inches.",
            ScaleSaid.How([Sheet(), Sheet(), Sheet()], Units));
        Assert.Equal("From the printed markers on all 3 sheets: 2 against your printer check, 1 in its own inches, with no printer check.",
            ScaleSaid.How([PrinterChecked(), Sheet(), PrinterChecked()], Units));
        Assert.Equal("From a length you measured on 2 pictures; no scale yet on 1 picture.",
            ScaleSaid.How([new LengthReference(new PointD(0, 0), new PointD(100, 0), 1), null, new LengthReference(new PointD(0, 0), new PointD(90, 0), 1)], Units, "picture"));
    }
}
