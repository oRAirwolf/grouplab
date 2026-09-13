using System.Text;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Binary;

/// <summary>Conformance tests 10 and 11, and the compression rule of section 5.7.</summary>
public class SharesAndCompressionTests
{
    private static readonly BinaryEncoding Reference =
        GltdBinary.Encode(GltdJsonReader.Read(Encoding.UTF8.GetBytes(Spec.Section4Example)).Definition!).Encoding!;

    [Fact]
    public void Test10AnyOneOfFourReplicatedFramesReconstructs()
    {
        var frames = Enumerable.Range(0, 4).Select(i => GltdBinary.ReplicatedFrame(Reference, shareIndex: (byte)i)).ToArray();

        foreach (var frame in frames)
        {
            var result = GltdBinary.Decode([frame]);
            Assert.Equal(Reference.DefinitionId, result.DefinitionId);
        }

        // Three damaged codes and one intact one still reconstruct.
        byte[][] damaged = [.. frames.Select(f => (byte[])f.Clone())];
        damaged[0][^1] ^= 0xFF;
        damaged[1] = damaged[1][..20];
        damaged[3][FrameCodec.HeaderLength] ^= 0x10;
        Assert.Equal(Reference.DefinitionId, GltdBinary.Decode(damaged).DefinitionId);
    }

    [Fact]
    public void Test11AnyTwoOfFourErasureSharesReconstructAndOneDoesNot()
    {
        var shares = GltdBinary.ErasureCodedFrames(Reference);
        Assert.Equal(4, shares.Length);
        Assert.All(shares, s => Assert.Equal(FrameCodec.HeaderLength + ((Reference.Body.Length + 1) / 2), s.Length));

        for (int i = 0; i < 4; i++)
        {
            var alone = GltdBinary.Decode([shares[i]]);
            Assert.Null(alone.Definition);
            Assert.Contains("need 2 distinct shares", alone.Diagnostics.Single().Message, StringComparison.Ordinal);

            for (int j = i + 1; j < 4; j++)
            {
                var pair = GltdBinary.Decode([shares[j], shares[i]]);
                Assert.Empty(pair.Diagnostics);
                Assert.Equal(Reference.DefinitionId, pair.DefinitionId);
            }
        }
    }

    [Fact]
    public void Test11ADamagedShareDoesNotProduceAWrongDefinition()
    {
        var shares = GltdBinary.ErasureCodedFrames(Reference);
        shares[2][FrameCodec.HeaderLength + 3] ^= 0x40;

        var result = GltdBinary.Decode([shares[0], shares[2]]);

        Assert.Null(result.Definition);
        Assert.Contains("CRC-32", result.Diagnostics.Single().Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SmallBodyIsNeverCompressed()
    {
        byte[] frame = GltdBinary.ReplicatedFrame(Reference);

        Assert.Equal(0, frame[3] & 0x02);
        Assert.Equal(FrameCodec.HeaderLength + Reference.Body.Length, frame.Length);
    }

    [Fact]
    public void LargeCompressibleBodyIsDeflatedAndDecodes()
    {
        var (model, _) = Projection.ToBody(GltdJsonReader.Read(Encoding.UTF8.GetBytes(Spec.Section4Example)).Definition!);
        var set = model!.RingSets[0];
        var body = BodyCodec.Write(model with { RingSets = [.. Enumerable.Repeat(set, 15)] });
        Assert.True(body.Body.Length >= 128);

        byte[] frame = FrameCodec.Replicated(body.Body, body.Flags);
        var result = GltdBinary.Decode([frame]);

        Assert.Equal(0x02, frame[3] & 0x02);
        Assert.True(frame.Length < FrameCodec.HeaderLength + body.Body.Length);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(DefinitionId.Compute(body.Body), result.DefinitionId);
        Assert.Equal(15, result.Definition!.RingSets.Count);
    }
}
