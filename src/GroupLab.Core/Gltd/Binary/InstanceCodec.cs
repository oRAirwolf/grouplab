using System.Buffers.Binary;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Gltd.Binary;

/// <summary>A GLTD-I frame, or the refusals naming the field that could not be carried (conformance test 30).</summary>
public sealed record InstanceEncodeResult(byte[]? Frame, IReadOnlyList<Diagnostic> Diagnostics);

/// <summary>
/// Decoded instance data. <see cref="Instance"/> is null when the frame belongs to another definition, which is
/// reported and never merged (conformance test 29).
/// </summary>
public sealed record InstanceDecodeResult(Instance? Instance, string? DefinitionId, IReadOnlyList<Diagnostic> Diagnostics);

/// <summary>
/// The GLTD-I instance frame of TARGET-SCHEMA.md section 3.11. Field lengths are one byte
/// (docs/SPEC-ERRATA.md C3), the date is a little-endian 24-bit day count from 2000-01-01, and the CRC
/// covers the uncompressed field bytes.
/// </summary>
public static partial class InstanceCodec
{
    public const int HeaderLength = 25;
    public const int FieldBudget = 152;

    /// <summary>A version 11 level Q symbol needs a 276 dmm footprint, so smaller reserves carry text instead.</summary>
    public const int MinimumReserveForCode = 280;

    private const byte WireVersion = 1;
    private const byte DeflateFlag = 1;

    private static readonly string[] NineFieldKeys =
        ["date", "distance", "cartridge", "bullet", "powder", "brass", "primer", "seating", "notes"];

    private static readonly string[] SixFieldKeys = ["date", "distance", "cartridge", "powder", "primer", "notes"];

    private static readonly DateOnly Epoch = new(2000, 1, 1);

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static IReadOnlyList<string> FieldKeys(FieldSet fieldSet) => fieldSet switch
    {
        FieldSet.Standard9 => NineFieldKeys,
        FieldSet.Standard6 => SixFieldKeys,
        _ => throw new ArgumentOutOfRangeException(nameof(fieldSet), fieldSet, "Explicit field sets have no complete byte layout."),
    };

    public static InstanceEncodeResult Encode(Instance instance, FieldSet fieldSet, string definitionId)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var diagnostics = new List<Diagnostic>();
        void Refuse(string code, string path, string message) => diagnostics.Add(Diagnostic.Error(code, path, message));

        if (!DefinitionId.TryParse(definitionId, out byte[] idBytes))
        {
            Refuse("instance.definitionId", "", $"\"{definitionId}\" is not a definition identifier.");
        }

        if (instance.Serial is null || !SerialPattern().IsMatch(instance.Serial))
        {
            Refuse("instance.serial", "/instance/serial", "An instance code needs a four-character Crockford serial.");
        }

        int days = 0;
        if (instance.Printed is null
            || !DateOnly.TryParseExact(instance.Printed, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var printed)
            || (days = printed.DayNumber - Epoch.DayNumber) is < 0 or > 0xFFFFFF)
        {
            Refuse("instance.printed", "/instance/printed", "An instance code needs a print date on or after 2000-01-01.");
        }

        var keys = FieldKeys(fieldSet);
        var values = instance.Values ?? [];
        foreach (var (key, _) in values.Where(v => !keys.Contains(v.Key)))
        {
            Refuse("instance.unknownField", $"/instance/values/{key}",
                $"\"{key}\" is not a field of {GltdNames.FieldSet.NameOf(fieldSet)}.");
        }

        var fields = new List<byte>(FieldBudget);
        foreach (string key in keys)
        {
            byte[] value = StrictUtf8.GetBytes(values.FirstOrDefault(v => v.Key == key).Value ?? "");
            if (value.Length > byte.MaxValue)
            {
                Refuse("instance.fieldTooLong", $"/instance/values/{key}",
                    $"\"{key}\" is {value.Length} bytes of UTF-8; a field carries at most {byte.MaxValue}.");
                break;
            }

            if (fields.Count + 1 + value.Length > FieldBudget)
            {
                Refuse("instance.overBudget", $"/instance/values/{key}",
                    $"\"{key}\" takes the field data past the {FieldBudget}-byte budget of the instance code (section 3.11).");
                break;
            }

            fields.Add((byte)value.Length);
            fields.AddRange(value);
        }

        if (diagnostics.Count > 0)
        {
            return new InstanceEncodeResult(null, diagnostics);
        }

