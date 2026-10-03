using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 353 step 2: real taps on the iOS simulator and the Android emulator, run with the sweeps the nightly starts
/// for every build that changes the application. These hold the wiring; the scenario itself runs headlessly in PhoneTouchScenarioTests.
/// </summary>
public class RealTouchTests
{
    private static string Text(params string[] parts) => File.ReadAllText(Repo.PathTo(parts)).ReplaceLineEndings("\n");

    /// <summary>The steps of one job, from its heading to the next job's.</summary>
    private static string Job(string workflow, string job)
    {
        int at = workflow.IndexOf($"\n  {job}:\n", StringComparison.Ordinal);
        Assert.True(at > 0, $"no job {job}");
        int next = workflow.IndexOf("\n  ", at + job.Length + 5, StringComparison.Ordinal);
        while (next > 0 && workflow[next + 3] == ' ')
        {
            next = workflow.IndexOf("\n  ", next + 1, StringComparison.Ordinal);
        }

        return next > 0 ? workflow[at..next] : workflow[at..];
    }

    [Fact]
    public void TheSimulatorTapsForRealAfterItsSweep()
    {
        string job = Job(Text(".github", "workflows", "ios-app.yml"), "dev-simulator");
        int sweep = job.IndexOf("- name: The screen sweep", StringComparison.Ordinal);
        int taps = job.IndexOf("- name: Real taps", StringComparison.Ordinal);
        Assert.True(sweep > 0 && taps > sweep, "Real taps comes after the screen sweep in GroupLab Dev's simulator job");
        string step = job[taps..];
        Assert.Contains("xcodegen generate --spec scripts/touch/ios/project.yml", step, StringComparison.Ordinal);
        Assert.Contains("TEST_RUNNER_GROUPLAB_TAPS=", step, StringComparison.Ordinal);
        Assert.Contains("cp scripts/scenarios/phone-touch.json \"$folder/scenario.json\"", step, StringComparison.Ordinal);
        Assert.Contains("python3 scripts/touch-test.py ios", step, StringComparison.Ordinal);
        Assert.Contains("python3 scripts/touch-test.py check touch scripts/scenarios/phone-touch.json", step, StringComparison.Ordinal);
        Assert.Contains("GROUPLAB_TAPS", Text("scripts", "touch", "ios", "Tests", "TouchTests.swift"), StringComparison.Ordinal);
        Assert.Contains("press(forDuration:", Text("scripts", "touch", "ios", "Tests", "TouchTests.swift"), StringComparison.Ordinal);
    }

    [Fact]
    public void TheEmulatorTapsForRealAfterItsSweep()
    {
        string workflow = Text(".github", "workflows", "android-emulator.yml");
        int sweep = workflow.IndexOf("script: bash scripts/android-sweep.sh", StringComparison.Ordinal);
        int taps = workflow.IndexOf("script: bash scripts/android-touch.sh apk/grouplab-dev-x86_64.apk android-touch", StringComparison.Ordinal);
        Assert.True(sweep > 0 && taps > sweep, "the real taps come after the emulator's sweep");

        // Held as a finger is: "input tap" puts the finger down and up in the same instant, and a page that moves between the two, which is
        // what build 157 did, would never be caught.
        string script = Text("scripts", "touch-test.py");
        Assert.Contains("\"input\", \"swipe\"", script, StringComparison.Ordinal);
        string shell = Text("scripts", "android-touch.sh");
        Assert.Contains("EXTRA=org.grouplab.test.scenario", shell, StringComparison.Ordinal);
        Assert.Contains("touch-test.py\" android", shell, StringComparison.Ordinal);
        Assert.Contains("show_ime_with_hard_keyboard 1", shell, StringComparison.Ordinal);

        // adb taps in pixels from the screen's corner, so the Android head says where its view is.
        Assert.Contains("GroupLab.Mobile.Dev.Scenario.ScreenPlace = ", Text("android", "GroupLab.Android", "MainActivity.cs"), StringComparison.Ordinal);
    }

    [Fact]
    public void TheTapScriptPassesItsOwnSelfTest()
    {
        if (IpadLogsTests.Python("scripts/touch-test.py --self-test") is { } run)
        {
            Assert.True(run.Exit == 0, run.Said);
        }
    }
}
