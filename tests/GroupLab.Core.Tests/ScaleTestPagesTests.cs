using GroupLab.Cli;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 278 section 3: the three test pages for question 67 differ from the built check page only in the card
/// outline, the note beside it and the test's name, so what the photographs measure is the outline and nothing else.
/// </summary>
public class ScaleTestPagesTests
{
    [Theory]
    [InlineData('A', 0)]
    [InlineData('B', 4)]
    [InlineData('C', 8)]
    public void OnlyTheOutlineAndTheWordsChange(char variant, int outlineRects)
    {
        var definition = GltdJsonReader.Read(File.ReadAllBytes(Repo.PathTo("targets", "GL-SCALE-LTR-1.gltd.json"))).Definition!;
        var built = SceneBuilder.Build(definition).Pages[0];
        var test = ScaleTestVerb.Variant(built, definition.Page, variant);

        // Every marker module is a rectangle too, so exactly four rectangles removed means no marker lost one.
        var before = built.Items.ToHashSet();
        var after = test.Items.ToHashSet();
        var removed = built.Items.Where(i => !after.Contains(i)).ToList();
        var added = test.Items.Where(i => !before.Contains(i)).ToList();

        Assert.All(removed, i => Assert.True(i is RectFill || (i is TextRun t && t.Text == Core.Gltd.Derivation.GridStyle4.CardNote)));
        Assert.Equal(variant == 'A' ? 0 : 4, removed.OfType<RectFill>().Count());
        Assert.Equal(outlineRects, added.OfType<RectFill>().Count());
        Assert.Contains(added.OfType<TextRun>(), t => t.Text == $"TEST {variant}, NOT FOR USE");
    }
}
