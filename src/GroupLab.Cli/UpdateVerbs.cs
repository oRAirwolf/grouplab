using System.Globalization;
using System.Text.Json;
using GroupLab.Core.Updates;

namespace GroupLab.Cli;

/// <summary>
/// The two commands the update train needs, NOTES-FROM-PLANNING.md entry 119 section 3: one to make the key pair, which Alan runs once, and
/// one the nightly workflow runs to write and sign a build's manifest.
/// <para>
/// Nothing here reaches the network, and the signing command refuses to write an unsigned manifest: a missing key fails the build rather
/// than publishing something the application would have to trust blindly.
/// </para>
/// </summary>
public static class UpdateVerbs
{
    public const string KeyUsage = "grouplab update-key";

    public const string ManifestUsage =
        "grouplab update-manifest --version <v> --train <name> --commit <sha> --notes <file> --out <manifest.json> [--versions <file>] [--asset <platform> <kind> <file> <url>]... [--own <platform> <kind> <file> <url>]... [--carry <update-manifest-2.json>]";

    /// <summary>Makes a key pair and says exactly what to do with each half. It prints; it writes nothing and sends nothing.</summary>
    public static int Key(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var (privateKey, publicKey) = UpdateSignature.NewKeyPair();
        output.WriteLine("A new update signing key, " + UpdateSignature.Algorithm + ".");
        output.WriteLine();
        output.WriteLine("1. The private half. Put it in the repository secret named " + UpdateKeys.SecretName + " and nowhere else:");
        output.WriteLine();
        output.WriteLine("   gh secret set " + UpdateKeys.SecretName);
        output.WriteLine("   (paste the line below when it asks, then press Ctrl+Z and Enter on Windows, or Ctrl+D elsewhere)");
        output.WriteLine();
        output.WriteLine(privateKey);
        output.WriteLine();
        output.WriteLine("2. The public half. Paste it into UpdateKeys.PublicKey in src/GroupLab.Core/Updates/UpdateKeys.cs and commit it:");
        output.WriteLine();
        output.WriteLine(publicKey);
        output.WriteLine();
        output.WriteLine("The private half never goes in the repository, in a file, or in a chat. If it leaks, make a new pair and rotate:");
        output.WriteLine("docs/UPDATES.md says how.");
        return 0;
    }