        var frame = new byte[HeaderLength + fields.Count];
        frame[0] = (byte)'G';
        frame[1] = (byte)'I';
        frame[2] = WireVersion;
        frame[3] = 0;
        idBytes.CopyTo(frame, 4);
        Encoding.ASCII.GetBytes(instance.Serial!, frame.AsSpan(14, 4));
        frame[18] = (byte)days;
        frame[19] = (byte)(days >> 8);
        frame[20] = (byte)(days >> 16);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(21), Crc32.Compute([.. fields]));
        fields.CopyTo(frame, HeaderLength);
        return new InstanceEncodeResult(frame, []);
    }

    /// <summary>
    /// Decodes an instance frame. One whose identifier differs from <paramref name="expectedDefinitionId"/> is
    /// reported and not merged: <see cref="InstanceDecodeResult.Instance"/> is null (conformance test 29).
    /// </summary>
    public static InstanceDecodeResult Decode(ReadOnlySpan<byte> frame, FieldSet fieldSet, string expectedDefinitionId)
    {
        if (frame.Length < HeaderLength)
        {
            return Rejected($"Truncated instance frame: {frame.Length} bytes, and the header alone is {HeaderLength}.");
        }

        if (frame[0] == (byte)'G' && frame[1] == (byte)'T')
        {
            return Rejected("This is a GLTD-B definition frame (magic \"GT\"), not a GLTD-I instance frame.");
        }

        if (frame[0] != (byte)'G' || frame[1] != (byte)'I')
        {
            return Rejected($"Bad magic 0x{frame[0]:X2} 0x{frame[1]:X2}; an instance frame starts \"GI\".");
        }

        if (frame[2] != WireVersion)
        {
            return Rejected($"Unknown instance wire version {frame[2]}.");
        }

        if ((frame[3] & ~DeflateFlag) != 0)
        {
            return Rejected($"Reserved instance flag bits are set (0x{frame[3]:X2}).");
        }

        string id = DefinitionId.Format(frame.Slice(4, DefinitionId.ByteLength));
        string serial = Encoding.ASCII.GetString(frame.Slice(14, 4));
        if (!SerialPattern().IsMatch(serial))
        {
            return Rejected("The sheet serial is not four Crockford base-32 characters.");
        }

        int days = frame[18] | (frame[19] << 8) | (frame[20] << 16);
        string printed = Epoch.AddDays(days).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        uint crc = BinaryPrimitives.ReadUInt32LittleEndian(frame[21..]);
        var keys = FieldKeys(fieldSet);

        byte[] fields;
        if ((frame[3] & DeflateFlag) != 0)
        {
            var inflated = Inflate(frame[HeaderLength..], keys.Count * (1 + byte.MaxValue));
            if (inflated is null)
            {
                return Rejected("The compressed instance fields are not valid raw DEFLATE, or inflate beyond what the field set can hold.");
            }

            fields = inflated;
        }
        else
        {
            fields = frame[HeaderLength..].ToArray();
        }

        if (Crc32.Compute(fields) != crc)
        {
            return Rejected("Instance CRC-32 mismatch.");
        }

        var values = new List<KeyValuePair<string, string>>();
        int position = 0;
        foreach (string key in keys)
        {
            if (position >= fields.Length)
            {
                return Rejected($"The instance fields end before \"{key}\".");
            }

            int length = fields[position++];
            if (length > fields.Length - position)
            {
                return Rejected($"Field \"{key}\" declares {length} bytes, but {fields.Length - position} remain.");
            }

            string value;
            try
            {
                value = StrictUtf8.GetString(fields, position, length);
            }
            catch (DecoderFallbackException)
            {
                return Rejected($"Field \"{key}\" is not valid UTF-8.");
            }

            position += length;
            if (value.Length > 0)
            {
                values.Add(new(key, value));
            }
        }

        if (position != fields.Length)
        {
            return Rejected($"{fields.Length - position} bytes follow the last instance field.");
        }

        if (id != expectedDefinitionId)
        {
            return new InstanceDecodeResult(null, id, [Diagnostic.Error("instance.definitionMismatch", "",
                $"The instance code belongs to {id}, not to {expectedDefinitionId}; its values are reported and not merged.", "29")]);
        }

        return new InstanceDecodeResult(new Instance(serial, printed, values), id, []);
    }

    private static byte[]? Inflate(ReadOnlySpan<byte> payload, int limit)
    {
        try
        {
            using var input = new MemoryStream(payload.ToArray());
            using var inflate = new DeflateStream(input, CompressionMode.Decompress);
            var buffer = new byte[limit + 1];
            int read = inflate.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
            return read > limit ? null : buffer[..read];
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static InstanceDecodeResult Rejected(string message) =>
        new(null, null, [Diagnostic.Error("instance.rejected", "", message)]);

    [GeneratedRegex(@"^[0-9A-HJKMNP-TV-Z]{4}\z")]
    private static partial Regex SerialPattern();
}
