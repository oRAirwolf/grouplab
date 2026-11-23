using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Rendering;

/// <summary>
/// Error report 55 (NOTES-FROM-PLANNING.md entry 510): on a sheet with a load block, "Filled in now, from these fields" with the serial
/// typed as "T-0002" printed the square at the bottom right of the block empty, while the preview showed words in it. The code that goes
/// there needs a serial of four letters or digits; refused, it drew nothing and said nothing. Now the square carries the sheet's code and
/// the serial in words whenever it cannot carry the code, and still carries the code when it can.
/// </summary>
public class Report55LoadBlockSquareTests
{
    private static readonly KeyValuePair<string, string>[] Fields =
        [new("cartridge", "7mm PRC"), new("primer", "CCI BR2"), new("seating", "2.780 COAL")];

    private static RenderResult Filled(string? serial) =>
        TargetRenderer.Render(BuiltIns.Load("GL-CF9-T-C.gltd.json"),
            new RenderOptions(DataBlockMode.Filled, new Instance(serial, "2026-11-22", Fields), TileIndex: 1));

    private static List<SceneItem> Square(RenderResult result) =>
        [.. result.Pages[0].Items.Where(i => i.Layer == SceneLayer.DataBlockContent)];

    [Theory]
    [InlineData("T-0002")]
    [InlineData(null)]
    [InlineData("TOOLONG")]
    public void WithoutACodeTheSquareCarriesTheSheetsCodeAndTheSerialInWords(string? serial)
    {
        var result = Filled(serial);
        Assert.NotNull(result.Pdf);
        Assert.Contains(result.Diagnostics, d => d.Code == "instance.serial");
        var words = Square(result).OfType<TextRun>().Select(t => t.Text).ToList();
        Assert.Contains(result.DefinitionId![..12], words);
        Assert.Contains("Serial " + (serial ?? "____"), words);
        Assert.Contains("7mm PRC", words);
    }

    [Fact]
    public void WithAFourCharacterSerialTheSquareStillCarriesTheCodeAndNoWords()
    {
        var result = Filled("T002");
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == Severity.Error);
        var square = Square(result);
        Assert.Contains(square, i => i is RectFill);
        Assert.DoesNotContain(square.OfType<TextRun>(), t => t.Text.StartsWith("Serial", StringComparison.Ordinal));
    }
}
