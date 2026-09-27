using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Gltd.Validation;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Library;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 243 section 4: the C bull, a black diamond standing on a point with a white diamond center and a dot, in the
/// format, on the wire, through the validator, drawn, and read back from a render with holes in the black, across the diamond's edges, on
/// its points and in its center.
/// </summary>
public class DiamondBullTests
{
    private const double Dpi = 300;

    /// <summary>
    /// How far a mark may sit from the hole it was made from, in dmm: 0.06 in. A synthetic hole is drawn wholly on ink or wholly on paper,
    /// chosen by its center, so one straddling an ink edge comes back off center on any bull. On the round E bull the same test loses one
    /// such hole outright and puts two 12 and 14 dmm off; on the diamond every hole is found, the worst 13.6 dmm off (entry 243 section 4).
    /// </summary>
    private const double Placed = 15;

    private static TargetDefinition CSheet() => BuiltIns.Load("GL-CF25-LTR-C.gltd.json");

    [Fact]
    public void TheCBullIsASquareOnAPointAroundASquareOnAPointAndARoundDot()
    {
        var discs = CSheet().RingSets.Single().Discs;
        Assert.Equal([(318, DiscShape.Square, 45), (92, DiscShape.Square, 45), (25, DiscShape.Circle, 0)], discs.Select(d => (d.Diameter, d.Shape, d.Rotation)));

        // The aim point card's proportions: the diamond 3.47 times the center, the dot 0.10 in on the 0.36 in center.
        Assert.InRange(discs[0].Diameter / (double)discs[1].Diameter, 3.4, 3.5);
    }

