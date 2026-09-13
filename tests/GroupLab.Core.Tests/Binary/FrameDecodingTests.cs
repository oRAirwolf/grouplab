using System.Text;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Binary;

/// <summary>Conformance tests 5 to 9 of TARGET-SCHEMA.md section 10, and decoding robustness beyond them.</summary>
public class FrameDecodingTests
{
    private static readonly BinaryEncoding Reference =
        GltdBinary.Encode(GltdJsonReader.Read(Encoding.UTF8.GetBytes(Spec.Section4Example)).Definition!).Encoding!;

    [Fact]
    public void ReferenceFrameDecodes()
    {
        var result = GltdBinary.Decode([GltdBinary.ReplicatedFrame(Reference)]);

        Assert.Empty(result.Diagnostics);
        Assert.Equal("GL-YCSK-DZZ1-R0VJ-4T5Y", result.DefinitionId);
    }

    [Fact]
    public void Test5BadCrcIsRejectedAndNamed()
    {
        byte[] frame = GltdBinary.ReplicatedFrame(Reference);
        frame[^1] ^= 0x01;

        AssertRejected(frame, "CRC-32");
    }

    [Fact]
    public void Test6UnknownWireVersionIsRejectedWithoutAPartialParse()
    {
        byte[] frame = GltdBinary.ReplicatedFrame(Reference);
        frame[2] = 2;

        var result = AssertRejected(frame, "wire version 2");
        Assert.Null(result.DefinitionId);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(15)]
    public void Test7ReservedFlagBitIsRejected(int bit)
    {
        byte[] frame = WithFlags(GltdBinary.ReplicatedFrame(Reference), 1 << bit);

        AssertRejected(frame, "Reserved flag bits");
    }

    [Theory]
    [InlineData(2, "cell")]
    [InlineData(3, "label")]
    [InlineData(4, "print")]
    [InlineData(5, "extension")]
    public void UnspecifiedBlockBitIsRejectedAndNamed(int bit, string block)
    {
        byte[] frame = WithFlags(GltdBinary.ReplicatedFrame(Reference), 1 << bit);

        AssertRejected(frame, $"a {block} block");
    }

    [Fact]
    public void Test8EveryTruncationIsRejected()
    {
        byte[] frame = GltdBinary.ReplicatedFrame(Reference);
        for (int length = 0; length < frame.Length; length++)
        {
            var result = GltdBinary.Decode([frame[..length]]);
            Assert.Null(result.Definition);
            Assert.NotEmpty(result.Diagnostics);
        }
    }

    [Fact]
    public void Test9OversizedTotalLenIsRejected()
    {
        byte[] frame = GltdBinary.ReplicatedFrame(Reference);
        frame[9] = 0xFF;
        frame[10] = 0xFF;

        AssertRejected(frame, "exceeds the 55 body bytes present");
    }

    [Fact]
    public void Test9UntrustedLengthIsNotAllocated()
    {
        // A compressed frame claiming a 65535-byte body from three payload bytes.
        byte[] frame = [.. GltdBinary.ReplicatedFrame(Reference)[..FrameCodec.HeaderLength], 0x01, 0x02, 0x03];
        frame[3] |= 0x02;
        frame[9] = 0xFF;
        frame[10] = 0xFF;
        GltdBinary.Decode([frame]);

        long before = GC.GetAllocatedBytesForCurrentThread();
        var result = GltdBinary.Decode([frame]);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Null(result.Definition);
        Assert.Contains("cannot inflate", result.Diagnostics.Single().Message, StringComparison.Ordinal);
        Assert.True(allocated < 16_000, $"Rejecting the frame allocated {allocated} bytes.");
    }

    [Fact]
    public void InstanceFrameIsNotAcceptedAsADefinition()
    {
        byte[] frame = GltdBinary.ReplicatedFrame(Reference);
        frame[1] = (byte)'I';

        AssertRejected(frame, "GLTD-I instance frame");
    }

    [Fact]
    public void ExplicitCodePlacementByteIsRejected()
    {
        var (model, _) = Projection.ToBody(GltdJsonReader.Read(Encoding.UTF8.GetBytes(Spec.Section4Example)).Definition!);
        var (body, flags) = BodyCodec.Write(model! with { Codes = model.Codes with { Placement = 1 } });

        AssertRejected(FrameCodec.Replicated(body, flags), "question 13");
    }

    [Fact]
    public void ExplicitFiducialSchemeByteIsRejected()
    {
        var (model, _) = Projection.ToBody(GltdJsonReader.Read(Encoding.UTF8.GetBytes(Spec.Section4Example)).Definition!);
        var (body, flags) = BodyCodec.Write(model! with { Fiducials = model.Fiducials with { Scheme = 0 } });

        AssertRejected(FrameCodec.Replicated(body, flags), "no byte layout");
    }

    [Fact]
    public void NonCanonicalBodyIsRejected()
    {
        // Boustrophedon over a single row places bulls exactly as row-major does, and row-major is canonical.
        var (model, _) = Projection.ToBody(GltdJsonReader.Read(Encoding.UTF8.GetBytes(Spec.Section4Example)).Definition!);
        var singleRow = model! with { Grid = model.Grid! with { Rows = 1, Order = 2 } };
        var (body, flags) = BodyCodec.Write(singleRow);

        AssertRejected(FrameCodec.Replicated(body, flags), "not in canonical form");
    }

    [Fact]
    public void RepeatedInkColourIsRejected()
    {
        var (model, _) = Projection.ToBody(GltdJsonReader.Read(Encoding.UTF8.GetBytes(Spec.Section4Example)).Definition!);
        var (body, flags) = BodyCodec.Write(model! with { Inks = [model.Inks[0], model.Inks[0]] });

        AssertRejected(FrameCodec.Replicated(body, flags), "repeats a colour");
    }

    [Fact]
    public void RandomCorruptionNeverThrowsOrYieldsAnotherDefinition()
    {
        byte[] original = GltdBinary.ReplicatedFrame(Reference);
        var rng = new Random(43);
        for (int i = 0; i < 3000; i++)
        {
            byte[] frame = [.. original];
            switch (rng.Next(3))
            {
                case 0:
                    for (int k = rng.Next(1, 4); k > 0; k--)
                    {
                        frame[rng.Next(frame.Length)] ^= (byte)rng.Next(1, 256);
                    }

                    break;
                case 1:
                    frame = frame[..rng.Next(frame.Length)];
                    break;
                default:
                    int at = rng.Next(frame.Length);
                    frame = [.. frame[..at], (byte)rng.Next(256), .. frame[at..]];
                    break;
            }

            var result = GltdBinary.Decode([frame]);
            Assert.True(result.Definition is null || result.DefinitionId == Reference.DefinitionId,
                $"Mutation {i} decoded to {result.DefinitionId}.");
        }
    }

    private static DecodeResult AssertRejected(byte[] frame, string reason)
    {
        var result = GltdBinary.Decode([frame]);
        Assert.Null(result.Definition);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Contains(reason, diagnostic.Message, StringComparison.Ordinal);
        return result;
    }

    private static byte[] WithFlags(byte[] frame, int extra)
    {
        int flags = frame[3] | (frame[4] << 8) | extra;
        frame[3] = (byte)flags;
        frame[4] = (byte)(flags >> 8);
        return frame;
    }
}
