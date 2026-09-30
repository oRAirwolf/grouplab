using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 306: the Mac build is signed with a Developer ID and the hardened runtime, notarized and stapled when the six
/// secrets of request 55 are set and right, left unsigned as before when the Mac's own two are not, and fails naming a malformed one.
/// </summary>
public class MacSigningTests
{
    [Fact]
    public void TheSecretsCheckPassesItsOwnSelfTest()
    {
        foreach (string python in new[] { "python3", "python" })
        {
            System.Diagnostics.Process? run;
            try
            {
                run = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(python, "scripts/macos-signing.py --self-test")
                {
                    WorkingDirectory = Repo.Root,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                });
            }
            catch (System.ComponentModel.Win32Exception)
            {
                continue;
            }

            string said = run!.StandardOutput.ReadToEnd() + run.StandardError.ReadToEnd();
            run.WaitForExit();
            if (run.ExitCode == 9009)
            {
                continue;   // the Windows store stub that stands in for a missing python
            }

            Assert.True(run.ExitCode == 0, "scripts/macos-signing.py --self-test failed:\n" + said);
            return;
        }

        Assert.True(Environment.GetEnvironmentVariable("CI") is null, "no python on this CI runner, so the Mac signing check's self-test did not run, and it must");
    }

    [Fact]
    public void ThePackageSignsNotarizesAndStaplesOnlyOnTheChecksWord()
    {
        string package = File.ReadAllText(Repo.PathTo(".github", "workflows", "package.yml")).ReplaceLineEndings("\n");
        Assert.Contains("python3 scripts/macos-signing.py --check", package, StringComparison.Ordinal);
        Assert.Contains("if: matrix.target.bundle && steps.macsign.outputs.sign == 'true'", package, StringComparison.Ordinal);
        Assert.Contains("--options runtime --entitlements scripts/macos/GroupLab.entitlements", package, StringComparison.Ordinal);
        Assert.Contains("xcrun notarytool submit", package, StringComparison.Ordinal);
        Assert.Contains("--wait", package, StringComparison.Ordinal);
        Assert.Contains("xcrun stapler staple", package, StringComparison.Ordinal);
        Assert.Contains("spctl --assess", package, StringComparison.Ordinal);
        Assert.True(File.Exists(Repo.PathTo("scripts", "macos", "GroupLab.entitlements")));

        // The callers hand their secrets down, or the check would never see them.
        foreach (string caller in new[] { "nightly.yml", "release.yml" })
        {
            string text = File.ReadAllText(Repo.PathTo(".github", "workflows", caller)).ReplaceLineEndings("\n");
            int at = text.IndexOf("uses: ./.github/workflows/package.yml", StringComparison.Ordinal);
            Assert.True(at > 0, caller);
            Assert.Contains("secrets: inherit", text[at..Math.Min(text.Length, at + 600)], StringComparison.Ordinal);
        }
    }
}
