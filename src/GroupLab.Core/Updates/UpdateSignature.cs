using System.Security.Cryptography;

namespace GroupLab.Core.Updates;

/// <summary>
/// Signing a manifest, and refusing one that is not signed by the key this build trusts, NOTES-FROM-PLANNING.md entry 119 section 3.2.
/// <para>
/// <b>The algorithm is ECDSA over P-256 with SHA-256, not Ed25519, and that is a deviation from the entry.</b> Ed25519 is not in .NET 10's
/// own cryptography, and this machine has no NuGet source configured, so no package can be added to provide it: a restore can only use
/// what is already in the local cache. P-256 is in the box on every platform GroupLab builds for, and OpenSSL on the build runner signs it
/// with one command. The algorithm is named in every signed manifest, so moving to Ed25519 later is a manifest the application can tell
/// apart rather than a silent change. Question 31 records it.
/// </para>
/// </summary>
public static class UpdateSignature
{
    /// <summary>The name that goes in a signed manifest, so a reader knows what to verify with.</summary>
    public const string Algorithm = "ecdsa-p256-sha256";

    /// <summary>Why a manifest was refused, in the words the application shows.</summary>
    public enum Refusal
    {
        None,
        NoKey,
        NotSigned,
        WrongAlgorithm,
        BadSignature,
        WrongTrain,
        NotNewer,
        UnknownManifest,
    }

    /// <summary>What to say to a person about a refusal. Never "verification failed".</summary>
    public static string Words(this Refusal refusal) => refusal switch
    {
        Refusal.NoKey => "This build carries no update key, so it cannot tell a real update from a forged one and will not install any. Download the newest build by hand instead.",
        Refusal.NotSigned => "The update GroupLab found is not signed, so it was refused. Nothing was downloaded.",
        Refusal.WrongAlgorithm => "The update is signed in a way this build does not know how to check, so it was refused.",
        Refusal.BadSignature => "The update's signature does not match GroupLab's key, so it was refused. Nothing was downloaded.",
        Refusal.WrongTrain => "The update GroupLab found belongs to another train, so it was left alone.",
        Refusal.NotNewer => "GroupLab is already on the newest build of its train.",
        Refusal.UnknownManifest => "The update information is in a newer format than this build understands. Download the newest build by hand.",
        _ => "",
    };

    /// <summary>Signs a manifest with a private key in PKCS#8, which is what the build workflow holds as a secret.</summary>
    public static SignedManifest Sign(UpdateManifest manifest, ReadOnlySpan<byte> privateKeyPkcs8)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        using var key = ECDsa.Create();
        key.ImportPkcs8PrivateKey(privateKeyPkcs8, out _);
        byte[] signature = key.SignData(manifest.Signable(), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        return new SignedManifest(Algorithm, Convert.ToBase64String(signature), manifest);
    }

    /// <summary>
    /// Whether a signed manifest was signed by the key given, in SubjectPublicKeyInfo form. A null or empty key is <see cref="Refusal.NoKey"/>:
    /// a build with no key installs nothing, which is the safe way round.
    /// </summary>
    public static Refusal Verify(SignedManifest? signed, string? publicKeyBase64)
    {
        if (string.IsNullOrWhiteSpace(publicKeyBase64))
        {
            return Refusal.NoKey;
        }

        if (signed is null || string.IsNullOrWhiteSpace(signed.Signature))
        {
            return Refusal.NotSigned;
        }

        if (!string.Equals(signed.Algorithm, Algorithm, StringComparison.Ordinal))
        {
            return Refusal.WrongAlgorithm;
        }

        if (signed.Payload is not { Manifest: UpdateManifest.Current })
        {
            return Refusal.UnknownManifest;
        }

        try
        {
            using var key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyBase64), out _);
            bool ok = key.VerifyData(signed.Payload.Signable(), Convert.FromBase64String(signed.Signature), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
            return ok ? Refusal.None : Refusal.BadSignature;
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            return Refusal.BadSignature;
        }
    }

    /// <summary>A new key pair: the private key to put in the build secret, and the public key to compile into the application.</summary>
    public static (string PrivateKeyBase64, string PublicKeyBase64) NewKeyPair()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return (Convert.ToBase64String(key.ExportPkcs8PrivateKey()), Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()));
    }

    /// <summary>The SHA-256 of a file, lower case, as a manifest records it.</summary>
    public static string Sha256(ReadOnlySpan<byte> bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
