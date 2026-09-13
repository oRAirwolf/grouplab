using System.Text;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Binary;

/// <summary>Conformance tests 1 to 3 of TARGET-SCHEMA.md section 10.</summary>
public class RoundTripTests
{
    [Fact]
    public void Test1Section4DocumentRoundTripsToItsProjection()
    {
        var source = GltdJsonReader.Read(Encoding.UTF8.GetBytes(Spec.Section4Example)).Definition!;
        var encoding = GltdBinary.Encode(source).Encoding!;

        var decoded = GltdBinary.Decode([GltdBinary.ReplicatedFrame(encoding)]);

        Assert.Empty(decoded.Diagnostics);
        var projection = decoded.Definition!;

        // J to B to J to B: identical bytes.
        var again = GltdBinary.Encode(projection).Encoding!;
        Assert.Equal(encoding.Body, again.Body);

        // J to B to J is a fixed point, and a valid document.
        byte[] canonical = CanonicalJsonWriter.Write(projection);
        var reread = GltdJsonReader.Read(canonical);
        Assert.Empty(reread.Diagnostics);
        var reprojected = GltdBinary.Decode([GltdBinary.ReplicatedFrame(GltdBinary.Encode(reread.Definition!).Encoding!)]).Definition!;
        Assert.Equal(canonical, CanonicalJsonWriter.Write(reprojected));

        // The projection keeps every piece of geometry the source declared.
        Assert.Equal(source.Page, projection.Page);
        Assert.Equal(
            source.Bulls.Select(b => (b.X, b.Y, b.Scoring, b.Label)),
            projection.Bulls.Select(b => (b.X, b.Y, b.Scoring, b.Label)));
        Assert.Equal(
            source.RingSets.Single().Discs.Select(d => (d.Diameter, IsPaper(source, d.Ink))),
            projection.RingSets.Single().Discs.Select(d => (d.Diameter, IsPaper(projection, d.Ink))));
        Assert.Equal(source.Codes!.Positions, projection.Codes!.Positions);
        Assert.Equal(source.Cells!.Grid, projection.Cells!.Grid);
        Assert.Equal(InkRole.Fiducial, projection.Inks.Single(i => i.Key == projection.Fiducials!.Ink).Role);
    }

    [Fact]
    public void Tests2And3RandomCanonicalBodiesRoundTripWithStableIdentifiers()
    {
        var rng = new Random(20260913);
        for (int i = 0; i < 500; i++)
        {
            var model = RandomBodies.Next(rng);
            var (body, flags) = BodyCodec.Write(model);

            var decoded = GltdBinary.DecodeBody(body, flags);
            Assert.True(decoded.Definition is not null, $"Body {i} ({Convert.ToHexStringLower(body)}) was rejected: {string.Join("; ", decoded.Diagnostics)}");

            var again = GltdBinary.Encode(decoded.Definition).Encoding!;
            Assert.Equal(body, again.Body);
            Assert.Equal(flags, again.BlockFlags);
            Assert.Equal(DefinitionId.Compute(body), again.DefinitionId);
            Assert.Equal(decoded.DefinitionId, again.DefinitionId);
        }
    }

    private static bool IsPaper(TargetDefinition d, string inkKey) => d.Inks.Single(i => i.Key == inkKey).Role == InkRole.Paper;
}
