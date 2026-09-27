using GroupLab.Cli.Library;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Validation;

namespace GroupLab.Core.Tests.Library;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 226 section 4: the target generator sizes the bull by the visibility rule for the lowest magnification, or by
/// a red dot's own size, never makes a small feature the aim, and gives as many sheets as the shots need, each carrying its place in the set.
/// </summary>
public class TargetGeneratorTests
{
    [Theory]
    [InlineData(100, 10, 25)]
    [InlineData(100, 6, 25)]
    [InlineData(100, 4, 25)]
    [InlineData(200, 10, 10)]
    [InlineData(25, 1, 10)]
    public void TheWhiteCenterSubtendsTheRuleAtTheLowestMagnificationAndTheSheetsPrint(double yards, double magnification, int shots)
    {
        double? dot = magnification < 1.5 ? 2 : null;
        var made = TargetGenerator.Generate(new GeneratorRequest(yards, magnification, dot, shots, "letter"));

        Assert.NotNull(made.Design);
        Assert.True(made.Design!.Printable, string.Join(" | ", made.Design.Checks.Select(c => c.Sentence)));
        double centerMoa = made.CenterDmm / 254.0 / (1.0472 * yards / 100);
        double seen = centerMoa * magnification;
        Assert.True(seen >= TargetGenerator.ArcminutesSeen - 0.05, $"{seen:0.00} arcminutes at {magnification}x");
        if (dot is { } d)
        {
            Assert.True(centerMoa >= TargetGenerator.DotMargin * d - 0.05, $"a {d} MOA dot in a {centerMoa:0.00} MOA center");
        }

        // The aim is the white center: the dot inside it is the only thing smaller, and the black disc is three times the center.
        Assert.InRange(made.OuterDmm, (3 * made.CenterDmm) - 2, (3 * made.CenterDmm) + 2);
        Assert.True(made.DotDmm < made.CenterDmm);
        Assert.True(made.Bulls >= shots);
        Assert.True(made.Bulls - shots < made.Columns * made.Rows || made.Sheets == 1);
        Assert.Equal(made.Sheets * made.Columns * made.Rows, made.Design.Definition!.Bulls.Count(b => b.Scoring) * made.Sheets);
    }

    [Fact]
    public void ASetOfSheetsCarriesEachSheetsPlaceInItsCodes()
    {
        var made = TargetGenerator.Generate(new GeneratorRequest(100, 4, null, 25, "letter"));
        var definition = made.Design!.Definition!;

        Assert.True(made.Sheets > 1);
        Assert.Equal(made.Sheets, definition.Tiling!.Cols * definition.Tiling.Rows);
        Assert.DoesNotContain(GltdValidator.Validate(definition), x => x.Severity == Severity.Error);
        var encoding = GltdBinary.Encode(definition).Encoding!;
        for (int sheet = 0; sheet < made.Sheets; sheet++)
        {
            var decoded = GltdBinary.Decode([GltdBinary.ReplicatedFrame(encoding, (byte)sheet)]);
            Assert.Equal(sheet, decoded.TileIndex);
            Assert.Equal(definition.Id, decoded.DefinitionId);
        }
    }

    [Fact]
    public void ARedDotAtFiftyYardsIsTooBigForLetterAndSaysWhatWouldFit()
    {
        var made = TargetGenerator.Generate(new GeneratorRequest(50, 1, 2, 10, "letter"));
        Assert.Null(made.Design);
        Assert.Contains(made.Explanation, s => s.Contains("larger page", StringComparison.Ordinal) && s.Contains("tiled", StringComparison.Ordinal));
    }

    [Fact]
    public void TwentyFiveShotsAtTenPowerFitOneLetterSheet()
    {
        var made = TargetGenerator.Generate(new GeneratorRequest(100, 10, null, 25, "letter"));
        Assert.Equal(1, made.Sheets);
        Assert.Equal(25, made.Bulls);
        Assert.Null(made.Design!.Definition!.Tiling);
        Assert.Contains(made.Explanation, s => s.Contains("crosshair", StringComparison.Ordinal));
    }
}
