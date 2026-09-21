using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GroupLab.Core.Updates;

/// <summary>One file a build offers for one platform, with what it is and how to know it arrived whole.</summary>
public sealed record UpdateAsset(string Platform, string Kind, string Name, long Bytes, string Sha256, string Url);

/// <summary>
/// What a train's newest build is, NOTES-FROM-PLANNING.md entry 119 section 3. One small JSON file at a fixed address per train, so a check
/// costs one HTTPS request and never touches an API with a rate limit.
/// <para>
/// It is signed, and the application refuses a manifest whose signature does not verify and a download whose hash does not match, in both
/// cases saying so in plain words rather than failing quietly. <see cref="UpdateSignature"/> is where that is done.
/// </para>
/// </summary>
public sealed record UpdateManifest(
    [property: JsonPropertyName("manifest")] int Manifest,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("train")] string Train,
    [property: JsonPropertyName("commit")] string Commit,
    [property: JsonPropertyName("publishedUtc")] string PublishedUtc,
    [property: JsonPropertyName("notes")] string Notes,
    [property: JsonPropertyName("assets")] IReadOnlyList<UpdateAsset> Assets)
{
    /// <summary>The only manifest version this application reads. A newer one is refused by name rather than half understood.</summary>
    public const int Current = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// How the signed bytes are written, and why they are not the same options the file is written with.
    /// <para>
    /// <b>The signed bytes must not depend on the machine that produced them.</b> Indented JSON does: from .NET 9 the writer's newline
    /// defaults to <see cref="Environment.NewLine"/>, so the same manifest serialises with a carriage return on Windows and without one on
    /// Linux. The nightly is signed on a Linux runner and verified on a Windows machine, so every published manifest was refused as
    /// BadSignature, and no test caught it because a test signs and verifies on one machine.
    /// </para>
    /// <para>
    /// So the signature is over compact JSON, which has no line breaks to differ over. Indentation is for a person reading the file; it has
    /// no business deciding whether an update installs.
    /// </para>
    /// </summary>
    private static readonly JsonSerializerOptions Signing = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>The version this manifest offers, or null where it does not say a version this application understands.</summary>
    public SemanticVersion? Offered => SemanticVersion.Parse(Version);

    /// <summary>The train this manifest belongs to, read from its own word rather than inferred.</summary>
    public UpdateTrain OnTrain => Train?.Trim().ToLowerInvariant() switch
    {
        "nightly" => UpdateTrain.Nightly,
        "beta" => UpdateTrain.Beta,
        "release" => UpdateTrain.Release,
        _ => UpdateTrain.Development,
    };

    /// <summary>The asset a platform installs, or null where this build has none for it.</summary>
    public UpdateAsset? For(string platform, string kind) =>
        Assets?.FirstOrDefault(a => string.Equals(a.Platform, platform, StringComparison.OrdinalIgnoreCase) && string.Equals(a.Kind, kind, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The bytes that are signed: the manifest as compact JSON with no signature in it, so signing and verifying cannot disagree about what
    /// was signed, whatever machine each of them runs on. See <see cref="Signing"/> for why compact rather than indented.
    /// </summary>
    public byte[] Signable() => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this, Signing));

    public string ToJson() => JsonSerializer.Serialize(this, Options);

    /// <summary>Reads a manifest, or null where the text is not one this application understands.</summary>
    public static UpdateManifest? Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, Options);
            return manifest is { Manifest: Current } && manifest.Offered is not null ? manifest : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>One line naming the build, for the log and for a report.</summary>
    public string Describe() => string.Create(CultureInfo.InvariantCulture, $"{Version} on the {Train} train, commit {Commit}, published {PublishedUtc}");
}

/// <summary>A manifest and the signature over it, which is what a release actually carries.</summary>
public sealed record SignedManifest(
    [property: JsonPropertyName("algorithm")] string Algorithm,
    [property: JsonPropertyName("signature")] string Signature,
    [property: JsonPropertyName("manifest")] UpdateManifest Payload)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public string ToJson() => JsonSerializer.Serialize(this, Options);

    public static SignedManifest? Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var signed = JsonSerializer.Deserialize<SignedManifest>(json, Options);
            return signed is { Payload: not null, Signature.Length: > 0 } ? signed : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
