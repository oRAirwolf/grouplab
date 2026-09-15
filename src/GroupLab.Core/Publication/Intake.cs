using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace GroupLab.Core.Publication;

/// <summary>What a person or a tool found about whether one photograph can be used at all, NOTES-FROM-PLANNING.md entry 27 section 1.</summary>
public sealed record TriageVerdict(bool Candidate, IReadOnlyList<string> Findings);

/// <summary>
/// One file of a submission: the names the upload page gave it, both hashes, what scrubbing removed and kept, the triage, and why it
/// was held back if it was. <see cref="OriginalName"/> is the contributor's own file name, recorded and never used as a path.
/// </summary>
public sealed record IntakeFile(
    int Index,
    string StoredName,
    string? OriginalName,
    long Bytes,
    string? SniffedType,
    string ReceivedSha256,
    string? PublishedSha256,
    IReadOnlyList<string> Removed,
    IReadOnlyList<string> Kept,
    IReadOnlyList<string> Triage,
    string? Held);

/// <summary>A submission's outcome: refused with the reason, or published into a directory with its provenance record.</summary>
public sealed record IntakeResult(string Submission, string? Refused, IReadOnlyList<IntakeFile> Files, string? PublishedDirectory);

/// <summary>
/// The single way a donated photograph enters public test data, NOTES-FROM-PLANNING.md entry 22 section 2, reading the upload page's
/// <c>meta.json</c> exactly as entry 28 section 1 records it, with entry 27 section 1's triage. In order:
/// <list type="number">
/// <item>Refuse any <c>schema_version</c> but 1, rather than guess at a format that has changed.</item>
/// <item>Refuse a submission whose <c>exclude_from_public_dataset</c> is true, or missing, which is unknown and not false. It is
/// the page's only opt-out; there is no sentinel file.</item>
/// <item>Refuse a submission without complete provenance: <c>submission_id</c>, <c>submitted_utc</c>, and a <c>consent</c> that was
/// agreed and carries its version, time and text. Empty answers are the normal case and never a reason to refuse.</item>
/// <item>Refuse a submission whose files do not match, one for one, the <c>stored_name</c>, <c>bytes</c> and <c>sha256</c>
/// recorded at upload.</item>
/// <item>Triage every file and say why it is or is not usable. Only candidates are published, unless a person who has looked
/// accepts a file by name.</item>
/// <item>Scrub every published file, <see cref="ImageScrubber"/>, and refuse the whole submission if a scrubbed file still fails
/// <see cref="PublicationCheck"/>.</item>
/// <item>Write the scrubbed files and <c>provenance.json</c>, with the consent text verbatim and the received and published hashes
/// side by side, into a new directory named by the submission identifier.</item>
/// </list>
/// Nothing is written unless every check passes, and an existing directory is never overwritten.
/// </summary>
public static partial class Intake
{
    public const string ProvenanceFormat = "grouplab-provenance-1";

    /// <summary>The only <c>meta.json</c> schema this tool reads.</summary>
    public const int SchemaVersion = 1;

    public static IntakeResult Run(string submissionDirectory, string publicRoot, Func<string, byte[], TriageVerdict> triage, IReadOnlyCollection<string>? accepted = null)
    {
        ArgumentNullException.ThrowIfNull(triage);
        string submission = Path.GetFileName(Path.TrimEndingDirectorySeparator(submissionDirectory));
        IntakeResult Refuse(string reason) => new(submission, reason, [], null);
        accepted ??= [];

        string metaPath = Path.Combine(submissionDirectory, "meta.json");
        if (!File.Exists(metaPath))
        {
            return Refuse("there is no meta.json, so there is no consent record or received hash");
        }

        JsonObject meta;
        try
        {
            meta = JsonNode.Parse(File.ReadAllText(metaPath)) as JsonObject ?? throw new JsonException("not an object");
        }
        catch (JsonException ex)
        {
            return Refuse("meta.json cannot be read: " + ex.Message);
        }

        if (meta["schema_version"] is not { } version || version.GetValueKind() != JsonValueKind.Number || version.GetValue<double>() != SchemaVersion)
        {
            return Refuse($"meta.json's schema_version is {meta["schema_version"]?.ToJsonString() ?? "missing"}, and this tool reads only version {SchemaVersion}");
        }

        switch (meta["exclude_from_public_dataset"]?.GetValueKind())
        {
            case JsonValueKind.True:
                return Refuse("the contributor opted out: exclude_from_public_dataset is true");
            case JsonValueKind.False:
                break;
            default:
                return Refuse("meta.json does not say whether the contributor opted out: exclude_from_public_dataset is missing, which is unknown and not false");
        }

        string? id = Text(meta, "submission_id"), submitted = Text(meta, "submitted_utc");
        var consent = meta["consent"] as JsonObject;
        string? consentVersion = consent is null ? null : Text(consent, "version"), agreedAt = consent is null ? null : Text(consent, "agreed_at_utc"), consentText = consent is null ? null : Text(consent, "text");
        if (id is null || submitted is null)
        {
            return Refuse("meta.json lacks submission_id or submitted_utc, which a published image's provenance must carry");
        }

        if (consent?["agreed"]?.GetValueKind() != JsonValueKind.True)
        {
            return Refuse("meta.json does not record that the contributor agreed to the consent text");
        }

        if (consentVersion is null || agreedAt is null || consentText is null)
        {
            return Refuse("meta.json's consent record lacks its version, its agreed_at_utc or its text, and the text is what the contributor agreed to");
        }

        if (!SafeName().IsMatch(id))
        {
            return Refuse($"the submission identifier \"{id}\" is not a safe directory name");
        }

        var recorded = new List<(int Index, string StoredName, string? OriginalName, long Bytes, string? SniffedType, string Sha256)>();
        foreach (var item in (meta["files"] as JsonArray ?? []).OfType<JsonObject>())
        {
            string? stored = Text(item, "stored_name"), sha = Text(item, "sha256");
            if (stored is null || sha is null || item["bytes"]?.GetValueKind() != JsonValueKind.Number)
            {
                return Refuse("a file in meta.json lacks its stored_name, bytes or sha256");
            }

            if (!SafeName().IsMatch(stored))
            {
                return Refuse($"the stored name \"{stored}\" is not a safe file name");
            }

            int index = item["index"]?.GetValueKind() == JsonValueKind.Number ? item["index"]!.GetValue<int>() : recorded.Count + 1;
            recorded.Add((index, stored, Text(item, "original_name"), item["bytes"]!.GetValue<long>(), Text(item, "sniffed_type"), sha));
        }

        var images = Directory.EnumerateFiles(submissionDirectory)
            .Where(p => PublicationCheck.ImageExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()))
            .Select(p => Path.GetFileName(p))
            .ToHashSet(StringComparer.Ordinal);
        if (recorded.Count == 0)
        {
            return Refuse("meta.json records no files");
        }

