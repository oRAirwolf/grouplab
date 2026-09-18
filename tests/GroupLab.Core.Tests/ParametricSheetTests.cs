using GroupLab.Cli.Library;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entries 99 and 100: the parametric target editor. It places a sheet by the rule the built-in library was laid out
/// with, so given a built-in sheet's parameters it must give back that sheet; and it refuses a layout that cannot work and warns, with the
/// number, on a spacing that is tight for the stated dispersion.
/// </summary>
public class ParametricSheetTests
{
    /// <summary>Four built-in sheets of different pages, rings, sighter rows and load blocks, rebuilt from their parameters alone.</summary>
    [Theory]
    [InlineData("GL-CF25-LTR", "letter", 5, 5, 1.4961, 254, 3, false)]
    [InlineData("GL-CF25-LTR-D", "letter", 5, 5, 1.4961, 254, 0, true)]
    [InlineData("GL-RF36-LTR", "letter", 6, 6, 1.0, 127, 4, false)]
    [InlineData("GL-LR25-TAB", "tabloid", 5, 5, 2.0, 381, 3, true)]
    [InlineData("GL-CF25-A4", "a4", 5, 5, 1.4961, 254, 3, false)]
    [InlineData("GL-CF30-LTR", "letter", 5, 6, 1.378, 222, 0, false)]
    [InlineData("GL-RF25-LTR", "letter", 5, 5, 1.0, 152, 5, true)]
    [InlineData("GL-RF25-A4", "a4", 5, 5, 1.0, 152, 5, true)]
    [InlineData("GL-LR25-A3", "a3", 5, 5, 2.0, 381, 3, true)]
    [InlineData("GL-LR30-TAB", "tabloid", 5, 6, 2.0, 356, 3, false)]
    public void ABuiltInSheetsParametersGiveBackThatSheet(string file, string page, int columns, int rows, double pitch, int ring, int sighters, bool loadBlock)
    {
        var builtIn = GltdJsonReader.ReadFile(Repo.PathTo("targets", file + ".gltd.json")).Definition!;

        var design = ParametricSheet.Design(new SheetSpec(file, page, columns, rows, pitch, ring, sighters, loadBlock));

        Assert.True(design.Printable, string.Join(" | ", design.Checks.Select(c => c.Sentence)));
        Assert.Equal(builtIn.Bulls.Select(b => (b.X, b.Y, b.Scoring)), design.Definition!.Bulls.Select(b => (b.X, b.Y, b.Scoring)));
        Assert.Equal(builtIn.Fiducials!.Markers!.Count, design.Markers);
        Assert.Equal(builtIn.Fiducials.Markers.Select(m => (m.X, m.Y)), design.Definition.Fiducials!.Markers!.Select(m => (m.X, m.Y)));
    }

    /// <summary>Entry 56 section 7's table, which was checked against 400,000 simulated shots, reproduced from the closed form.</summary>
    [Theory]
    [InlineData(3, 0.249)]
    [InlineData(4, 0.089)]
    [InlineData(5, 0.025)]
    [InlineData(6, 0.0054)]
    [InlineData(7, 0.0009)]
    public void TheMisassignmentRateIsEntry56sTable(double ratio, double expected) =>
        Assert.Equal(expected, ParametricSheet.Misassigned(ratio), expected < 0.01 ? 4 : 3);

    /// <summary>
    /// Entry 99 section 4 item 1's own example: 25 mm spacing for a rifle that shoots an inch. The warning gives the number and what it means,
    /// and nothing about whether to proceed (entry 100 section 3).
    /// </summary>
    [Fact]
    public void ATightSpacingIsAWarningWithTheNumberAndNoVerdict()
    {
        var tight = ParametricSheet.Spacing(1.0, 1.0, 100);
        var wide = ParametricSheet.Spacing(2.0, 0.5, 100);

        Assert.Equal(CheckLevel.Warning, tight.Level);
        Assert.Contains("would land nearer a neighbouring bull than its own", tight.Sentence, StringComparison.Ordinal);
        Assert.DoesNotContain("should", tight.Sentence, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("do not", tight.Sentence, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(CheckLevel.Fine, wide.Level);
    }

    [Fact]
    public void ALayoutThatDoesNotFitOrOverlapsIsRefusedWithTheReason()
    {
        var tall = ParametricSheet.Design(new SheetSpec("tall", "letter", 5, 9, 1.5, 254, 3, true));
        Assert.False(tall.Printable);
        Assert.Contains("more height than a letter page has", Assert.Single(tall.Checks).Sentence, StringComparison.Ordinal);

        var crowded = ParametricSheet.Design(new SheetSpec("crowded", "letter", 5, 5, 0.8, 254, 0, false));
        Assert.False(crowded.Printable);
        Assert.Contains("overlap", Assert.Single(crowded.Checks).Sentence, StringComparison.Ordinal);
    }

    /// <summary>A sheet the library does not carry, a 6 by 5 at a spacing nobody has printed, designs cleanly and says how many markers it has.</summary>
    [Fact]
    public void ASheetTheLibraryDoesNotCarryDesignsAndSaysWhatItCarries()
    {
        var design = ParametricSheet.Design(new SheetSpec("6 by 5", "letter", 6, 5, 1.2, 222, 0, false));

        Assert.True(design.Printable, string.Join(" | ", design.Checks.Select(c => c.Sentence)));
        Assert.Equal(30, design.Definition!.Bulls.Count);
        Assert.True(design.Markers >= ParametricSheet.FewestMarkers);
        Assert.Contains(design.Checks, c => c.Sentence.StartsWith("Print at actual size", StringComparison.Ordinal));
        Assert.StartsWith("GL-", design.Definition.Id, StringComparison.Ordinal);
    }
}
