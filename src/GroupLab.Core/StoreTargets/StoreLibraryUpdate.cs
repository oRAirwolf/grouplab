using System.Text;
using GroupLab.Core.Updates;

namespace GroupLab.Core.StoreTargets;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 347, planning's answer to question 80: a newer library of store-bought fingerprints reaches people the way
/// updates do. The nightly signs one library file, attaches it to its release and lists it in the signed update manifest with its SHA-256;
/// grouplab.org keeps a copy. The application reads the manifest on its regular check, refuses a manifest or a library whose signature
/// does not check, keeps the built-in library as the floor, and installs a library only when it is newer than the one it shipped with. The
/// Microsoft Store and TestFlight copies, which do not update themselves, use the same route, since it changes data and not code.
/// </summary>
public static class StoreLibraryUpdate
{
    /// <summary>The library file's name, on every release and on grouplab.org.</summary>
    public const string FileName = "grouplab-target-library.json";

    /// <summary>The library's platform and kind in the update manifest; every other platform's lookup passes it over.</summary>
    public const string Platform = "library";

    /// <summary>The library's kind in the update manifest.</summary>
    public const string Kind = "targets";

    /// <summary>The copy on grouplab.org, read when GitHub cannot be reached; its own signature is all that is trusted there.</summary>
    public const string Mirror = "https://grouplab.org/library/" + FileName;

    /// <summary>Where a fetched library is kept, so the next start has it without fetching.</summary>
    public static string SavedPath(string folder) => Path.Combine(folder, FileName);

    /// <summary>
    /// At start: the library kept from an earlier fetch, installed when its signature checks and it is newer than the built-in one. Says
    /// what it did, for the log.
    /// </summary>
    public static string LoadSaved(string folder, string? publicKey = UpdateKeys.PublicKey)
    {
        string path = SavedPath(folder);
        if (!File.Exists(path))
        {
            return "no fetched library kept";
        }

        var (refusal, contents) = StoreLibraryFile.Read(File.ReadAllText(path), publicKey);
        if (contents is null)
        {
            return "the kept library was refused: " + refusal;
        }

        if (contents.Version <= StoreTargetLibrary.BuiltInVersion)
        {
            return $"the kept library, version {contents.Version}, is no newer than the built-in {StoreTargetLibrary.BuiltInVersion}";
        }

        StoreTargetLibrary.Install(contents.Targets);
        return $"the kept library, version {contents.Version}, {contents.Targets.Count} products";
    }

    /// <summary>
    /// On the regular check: the manifest of <paramref name="train"/>, its library asset, the file itself checked against the manifest's
    /// SHA-256 and its own signature, kept and installed when newer. Nothing is downloaded when the kept file already has the manifest's
    /// SHA-256. Says what it did, for the log; never throws for a network or a refusal.
    /// </summary>
    public static async Task<string> CheckAsync(IOutsideWorld outside, string folder, UpdateTrain train = UpdateTrain.Nightly, string? publicKey = UpdateKeys.PublicKey,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(outside);
        string? json = null;
        UpdateAsset? asset = null;
        if (train.PublishedAddress() is { } address && await outside.GetTextAsync(address, token).ConfigureAwait(false) is { } sealedJson)
        {
            var published = PublishedManifest.Read(sealedJson);
            if (UpdateSignature.Verify(published, publicKey) != UpdateSignature.Refusal.None)
            {
                return "the update manifest's signature did not check, so no library was read";
            }

            asset = published!.Body?.For(Platform, Kind);
            if (asset is null)
            {
                return "the newest build lists no library";
            }

            string path = SavedPath(folder);
            if (File.Exists(path) && UpdateSignature.Sha256(File.ReadAllBytes(path)) == asset.Sha256)
            {
                return "the kept library is the newest";
            }

            json = await outside.GetTextAsync(asset.Url, token).ConfigureAwait(false);
            if (json is not null && UpdateSignature.Sha256(Encoding.UTF8.GetBytes(json)) != asset.Sha256)
            {
                return "the library did not match the manifest's SHA-256, so it was not used";
            }
        }

        // GitHub not reached, or the file not there: grouplab.org's copy, trusted only by its own signature.
        json ??= await outside.GetTextAsync(Mirror, token).ConfigureAwait(false);
        if (json is null)
        {
            return "no library could be reached";
        }

        var (refusal, contents) = StoreLibraryFile.Read(json, publicKey);
        if (contents is null)
        {
            return "the library was refused: " + refusal;
        }

        if (contents.Version <= StoreTargetLibrary.BuiltInVersion)
        {
            return $"the newest library, version {contents.Version}, is no newer than the built-in {StoreTargetLibrary.BuiltInVersion}";
        }

        Directory.CreateDirectory(folder);
        File.WriteAllText(SavedPath(folder), json);
        StoreTargetLibrary.Install(contents.Targets);
        return $"installed library version {contents.Version}, {contents.Targets.Count} products";
    }
}
