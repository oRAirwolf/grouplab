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
        "grouplab update-manifest --version <v> --train <name> --commit <sha> --notes <file> --out <manifest.json> [--asset <platform> <kind> <file> <url>]...";

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
        string? version = null, train = null, commit = null, notesFile = null, into = null;
        var assets = new List<UpdateAsset>();
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
                case "--asset" when i + 4 < args.Length:
                    string platform = args[i + 1], kind = args[i + 2], file = args[i + 3], url = args[i + 4];
                    i += 4;
                    if (!File.Exists(file))
                    {
                        error.WriteLine($"update-manifest: there is no {file}");
                        return 1;
                    }

                    byte[] bytes = File.ReadAllBytes(file);
                    assets.Add(new UpdateAsset(platform, kind, Path.GetFileName(file), bytes.LongLength, UpdateSignature.Sha256(bytes), url));
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
        var manifest = new UpdateManifest(
            UpdateManifest.Current, version, train, commit,
            DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            notes, assets);

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

        SignedManifest signed;
        try
        {
            signed = UpdateSignature.Sign(manifest, Convert.FromBase64String(key.Trim()));
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
            var refusal = UpdateSignature.Verify(signed, Convert.ToBase64String(check.ExportSubjectPublicKeyInfo()));
            if (refusal != UpdateSignature.Refusal.None)
            {
                error.WriteLine($"update-manifest: the manifest this tool just signed does not verify ({refusal}). Nothing was written.");
                return 1;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(into))!);
        File.WriteAllText(into, signed.ToJson());
        output.WriteLine($"Wrote {into}: {manifest.Describe()}, {assets.Count} assets, signed with {UpdateSignature.Algorithm}.");
        foreach (var asset in assets)
        {
            output.WriteLine($"  {asset.Platform} {asset.Kind}: {asset.Name}, {asset.Bytes / 1024 / 1024} MB, sha256 {asset.Sha256}");
        }

        return 0;
    }

    /// <summary>Reads a signed manifest and says whether it verifies against a public key, for the proof step and for a person checking by hand.</summary>
    public static int Check(string path, string? publicKey, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        if (!File.Exists(path))
        {
            error.WriteLine($"update-check: there is no {path}");
            return 1;
        }

        var signed = SignedManifest.Read(File.ReadAllText(path));
        if (signed is null)
        {
            error.WriteLine($"update-check: {path} is not a signed manifest this build understands.");
            return 1;
        }

        string key = publicKey ?? UpdateKeys.PublicKey;
        var refusal = UpdateSignature.Verify(signed, key);
        output.WriteLine(signed.Payload.Describe());
        output.WriteLine(refusal == UpdateSignature.Refusal.None
            ? "The signature verifies against the key given."
            : "Refused: " + refusal + ". " + refusal.Words());
        foreach (var asset in signed.Payload.Assets)
        {
            output.WriteLine($"  {asset.Platform} {asset.Kind}: {asset.Name}, sha256 {asset.Sha256}");
        }

        return refusal == UpdateSignature.Refusal.None ? 0 : 1;
    }
}
