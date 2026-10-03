using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 354 section 1: what a GroupLab sheet printed, as the hole finder knows it. A mark past everything printed is on
/// the margin, where the submitted curled and torn .22 sheet showed the board behind it; and a mark lying wholly on a bull's printed number is the
/// number, which the expected artwork leaves out. The photographs themselves are held in <see cref="Analysis.TwentyTwoPhotographTests"/>; this
/// holds the geometry on the sheet everybody prints, so it runs everywhere.
/// </summary>
public class PrintedMatterTests
{
    private static readonly PrintedMatter Letter = RenderDifferenceHoleDetector.Printed(BuiltIns.Load("GL-CF25-LTR.gltd.json"), 0);

    /// <summary>Image pixels are page dmm, so a hull can be written in dmm.</summary>
    private static readonly IPageMapping Plain = new HomographyMapping(new Homography([1, 0, 0, 0, 1, 0, 0, 0, 1]));

    [Fact]
    public void EverythingPrintedLiesInsideTheBoxAndTheMarginsAreOutsideIt()
    {
        // The print instruction is the lowest line on the sheet, 45 dmm above the bottom edge on its baseline; the codes reach the corners.
        Assert.InRange(Letter.Bottom, 2794 - 50, 2794 - 30);
        Assert.InRange(Letter.Top, 20, 140);
        Assert.InRange(Letter.Left, 20, 140);
        Assert.InRange(Letter.Right, 2159 - 140, 2159 - 20);

        // Where the submitted sheet gave its false marks: its torn top corner, its curled top edge and below the print instruction.
        Assert.True(RenderDifferenceHoleDetector.IsPastThePrint(Letter, new PointD(8.441 * 254, 0.713 * 254)));
        Assert.True(RenderDifferenceHoleDetector.IsPastThePrint(Letter, new PointD(7.714 * 254, 0.160 * 254)));
        Assert.True(RenderDifferenceHoleDetector.IsPastThePrint(Letter, new PointD(5.606 * 254, 10.901 * 254)));

        // And never a bull, nor the marker row below the last bulls.
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        Assert.All(definition.Bulls, b => Assert.False(RenderDifferenceHoleDetector.IsPastThePrint(Letter, new PointD(b.X, b.Y))));
    }

    [Fact]
    public void AMarkWhollyOnABullsNumberIsTheNumberAndAHoleOverItIsNot()
    {
        var (_, left, top, right, bottom) = Letter.Words.First(w => w.Text == "25");
        double cx = (left + right) / 2, cy = (top + bottom) / 2;

        // The number moved by a crinkle, as on the submitted photograph: a mark the size of the number, 0.02 in off where the sheet set it.
        double shift = 0.02 * 254, halfX = (right - left) / 2, halfY = (bottom - top) / 2;
        var number = Square(cx + shift, cy + shift, halfX, halfY);
        Assert.Equal("25", RenderDifferenceHoleDetector.WordsUnder(Letter, number, Plain));

        // A .22 hole through the number, 0.21 in across, reaches past it and is a hole.
        var hole = Square(cx, cy, 0.105 * 254, 0.105 * 254);
        Assert.Null(RenderDifferenceHoleDetector.WordsUnder(Letter, hole, Plain));
    }

    private static List<PointD> Square(double cx, double cy, double halfX, double halfY) =>
        [new(cx - halfX, cy - halfY), new(cx + halfX, cy - halfY), new(cx + halfX, cy + halfY), new(cx - halfX, cy + halfY)];
}
