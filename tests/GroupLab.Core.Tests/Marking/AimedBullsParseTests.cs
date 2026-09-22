using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 5.3.4: the three ways a person says which bulls they aimed at.
/// <para>
/// <b>Why this lives in Core rather than in the window.</b> The proof that GroupLab's assignments match a shooter's own table is run from
/// the command line and the feature is used from the window. If the two had their own parsers, "bulls 2 to 5 of every row" could mean
/// different things in the proof and in the product, and the proof would be of nothing.
/// </para>
/// </summary>
public class AimedBullsParseTests
{
    /// <summary>A five by five sheet numbered the way they are printed, left to right and top to bottom.</summary>
    private static IReadOnlyList<BullAim> Sheet() =>
        [.. Enumerable.Range(0, 25).Select(i => new BullAim(i, (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
            new GroupLab.Core.Imaging.PointD(100 + (40 * (i % 5)), 100 + (40 * (i / 5))))
            { Declared = new GroupLab.Core.Imaging.PointD(100 + (40 * (i % 5)), 100 + (40 * (i / 5))) })];

    private static IReadOnlyList<string> Labels(AssignmentRule? rule) =>
        [.. AimedBulls.Of(rule, Sheet()).Select(i => Sheet()[i].Label).Order(StringComparer.Ordinal)];

    /// <summary>Nothing said means every bull, which is what a sheet shot the ordinary way is.</summary>
    [Fact]
    public void NothingSaidMeansEveryBull()
    {
        Assert.Equal(25, AimedBulls.Of(AimedBulls.Parse("", Sheet()), Sheet()).Count);
        Assert.Equal(25, AimedBulls.Of(AimedBulls.Parse(null, Sheet()), Sheet()).Count);
        Assert.Equal(25, AimedBulls.Of(AimedBulls.Parse("   ", Sheet()), Sheet()).Count);
    }

    /// <summary>
    /// Scan 5 of entry 120, which is the sheet the whole feature exists for: "bulls 2, 3, 4 and 5 of every row", twenty bulls.
    /// </summary>
    [Fact]
    public void ColumnsOfEveryRowIsScanFivesTable()
    {
        var aimed = Labels(AimedBulls.Parse("columns 2-5", Sheet()));

        Assert.Equal(20, aimed.Count);
        Assert.Equal(
            ["10", "12", "13", "14", "15", "17", "18", "19", "2", "20", "22", "23", "24", "25", "3", "4", "5", "7", "8", "9"],
            aimed);
    }

    /// <summary>Whole rows, which is how a sheet shot top to bottom is described.</summary>
    [Fact]
    public void RowsTakesWholeRows()
    {
        var aimed = Labels(AimedBulls.Parse("rows 1-3", Sheet()));

        Assert.Equal(15, aimed.Count);
        Assert.DoesNotContain("16", aimed);
        Assert.Contains("15", aimed);
    }

    /// <summary>Scan 4's table, which is a list with gaps in it rather than a pattern.</summary>
    [Fact]
    public void AListOfBullsTakesExactlyThoseBulls()
    {
        var aimed = Labels(AimedBulls.Parse("1-4, 6-9, 11-25", Sheet()));

        Assert.Equal(23, aimed.Count);
        Assert.DoesNotContain("5", aimed);
        Assert.DoesNotContain("10", aimed);
    }

    /// <summary>Text that names nothing on this sheet is refused rather than quietly read as every bull.</summary>
    [Theory]
    [InlineData("26")]
    [InlineData("rows 9")]
    [InlineData("columns 7")]
    [InlineData("what")]
    [InlineData("1-")]
    public void TextThatNamesNothingIsRefused(string said) =>
        Assert.Null(AimedBulls.Parse(said, Sheet()));

    /// <summary>"row" and "rows" are the same word to somebody typing quickly, and so are the columns.</summary>
    [Fact]
    public void SingularAndPluralAreTheSame()
    {
        Assert.Equal(Labels(AimedBulls.Parse("rows 2", Sheet())), Labels(AimedBulls.Parse("row 2", Sheet())));
        Assert.Equal(Labels(AimedBulls.Parse("columns 2", Sheet())), Labels(AimedBulls.Parse("column 2", Sheet())));
    }
}
