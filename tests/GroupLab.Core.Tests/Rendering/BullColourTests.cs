using GroupLab.Core.Evaluation;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Rendering;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 297 and entry 309 section 4: bulls in black, blue or red. Only the bulls, their rings and numbers take the
/// color; the codes, the markers, the title and the load block stay black; large solid areas print as a 60 percent tint; and no color draws
/// anything thicker or larger than black, so a sheet's shapes are the same in every color.
/// </summary>
public class BullColourTests
{
    private static Scene Page(string file, BullColour colour) => SceneBuilder.Build(BuiltIns.Load(file), new RenderOptions(BullColour: colour)).Pages[0];

    [Theory]
    [InlineData("GL-CF25-LTR.gltd.json")]
    [InlineData("GL-CF25-LTR-C.gltd.json")]
    [InlineData("GL-ZERO-MOA-100Y.gltd.json")]
    public void OnlyTheBullsTakeTheColorAndEveryShapeIsTheSame(string file)
    {
        var black = Page(file, BullColour.Black);
        foreach (var colour in new[] { BullColour.Blue, BullColour.Red })
        {
            var page = Page(file, colour);
            Assert.Equal(black.Items.Count, page.Items.Count);
            for (int i = 0; i < page.Items.Count; i++)
            {
                var (was, now) = (black.Items[i], page.Items[i]);
                Assert.Equal(was with { Colour = now.Colour }, now);
                if (!BullColours.Coloured(was.Layer))
                {
                    Assert.Equal(was.Colour, now.Colour);
                }
                else if (was.Colour == new Rgb(0, 0, 0))
                {
                    Assert.Equal(BullColours.Solid(was) ? BullColours.Mix(BullColours.Of(colour), BullColours.SolidTint) : BullColours.Of(colour), now.Colour);
                }
            }

            Assert.Contains(page.Items, item => item.Layer is SceneLayer.Markers or SceneLayer.Codes && item.Colour == new Rgb(0, 0, 0));
            Assert.Contains(page.Items, item => item.Layer == SceneLayer.Bulls && item.Colour != new Rgb(0, 0, 0));
        }
    }

    [Fact]
    public void TheHuesAreTheOnesAlanApproved()
    {
        Assert.Equal("#D22630", BullColours.Of(BullColour.Red).ToString());
        Assert.Equal("#1F5FBF", BullColours.Of(BullColour.Blue).ToString());
        Assert.Equal(0.6, BullColours.SolidTint);
        Assert.All(BullColours.All, c => Assert.Equal(c, BullColours.Parse(BullColours.Name(c))));
    }

    [Fact]
    public void TheCBullsDiamondIsATintAndItsRingsAreFull()
    {
        var page = Page("GL-CF25-LTR-C.gltd.json", BullColour.Red);
        var bulls = page.Items.OfType<DiscBand>().Where(b => b.Layer == SceneLayer.Bulls).ToList();
        Assert.Contains(bulls, b => BullColours.Solid(b) && b.Colour == BullColours.Mix(BullColours.Of(BullColour.Red), 0.6));
    }

    /// <summary>
    /// Entry 297 section 4: a color is held to the black sheet's line for the same condition, shadow, glare and poor light; the committed
    /// baseline has a line for each, and a color that fell short would not be offered.
    /// </summary>
    [Fact]
    public void EveryColorLineIsOnTheBaselineAndNoWorseThanBlack()
    {
        var baseline = Scoreboard.FromJson(File.ReadAllText(Repo.PathTo("docs", "scoreboard", "synthetic-baseline.json")));
        foreach (var colour in new[] { BullColour.Blue, BullColour.Red })
        {
            foreach (string condition in Scoreboard.ColourConditions)
            {
                var line = baseline.Rows.Single(r => r.Condition == Scoreboard.ColourLine(colour, condition));
                var black = baseline.Rows.Single(r => r.Condition == condition);
                Assert.True(line.Found >= black.Found - baseline.Margin.Holes, $"{line.Condition}: {line.Found} found against black's {black.Found}");
                Assert.True(line.FalseMarks <= black.FalseMarks + baseline.Margin.FalseMarks, $"{line.Condition}: {line.FalseMarks} false marks against black's {black.FalseMarks}");
                Assert.Equal(black.Registered, line.Registered);
            }
        }
    }
}
