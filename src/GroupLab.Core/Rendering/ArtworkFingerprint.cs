using System.Security.Cryptography;
using GroupLab.Core.Gltd.Json;

namespace GroupLab.Core.Rendering;

/// <summary>
/// What a definition prints, as a SHA-256 of its PDF, NOTES-FROM-PLANNING.md entry 77 section 3 item 2. The render is deterministic, so the
/// hash changes exactly when the printed artwork does. <c>grouplab corpus counts</c> records these beside the corpus's detection counts, and a
/// test compares them with the current renderer, so a change to printed artwork cannot land without the corpus being re-run and its counts
/// compared before and after. The render-and-difference check that caught entry 76's caption change was run by chance; this makes it standing.
/// </summary>
public static class ArtworkFingerprint
{
    /// <summary>The definition files whose artwork is fingerprinted: every definition under <paramref name="targets"/>, frozen ones included, relative paths with forward slashes.</summary>
    public static IReadOnlyList<string> Files(string targets) =>
        [.. Directory.EnumerateFiles(targets, "*.gltd.json", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(targets, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)];

    /// <summary>The fingerprint of the definition file at <paramref name="path"/>, or null with the reason when it cannot be read or rendered.</summary>
    public static string? Of(string path, out string? failure)
    {
        failure = null;
        var read = GltdJsonReader.Read(File.ReadAllBytes(path));
        if (read.Definition is null)
        {
            failure = string.Join("; ", read.Diagnostics.Select(d => d.Message));
            return null;
        }

        // A frozen definition is what was printed, valid or not, so it is rendered as the analyser renders it. The print screen can add the
        // actual-size sentence, which is printed artwork too, so both renders are hashed.
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string? note in new[] { null, SceneBuilder.ActualSizeNote })
        {
            var render = TargetRenderer.Render(read.Definition, new RenderOptions(AllowInvalid: true, PrintNote: note));
            if (render.Pdf is null)
            {
                failure = string.Join("; ", render.Diagnostics.Select(d => d.Message));
                return null;
            }

            hash.AppendData(render.Pdf);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    /// <summary>Every fingerprint under <paramref name="targets"/>, keyed by relative path; a definition that cannot be rendered is keyed to its failure.</summary>
    public static IReadOnlyDictionary<string, string> All(string targets) =>
        Files(targets).ToDictionary(f => f, f => Of(Path.Combine(targets, f), out string? failure) ?? $"unrenderable: {failure}", StringComparer.Ordinal);
}
