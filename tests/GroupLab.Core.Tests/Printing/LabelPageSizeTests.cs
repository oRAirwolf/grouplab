using GroupLab.Cli.Library;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Printing;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Printing;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 358 section 1: 4x6, A6 and 100 x 150 mm beside Letter and A4, as named sizes of the format, on the wire, in
/// the print checks and in the designer. Not 70 x 80 mm.
/// </summary>
public class LabelPageSizeTests
{
    public static TheoryData<PageSize, string, byte, int, int> Labels() => new()
    {
        { PageSize.A6, "a6", 10, 1050, 1480 },
        { PageSize.Label4x6, "label-4x6", 11, 1016, 1524 },
        { PageSize.Label100x150, "label-100x150", 12, 1000, 1500 },
    };

    [Theory]
    [MemberData(nameof(Labels))]
    public void EachLabelSizeHasItsNameItsWireCodeAndItsDimensions(PageSize size, string name, byte code, int width, int height)
    {
        Assert.Equal(name, GltdNames.PageSize.NameOf(size));
        Assert.Equal(code, WireCodes.PageCode(size));
        Assert.Equal(size, WireCodes.PageSizeOf(code));
        Assert.False(WireCodes.IsRoll(code));
        Assert.Equal((width, height), PageSizes.Standard(size));
        Assert.True(PageSizes.IsLabel(size));
    }

    [Fact]
    public void TheRollsStillCarryTheirHeightAndNothingElseDoes()
    {
        Assert.True(WireCodes.IsRoll(7) && WireCodes.IsRoll(8) && WireCodes.IsRoll(9));
        Assert.False(WireCodes.IsRoll(0) || WireCodes.IsRoll(1) || WireCodes.IsRoll(10));
        Assert.False(PageSizes.IsLabel(PageSize.Letter) || PageSizes.IsLabel(PageSize.A4));
    }

    [Theory]
    [MemberData(nameof(Labels))]
    public void ASheetOnALabelEncodesInThreePageBytesAndReadsBack(PageSize size, string name, byte code, int width, int height)
    {
        _ = name;
        var design = ParametricSheet.Design(new SheetSpec("One bull", size switch { PageSize.A6 => "a6", PageSize.Label4x6 => "4x6", _ => "100x150" },
            2, 2, 1.5, 254, 0, false));
        Assert.True(design.Definition is not null, string.Join(" ", design.Checks.Select(c => c.Sentence)));
        var definition = design.Definition!;
        Assert.Equal((size, width, height), (definition.Page.Size, definition.Page.Width, definition.Page.Height));

        var encoded = GltdBinary.Encode(definition).Encoding!;
        Assert.Equal(code, encoded.Body[0]);
        var decoded = GltdBinary.Decode([GltdBinary.ReplicatedFrame(encoded)]).Definition!;
        Assert.Equal((size, width, height), (decoded.Page.Size, decoded.Page.Width, decoded.Page.Height));

        var reread = GltdJsonReader.Read(CanonicalJsonWriter.Write(definition)).Definition!;
        Assert.Equal(size, reread.Page.Size);
    }

    [Fact]
    public void ThePrintChecksNameTheLabelPapers()
    {
        Assert.StartsWith("4x6 label (101.6 x 152.4 mm)", PrintFit.PaperName(2032, 3048), StringComparison.Ordinal);
        Assert.StartsWith("A6 (105 x 148 mm)", PrintFit.PaperName(2100, 2960), StringComparison.Ordinal);
        Assert.StartsWith("100 x 150 mm label", PrintFit.PaperName(2000, 3000), StringComparison.Ordinal);
    }

    [Fact]
    public void TheDesignerOffersTheLabelsBesideLetterAndA4AndNotSeventyByEighty()
    {
        Assert.Equal(["letter", "a4", "tabloid", "a3", "4x6", "a6", "100x150"], ParametricSheet.Pages);
        Assert.Equal("4x6 label", ParametricSheet.PageWords("4x6"));
        Assert.Equal("A6", ParametricSheet.PageWords("a6"));
        Assert.Equal("100 x 150 mm label", ParametricSheet.PageWords("100x150"));
        Assert.DoesNotContain(Enum.GetNames<PageSize>(), n => n.Contains("70", StringComparison.Ordinal));
    }

    [Fact]
    public void TheLibraryCallsALabelByItsName()
    {
        Assert.Equal(1, TargetLibrary.PaperRank(PageSize.Label4x6));
        Assert.Equal(2, TargetLibrary.PaperRank(PageSize.A6));
        Assert.Equal(2, TargetLibrary.PaperRank(PageSize.Label100x150));
        _ = Repo.PathTo("targets");
    }
}
