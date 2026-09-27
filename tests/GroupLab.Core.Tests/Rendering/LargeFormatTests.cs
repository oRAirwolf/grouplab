using GroupLab.Core.Capture;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Rendering;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 226 section 5: a large target a flatbed cannot take. Printed as tiles on one large page, every tile is a
/// piece with its own markers and codes and no bull, marker or code lies on a cut line; and whether a sheet can be photographed whole is
/// worked out from the pixels a phone gives it.
/// </summary>
public class LargeFormatTests
{
    [Theory]
    [InlineData("GL-LR300-T.gltd.json")]
    [InlineData("GL-LR300-TA4.gltd.json")]
    [InlineData("GL-LR300-T.3x2.gltd.json")]
    public void TilesOnOnePageAreWholePiecesWithCutLinesBetweenThem(string file)
    {
        var d = BuiltIns.Load(file);
        var tiles = TargetRenderer.Render(d).Pages;
        var one = TargetRenderer.Render(d, new RenderOptions(OneSheet: true));

        var page = Assert.Single(one.Pages);
        Assert.Equal(2L * d.Tiling!.Cols * d.Tiling.SheetWidth, page.Width);
        Assert.Equal(2L * d.Tiling.Rows * d.Tiling.SheetHeight, page.Height);
        Assert.Equal(tiles.Sum(t => t.Items.Count), page.Items.Count(i => i.Layer != SceneLayer.CutLines));
        var cuts = page.Items.OfType<RectFill>().Where(r => r.Layer == SceneLayer.CutLines).ToList();
        Assert.NotEmpty(cuts);
        foreach (var item in page.Items.Where(i => i.Layer is SceneLayer.Bulls or SceneLayer.Markers or SceneLayer.Codes))
        {
            var (x0, y0, x1, y1) = item switch
            {
                RectFill r => (r.X, r.Y, r.X + r.Width, r.Y + r.Height),
                DiscBand b => (b.CentreX - b.OuterRadius, b.CentreY - b.OuterRadius, b.CentreX + b.OuterRadius, b.CentreY + b.OuterRadius),
                _ => (0L, 0L, 0L, 0L),
            };
            Assert.DoesNotContain(cuts, c => c.X < x1 && x0 < c.X + c.Width && c.Y < y1 && y0 < c.Y + c.Height);
        }

        Assert.NotNull(one.Pdf);
    }

    [Fact]
    public void ASingleSheetIsNotCutAndSaysWhy()
    {
        var d = BuiltIns.Load("GL-LR25-TAB.gltd.json");
        Assert.NotNull(CutSheet.Refusal(d));
        Assert.Equal(TargetRenderer.Render(d).Pages.Count, TargetRenderer.Render(d, new RenderOptions(OneSheet: true)).Pages.Count);
    }

    [Theory]
    [InlineData(8.5, 11, null)]
    [InlineData(8.27, 11.69, null)]
    [InlineData(11, 17, "A 12 MP photograph of the whole sheet is enough")]
    [InlineData(24, 28, "only at a 50 MP phone's full resolution")]
    [InlineData(36, 24, "only at a 50 MP phone's full resolution")]
    [InlineData(42, 24, "only at a 50 MP phone's full resolution")]
    public void WhatAPhoneGivesAWholeSheetDecidesTheAdvice(double width, double height, string? says)
    {
        string? advice = PhotographLimit.Advice(width, height);
        if (says is null)
        {
            Assert.Null(advice);
            return;
        }

        Assert.Contains(says, advice, StringComparison.Ordinal);
    }

    [Fact]
    public void TheFortyTwoInchRollGivesATwelveMegapixelPhoneTooFewPixels()
    {
        var ppi = PhotographLimit.PixelsPerInch(42, 24);
        Assert.InRange(ppi[0].PixelsPerInch, 80, 90);
        Assert.InRange(ppi[1].PixelsPerInch, 170, 180);
        Assert.True(ppi[0].PixelsPerInch < PhotographLimit.Good);
    }
}
