using System.Buffers.Binary;
using System.IO.Compression;

namespace GroupLab.Core.Gltd.Binary;

/// <summary>
/// The 15-byte frame header of TARGET-SCHEMA.md section 5.1. <see cref="TotalLength"/> and <see cref="Crc"/> describe
/// the uncompressed, unsharded body, so the CRC checks reconstruction end to end.
/// </summary>
public sealed record FrameHeader(ushort Flags, byte ShareIndex, byte ShareCount, byte ShareK, byte TileIndex, ushort TotalLength, uint Crc);

/// <summary>One parsed frame: a header and its payload, or the reason it was rejected.</summary>
public sealed record FrameParseResult(FrameHeader? Header, byte[]? Payload, string? Error);

/// <summary>A body rebuilt from one or more frames, or the reason none could be (conformance tests 5 to 11).</summary>
public sealed record FrameAssemblyResult(byte[]? Body, FrameHeader? Header, string? Error);

/// <summary>
/// The GLTD-B frame of TARGET-SCHEMA.md section 5.1. Parsing checks the header and every declared length
/// against the bytes actually present before anything is allocated on the strength of it.
/// </summary>
public static class FrameCodec
{
    public const int HeaderLength = 15;
    public const byte WireVersion = 1;

    private const int CompressionThreshold = 128;

    // Raw DEFLATE cannot expand input by more than this factor, so a larger totalLen is a lie.
    private const int MaxDeflateRatio = 1032;

    private static readonly string[] UnspecifiedBlockNames = ["cell", "label", "print", "extension"];

    /// <summary>
    /// A replicated frame. Raw DEFLATE is applied only to a body of at least 128 bytes, and only when it
    /// makes the payload strictly smaller (section 5.7).
    /// </summary>
    public static byte[] Replicated(ReadOnlySpan<byte> body, ushort blockFlags, byte tileIndex = 0, byte shareIndex = 0)
    {
        byte[]? compressed = body.Length >= CompressionThreshold ? Deflate(body) : null;
        return compressed is not null && compressed.Length < body.Length
            ? Build((ushort)(blockFlags | WireCodes.DeflateFlag), shareIndex, 1, 1, tileIndex, body, compressed)
            : Build(blockFlags, shareIndex, 1, 1, tileIndex, body, body);
    }

    /// <summary>Four erasure-coded frames, any two of which rebuild the body (section 5.6). Never compressed.</summary>
    public static byte[][] ErasureCoded(ReadOnlySpan<byte> body, ushort blockFlags, byte tileIndex = 0)
    {
        var shares = ErasureCoder.Split(body);
        var frames = new byte[ErasureCoder.ShareCount][];
        for (int i = 0; i < frames.Length; i++)
        {
            frames[i] = Build(blockFlags, (byte)i, ErasureCoder.ShareCount, ErasureCoder.SharesNeeded, tileIndex, body, shares[i]);
        }

        return frames;
    }

    public static FrameParseResult Parse(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < HeaderLength)
        {
            return Reject($"Truncated frame: {frame.Length} bytes, and the header alone is {HeaderLength}.");
        }

        if (frame[0] == (byte)'G' && frame[1] == (byte)'I')
        {
            return Reject("This is a GLTD-I instance frame (magic \"GI\"), not a GLTD-B definition frame.");
        }

        if (frame[0] != (byte)'G' || frame[1] != (byte)'T')
        {
            return Reject($"Bad magic 0x{frame[0]:X2} 0x{frame[1]:X2}; a definition frame starts \"GT\".");
        }

        if (frame[2] != WireVersion)
        {
            return Reject($"Unknown wire version {frame[2]}; this decoder reads wire version {WireVersion} only.");
        }

        ushort flags = BinaryPrimitives.ReadUInt16LittleEndian(frame[3..]);
        if ((flags & WireCodes.ReservedFlags) != 0)
        {
            return Reject($"Reserved flag bits are set (0x{flags & WireCodes.ReservedFlags:X4}); the writer used a feature this decoder does not implement.");
        }

        if ((flags & WireCodes.UnspecifiedBlockFlags) != 0)
        {
            var names = UnspecifiedBlockNames.Where((_, bit) => (flags & (1 << (bit + 2))) != 0);
            return Reject($"The frame declares a {string.Join(", ", names)} block, which has no byte layout (TARGET-SCHEMA.md section 11, question 10).");
        }

        var header = new FrameHeader(
            flags, frame[5], frame[6], frame[7], frame[8],
            BinaryPrimitives.ReadUInt16LittleEndian(frame[9..]),
            BinaryPrimitives.ReadUInt32LittleEndian(frame[11..]));
        int available = frame.Length - HeaderLength;
        bool compressed = (flags & WireCodes.DeflateFlag) != 0;

        if (header.TotalLength == 0)
        {
            return Reject("totalLen is 0; a body is never empty.");
        }

