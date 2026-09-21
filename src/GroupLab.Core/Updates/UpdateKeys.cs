namespace GroupLab.Core.Updates;

/// <summary>
/// The public key this build trusts for update manifests, NOTES-FROM-PLANNING.md entry 119 section 3.2: "the public key is compiled into the
/// application".
/// <para>
/// <b>Set 2026-09-21.</b> Alan made the pair with <c>grouplab update-key</c>, put the private half in the repository secret named below,
/// and gave the public half to the planning session, which is how it reaches this file. The private half is not in this repository, in any
/// working copy, or in any message; only the half below is, and it is the half that can check a signature and cannot make one.
/// </para>
/// <para>
/// <b>An empty key is the safe state, not a broken one.</b> A build with none refuses every update with
/// <see cref="UpdateSignature.Refusal.NoKey"/> and says why, rather than installing whatever it is handed.
/// <c>docs/UPDATES.md</c> has the steps and how the key is rotated, which costs every installed build its trust in the old one.
/// </para>
/// </summary>
public static class UpdateKeys
{
    /// <summary>The trusted public key, base64 SubjectPublicKeyInfo, or empty while there is none.</summary>
    public const string PublicKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE7WkbyK0CR9S+FWQGbiopVRrDOuXsoW5S6yacT03OYVDaxeRHYDWoLQJU9GVqw+n01qewlvQdOMe9tmEBzdqQkw==";

    /// <summary>The name of the repository secret holding the private half. The workflow fails loudly when it is missing.</summary>
    public const string SecretName = "GROUPLAB_UPDATE_SIGNING_KEY";

    /// <summary>Whether this build can check an update at all.</summary>
    public static bool Trusts => !string.IsNullOrWhiteSpace(PublicKey);
}