    [Fact]
    public void AShapeSurvivesTheJsonAndTheWireAndACircleWritesNothingNew()
    {
        var sheet = CSheet();
        byte[] json = CanonicalJsonWriter.Write(sheet);
        Assert.Contains("\"shape\": \"square\"", System.Text.Encoding.UTF8.GetString(json), StringComparison.Ordinal);
        Assert.Equal(sheet.RingSets, GltdJsonReader.Read(json).Definition!.RingSets, new RingSetsEqual());

        var encoded = GltdBinary.Encode(sheet).Encoding!;
        var decoded = GltdBinary.Decode([GltdBinary.ReplicatedFrame(encoded)]).Definition!;
        Assert.Equal(sheet.RingSets.Single().Discs.Select(d => (d.Diameter, d.Shape, d.Rotation)), decoded.RingSets.Single().Discs.Select(d => (d.Diameter, d.Shape, d.Rotation)));

        // Every earlier sheet is a circle, and its bytes are what they were: no shape, no rotation.
        string plain = System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Repo.PathTo("targets", "GL-CF25-LTR.gltd.json")));
        Assert.DoesNotContain("\"shape\"", plain, StringComparison.Ordinal);
        Assert.DoesNotContain("\"rotation\"", plain, StringComparison.Ordinal);
    }

    [Fact]
    public void TheWireRefusesTheBitsItStillReservesAndACircleOnAPoint()
    {
        var encoded = GltdBinary.Encode(CSheet()).Encoding!;
        var model = BodyCodec.Read(encoded.Body, encoded.BlockFlags).Body!;
        Assert.Equal(encoded.Body, BodyCodec.Write(model).Body);

        // The dot written with a reserved bit set, and then with the "on a point" bit alone, which a circle cannot have.
        string Refusal(byte shape)
        {
            var set = model.RingSets[0];
            var changed = model with { RingSets = [[.. set.Take(set.Count - 1), set[^1] with { Shape = shape }]] };
            var (body, flags) = BodyCodec.Write(changed);
            return BodyCodec.Read(body, flags).Error ?? "";
        }

        Assert.Contains("reserved ink bits 6 and 7", Refusal(4), StringComparison.Ordinal);
        Assert.Contains("circle turned onto a point", Refusal(BodyDisc.OnAPoint), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(DiscShape.Square, 30, "20b")]
    [InlineData(DiscShape.Circle, 45, "20b")]
    public void ASquareStandsOnASideOrAPointAndACircleDoesNotTurn(DiscShape shape, int rotation, string rule)
    {
        var sheet = CSheet();
        var turned = sheet with { RingSets = [new RingSet("c", [new Disc(318, "black", shape, rotation), new Disc(92, "paper", DiscShape.Square, 45), new Disc(25, "black")])] };
        Assert.Contains(GltdValidator.Validate(turned), d => d.Test == rule && d.Severity == Severity.Error);
    }

    [Fact]
    public void ADiscThatReachesPastTheDiamondAroundItIsRefused()
    {
        // A 70 dmm circle has a smaller diameter than the 92 dmm diamond around it, and still reaches past its sides, 32.5 dmm from the center.
        var sheet = CSheet();
        var wide = sheet with { RingSets = [new RingSet("c", [new Disc(318, "black", DiscShape.Square, 45), new Disc(92, "paper", DiscShape.Square, 45), new Disc(70, "black")])] };
        Assert.Contains(GltdValidator.Validate(wide), d => d.Test == "20a" && d.Severity == Severity.Error);
        Assert.DoesNotContain(GltdValidator.Validate(sheet), d => d.Severity == Severity.Error);
    }

    [Fact]
    public void TheDiamondIsDrawnAsADiamondStandingOnAPoint()
    {
        var sheet = CSheet();
        var page = SceneBuilder.Build(sheet).Pages[0];
        var bull = sheet.Bulls.First(b => b.Scoring);
        var bands = page.Items.OfType<DiscBand>().Where(b => b.CentreX == 2L * bull.X && b.CentreY == 2L * bull.Y).ToList();
        Assert.Equal([(DiscShape.Square, 45, DiscShape.Square, 45), (DiscShape.Circle, 0, DiscShape.Circle, 0)],
            bands.Select(b => (b.OuterShape, b.OuterRotation, b.InnerShape, b.InnerRotation)));

        var raster = SceneRasterizer.Rasterize(page, Dpi);
        byte At(double dx, double dy) => raster[(int)((bull.X + dx) * Dpi / 254), (int)((bull.Y + dy) * Dpi / 254)];
        Assert.True(At(0, 0) < 64, "the dot is black");
        Assert.True(At(0, 35) > 192, "the white center is paper");
        Assert.True(At(0, 140) < 64 && At(140, 0) < 64, "the black reaches toward its points on the vertical and horizontal lines");
        Assert.True(At(0, 170) > 192, "past the point is paper");

        // The test a disc would fail: 141 dmm out on the diagonal is inside a 318 dmm circle, and outside a diamond of that size.
        Assert.True(At(100, 100) > 192, "the diagonal past the diamond's side is paper");
        Assert.True(At(70, 70) < 64, "the diagonal inside the diamond's side is black");

        string content = System.Text.Encoding.Latin1.GetString(TargetRenderer.Render(sheet).Pdf!);
        Assert.Contains(" l\n", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Section 4 items 2 and 5: one .308 hole a bull, placed in turn on the dot, in the black, across the diamond's sides, on a point, across
    /// the white center's side and out on the paper of the diagonal. Every bull is found as closely as a round one, every hole is found, and
    /// every hole goes to its own bull, the ones at the points included, which sit 31 dmm from the gap to the next bull's.
    /// </summary>
    [Fact]
    public void HolesInTheBlackAcrossItsEdgesAndAtItsPointsAreFoundAndAssigned()
    {
        var sheet = CSheet();
        const double reach = 159, side = reach / 2;
        var scene = SceneBuilder.Build(sheet);
        var render = SceneRasterizer.Rasterize(scene.Pages[0], Dpi);
        (double X, double Y)[] offsets = [(0, 0), (0, -100), (60, 60), (side, side), (0, reach), (reach, 0), (0, -reach), (23, 23), (120, 120), (-side, -side)];
        var scoring = sheet.Bulls.Select((b, i) => (b, i)).Where(x => x.b.Scoring).ToList();
        var random = new Random(2434);
        bool OnInk(double x, double y) => render[(int)(x * Dpi / 254), (int)(y * Dpi / 254)] < 128;
        var planned = scoring.Select((x, n) => (Bull: x.i, X: x.b.X + offsets[n % offsets.Length].X, Y: x.b.Y + offsets[n % offsets.Length].Y)).ToList();
        var holes = planned.Select(p => SyntheticSheet.SampleHole(random, p.X, p.Y, OnInk(p.X, p.Y), HoleBacking.ScannerLid, 0.871)).ToList();
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var image = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], random);
        var metadata = new ImageMetadata("PNG", image.Width, image.Height, Dpi, Dpi, null, null, null, null, null);
        var result = AutomaticMarking.Run(image, image, metadata, sheet, new OpenCvSharpBackend(), calibre: Calibre.Of(0.308));
        Assert.True(result.Failure is null, result.Failure);

        // The edge fit, along each ray to the diamond's side: within the conformance gate of 0.001 in on every bull.
        foreach (var located in result.Measurement.Bulls.Where(b => sheet.Bulls[b.Index].Scoring))
        {
            Assert.True(located.Recovered is not null, $"bull {located.Name}: {located.Failure}");
            Assert.True(located.Error < 0.254, $"bull {located.Name} is {located.Error:0.000} dmm from where it was printed");
        }

        var found = result.Detections.Select(d => truth.ToPage(d.Image)).ToList();
        var missed = planned.Where(p => !found.Any(f => Math.Pow(f.X - p.X, 2) + Math.Pow(f.Y - p.Y, 2) < 100)).Select(p => $"({p.X - sheet.Bulls[p.Bull].X:0}, {p.Y - sheet.Bulls[p.Bull].Y:0}) on bull {sheet.Bulls[p.Bull].Label}, nearest mark {found.Min(f => Math.Sqrt(Math.Pow(f.X - p.X, 2) + Math.Pow(f.Y - p.Y, 2))):0.0} dmm away");
        Assert.True(planned.Count == result.Detections.Count, $"{result.Detections.Count} marks for {planned.Count} holes; missed {string.Join(", ", missed)}; rejected {string.Join("; ", (result.Rejected ?? []).Take(12).Select(r => r.Reason))}");
        foreach (var shot in result.Detections)
        {
            var page = truth.ToPage(shot.Image);
            var nearest = planned.MinBy(p => Math.Pow(p.X - page.X, 2) + Math.Pow(p.Y - page.Y, 2));
            double off = Math.Sqrt(Math.Pow(nearest.X - page.X, 2) + Math.Pow(nearest.Y - page.Y, 2));
            Assert.True(off < Placed, $"a mark at ({page.X:0}, {page.Y:0}), {off:0.0} dmm from the nearest hole");
            Assert.Equal(nearest.Bull, shot.Assignment.Bull);
        }
    }

    private sealed class RingSetsEqual : IEqualityComparer<IReadOnlyList<RingSet>>
    {
        public bool Equals(IReadOnlyList<RingSet>? x, IReadOnlyList<RingSet>? y) =>
            x is not null && y is not null && x.Count == y.Count && x.Zip(y).All(p => p.First.Key == p.Second.Key && p.First.Discs.SequenceEqual(p.Second.Discs));

        public int GetHashCode(IReadOnlyList<RingSet> obj) => obj.Count;
    }
}
