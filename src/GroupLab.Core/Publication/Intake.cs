using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace GroupLab.Core.Publication;

/// <summary>What a person or a tool found about whether one photograph can be used at all, NOTES-FROM-PLANNING.md entry 27 section 1.</summary>
public sealed record TriageVerdict(bool Candidate, IReadOnlyList<string> Findings);

/// <summary>One file of a submission: both hashes, what scrubbing removed and kept, the triage, and why it was held back if it was.</summary>
public sealed record IntakeFile(string Name, string ReceivedSha256, string? PublishedSha256, IReadOnlyList<string> Removed, IReadOnlyList<string> Kept, IReadOnlyList<string> Triage, string? Held);

/// <summary>A submission's outcome: refused with the reason, or published into a directory with its provenance record.</summary>
public sealed record IntakeResult(string Submission, string? Refused, IReadOnlyList<IntakeFile> Files, string? PublishedDirectory);

/// <summary>
/// The single way a donated photograph enters public test data, NOTES-FROM-PLANNING.md entry 22 section 2, with entry 27 section 1's
/// triage. A submission is a directory of original files and the <c>meta.json</c> the upload page wrote. In order:
/// <list type="number">
/// <item>Refuse a directory holding a <c>DO-NOT-PUBLISH</c> file, and one whose <c>meta.json</c> says so, the file first because a
/// file is harder to miss.</item>
/// <item>Refuse a submission whose provenance is incomplete (its identifier, consent text version or submission time), or whose files
/// do not match, one for one, the SHA-256 recorded at upload.</item>
/// <item>Triage every file and say why it is or is not usable. Only candidates are published, unless a person who has looked
/// accepts a file by name: entry 27 found that an open request mostly produces photographs nothing can measure.</item>
/// <item>Scrub every published file, <see cref="ImageScrubber"/>, and refuse the whole submission if a scrubbed file still fails
/// <see cref="PublicationCheck"/>.</item>
/// <item>Write the scrubbed files and <c>provenance.json</c>, carrying the received and published hashes side by side, the consent
/// version, the submission time and the answers, into a new directory named by the submission identifier.</item>
/// </list>
/// Nothing is written unless every check passes, and an existing directory is never overwritten.
/// </summary>
public static partial class Intake
{
    public const string ProvenanceFormat = "grouplab-provenance-1";

    public static IntakeResult Run(string submissionDirectory, string publicRoot, Func<string, byte[], TriageVerdict> triage, IReadOnlyCollection<string>? accepted = null)
    {
        ArgumentNullException.ThrowIfNull(triage);
        string submission = Path.GetFileName(Path.TrimEndingDirectorySeparator(submissionDirectory));
        IntakeResult Refuse(string reason) => new(submission, reason, [], null);
        accepted ??= [];

        if (File.Exists(Path.Combine(submissionDirectory, PublicationCheck.DoNotPublish)))
        {
            return Refuse($"the contributor opted out: {PublicationCheck.DoNotPublish} is present");
        }

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

        if (Flag(meta, "doNotPublish") || Flag(meta, "optOut"))
        {
            return Refuse("meta.json records that the contributor opted out of publication");
        }

        string? id = Text(meta, "submissionId"), consent = Text(meta, "consentVersion"), submitted = Text(meta, "submittedAt");
        if (id is null || consent is null || submitted is null)
        {
            return Refuse("meta.json lacks the provenance a published image must carry: submissionId, consentVersion and submittedAt are all required");
        }

        if (!SafeIdentifier().IsMatch(id))
        {
            return Refuse($"the submission identifier \"{id}\" is not a safe directory name");
        }

        var recorded = RecordedHashes(meta);
        var images = Directory.EnumerateFiles(submissionDirectory)
            .Where(p => PublicationCheck.ImageExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()))
            .Select(p => Path.GetFileName(p))
            .Order(StringComparer.Ordinal)
            .ToList();
        if (images.Count == 0)
        {
            return Refuse("the submission holds no JPEG or PNG files");
        }

        foreach (string name in images.Where(n => !recorded.ContainsKey(n!)))
        {
            return Refuse($"{name} has no hash in meta.json, so it cannot be shown to be the file that was consented to");
        }

        foreach (string name in recorded.Keys.Where(n => !images.Contains(n)))
        {
            return Refuse($"meta.json records {name}, which is not in the submission");
        }

        string target = Path.Combine(publicRoot, id);
        if (Directory.Exists(target))
        {
            return Refuse($"{target} already exists, and published data is never overwritten");
        }

        var files = new List<IntakeFile>();
        var published = new List<(string Name, byte[] Bytes)>();
        foreach (string name in images!)
        {
            byte[] original = File.ReadAllBytes(Path.Combine(submissionDirectory, name));
            string received = Sha256(original);
            if (!string.Equals(received, recorded[name], StringComparison.OrdinalIgnoreCase))
            {
                return Refuse($"{name} does not match the SHA-256 recorded at upload: it is {received}, meta.json says {recorded[name]}");
            }

            var verdict = triage(name, original);
            bool publish = verdict.Candidate || accepted.Contains(name);
            if (!publish)
            {
                files.Add(new IntakeFile(name, received, null, [], [], verdict.Findings, "held until a person accepts it: " + string.Join("; ", verdict.Findings)));
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

            files.Add(new IntakeFile(name, received, Sha256(scrubbed.Bytes), scrubbed.Removed, scrubbed.Kept, verdict.Findings, null));
            published.Add((name, scrubbed.Bytes));
        }

        Directory.CreateDirectory(target);
        foreach (var (name, bytes) in published)
        {
            File.WriteAllBytes(Path.Combine(target, name), bytes);
        }

        var provenance = new
        {
            format = ProvenanceFormat,
            submissionId = id,
            consentVersion = consent,
            submittedAt = submitted,
            answers = meta["answers"]?.DeepClone(),
            intake = new { tool = "grouplab intake", at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture) },
            files = files.Select(f => new { f.Name, f.ReceivedSha256, f.PublishedSha256, f.Removed, f.Kept, f.Triage, f.Held }),
        };
        File.WriteAllText(Path.Combine(target, PublicationCheck.ProvenanceFile), JsonSerializer.Serialize(provenance, JsonOptions));
        return new IntakeResult(submission, null, files, target);
    }

    public static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>The received hashes, from <c>files</c> as an object of name to hash or as an array of objects with a name and a sha256.</summary>
    private static Dictionary<string, string> RecordedHashes(JsonObject meta)
    {
        var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
        switch (meta["files"])
        {
            case JsonObject map:
                foreach (var (name, value) in map)
                {
                    if (value?.GetValueKind() == JsonValueKind.String)
                    {
                        hashes[name] = value.GetValue<string>();
                    }
                }

                break;
            case JsonArray list:
                foreach (var item in list.OfType<JsonObject>())
                {
                    if (Text(item, "name") is { } name && Text(item, "sha256") is { } hash)
                    {
                        hashes[name] = hash;
                    }
                }

                break;
        }

        return hashes;
    }

    private static string? Text(JsonObject o, string key) =>
        o[key] is { } node && node.GetValueKind() == JsonValueKind.String && node.GetValue<string>() is { Length: > 0 } s ? s : null;

    private static bool Flag(JsonObject o, string key) => o[key] is { } node && node.GetValueKind() == JsonValueKind.True;

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9_-]{0,99}$")]
    private static partial Regex SafeIdentifier();

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}