        if (header.ShareCount == 1)
        {
            if (header.ShareK != 1 || header.ShareIndex > 7)
            {
                return Reject($"A replicated frame has shareK 1 and shareIndex 0 to 7, not {header.ShareK} and {header.ShareIndex}.");
            }

            if (!compressed && header.TotalLength > available)
            {
                return Reject($"totalLen {header.TotalLength} exceeds the {available} body bytes present.");
            }

            if (!compressed && header.TotalLength < available)
            {
                return Reject($"{available - header.TotalLength} bytes follow the {header.TotalLength}-byte body.");
            }

            if (compressed && (available == 0 || header.TotalLength > (long)available * MaxDeflateRatio))
            {
                return Reject($"totalLen {header.TotalLength} cannot inflate from {available} compressed bytes.");
            }
        }
        else if (header.ShareCount == ErasureCoder.ShareCount)
        {
            if (header.ShareK != ErasureCoder.SharesNeeded || header.ShareIndex >= ErasureCoder.ShareCount)
            {
                return Reject($"An erasure-coded frame has shareK 2 and shareIndex 0 to 3, not {header.ShareK} and {header.ShareIndex}.");
            }

            if (compressed)
            {
                return Reject("Erasure-coded frames are never compressed.");
            }

            int shareLength = (header.TotalLength + 1) / 2;
            if (shareLength != available)
            {
                return Reject($"totalLen {header.TotalLength} needs shares of {shareLength} bytes, but {available} are present.");
            }
        }
        else
        {
            return Reject($"shareCount {header.ShareCount} is not supported: 1 is replicated, and 4 with shareK 2 is the only erasure scheme (section 5.6).");
        }

        return new FrameParseResult(header, frame[HeaderLength..].ToArray(), null);
    }

    /// <summary>
    /// Rebuilds the body from one or more frames of the same definition: any one intact replicated frame,
    /// or any two distinct erasure shares. The CRC is checked last, on the rebuilt uncompressed body.
    /// </summary>
    public static FrameAssemblyResult Assemble(IReadOnlyList<byte[]> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        var parsed = new List<(FrameHeader Header, byte[] Payload)>();
        string? error = frames.Count == 0 ? "No frames." : null;
        foreach (var frame in frames)
        {
            var result = Parse(frame);
            if (result.Header is null)
            {
                error ??= result.Error;
                continue;
            }

            parsed.Add((result.Header, result.Payload!));
        }

        foreach (var (header, payload) in parsed.Where(p => p.Header.ShareCount == 1))
        {
            byte[]? body = (header.Flags & WireCodes.DeflateFlag) != 0 ? Inflate(payload, header.TotalLength, out error) : payload;
            if (body is not null && CheckCrc(body, header, out error))
            {
                return new FrameAssemblyResult(body, header, null);
            }
        }

        var shares = parsed.Where(p => p.Header.ShareCount == ErasureCoder.ShareCount).ToList();
        if (shares.Count > 0)
        {
            var first = shares[0].Header;
            var distinct = shares
                .Where(s => s.Header with { ShareIndex = 0 } == first with { ShareIndex = 0 })
                .DistinctBy(s => s.Header.ShareIndex)
                .ToList();
            if (distinct.Count < ErasureCoder.SharesNeeded)
            {
                return new FrameAssemblyResult(null, null,
                    $"Erasure-coded frames need {ErasureCoder.SharesNeeded} distinct shares of one body to rebuild it; {distinct.Count} present.");
            }

            byte[] joined = ErasureCoder.Reconstruct(distinct[0].Header.ShareIndex, distinct[0].Payload, distinct[1].Header.ShareIndex, distinct[1].Payload);
            byte[] body = joined[..first.TotalLength];
            if (CheckCrc(body, first, out error))
            {
                return new FrameAssemblyResult(body, first with { ShareIndex = 0 }, null);
            }
        }

        return new FrameAssemblyResult(null, null, error ?? "No frame rebuilt a body.");
    }

    private static bool CheckCrc(byte[] body, FrameHeader header, out string? error)
    {
        uint actual = Crc32.Compute(body);
        error = actual == header.Crc
            ? null
            : $"CRC-32 mismatch: the header says 0x{header.Crc:X8} and the rebuilt body computes to 0x{actual:X8}.";
        return error is null;
    }

    private static byte[] Build(ushort flags, byte shareIndex, byte shareCount, byte shareK, byte tileIndex, ReadOnlySpan<byte> body, ReadOnlySpan<byte> payload)
    {
        if (body.Length is 0 or > ushort.MaxValue)
        {
            throw new ArgumentException($"A body is 1 to {ushort.MaxValue} bytes, not {body.Length}.", nameof(body));
        }

        var frame = new byte[HeaderLength + payload.Length];
        frame[0] = (byte)'G';
        frame[1] = (byte)'T';
        frame[2] = WireVersion;
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(3), flags);
        frame[5] = shareIndex;
        frame[6] = shareCount;
        frame[7] = shareK;
        frame[8] = tileIndex;
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(9), (ushort)body.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(11), Crc32.Compute(body));
        payload.CopyTo(frame.AsSpan(HeaderLength));
        return frame;
    }

    private static byte[] Deflate(ReadOnlySpan<byte> body)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            deflate.Write(body);
        }

        return output.ToArray();
    }

    private static byte[]? Inflate(byte[] payload, int totalLength, out string? error)
    {
        try
        {
            using var input = new MemoryStream(payload);
            using var inflate = new DeflateStream(input, CompressionMode.Decompress);
            var body = new byte[totalLength];
            int read = inflate.ReadAtLeast(body, totalLength, throwOnEndOfStream: false);
            if (read != totalLength)
            {
                error = $"The compressed body inflates to {read} bytes, not the {totalLength} totalLen declares.";
                return null;
            }

            if (inflate.ReadByte() != -1)
            {
                error = $"The compressed body inflates beyond the {totalLength} bytes totalLen declares.";
                return null;
            }

            error = null;
            return body;
        }
        catch (InvalidDataException ex)
        {
            error = $"The compressed body is not valid raw DEFLATE: {ex.Message}";
            return null;
        }
    }

    private static FrameParseResult Reject(string error) => new(null, null, error);
}
