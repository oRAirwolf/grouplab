using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 352 item 1: the screen sweep on an Android emulator, started by the nightly as the iOS simulator's is, with
/// GroupLab Dev built for x86_64 and the same scenario named by the same extra the Android head reads.
/// </summary>
public class AndroidEmulatorSweepTests
{
    private static string Text(params string[] parts) => File.ReadAllText(Repo.PathTo(parts)).ReplaceLineEndings("\n");

    [Fact]
    public void TheNightlyStartsTheEmulatorSweepWithoutWaitingOnIt()
    {
        string nightly = Text(".github", "workflows", "nightly.yml");
        int job = nightly.IndexOf("\n  android-emulator:\n", StringComparison.Ordinal);
        int publish = nightly.IndexOf("\n  publish:\n", StringComparison.Ordinal);
        Assert.True(job > 0 && publish > job, "the nightly has an android-emulator job before publish");
        string text = nightly[job..publish];
        Assert.Contains("needs.name-it.outputs.application-changed == 'yes'", text, StringComparison.Ordinal);
        Assert.Contains("continue-on-error: true", text, StringComparison.Ordinal);
        Assert.Contains("gh workflow run android-emulator.yml", text, StringComparison.Ordinal);
        string needs = nightly[publish..].Split('\n').First(l => l.TrimStart().StartsWith("needs:", StringComparison.Ordinal));
        Assert.DoesNotContain("emulator", needs, StringComparison.Ordinal);
    }

    [Fact]
    public void TheEmulatorWorkflowRunsTheSweepOnGroupLabDevForX8664AndNeverOnAPush()
    {
        string workflow = Text(".github", "workflows", "android-emulator.yml");
        string on = workflow[workflow.IndexOf("\non:\n", StringComparison.Ordinal)..workflow.IndexOf("\nconcurrency:", StringComparison.Ordinal)];
        Assert.DoesNotContain("push", on, StringComparison.Ordinal);
        Assert.Contains("workflow_dispatch", on, StringComparison.Ordinal);
        Assert.Contains("-p:GroupLabDev=true -p:GroupLabEmulator=true", workflow, StringComparison.Ordinal);
        Assert.Contains("arch: x86_64", workflow, StringComparison.Ordinal);
        Assert.Contains("lib/x86_64/libOpenCvSharpExtern.so", workflow, StringComparison.Ordinal);
        Assert.Contains("scripts/android-sweep.sh", workflow, StringComparison.Ordinal);

        string project = Text("android", "GroupLab.Android", "GroupLab.Android.csproj");
        Assert.Contains("<PropertyGroup Condition=\"'$(GroupLabEmulator)' == 'true'\">", project, StringComparison.Ordinal);
        Assert.Contains("<RuntimeIdentifiers>android-x64</RuntimeIdentifiers>", project, StringComparison.Ordinal);
        Assert.Contains("<RuntimeIdentifiers>android-arm64</RuntimeIdentifiers>", project, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSweepNamesTheScenarioByTheExtraTheAndroidHeadReads()
    {
        string script = Text("scripts", "android-sweep.sh");
        string activity = Text("android", "GroupLab.Android", "MainActivity.cs");
        Assert.Contains("internal const string TestScenarioExtra = \"org.grouplab.test.scenario\";", activity, StringComparison.Ordinal);
        Assert.Contains("Scenario.Prepare(FilesDir!.AbsolutePath, TestLoop(Intent) ?? Intent?.GetStringExtra(TestScenarioExtra))", activity, StringComparison.Ordinal);
        Assert.Contains("EXTRA=org.grouplab.test.scenario", script, StringComparison.Ordinal);
        Assert.Contains("scripts/scenarios/phone-sweep.json", script, StringComparison.Ordinal);
        Assert.Contains("--es \"$EXTRA\" \"$NAME\"", script, StringComparison.Ordinal);
        Assert.Contains("scripts/phone-sweep-check.py", script, StringComparison.Ordinal);
    }

    /// <summary>The emulator's pass is judged as the simulator's is: the same steps allowed to find nothing, the same eleven screenshots.</summary>
    [Fact]
    public void TheSweepsJudgePassesItsOwnSelfTest()
    {
        if (IpadLogsTests.Python("scripts/phone-sweep-check.py --self-test") is { } run)
        {
            Assert.True(run.Exit == 0, run.Said);
        }

        Assert.Contains("MAY_FIND_NOTHING = [\"Use this picture|Use it anyway\"]", Text("scripts", "phone-sweep-check.py"), StringComparison.Ordinal);
        Assert.Contains("may_find_nothing = [\"Use this picture|Use it anyway\"]", Text(".github", "workflows", "ios-app.yml"), StringComparison.Ordinal);
    }
}
