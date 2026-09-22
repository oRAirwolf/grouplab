using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GroupLab.Core.Updates;

/// <summary>
/// The manifest as published, NOTES-FROM-PLANNING.md entry 139: the signature covers the exact bytes that were written, and those bytes are
/// what is verified, byte for byte, before anything reads them.
/// <para>
/// <b>What this exists to prevent.</b> The first format signs a record and verifies by serialising the record it was read into. A build that
/// does not know a field drops it on the way back out, so the bytes it checks are not the bytes that were signed, and the manifest is refused
/// as <c>BadSignature</c>. Entry 138 section 5 added one field, nightly 42 published with it, and nightly 37 answered
/// <c>update.check result=Refused refusal=BadSignature</c>: every build already installed was unable to update itself at all. That makes the
/// first format frozen for ever, and one careless change bricks the updater for everybody who has GroupLab installed.
/// </para>
/// <para>
/// <b>So the payload travels as bytes.</b> The file carries the signed JSON base64 encoded, and verification decodes it and checks the
/// signature over exactly what arrived. Nothing is ever re-serialised, so a field this build does not know is simply a field it ignores, and
/// the manifest can grow for ever without stranding anybody. The cost is that the file is not readable at a glance; that is what
/// <see cref="Body"/> is for, and the old file stays beside it while any build that reads only it may still be installed.
/// </para>
/// </summary>
/// <param name="Algorithm">The signature algorithm, named so a change of algorithm is something a reader can tell apart.</param>
/// <param name="Payload">The exact JSON bytes that were signed, base64.</param>
/// <param name="Signature">The signature over those bytes, base64.</param>
public sealed record PublishedManifest(
    [property: JsonPropertyName("algorithm")] string Algorithm,
    [property: JsonPropertyName("payload")] string Payload,
    [property: JsonPropertyName("signature")] string Signature)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <summary>The bytes the signature covers, or null where the payload is not base64. They are never regenerated from anything.</summary>
    public byte[]? Bytes
    {
        get
        {
            try
            {
                return Convert.FromBase64String(Payload);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// What the payload says, read after the signature has been checked, or null where those bytes are not a manifest this build understands.
    /// A field this build does not know is ignored rather than refused, which is the whole point of the format.
    /// </summary>
    public UpdateManifest? Body => Bytes is { } bytes ? UpdateManifest.Read(Encoding.UTF8.GetString(bytes)) : null;

    public string ToJson() => JsonSerializer.Serialize(this, Options);

    public static PublishedManifest? Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var published = JsonSerializer.Deserialize<PublishedManifest>(json, Options);
            return published is { Payload.Length: > 0, Signature.Length: > 0 } ? published : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
