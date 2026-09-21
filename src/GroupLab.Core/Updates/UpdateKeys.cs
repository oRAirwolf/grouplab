namespace GroupLab.Core.Updates;

/// <summary>
/// The public key this build trusts for update manifests, NOTES-FROM-PLANNING.md entry 119 section 3.2: "the public key is compiled into the
/// application".
/// <para>
/// <b>It is empty, and that is deliberate.</b> The private key lives only as a GitHub Actions secret, which nobody but Alan can create, and
/// the public key is the half of the pair that goes here. Until he makes one, every update is refused with
/// <see cref="UpdateSignature.Refusal.NoKey"/> and the settings page says why, which is the safe way round: a build that trusted nothing yet
/// installs nothing, rather than installing whatever it is handed.
/// </para>
/// <para>
/// <b>To fill it in:</b> run <c>grouplab update-key</c>, put the private key in the repository secret it names, and paste the public key
/// here in one commit. <c>docs/UPDATES.md</c> has the steps and how the key is rotated.
/// </para>
/// </summary>
public static class UpdateKeys
{
    /// <summary>The trusted public key, base64 SubjectPublicKeyInfo, or empty while there is none.</summary>
    public const string PublicKey = "";

    /// <summary>The name of the repository secret holding the private half. The workflow fails loudly when it is missing.</summary>
    public const string SecretName = "GROUPLAB_UPDATE_SIGNING_KEY";

    /// <summary>Whether this build can check an update at all.</summary>
    public static bool Trusts => !string.IsNullOrWhiteSpace(PublicKey);
}
