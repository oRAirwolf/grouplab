using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Gltd.Binary;

/// <summary>
/// A canonical body with the block flags of section 5.1 and its identifier, which section 6 computes from the body
/// rather than the JSON.
/// </summary>
public sealed record BinaryEncoding(byte[] Body, ushort BlockFlags, string DefinitionId);

/// <summary>The encoding, or the refusals naming each field the body could not carry.</summary>
public sealed record EncodeResult(BinaryEncoding? Encoding, IReadOnlyList<Diagnostic> Diagnostics);

/// <summary>
/// The canonical projection decoded from frames (section 6), its identifier, and the tile index from the frame
/// header, which section 3.12 keeps out of the body.
/// </summary>
public sealed record DecodeResult(TargetDefinition? Definition, string? DefinitionId, byte TileIndex, IReadOnlyList<Diagnostic> Diagnostics);

/// <summary>GLTD-J to GLTD-B and back, TARGET-SCHEMA.md sections 5 and 6.</summary>
public static class GltdBinary
{
    public static EncodeResult Encode(TargetDefinition definition)
    {
        var (model, diagnostics) = Projection.ToBody(definition);
        if (model is null)
        {
            return new EncodeResult(null, diagnostics);
        }

        var (body, flags) = BodyCodec.Write(model);
        if (body.Length > ushort.MaxValue)
        {
            return new EncodeResult(null,
                [.. diagnostics, Diagnostic.Error("encode.tooLarge", "", $"The body is {body.Length} bytes; totalLen addresses at most {ushort.MaxValue}.")]);
        }

        return new EncodeResult(new BinaryEncoding(body, flags, DefinitionId.Compute(body)), diagnostics);
    }

    public static byte[] ReplicatedFrame(BinaryEncoding encoding, byte tileIndex = 0, byte shareIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(encoding);
        return FrameCodec.Replicated(encoding.Body, encoding.BlockFlags, tileIndex, shareIndex);
    }

    public static byte[][] ErasureCodedFrames(BinaryEncoding encoding, byte tileIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(encoding);
        return FrameCodec.ErasureCoded(encoding.Body, encoding.BlockFlags, tileIndex);
    }

    public static DecodeResult Decode(IReadOnlyList<byte[]> frames)
    {
        var assembly = FrameCodec.Assemble(frames);
        return assembly.Body is null
            ? Rejected("frame.rejected", assembly.Error!)
            : DecodeBody(assembly.Body, assembly.Header!.Flags, assembly.Header.TileIndex);
    }

    public static DecodeResult DecodeBody(byte[] body, ushort flags, byte tileIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(body);
        var read = BodyCodec.Read(body, flags);
        if (read.Body is null)
        {
            return Rejected("body.malformed", read.Error!);
        }

        string id = DefinitionId.Compute(body);
        var definition = Projection.FromBody(read.Body, id);

        // The decode must be a valid GLTD-J document, or the projection has manufactured something the
        // format does not allow, such as a code centre off the page.
        var reread = GltdJsonReader.Read(CanonicalJsonWriter.Write(definition));
        if (reread.Definition is null)
        {
            return Rejected("body.invalidProjection",
                $"The body decodes to an invalid GLTD-J document: {reread.Diagnostics.First(x => x.Severity == Severity.Error)}");
        }

        // Section 6: a body has exactly one legal encoding. One that would re-encode differently would
        // change identifier on its first save, so it is refused rather than half-trusted.
        var again = Encode(definition);
        ushort blockFlags = (ushort)(flags & ~WireCodes.DeflateFlag);
        if (again.Encoding is null || again.Encoding.BlockFlags != blockFlags || !again.Encoding.Body.AsSpan().SequenceEqual(body))
        {
            return Rejected("body.nonCanonical",
                "The body is not in canonical form: re-encoding its own decode gives different bytes (TARGET-SCHEMA.md section 6).");
        }

        return new DecodeResult(definition, id, tileIndex, []);
    }

    private static DecodeResult Rejected(string code, string message) =>
        new(null, null, 0, [Diagnostic.Error(code, "", message)]);
}
