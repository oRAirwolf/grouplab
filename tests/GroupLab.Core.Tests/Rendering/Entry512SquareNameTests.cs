using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Pdf;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Rendering;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 512 section 3, Alan's request 114 answered (b): a session's name typed before printing prints in the load
/// block's square, and each sheet of a set prints its place in the set large ("2 of 3"). The code keeps its size and quiet zone and is never
/// shrunk to make room; a name that does not fit is cut with dots and never wraps; a sheet printed without a name looks as it did apart from
/// the place in its set. Where the code fills the square nothing is added (a question to the planning session).
/// </summary>
public class Entry512SquareNameTests
{
    private const string Set = "GL-CF9-T-C.gltd.json";
    private const string Single = "GL-CF25-LTR-D-C.gltd.json";

    private static readonly KeyValuePair<string, string>[] Fields = [new("cartridge", "7mm PRC")];

    private static Scene Page(string file, int tile, string? name, DataBlockMode mode = DataBlockMode.Blank, string? serial = null) =>
        SceneBuilder.Build(BuiltIns.Load(file), new RenderOptions(mode,
            mode == DataBlockMode.Filled ? new Instance(serial, "2026-11-23", Fields) : serial is null ? null : new Instance(serial, null, null),
            TileIndex: tile, SheetName: name)).Pages[0];

    private static List<SceneItem> Square(Scene page) => [.. page.Items.Where(i => i.Layer == SceneLayer.DataBlockContent)];

    private static List<TextRun> Words(Scene page) => [.. Square(page).OfType<TextRun>()];

    private static DataFieldCell SquareCell(string file) => DataBlockCells.Derive(BuiltIns.Load(file).DataBlock!).Reserve!;

    [Fact]
    public void EachSheetOfASetPrintsItsPlaceInTheSetLargerThanTheOtherWords()
    {
        for (int tile = 0; tile < 3; tile++)
        {
            var words = Words(Page(Set, tile, null));
            var place = Assert.Single(words, w => w.Text == $"{tile + 1} of 3");
            Assert.All(words.Where(w => w != place), w => Assert.True(place.FontSize >= 3 * w.FontSize, $"{place.FontSize} against {w.FontSize}"));
            Assert.Contains(words, w => w.Text == "Serial ____");
        }
    }

    [Fact]
    public void ASingleSheetWithoutANamePrintsExactlyAsBefore()
    {
        var before = Square(Page(Single, 0, null));
        Assert.Equal(before, Square(Page(Single, 0, "   ")));
        Assert.Equal(3, before.OfType<TextRun>().Count());
        Assert.DoesNotContain(before.OfType<TextRun>(), w => w.Text.Contains(" of ", StringComparison.Ordinal));
    }

    [Fact]
    public void ATypedNamePrintsAboveTheSheetsCodeOnOneLine()
    {
        var words = Words(Page(Set, 1, "  Rifle A\n140 gr  "));
        Assert.Equal(["Rifle A 140 gr", "2 of 3"], words.Take(2).Select(w => w.Text));
        Assert.True(words[0].Baseline < words[1].Baseline);
        Assert.Contains(words, w => w.Text == "Serial ____");
    }

    [Fact]
    public void ANameThatDoesNotFitIsCutWithDotsAndEveryLineStaysInsideTheSquare()
    {
        string name = "A very long session name for the seven millimetre PRC with the 180 grain bullet";
        var square = SquareCell(Set);
        var words = Words(Page(Set, 2, name));
        var cut = words[0];
        Assert.EndsWith("...", cut.Text, StringComparison.Ordinal);
        Assert.StartsWith(cut.Text[..^3], name, StringComparison.Ordinal);
        Assert.Equal(SceneBuilder.SquareNameMinimumSize, cut.FontSize);
        foreach (var w in words)
        {
            Assert.True(HelveticaMetrics.TextWidth(w.Text, w.FontSize) <= (2L * square.Width) - 80, w.Text);
            Assert.True(w.Baseline - w.FontSize >= 2L * square.Y && w.Baseline + (w.FontSize / 4) <= 2L * (square.Y + square.Height), w.Text);
        }
    }

    [Fact]
    public void WhereTheCodeFillsTheSquareItIsUntouchedAndNothingIsAdded()
    {
        var without = Square(Page(Set, 1, null, DataBlockMode.Filled, "T002"));
        var with = Square(Page(Set, 1, "Rifle A", DataBlockMode.Filled, "T002"));
        Assert.Contains(without, i => i is RectFill);
        Assert.Equal(without.Where(i => i is not TextRun), with.Where(i => i is not TextRun));
        Assert.DoesNotContain(with.OfType<TextRun>(), w => w.Text == "Rifle A" || w.Text == "2 of 3");
    }

    [Fact]
    public void AFilledSquareWithoutItsCodeCarriesTheNameToo()
    {
        var words = Words(Page(Set, 0, "Rifle A", DataBlockMode.Filled, "T-0002"));
        Assert.Contains(words, w => w.Text == "Rifle A");
        Assert.Contains(words, w => w.Text == "1 of 3");
        Assert.Contains(words, w => w.Text == "Serial T-0002");
        Assert.Contains(words, w => w.Text == "7mm PRC");
    }
}
