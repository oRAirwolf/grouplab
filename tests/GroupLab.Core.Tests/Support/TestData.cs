using System.Security.Cryptography;

namespace GroupLab.Core.Tests.Support;

/// <summary>
/// Large test files, NOTES-FROM-PLANNING.md entry 171 section 6: a sample over about 10 MB lives on the <c>test-data</c> release, never in
/// the repository. CI downloads each one with <c>scripts/test-data.py</c>, verifies its SHA-256 and names the folder in
/// <c>GROUPLAB_TEST_DATA</c>. On a machine without that, the file is looked for where its fixture says it was measured.
/// </summary>
internal static class TestData
{
    /// <summary>The folder a file is in: the downloaded copy where there is one, otherwise the folder its fixture names.</summary>
    public static string Folder(string file, string measuredIn) =>
        Environment.GetEnvironmentVariable("GROUPLAB_TEST_DATA") is { Length: > 0 } fetched && File.Exists(System.IO.Path.Combine(fetched, file))
            ? fetched
            : measuredIn;

    public static string Path(string file, string measuredIn) => System.IO.Path.Combine(Folder(file, measuredIn), file);

    /// <summary>Whether the file at <paramref name="path"/> is the one with this hash.</summary>
    public static bool Matches(string path, string sha256) =>
        File.Exists(path) && string.Equals(Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path))), sha256, StringComparison.OrdinalIgnoreCase);
}
