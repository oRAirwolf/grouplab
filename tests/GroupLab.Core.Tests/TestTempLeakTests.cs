using GroupLab.Core.Tests.Support;
using GroupLab.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 179: every file a test writes lands in the run's own folder, which goes when the run ends. This holds the
/// half a test can see from inside, that the redirection is in force; CI's step after the suite, <c>scripts/temp-leak-check.py</c>, holds
/// the other half, that nothing is left behind.
/// </summary>
public class TestTempLeakTests
{
    [Fact]
    public void EveryTemporaryFileGoesIntoTheRunsOwnFolder()
    {
        Assert.False(string.IsNullOrEmpty(TestTempRoot.Folder), "the run's folder was never made, so tests are writing straight into the system's temporary directory");
        Assert.True(Directory.Exists(TestTempRoot.Folder));
        string temp = Path.GetFullPath(Path.GetTempPath());
        Assert.StartsWith(Path.GetFullPath(TestTempRoot.Folder), temp, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Path.DirectorySeparatorChar + TestTempRoot.Parent + Path.DirectorySeparatorChar, temp, StringComparison.OrdinalIgnoreCase);

        // And what a test makes the usual way lands there.
        string folder = Temp.Folder("leak-check");
        Assert.StartsWith(Path.GetFullPath(TestTempRoot.Folder), Path.GetFullPath(folder), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The scratch profiling tests of entry 170 answered their questions and went; none may come back to write on every run.</summary>
    [Fact]
    public void NoScratchTestsAreLeftInTheSuite()
    {
        var scratch = Directory.EnumerateFiles(Repo.PathTo("tests"), "Scratch*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();
        Assert.Empty(scratch);
    }
}
