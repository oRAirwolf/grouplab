using System.Globalization;
using System.Text.RegularExpressions;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Pdf;
using GroupLab.Core.Tests.Support;
using SkiaSharp;

namespace GroupLab.Core.Tests.Rendering;

/// <summary>Conformance tests 27, 38, 40 and 41 of TARGET-SCHEMA.md section 10, over every built-in sheet.</summary>
[Collection(PdfiumCollection.Name)]
public partial class RenderingTests
{
    public static TheoryData<string> Files()
    {
        var data = new TheoryData<string>();
        foreach (string file in BuiltIns.Files)
        {
            data.Add(file);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void Test38RenderingTheSameDefinitionTwiceIsByteIdentical(string file)
    {
        var first = TargetRenderer.Render(BuiltIns.Load(file));
        var second = TargetRenderer.Render(BuiltIns.Load(file));

        Assert.DoesNotContain(first.Diagnostics, d => d.Severity == Severity.Error);
        Assert.NotNull(first.Pdf);
        Assert.Equal(first.Pdf, second.Pdf);
        Assert.Equal(first.Pages.Count, BuiltIns.Load(file).Tiling is { } t ? t.Cols * t.Rows : 1);
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void Test40EveryGeometricValueIsTheDefinitionsInteger(string file)
    {
        var d = BuiltIns.Load(file);
        var result = TargetRenderer.Render(d);
        var page = result.Pages[0];

        // Discs: one band per inked disc, bounded by the two declared diameters, centred on the declared bull.
        var sets = d.RingSets.ToDictionary(s => s.Key);
        var expectedBands = d.Bulls.SelectMany(b =>
        {
            var discs = sets[b.RingSet].Discs;
            return discs.Select((disc, i) => (disc, i))
                .Where(x => d.Inks.Single(ink => ink.Key == x.disc.Ink).Role != InkRole.Paper)
                .Select(x => (2L * b.X, 2L * b.Y, (long)x.disc.Diameter, x.i + 1 < discs.Count ? (long)discs[x.i + 1].Diameter : 0L));
        });
        Assert.Equal(expectedBands, page.Items.OfType<DiscBand>().Select(b => (b.CentreX, b.CentreY, b.OuterRadius, b.InnerRadius)));

        // Markers: every module rectangle lies on the module grid of a derived marker square.
        var f = d.Fiducials!;
        long module = f.MarkerSize / 4;
        var squares = f.Markers!.Select(m => ((2L * m.X) - f.MarkerSize, (2L * m.Y) - f.MarkerSize)).ToHashSet();
        foreach (var rect in page.Items.OfType<RectFill>().Where(r => r.Layer == SceneLayer.Markers))
        {
            Assert.Contains(squares, s => rect.X >= s.Item1 && rect.Y >= s.Item2 && rect.X + rect.Width <= s.Item1 + (8 * module)
                && rect.Y + rect.Height <= s.Item2 + (8 * module) && (rect.X - s.Item1) % module == 0 && (rect.Y - s.Item2) % module == 0);
        }

        // Measurement grid lines sit on the derived positions, section 3.13.
        foreach (var g in d.Grids ?? [])
        {
            var lines = page.Items.OfType<RectFill>().Where(r => r.Layer == SceneLayer.MeasurementGrid && r.Height == 4L * g.Half)
                .Select(r => r.X + (r.Width / 2)).Distinct().Order();
            Assert.Equal(MeasurementGridLines.Positions(g.CentreX, g.Half, g.Divisions).Select(p => 2L * p), lines);
        }

        // The content stream writes every rectangle and every path anchor as an integer.
        foreach (Match m in PathOperator().Matches(PdfWriter.Content(page)))
        {
            string[] operands = m.Groups["operands"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var anchors = m.Groups["op"].Value == "c" ? operands[^2..] : operands;
            Assert.All(anchors, a => Assert.True(long.TryParse(a, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _), $"Non-integer anchor {a} in \"{m.Value}\"."));
        }
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void Test41PaperDiscsLayNoInkInThePdf(string file)
    {
        var d = BuiltIns.Load(file);
        var result = TargetRenderer.Render(d);

        foreach (var page in result.Pages)
        {
            string content = PdfWriter.Content(page);
            Assert.DoesNotContain("1 1 1 rg", content, StringComparison.Ordinal);
            Assert.All(page.Items, item => Assert.Equal(new GroupLab.Core.Gltd.Binary.Rgb(0, 0, 0), item.Colour));
        }

        int inkedDiscs = d.Bulls.Sum(b => d.RingSets.Single(s => s.Key == b.RingSet).Discs.Count(disc => disc.Ink != "paper"));
        Assert.Equal(inkedDiscs, result.Pages[0].Items.OfType<DiscBand>().Count());
    }

    [Fact]
    public void Test41KnockoutsRevealTheSubstrateNotWhite()
    {
        // Rendered onto buff stock: a paper band must show the stock, which a white fill would hide.
        var buff = new SKColor(238, 222, 176);
        var d = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var page = TargetRenderer.Render(d).Pages[0];
        var bull = d.Bulls[0];
        var raster = Raster.Render(page, bull.X, bull.Y, 300, 600, buff);

        for (int k = 0; k < 8; k++)
        {
            double angle = k * Math.PI / 4;
            foreach (double radius in (double[])[91, 35])
            {
                var colour = raster.ColourAt(bull.X + (radius * Math.Cos(angle)), bull.Y + (radius * Math.Sin(angle)));
                Assert.True(Math.Abs(colour.Red - buff.Red) <= 3 && Math.Abs(colour.Green - buff.Green) <= 3 && Math.Abs(colour.Blue - buff.Blue) <= 3,
                    $"Paper band at radius {radius} dmm shows {colour}, not the stock {buff}.");
            }
        }

        Assert.True(raster.Ink(bull.X, bull.Y) > 0.9, "The centre dot is not inked.");
    }

    [Fact]
    public void Test41AKnockoutInTheAimingMarkShowsTheGridBeneathIt()
    {
        // Section 3.4: a paper disc reveals what is underneath. On a zeroing sheet that is the grid's axis lines. The
        // aiming mark is discs of 127, 114 and 25 dmm, so its knockout is the band between radii 12.5 and 57 dmm.
        var d = BuiltIns.Load("GL-ZERO-MOA-100Y.gltd.json");
        var page = TargetRenderer.Render(d).Pages[0];
        var aim = d.Bulls[0];
        var raster = Raster.Render(page, aim.X, aim.Y, 200, 600);

        Assert.True(raster.Ink(aim.X + 35, aim.Y) > 0.9, "The horizontal axis is hidden inside the knockout band.");
        Assert.True(raster.Ink(aim.X, aim.Y + 35) > 0.9, "The vertical axis is hidden inside the knockout band.");
        Assert.True(raster.Ink(aim.X + 24.7, aim.Y + 24.7) < 0.05, "The knockout band laid ink away from the axes.");
        Assert.True(raster.Ink(aim.X + 42.4, aim.Y + 42.4) > 0.9, "The ring between 114 and 127 dmm is not inked.");
    }

    [Fact]
    public void Test27BlankAndFilledDifferOnlyInsideTheDataBlock()
    {
        var instance = InstanceFromSection311();
        foreach (string file in (string[])["GL-CF25-LTR-D.gltd.json", "GL-ZERO-MIL-100Y.gltd.json"])
        {
            var d = BuiltIns.Load(file);
            var blank = TargetRenderer.Render(d, new RenderOptions(DataBlockMode.Blank, instance));
            var filled = TargetRenderer.Render(d, new RenderOptions(DataBlockMode.Filled, instance));

            Assert.DoesNotContain(filled.Diagnostics, x => x.Severity == Severity.Error);
            Assert.Equal(blank.DefinitionId, filled.DefinitionId);
            Assert.Equal(
                blank.Pages[0].Items.Where(i => i.Layer != SceneLayer.DataBlockContent),
                filled.Pages[0].Items.Where(i => i.Layer != SceneLayer.DataBlockContent));

            var filledContent = filled.Pages[0].Items.Where(i => i.Layer == SceneLayer.DataBlockContent).ToList();
            bool codeExpected = d.DataBlock!.Reserve >= 280;

            // Section 3.10: an instance code only in a square of 280 dmm or more; the identifier as text otherwise.
            Assert.Equal(codeExpected, filledContent.OfType<RectFill>().Any());
            Assert.Equal(!codeExpected, filledContent.OfType<TextRun>().Any(t => t.Text.StartsWith(blank.DefinitionId![..12], StringComparison.Ordinal)));
            Assert.Contains(blank.Pages[0].Items.OfType<TextRun>(), t => t.Layer == SceneLayer.DataBlockContent && t.Text == "Serial A7K3");
        }
    }

    private static Instance InstanceFromSection311()
    {
        var doc = Spec.Section4Node();
        var (name, value) = Spec.Fragment("### 3.11 ");
        doc[name] = value;
        return GltdJsonReader.Read(Spec.Utf8(doc)).Definition!.Instance!;
    }

    [GeneratedRegex(@"(?<operands>(?:-?[0-9.]+ )+)(?<op>re|m|l|c)\n")]
    private static partial Regex PathOperator();
}
