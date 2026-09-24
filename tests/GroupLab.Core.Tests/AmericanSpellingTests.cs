using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 169 section 9: every string a user reads, in the application and on the site, is spelled the American way,
/// center, caliber, analyze, color. scripts/american-spelling.py is the one place that knows what is British and what counts as user text,
/// with its allowances for quoted material, for keys that files depend on, and for the published release notes; this runs it in its checking
/// mode, which lists each British form by file and line and fails.
/// </summary>
public class AmericanSpellingTests
{
    [Fact]
    public void NothingAUserReadsIsSpelledTheBritishWay()
    {
        foreach (string python in new[] { "python3", "python" })
        {
            System.Diagnostics.Process? run;
            try
            {
                run = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(python, "scripts/american-spelling.py")
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

            Assert.True(run.ExitCode == 0, "British spelling in text a user reads; run python scripts/american-spelling.py --fix, or mark a line that must keep it:\n" + said);
            return;
        }

        Assert.True(Environment.GetEnvironmentVariable("CI") is null, "no python on this CI runner, so the spelling check did not run, and it must");
    }

    /// <summary>
    /// Entry 189 section 2: "Calibre" sat on the Setup panel because a literal with no space was taken for a key. The script's self-test
    /// holds the cases: a one-word label is caught, a key marked British on purpose is not, and neither is an identifier.
    /// </summary>
    [Fact]
    public void AOneWordLabelIsCheckedAndAMarkedKeyIsNot()
    {
        foreach (string python in new[] { "python3", "python" })
        {
            System.Diagnostics.Process? run;
            try
            {
                run = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(python, "scripts/american-spelling.py --self-test")
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
                continue;
            }

            Assert.True(run.ExitCode == 0, said);
            Assert.Contains("ok   setup.Children.Add(Needed(\"Calibre\"", said, StringComparison.Ordinal);
            return;
        }

        Assert.True(Environment.GetEnvironmentVariable("CI") is null, "no python on this CI runner, so the spelling self-test did not run, and it must");
    }

    /// <summary>The allowances are the script's, and they are the ones the entry names: quoted material, and nothing that is not read.</summary>
    [Fact]
    public void TheCheckKeepsItsAllowances()
    {
        string script = File.ReadAllText(Repo.PathTo("scripts", "american-spelling.py"));
        Assert.Contains("Quoted material is left as it was written", script, StringComparison.Ordinal);
        Assert.Contains("docs/RELEASE-NOTES.md is not swept", script, StringComparison.Ordinal);
        Assert.Contains("British on purpose", File.ReadAllText(Repo.PathTo("src", "GroupLab.Core", "Marking", "CartridgeTable.cs")), StringComparison.Ordinal);
    }
}
