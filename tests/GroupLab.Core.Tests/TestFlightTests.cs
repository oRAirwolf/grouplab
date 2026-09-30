using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 310: TestFlight's GroupLab Team and Public Beta always on the same build. scripts/testflight.py decides each
/// step, and its self-test holds every case against a made-up App Store Connect: the newest build into Public Beta and submitted, the team
/// moved only once Public Beta can install it, a rejection moving nothing, a missing group doing nothing, and nothing done twice.
/// </summary>
public class TestFlightTests
{
    [Fact]
    public void TheStepsPassTheirOwnSelfTest()
    {
        foreach (string python in new[] { "python3", "python" })
        {
            System.Diagnostics.Process? run;
            try
            {
                run = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(python, "scripts/testflight.py --self-test")
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

            Assert.True(run.ExitCode == 0, "scripts/testflight.py --self-test failed:\n" + said);
            return;
        }

        Assert.True(Environment.GetEnvironmentVariable("CI") is null, "no python on this CI runner, so the TestFlight steps' self-test did not run, and it must");
    }

    [Fact]
    public void TheWorkflowFollowsEveryNightlyAndNeverHoldsAMacRunner()
    {
        string workflow = File.ReadAllText(Repo.PathTo(".github", "workflows", "testflight.yml")).ReplaceLineEndings("\n");
        Assert.Contains("workflows: [\"nightly\"]", workflow, StringComparison.Ordinal);
        Assert.Contains("runs-on: ubuntu-latest", workflow, StringComparison.Ordinal);
        Assert.Contains("cancel-in-progress: false", workflow, StringComparison.Ordinal);
        Assert.Contains("python scripts/testflight.py --run", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("macos", workflow, StringComparison.Ordinal);
    }
}
