using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// Entry 474 section 1: a large page drawn in bands of rows at once is the page drawn on one thread, to the bit, at the sizes and in the
/// ways the application draws: the hole finder's 600 dpi page, a word-drawn preview, a stretched page, a part of a page, a scaled page and
/// a colour channel.
/// </summary>
public class SceneRasterizerBandsTests
{
    public static TheoryData<string> Sheets => ["GL-CF25-LTR.gltd.json", "GL-CF25-A4-C.gltd.json"];

    [Theory]
    [MemberData(nameof(Sheets))]
    public void BandsGiveTheOneThreadsPixels(string sheet)
    {
        var page = SceneBuilder.Build(BuiltIns.Load(sheet), new RenderOptions(AllowInvalid: true)).Pages[0];
        var cases = new (string Name, double Dpi, double Scale, PixelRegion? Region, bool Words, Func<GroupLab.Core.Gltd.Binary.Rgb, double>? Level, double StretchY)[]
        {
            ("600 dpi", 600, 1, null, false, null, 1),
            ("words at 300 dpi", 300, 1, null, true, null, 1),
            ("stretched", 400, 1, null, false, null, 1.013),
            ("a region", 600, 1, new PixelRegion(300, 1700, 2400, 2100), false, null, 1),
            ("scaled", 500, 0.962, null, false, null, 1),
            ("the value channel", 600, 1, null, false, SceneRasterizer.Value, 1),
        };
        foreach (var c in cases)
        {
            var one = SceneRasterizer.Rasterize(page, c.Dpi, c.Scale, c.Region, c.Words, c.Level, c.StretchY, bands: 1);
            foreach (int bands in new[] { 0, 3, 7, 64 })
            {
                var many = SceneRasterizer.Rasterize(page, c.Dpi, c.Scale, c.Region, c.Words, c.Level, c.StretchY, bands);
                Assert.True(one.Pixels.AsSpan().SequenceEqual(many.Pixels), $"{sheet}, {c.Name}, {bands} bands");
            }
        }
    }
}
