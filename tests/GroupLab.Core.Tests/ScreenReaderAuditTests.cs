using System.Text.Json.Nodes;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 461 section 2.1: the screen reader audits on the emulator and the simulator stay wired up, and the script
/// that judges them passes its own test.
/// </summary>
public class ScreenReaderAuditTests
{
    private static string Text(params string[] parts) => File.ReadAllText(Repo.PathTo(parts)).ReplaceLineEndings("\n");

    [Fact]
    public void TheAuditScriptPassesItsOwnSelfTest()
    {
        if (IpadLogsTests.Python("scripts/screen-reader-audit.py --self-test") is { } run)
        {
            Assert.True(run.Exit == 0, run.Said);
        }
    }

    [Fact]
    public void BothPlatformsRunTheAuditOnTheSweepsScreens()
    {
        string ios = Text(".github", "workflows", "ios-app.yml");
        Assert.Contains("- name: Screen reader audit", ios, StringComparison.Ordinal);
        Assert.Contains("screen-reader-audit.py ios", ios, StringComparison.Ordinal);
        Assert.Contains("screen-reader-audit.py judge", ios, StringComparison.Ordinal);
        Assert.Contains("performAccessibilityAudit", Text("scripts", "touch", "ios", "Tests", "TouchTests.swift"), StringComparison.Ordinal);

        string android = Text(".github", "workflows", "android-emulator.yml");
        Assert.Contains("- name: Screen reader audit", android, StringComparison.Ordinal);
        Assert.Contains("scripts/android-audit.sh", android, StringComparison.Ordinal);
        Assert.Contains("screen-reader-audit.py\" android", Text("scripts", "android-audit.sh"), StringComparison.Ordinal);

        // The audit visits every screen the sweep pictures, so a screen added to the sweep is read too.
        var sweep = JsonNode.Parse(Text("scripts", "scenarios", "phone-sweep.json"))!["steps"]!.AsArray()
            .Where(s => (string?)s!["do"] == "tree").Select(s => (string?)s!["name"]).ToList();
        var audit = JsonNode.Parse(Text("scripts", "scenarios", "phone-audit.json"))!["steps"]!.AsArray()
            .Where(s => (string?)s!["do"] == "audit").Select(s => (string?)s!["name"]).ToList();
        Assert.Equal(sweep, audit);
    }
}
