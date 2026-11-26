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

    /// <summary>
    /// Entry 311 section 3 item 4: TestFlight's feedback and crashes summarized, never a screenshot or a tester's email, and never failing
    /// the run that reads them.
    /// </summary>
    [Fact]
    public void TheFeedbackSummaryPassesItsOwnSelfTestAndRunsAfterTheSteps()
    {
        if (IpadLogsTests.Python("scripts/testflight-feedback.py --self-test") is { } run)
        {
            Assert.True(run.Exit == 0, "scripts/testflight-feedback.py --self-test failed:\n" + run.Said);
        }

        string workflow = File.ReadAllText(Repo.PathTo(".github", "workflows", "testflight.yml")).ReplaceLineEndings("\n");
        int feedback = workflow.IndexOf("python scripts/testflight-feedback.py --run", StringComparison.Ordinal);
        Assert.True(feedback > workflow.IndexOf("python scripts/testflight.py --run", StringComparison.Ordinal));
        Assert.Contains("continue-on-error: true", workflow[..feedback], StringComparison.Ordinal);
        string script = File.ReadAllText(Repo.PathTo("scripts", "testflight-feedback.py"));
        Assert.DoesNotContain("email", script.Split("FIELDS = ")[1].Split('\n')[0], StringComparison.Ordinal);
    }

    /// <summary>
    /// Entry 513: every new build to App Review for iPhone, iPad and the Mac, released by itself once approved, one review at a time on
    /// each platform and never cancelling one. scripts/app-store.py's self-test holds the cases; the workflow runs it after TestFlight's
    /// step, never failing the run, with the pause switch passed in.
    /// </summary>
    [Fact]
    public void TheAppStoreSubmissionsPassTheirOwnSelfTestAndCanBePaused()
    {
        if (IpadLogsTests.Python("scripts/app-store.py --self-test") is { } run)
        {
            Assert.True(run.Exit == 0, "scripts/app-store.py --self-test failed:\n" + run.Said);
        }

        string workflow = File.ReadAllText(Repo.PathTo(".github", "workflows", "testflight.yml")).ReplaceLineEndings("\n");
        int submit = workflow.IndexOf("python scripts/app-store.py ${{ inputs.inspect", StringComparison.Ordinal);
        Assert.True(submit > workflow.IndexOf("python scripts/testflight.py --run", StringComparison.Ordinal));
        string step = workflow[workflow.LastIndexOf("- name:", submit, StringComparison.Ordinal)..submit];
        Assert.Contains("continue-on-error: true", step, StringComparison.Ordinal);
        Assert.Contains("STORE_SUBMISSIONS_PAUSED: ${{ vars.STORE_SUBMISSIONS_PAUSED }}", step, StringComparison.Ordinal);
        string script = File.ReadAllText(Repo.PathTo("scripts", "app-store.py"));
        Assert.DoesNotContain("appStoreVersionSubmissions\", {\"data\": {\"type\": \"appStoreVersionSubmissions\"", script, StringComparison.Ordinal);
        Assert.DoesNotContain("\"DELETE\"", script, StringComparison.Ordinal);   // nothing here ever removes a review

        // Request 122: a review is taken out only by the step run by hand with a version named, before the submissions, failing when it
        // took out nothing; the half-hourly step never cancels one.
        int withdraw = workflow.IndexOf("python scripts/app-store.py --withdraw", StringComparison.Ordinal);
        Assert.True(withdraw > 0 && withdraw < submit);
        string named = workflow[workflow.LastIndexOf("- name:", withdraw, StringComparison.Ordinal)..withdraw];
        Assert.Contains("if: github.event_name == 'workflow_dispatch' && inputs.withdraw != ''", named, StringComparison.Ordinal);
        Assert.DoesNotContain("continue-on-error", named, StringComparison.Ordinal);
        // Two cancels and no more: a submission Apple already answered (entry 516) and the one Alan names (request 122).
        Assert.Equal(2, script.Split("\"canceled\": True").Length - 1);
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
