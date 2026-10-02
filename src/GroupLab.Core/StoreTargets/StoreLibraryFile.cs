using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Core.Updates;

namespace GroupLab.Core.StoreTargets;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 344 section 3: the library of store-bought targets as one signed file, so a target added after a build reaches
/// everybody on the application's regular update check without a new build. It is signed the way GroupLab's updates are (ECDSA P-256 with
/// SHA-256, <see cref="UpdateSignature.Algorithm"/>), with the update key by default, and read only after its signature checks: a library
/// anyone could write would let anyone choose the scale other people's groups are measured at.
/// </summary>
public static class StoreLibraryFile
{
    /// <summary>The signed file's format name.</summary>
    public const string Format = "grouplab-target-library-1";

    /// <summary>Why a fetched library was refused, or None.</summary>
    public enum Refusal
    {
        None,
        NoKey,
        NotSigned,
        WrongAlgorithm,
        BadSignature,
        UnknownFormat,

        /// <summary>Entry 352 item 3: far larger than any library, so not read at all.</summary>
        TooLarge,
    }

    /// <summary>
    /// The largest library file read: 64 MB of text, some four hundred products at the shipped fingerprints' size, signed and encoded
    /// twice. A larger one is refused before it is parsed.
    /// </summary>
    public const int MostChars = 64 * 1024 * 1024;

    /// <summary>What a library file carries once its signature checks: its version and its products.</summary>
    public sealed record Contents(int Version, IReadOnlyList<TargetReference> Targets);

    /// <summary>The library's payload: its format, its version and every reference, as the bytes that are signed.</summary>
    public static byte[] Payload(int version, IEnumerable<TargetReference> references)
    {
        ArgumentNullException.ThrowIfNull(references);
        var root = new JsonObject
        {
            ["format"] = Format,
            ["version"] = version,
            ["targets"] = new JsonArray([.. references.Select(r => (JsonNode)r.ToJson())]),
        };
        return Encoding.UTF8.GetBytes(root.ToJsonString());
    }

    /// <summary>The signed file: the payload, base64, with its signature.</summary>
    public static string Sign(byte[] payload, ReadOnlySpan<byte> privateKeyPkcs8)
    {
        ArgumentNullException.ThrowIfNull(payload);
        using var key = ECDsa.Create();
        key.ImportPkcs8PrivateKey(privateKeyPkcs8, out _);
        byte[] signature = key.SignData(payload, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        return new JsonObject
        {
            ["format"] = Format,
            ["algorithm"] = UpdateSignature.Algorithm,
            ["payload"] = Convert.ToBase64String(payload),
            ["signature"] = Convert.ToBase64String(signature),
        }.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// A signed library read: refused unless it is signed by <paramref name="publicKeyBase64"/> (the update key by default), and only then
    /// its payload read.
    /// </summary>
    public static (Refusal Refusal, Contents? Contents) Read(string? json, string? publicKeyBase64 = UpdateKeys.PublicKey)
    {
        if (string.IsNullOrWhiteSpace(publicKeyBase64))
        {
            return (Refusal.NoKey, null);
        }

        // Entry 352 item 3: a file far larger than any library is refused before it is parsed.
        if (json is { Length: > MostChars })
        {
            return (Refusal.TooLarge, null);
        }

        JsonObject? file;
        try
        {
            file = string.IsNullOrWhiteSpace(json) ? null : JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            file = null;
        }

        // Each field only as text: a number or an object where text belongs is not a signed library, and never an error.
        static string? TextOf(JsonNode? node) => node is JsonValue v && v.TryGetValue(out string? s) ? s : null;
        if (file is null || TextOf(file["payload"]) is not { Length: > 0 } payload64 || TextOf(file["signature"]) is not { Length: > 0 } signature64)
        {
            return (Refusal.NotSigned, null);
        }

        if (TextOf(file["algorithm"]) != UpdateSignature.Algorithm)
        {
            return (Refusal.WrongAlgorithm, null);
        }

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(payload64);
            using var key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyBase64), out _);
            if (!key.VerifyData(payload, Convert.FromBase64String(signature64), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence))
            {
                return (Refusal.BadSignature, null);
            }
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            return (Refusal.BadSignature, null);
        }

        // Only now is the payload read.
        try
        {
            var root = JsonNode.Parse(payload) as JsonObject;
            if (root is null || TextOf(root["format"]) != Format || root["targets"] is not JsonArray list)
            {
                return (Refusal.UnknownFormat, null);
            }

            var targets = list.Select(TargetReference.Read).ToList();
            return (Refusal.None, new Contents((int?)root["version"] ?? 0, targets));
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or FormatException or InvalidOperationException or EndOfStreamException or ArgumentException)
        {
            return (Refusal.UnknownFormat, null);
        }
    }
}