    /// <summary>Writes a build's manifest and signs it with the key in an environment variable, which is how the workflow holds the secret.</summary>
    public static int Manifest(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        string? version = null, train = null, commit = null, notesFile = null, into = null, versionsFile = null, carryFile = null;
        var assets = new List<UpdateAsset>();

        // Entry 454: this build's files on its own numbered release, for each platform's newest build, and the manifest before this one,
        // whose entries are carried for a platform this build has no file for.
        var own = new List<UpdateAsset>();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--version" when i + 1 < args.Length:
                    version = args[++i];
                    break;
                case "--train" when i + 1 < args.Length:
                    train = args[++i];
                    break;
                case "--commit" when i + 1 < args.Length:
                    commit = args[++i];
                    break;
                case "--notes" when i + 1 < args.Length:
                    notesFile = args[++i];
                    break;
                case "--out" when i + 1 < args.Length:
                    into = args[++i];
                    break;
                case "--versions" when i + 1 < args.Length:
                    versionsFile = args[++i];
                    break;
                case "--carry" when i + 1 < args.Length:
                    carryFile = args[++i];
                    break;
                case "--asset" or "--own" when i + 4 < args.Length:
                    string platform = args[i + 1], kind = args[i + 2], file = args[i + 3], url = args[i + 4];
                    var list = args[i] == "--own" ? own : assets;
                    i += 4;
                    if (!File.Exists(file))
                    {
                        error.WriteLine($"update-manifest: there is no {file}");
                        return 1;
                    }

                    byte[] bytes = File.ReadAllBytes(file);
                    list.Add(new UpdateAsset(platform, kind, Path.GetFileName(file), bytes.LongLength, UpdateSignature.Sha256(bytes), url));
                    break;
                default:
                    error.WriteLine($"update-manifest: unknown or incomplete option {args[i]}");
                    error.WriteLine(ManifestUsage);
                    return 2;
            }
        }

        if (version is null || train is null || commit is null || into is null)
        {
            error.WriteLine(ManifestUsage);
            return 2;
        }

        if (assets.Count == 0)
        {
            error.WriteLine("update-manifest: a manifest with no assets offers nothing, so it is refused.");
            return 1;
        }

        if (SemanticVersion.Parse(version) is null)
        {
            error.WriteLine($"update-manifest: {version} is not a version this application can order, so nothing was written.");
            return 1;
        }

        string notes = notesFile is not null && File.Exists(notesFile) ? File.ReadAllText(notesFile) : "";

        // Entry 139 section 3: each recent build's own notes, so somebody who skipped five builds sees all five. They travel only in the
        // second format, where a field a build does not know is a field it ignores.
        IReadOnlyList<VersionNotes>? versions = null;
        if (versionsFile is not null)
        {
            if (!File.Exists(versionsFile))
            {
                error.WriteLine($"update-manifest: there is no {versionsFile}");
                return 1;
            }

            try
            {
                versions = System.Text.Json.JsonSerializer.Deserialize<List<VersionNotes>>(File.ReadAllText(versionsFile), VersionsJson);
            }
            catch (System.Text.Json.JsonException ex)
            {
                error.WriteLine($"update-manifest: {versionsFile} is not a list of versions and their notes: {ex.Message}");
                return 1;
            }
        }

        // Entry 119 section 3.2: no key, no manifest. A build that published an unsigned one would be asking the application to trust it.
        string? key = Environment.GetEnvironmentVariable(UpdateKeys.SecretName);
        if (string.IsNullOrWhiteSpace(key))
        {
            error.WriteLine($"update-manifest: the signing key is not here. Set the repository secret {UpdateKeys.SecretName} and pass it to");
            error.WriteLine("this step as an environment variable of the same name. Nothing was written, and nothing must be published:");
            error.WriteLine("an unsigned manifest is one the application would have to trust without being able to check it.");
            error.WriteLine("Run `grouplab update-key` once to make the pair, and see docs/UPDATES.md.");
            return 1;
        }

        string now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        var platforms = own.Count > 0 || carryFile is not null ? Platforms(version, commit, now, own, carryFile, key, error) : [];
        var manifest = new UpdateManifest(
            UpdateManifest.Current, version, train, commit, now,
            notes, assets, versions is { Count: > 0 } ? versions : null, platforms.Count > 0 ? platforms : null);

        SignedManifest signed;
        PublishedManifest published;
        try
        {
            signed = UpdateSignature.Sign(manifest, Convert.FromBase64String(key.Trim()));
            published = UpdateSignature.Publish(manifest, Convert.FromBase64String(key.Trim()));
        }
        catch (Exception ex) when (ex is FormatException or System.Security.Cryptography.CryptographicException)
        {
            error.WriteLine($"update-manifest: the value of {UpdateKeys.SecretName} is not a private key this tool can use: {ex.Message}");
            return 1;
        }

        // What was just signed is verified before it is written, with the public half taken from the private one, so a manifest that cannot
        // be checked never reaches a release.
        using (var check = System.Security.Cryptography.ECDsa.Create())
        {
            check.ImportPkcs8PrivateKey(Convert.FromBase64String(key.Trim()), out _);
            string publicKey = Convert.ToBase64String(check.ExportSubjectPublicKeyInfo());
            foreach (var (which, refusal) in new[]
            {
                ("first", UpdateSignature.Verify(signed, publicKey)),
                ("second", UpdateSignature.Verify(published, publicKey)),
            })
            {
                if (refusal != UpdateSignature.Refusal.None)
                {
                    error.WriteLine($"update-manifest: the {which} format this tool just signed does not verify ({refusal}). Nothing was written.");
                    return 1;
                }
            }

            // Entry 139 section 2: the first format must carry nothing it did not carry before, or every installed build stops updating.
            if (signed.Payload.Versions is not null || signed.Payload.Platforms is not null)
            {
                error.WriteLine("update-manifest: the first format was given a field it cannot carry. Nothing was written.");
                return 1;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(into))!);
        File.WriteAllText(into, signed.ToJson());
        string second = SecondFormatPath(into);
        File.WriteAllText(second, published.ToJson());
        output.WriteLine($"Wrote {into}: {manifest.Describe()}, {assets.Count} assets, signed with {UpdateSignature.Algorithm}.");
        output.WriteLine($"Wrote {second}: the same build, signed over the bytes as written, with {versions?.Count ?? 0} versions of notes.");
        foreach (var asset in assets)
        {
            output.WriteLine($"  {asset.Platform} {asset.Kind}: {asset.Name}, {asset.Bytes / 1024 / 1024} MB, sha256 {asset.Sha256}");
        }

        foreach (var build in platforms)
        {
            output.WriteLine($"  newest for {build.Platform}: {build.Version}{(build.Version == version ? "" : ", carried from the manifest before")}, {build.Assets.Count} files");
        }

        return 0;
    }

    /// <summary>
    /// Each platform's newest build, entry 454: this build where it has a file for the platform, and otherwise the entry the manifest before
    /// it named, carried as it was so long as it is an older build. A manifest to carry from that does not verify with the key's own public
    /// half carries nothing, and says so: a forged entry must never be signed into a new manifest.
    /// </summary>
    public static IReadOnlyList<PlatformBuild> Platforms(string version, string commit, string publishedUtc, IReadOnlyList<UpdateAsset> own,
        string? carryFile, string privateKeyBase64, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(own);
        ArgumentNullException.ThrowIfNull(privateKeyBase64);
        ArgumentNullException.ThrowIfNull(error);
        var builds = own.GroupBy(a => a.Platform, StringComparer.OrdinalIgnoreCase)
            .Select(g => new PlatformBuild(g.Key, version, commit, publishedUtc, [.. g]))
            .ToList();
        if (carryFile is null || !File.Exists(carryFile))
        {
            return builds;
        }

        string publicKey;
        using (var keys = System.Security.Cryptography.ECDsa.Create())
        {
            keys.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKeyBase64.Trim()), out _);
            publicKey = Convert.ToBase64String(keys.ExportSubjectPublicKeyInfo());
        }

        var before = PublishedManifest.Read(File.ReadAllText(carryFile));
        if (UpdateSignature.Verify(before, publicKey) != UpdateSignature.Refusal.None || before!.Body is not { } previous)
        {
            error.WriteLine($"update-manifest: {carryFile} does not verify, so no platform's earlier build is carried from it.");
            return builds;
        }

        var ours = SemanticVersion.Parse(version);
        foreach (var earlier in previous.Platforms ?? [])
        {
            if (builds.Any(b => string.Equals(b.Platform, earlier.Platform, StringComparison.OrdinalIgnoreCase))
                || ours is null || SemanticVersion.Parse(earlier.Version) is not { } theirs || !UpdateOrder.IsNewer(theirs, ours))
            {
                continue;
            }

            builds.Add(earlier);
        }

        return builds;
    }

    /// <summary>Where the second format goes, beside the first: entry 139 section 1's <c>update-manifest-2.json</c>.</summary>
    public static string SecondFormatPath(string first)
    {
        ArgumentNullException.ThrowIfNull(first);
        string directory = Path.GetDirectoryName(first) ?? "";
        string name = Path.GetFileNameWithoutExtension(first) + "-2" + Path.GetExtension(first);
        return Path.Combine(directory, name);
    }

    private static readonly System.Text.Json.JsonSerializerOptions VersionsJson = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Reads a signed manifest and says whether it verifies against a public key, for the proof step and for a person checking by hand.</summary>
    /// <param name="asInstalled">Entry 454: a platform and an installed version, to say what that copy would be offered, for the proof and for
    /// a person asking why a phone did or did not update.</param>
    public static int Check(string path, string? publicKey, TextWriter output, TextWriter error, (string Platform, string Version)? asInstalled = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        if (!File.Exists(path))
        {
            error.WriteLine($"update-check: there is no {path}");
            return 1;
        }

        string text = File.ReadAllText(path);
        string key = publicKey ?? UpdateKeys.PublicKey;

        // Either format, told apart by what is in the file rather than by its name, so a person can check whichever one they have.
        UpdateManifest? manifest;
        UpdateSignature.Refusal refusal;
        int format;
        if (PublishedManifest.Read(text) is { } published)
        {
            (manifest, refusal, format) = (published.Body, UpdateSignature.Verify(published, key), 2);
        }
        else if (SignedManifest.Read(text) is { } signed)
        {
            (manifest, refusal, format) = (signed.Payload, UpdateSignature.Verify(signed, key), 1);
        }
        else
        {
            error.WriteLine($"update-check: {path} is not a signed manifest this build understands.");
            return 1;
        }

        output.WriteLine((manifest?.Describe() ?? "a manifest this build cannot read") + $", in format {format}");
        output.WriteLine(refusal == UpdateSignature.Refusal.None
            ? "The signature verifies against the key given."
            : "Refused: " + refusal + ". " + refusal.Words());
        foreach (var asset in manifest?.Assets ?? [])
        {
            output.WriteLine($"  {asset.Platform} {asset.Kind}: {asset.Name}, sha256 {asset.Sha256}");
        }

        if (manifest?.Versions is { Count: > 0 } versions)
        {
            output.WriteLine($"  and the notes of {versions.Count} builds, newest first: {string.Join(", ", versions.Select(v => v.Version))}");
        }

        foreach (var build in manifest?.Platforms ?? [])
        {
            output.WriteLine($"  newest for {build.Platform}: {build.Version}, commit {build.Commit}, {string.Join(", ", build.Assets.Select(a => a.Url))}");
        }

        if (asInstalled is { } copy && manifest is not null && refusal == UpdateSignature.Refusal.None)
        {
            var installed = BuildIdentity.Read(copy.Version, manifest.Train);
            var decision = format == 2
                ? UpdatePolicy.Decide(installed, UpdatePreferences.Default(installed.Train), PublishedManifest.Read(text), key, copy.Platform)
                : UpdatePolicy.Decide(installed, UpdatePreferences.Default(installed.Train), SignedManifest.Read(text), key);
            var seen = format == 2 ? manifest.OnPlatform(copy.Platform) : manifest;
            output.WriteLine(decision.Offer
                ? $"The {copy.Platform} copy on {installed.Version.Number} is offered {decision.Version!.Number}: {string.Join(", ", seen.Assets.Where(a => string.Equals(a.Platform, copy.Platform, StringComparison.OrdinalIgnoreCase)).Select(a => a.Kind + " " + a.Url))}"
                : $"The {copy.Platform} copy on {installed.Version.Number} is offered nothing: {decision.Reason}");
        }

        return refusal == UpdateSignature.Refusal.None ? 0 : 1;
    }
}
