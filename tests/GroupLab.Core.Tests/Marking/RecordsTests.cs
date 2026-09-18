using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 97 section 2: rifle, barrel and load records, kept small, so that entry 91's zero correction can say clicks.
/// </summary>
public class RecordsTests
{
    /// <summary>One true MOA at 100 yd is 1.047 in, four quarter-MOA clicks; one milliradian is 3.6 in, ten tenth-mil clicks.</summary>
    [Theory]
    [InlineData(1.0472, 0.25, AngularUnit.Moa, 4)]
    [InlineData(3.6, 0.1, AngularUnit.Mrad, 10)]
    [InlineData(0.5236, 0.125, AngularUnit.Moa, 4)]
    public void AnOffsetBecomesWholeClicksAndWhatRoundingLeaves(double offsetInches, double click, AngularUnit unit, int expected)
    {
        var clicks = Clicks.For(offsetInches, 3600, new Rifle("test", click, unit), "right");

        Assert.Equal(expected, clicks.Count);
        Assert.True(Math.Abs(clicks.ResidualAngle) < click / 2, $"residual {clicks.ResidualAngle} beyond half a click");
        Assert.Equal($"{expected} clicks right", clicks.Describe());
    }

    [Fact]
    public void TheBookRoundTripsReplacesByNameAndCountsRoundsOnlyWhenAsked()
    {
        var book = RecordBook.Empty
            .With(new Rifle("Tikka T3x", 0.25, AngularUnit.Moa))
            .With(new Barrel("Bartlein 1", "Tikka T3x", 412))
            .With(new Load("41.5 H4350", "140 gr Hybrid, CCI BR-2, Lapua"))
            .With(new Rifle("tikka t3x", 0.1, AngularUnit.Mrad));

        var read = RecordBook.Read(book.Write());

        var rifle = Assert.Single(read.Rifles);
        Assert.Equal(0.1, rifle.ClickValue);
        Assert.Equal(AngularUnit.Mrad, rifle.ClickUnit);
        Assert.Equal("0.1 mil a click", rifle.DescribeClick());
        Assert.Equal(412, read.FindBarrel("bartlein 1")!.Rounds);
        Assert.Equal(422, read.Fired("Bartlein 1", 10).FindBarrel("Bartlein 1")!.Rounds);
        Assert.Equal("140 gr Hybrid, CCI BR-2, Lapua", read.FindLoad("41.5 H4350")!.Components);
        Assert.Same(RecordBook.Empty, RecordBook.Read("not json"));
    }

    /// <summary>
    /// The zero correction in clicks: ten shots sitting 1.2 in right at 100 yd, a quarter-MOA scope, which is about 1.15 MOA and so five clicks
    /// left, with the rifle and the equipment kept in the marking file.
    /// </summary>
    [Fact]
    public void TheZeroCorrectionSaysClicksOnceARifleAndADistanceAreSet()
    {
        var session = new MarkingSession();
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        session.SetPointOfAim(new PointD(0, 0));
        double radius = 0.27 * Math.Sqrt(2);
        for (int k = 0; k < 10; k++)
        {
            double a = 2 * Math.PI * k / 10;
            session.AddShot(new PointD(100 * (1.2 + (radius * Math.Cos(a))), 100 * radius * Math.Sin(a)));
        }

        Assert.Null(Zeroing.For(session.State)!.Windage.Clicks);
        session.SetShotDistance(3600);
        Assert.Null(Zeroing.For(session.State)!.Windage.Clicks);

        session.SetEquipment(new Rifle("Tikka T3x", 0.25, AngularUnit.Moa), "Bartlein 1", "41.5 H4350");
        var zero = Zeroing.For(session.State)!;

        Assert.Equal("5 clicks left", zero.Windage.Clicks!.Describe());
        Assert.Null(zero.Elevation.Clicks);
        var (read, _) = MarkingFile.Read(MarkingFile.Write(session.State));
        Assert.Equal(session.State.Rifle, read.Rifle);
        Assert.Equal("Bartlein 1", read.Barrel);
        Assert.Equal("41.5 H4350", read.Load);
    }
}