        foreach (string name in images.Where(n => recorded.All(r => r.StoredName != n)))
        {
            return Refuse($"{name} is in the submission but not in meta.json, so it cannot be shown to be a file that was consented to");
        }

        foreach (var r in recorded.Where(r => !File.Exists(Path.Combine(submissionDirectory, r.StoredName))))
        {
            return Refuse($"meta.json records {r.StoredName}, which is not in the submission");
        }

        string target = Path.Combine(publicRoot, id);
        if (Directory.Exists(target))
        {
            return Refuse($"{target} already exists, and published data is never overwritten");
        }

        var files = new List<IntakeFile>();
        var published = new List<(string Name, byte[] Bytes)>();
        foreach (var r in recorded.OrderBy(r => r.Index))
        {
            byte[] original = File.ReadAllBytes(Path.Combine(submissionDirectory, r.StoredName));
            if (original.LongLength != r.Bytes)
            {
                return Refuse($"{r.StoredName} is {original.LongLength} bytes, where meta.json recorded {r.Bytes}");
            }

            string received = Sha256(original);
            if (!string.Equals(received, r.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                return Refuse($"{r.StoredName} does not match the SHA-256 recorded at upload: it is {received}, meta.json says {r.Sha256}");
            }

            var verdict = triage(r.StoredName, original);
            if (!verdict.Candidate && !accepted.Contains(r.StoredName))
            {
                files.Add(new IntakeFile(r.Index, r.StoredName, r.OriginalName, r.Bytes, r.SniffedType, received, null, [], [], verdict.Findings, "held until a person accepts it: " + string.Join("; ", verdict.Findings)));
                continue;
            }

            ScrubResult scrubbed;
            try
            {
                scrubbed = ImageScrubber.Scrub(original);
            }
            catch (Exception ex) when (ex is InvalidDataException or NotSupportedException)
            {
                return Refuse($"{r.StoredName} could not be scrubbed: {ex.Message}");
            }

            if (PublicationCheck.LocationProblems(scrubbed.Bytes) is { Count: > 0 } problems)
            {
                return Refuse($"{r.StoredName} still carries {string.Join(", ", problems)} after scrubbing");
            }

            files.Add(new IntakeFile(r.Index, r.StoredName, r.OriginalName, r.Bytes, r.SniffedType, received, Sha256(scrubbed.Bytes), scrubbed.Removed, scrubbed.Kept, verdict.Findings, null));
            published.Add((r.StoredName, scrubbed.Bytes));
        }

        Directory.CreateDirectory(target);
        foreach (var (name, bytes) in published)
        {
            File.WriteAllBytes(Path.Combine(target, name), bytes);
        }

        var provenance = new
        {
            format = ProvenanceFormat,
            schemaVersion = SchemaVersion,
            submissionId = id,
            submittedUtc = submitted,
            excludeFromPublicDataset = false,
            consent = new { agreed = true, version = consentVersion, agreedAtUtc = agreedAt, text = consentText },
            answers = meta["answers"]?.DeepClone(),
            intake = new { tool = "grouplab intake", at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture) },
            files,
        };
        File.WriteAllText(Path.Combine(target, PublicationCheck.ProvenanceFile), JsonSerializer.Serialize(provenance, JsonOptions));
        return new IntakeResult(submission, null, files, target);
    }

    public static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static string? Text(JsonObject o, string key) =>
        o[key] is { } node && node.GetValueKind() == JsonValueKind.String && node.GetValue<string>() is { Length: > 0 } s ? s : null;

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,199}$")]
    private static partial Regex SafeName();

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}
