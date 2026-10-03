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
            if (g.StyleOrDefault == GridStyle4.Style)
            {
                // Entry 273: the printer check page's crosshairs are centered on their derived points and its rulers are their stated
                // lengths exactly, tick to tick, since those are what a caliper and a ruler are read against.
                var rects = page.Items.OfType<RectFill>().Where(r => r.Layer == SceneLayer.MeasurementGrid).ToList();
                foreach (var c in GridStyle4.Crosshairs(d.Page))
                {
                    Assert.Contains(rects, r => r.Width > r.Height && r.X + (r.Width / 2) == 2L * c.X && r.Y + (r.Height / 2) == 2L * c.Y);
                    Assert.Contains(rects, r => r.Height > r.Width && r.X + (r.Width / 2) == 2L * c.X && r.Y + (r.Height / 2) == 2L * c.Y);
                }

                if (GridStyle4.IsLabel(d.Page))
                {
                    // Entry 358 section 3: the check label's ruler along the feed, its stated length tick to tick.
                    var (rx, rtop, rbottom) = GridStyle4.LabelRuler(d.Page);
                    Assert.Contains(rects, r => r.X + (r.Width / 2) == 2L * rx && r.Y == 2L * rtop && r.Height == 2L * (rbottom - rtop) && rbottom - rtop == GridStyle4.LabelRulerDmm);
                    continue;
                }

                var (dx, top, bottom) = GridStyle4.RulerDown(d.Page);
                Assert.Contains(rects, r => r.X + (r.Width / 2) == 2L * dx && r.Y == 2L * top && r.Height == 2L * (bottom - top) && bottom - top == GridStyle4.RulerDownDmm);
                var (ay, left, right) = GridStyle4.RulerAcross(d.Page);
                Assert.Contains(rects, r => r.Y + (r.Height / 2) == 2L * ay && r.X == 2L * left && r.Width == 2L * (right - left) && right - left == GridStyle4.RulerAcrossDmm);
                continue;
            }

            if (g.StyleOrDefault == GridStyle3.Style)
            {
                // Style 3 draws every line inside its field, the centre cross left out inside the aim's white; a tick is shorter than any
                // line, so every upright piece longer than a tick is on a derived line.
                var derived3 = GridStyle3.Lines(g, g.HalfX).Select(l => 2L * (g.CentreX + l.Offset)).ToHashSet();
                var upright3 = page.Items.OfType<RectFill>().Where(r => r.Layer == SceneLayer.MeasurementGrid && r.Height > r.Width
                    && r.Height > 8L * GridStyle3.TickReach && r.Y >= 2L * (g.CentreY - g.HalfY - GridStyle3.HeavyStroke) && r.Y + r.Height <= 2L * (g.CentreY + g.HalfY + GridStyle3.HeavyStroke))
                    .Select(r => r.X + (r.Width / 2)).ToHashSet();
                Assert.Equal(derived3.Order(), upright3.Order());
                continue;
            }

            if (g.StyleOrDefault == GridStyle2.Style)
            {
                // Style 2 breaks a line where a label sits, and draws only inside its field: every upright piece is on a derived line.
                var derived = GridStyle2.Lines(g, g.HalfX).Select(l => 2L * (g.CentreX + l.Offset)).ToHashSet();
                var upright = page.Items.OfType<RectFill>().Where(r => r.Layer == SceneLayer.MeasurementGrid && r.Height > r.Width
                    && r.Y >= 2L * (g.CentreY - g.HalfY) && r.Y + r.Height <= 2L * (g.CentreY + g.HalfY)).Select(r => r.X + (r.Width / 2)).ToHashSet();
                Assert.Equal(derived.Order(), upright.Order());
                continue;
            }

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
        // Section 3.4: a paper disc reveals what is underneath. On a style 2 zeroing sheet (frozen since entry 254) that is the grid's axis
        // lines; the aiming ring is discs of 200 and 150 dmm, so its knockout is everything inside a radius of 75 dmm. A C3 sheet keeps its
        // white centre clear instead (Test41BTheC3DiamondsWhiteCentreStaysWhite).
        var d = BuiltIns.Load("frozen/zero-grid-2/GL-6DX8-6NNC-QF2S-BAWN.gltd.json");
        var page = TargetRenderer.Render(d).Pages[0];
        var aim = d.Bulls[0];
        var raster = Raster.Render(page, aim.X, aim.Y, 250, 600);

        Assert.True(raster.Ink(aim.X + 50, aim.Y) > 0.9, "The horizontal axis is hidden inside the knockout.");
        Assert.True(raster.Ink(aim.X, aim.Y + 50) > 0.9, "The vertical axis is hidden inside the knockout.");
        Assert.True(raster.Ink(aim.X + 35, aim.Y + 35) < 0.05, "The knockout laid ink away from the axes.");
        Assert.True(raster.Ink(aim.X + 61.9, aim.Y + 61.9) > 0.9, "The ring between 150 and 200 dmm is not inked.");
    }

    /// <summary>Entry 251: a C3 diamond's white centre is left white, the centre cross broken inside it, and its dot and black are inked.</summary>
    [Fact]
    public void Test41BTheC3DiamondsWhiteCentreStaysWhite()
    {
        var d = BuiltIns.Load("GL-ZERO-MOA-100Y.gltd.json");
        var page = TargetRenderer.Render(d).Pages[0];
        var aim = d.Bulls[0];
        double white = d.RingSets[0].Discs[1].Diameter / 2.0, outer = d.RingSets[0].Discs[0].Diameter / 2.0;
        var raster = Raster.Render(page, aim.X, aim.Y, 400, 600);

        Assert.True(raster.Ink(aim.X + (0.7 * white), aim.Y) < 0.05, "The horizontal axis runs through the white centre.");
        Assert.True(raster.Ink(aim.X, aim.Y - (0.7 * white)) < 0.05, "The vertical axis runs through the white centre.");
        Assert.True(raster.Ink(aim.X, aim.Y) > 0.9, "The dot is not inked.");
        Assert.True(raster.Ink(aim.X + ((white + outer) / 2), aim.Y) > 0.9, "The diamond's black is not inked.");
    }

    [Fact]
    public void Test27BlankAndFilledDifferOnlyInsideTheDataBlock()
    {
        var instance = InstanceFromSection311();
        foreach (string file in (string[])["GL-CF25-LTR-D.gltd.json", "frozen/zero-grid-2/GL-JZ3H-FDJH-NXQF-BN49.gltd.json"])
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
