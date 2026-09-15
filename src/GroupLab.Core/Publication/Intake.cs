using System.Globalization;
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
/// <item>Triage every file and say why it is or is not usable, and hold any file that is not a camera original,
/// <see cref="CameraOriginal"/> (entry 35 section 1). Only camera originals that triage finds usable are published, unless a person
/// who has looked accepts a file by name.</item>
/// <item>Scrub every published file, <see cref="ImageScrubber"/>, and refuse the whole submission if a scrubbed file still fails
/// <see cref="PublicationCheck"/>.</item>
/// <item>Write the scrubbed files and <c>provenance.json</c>, with the consent text verbatim and the received and published hashes
/// side by side, into a new directory named as the upload page names it, the UTC date and the identifier (entry 34 section 2).</item>
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

        if (DirectoryName(id, submitted) is not { } directoryName)
        {
            return Refuse($"meta.json's submitted_utc \"{submitted}\" is not a UTC time, so the published directory cannot be named by its date");
        }

        string target = Path.Combine(publicRoot, directoryName);
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
            string? notOriginal = CameraOriginal.Problem(original, r.OriginalName, r.StoredName);
            if ((notOriginal is not null || !verdict.Candidate) && !accepted.Contains(r.StoredName))
            {
                IEnumerable<string> why = notOriginal is null ? verdict.Findings : [notOriginal, .. verdict.Findings];
                files.Add(new IntakeFile(r.Index, r.StoredName, r.OriginalName, r.Bytes, r.SniffedType, received, null, [], [], verdict.Findings, "held until a person accepts it: " + string.Join("; ", why)));
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

    /// <summary>
    /// The directory a published submission lives in, named as the upload page names it, NOTES-FROM-PLANNING.md entry 34 section 2:
    /// the UTC date of submission and the identifier, <c>YYYY-MM-DD_&lt;id&gt;</c>, as the first real submission arrived
    /// (<c>2026-09-14_1a8f39ad</c>, submitted at 2026-09-14T20:41:55Z). Null when the time is not a UTC time.
    /// </summary>
    public static string? DirectoryName(string submissionId, string submittedUtc) =>
        submittedUtc.EndsWith('Z') && DateTime.TryParse(submittedUtc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var when)
            ? when.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "_" + submissionId
            : null;

    public const string OwnerProvenanceFormat = "grouplab-provenance-owner-1";

    /// <summary>
    /// Photographs published by their copyright holder directly, NOTES-FROM-PLANNING.md entry 34 section 2: taken before the upload page
    /// existed, so there is no submission, no consent record and none needed, and no submission record is invented for them. Every file
    /// is scrubbed as a donated one is, and the provenance record says who took them and on what terms they are published, with the
    /// original name, both hashes and what scrubbing removed. A file named in <paramref name="held"/>, or one that is not a camera original
    /// (<see cref="CameraOriginal"/>, entry 35 section 1), is recorded with the reason and not published. Stored names replace any character a safe name cannot carry with a hyphen. Nothing is written unless every file passes,
    /// and an existing directory is never overwritten.
    /// </summary>
    public static IntakeResult PublishOwner(string sourceDirectory, string target, string takenBy, string statement, IReadOnlyDictionary<string, string>? held = null)
    {
        string source = Path.GetFileName(Path.TrimEndingDirectorySeparator(sourceDirectory));
        IntakeResult Refuse(string reason) => new(source, reason, [], null);
        held ??= new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(takenBy) || string.IsNullOrWhiteSpace(statement))
        {
            return Refuse("an owner's provenance record must say who took the photographs and on what terms they are published");
        }

        if (!Directory.Exists(sourceDirectory))
        {
            return Refuse($"{sourceDirectory} does not exist");
        }

        if (Directory.Exists(target))
        {
            return Refuse($"{target} already exists, and published data is never overwritten");
        }

        var names = Directory.EnumerateFiles(sourceDirectory)
            .Where(p => PublicationCheck.ImageExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()))
            .Select(p => Path.GetFileName(p))
            .Order(StringComparer.Ordinal)
            .ToList();
        foreach (string name in held.Keys.Where(k => !names.Contains(k)))
        {
            return Refuse($"{name} is to be held, and is not an image in {source}");
        }

        var files = new List<IntakeFile>();
        var published = new List<(string Name, byte[] Bytes)>();
        var stored = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int index = 0;
        foreach (string name in names)
        {
            index++;
            string storedName = Unsafe().Replace(name, "-");
            if (!SafeName().IsMatch(storedName) || !stored.Add(storedName))
            {
                return Refuse($"{name} has no safe stored name distinct from another file's");
            }

            byte[] original = File.ReadAllBytes(Path.Combine(sourceDirectory, name));
            string received = Sha256(original);
            string? reason = held.TryGetValue(name, out string? given) ? given : CameraOriginal.Problem(original, name);
            if (reason is not null)
            {
                files.Add(new IntakeFile(index, storedName, name, original.LongLength, null, received, null, [], [], [], "held: " + reason));
                continue;
            }

            ScrubResult scrubbed;
            try
            {
                scrubbed = ImageScrubber.Scrub(original);
            }
            catch (Exception ex) when (ex is InvalidDataException or NotSupportedException)
            {
                return Refuse($"{name} could not be scrubbed: {ex.Message}");
            }

            if (PublicationCheck.LocationProblems(scrubbed.Bytes) is { Count: > 0 } problems)
            {
                return Refuse($"{name} still carries {string.Join(", ", problems)} after scrubbing");
            }

            files.Add(new IntakeFile(index, storedName, name, original.LongLength, null, received, Sha256(scrubbed.Bytes), scrubbed.Removed, scrubbed.Kept, [], null));
            published.Add((storedName, scrubbed.Bytes));
        }

        if (published.Count == 0)
        {
            return Refuse("nothing would be published");
        }

        Directory.CreateDirectory(target);
        foreach (var (name, bytes) in published)
        {
            File.WriteAllBytes(Path.Combine(target, name), bytes);
        }

        var provenance = new
        {
            format = OwnerProvenanceFormat,
            takenBy,
            statement,
            intake = new { tool = "grouplab publish-owner", at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) },
            files,
        };
        File.WriteAllText(Path.Combine(target, PublicationCheck.ProvenanceFile), JsonSerializer.Serialize(provenance, JsonOptions));
        return new IntakeResult(source, null, files, target);
    }

    public static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    [GeneratedRegex("[^A-Za-z0-9._-]")]
    private static partial Regex Unsafe();

    private static string? Text(JsonObject o, string key) =>
        o[key] is { } node && node.GetValueKind() == JsonValueKind.String && node.GetValue<string>() is { Length: > 0 } s ? s : null;

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,199}$")]
    private static partial Regex SafeName();

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}
