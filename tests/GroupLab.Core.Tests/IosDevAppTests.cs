using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 315's amendment, sections 2 and 5: GroupLab Dev for iPhone and iPad is its own application beside GroupLab,
/// built from the same sources with the developer tools compiled in, unsigned until its own profiles exist, and never failing a nightly
/// for want of them.
/// </summary>
public class IosDevAppTests
{
    private static string Text(params string[] parts) => File.ReadAllText(Repo.PathTo(parts)).ReplaceLineEndings("\n");

    [Fact]
    public void GroupLabDevsFilesAreInStepWithGroupLabsOwn()
    {
        if (IpadLogsTests.Python("scripts/ios-dev-app.py --check") is { } run)
        {
            Assert.True(run.Exit == 0, run.Said);
        }
    }

    [Fact]
    public void EachProjectBecomesGroupLabDevOnlyWithTheProperty()
    {
        string app = Text("ios", "GroupLab.iOS", "GroupLab.iOS.csproj");
        string share = Text("ios", "GroupLab.Share", "GroupLab.Share.csproj");
        foreach (string project in new[] { app, share })
        {
            string dev = project[project.IndexOf("<PropertyGroup Condition=\"'$(GroupLabDev)' == 'true'\">", StringComparison.Ordinal)..];
            dev = dev[..dev.IndexOf("</PropertyGroup>", StringComparison.Ordinal)];
            Assert.Contains("<CodesignEntitlements>Entitlements.dev.plist</CodesignEntitlements>", dev, StringComparison.Ordinal);
            Assert.Contains("<AppBundleManifest>Info.dev.plist</AppBundleManifest>", dev, StringComparison.Ordinal);
            Assert.Contains("GROUPLAB_DEV", dev, StringComparison.Ordinal);
        }

        Assert.Contains("<ApplicationId>org.grouplab.app.dev</ApplicationId>", app, StringComparison.Ordinal);
        Assert.Contains("<AppIcon>AppIconDev</AppIcon>", app, StringComparison.Ordinal);
        Assert.Contains("<ApplicationId>org.grouplab.app.dev.share</ApplicationId>", share, StringComparison.Ordinal);
        Assert.True(File.Exists(Repo.PathTo("ios", "GroupLab.iOS", "Assets.xcassets", "AppIconDev.appiconset", "icon-1024.png")));

        // The public application's own values stay as they were.
        Assert.Contains("<ApplicationId>org.grouplab.app</ApplicationId>", app, StringComparison.Ordinal);
        string handoff = Text("ios", "Shared", "Handoff.cs");
        Assert.Contains("\"group.org.grouplab.app.dev\"", handoff, StringComparison.Ordinal);
        Assert.Contains("\"group.org.grouplab.app\"", handoff, StringComparison.Ordinal);
    }

    [Fact]
    public void TheNightlyBuildsGroupLabDevAndSignsItOnlyOnItsOwnCheck()
    {
        string nightly = Text(".github", "workflows", "nightly.yml");
        int dev = nightly.IndexOf("\n  ios-dev:\n", StringComparison.Ordinal);
        int publish = nightly.IndexOf("\n  publish:\n", StringComparison.Ordinal);
        Assert.True(dev > 0 && publish > dev, "the nightly has an ios-dev job before publish");
        string job = nightly[dev..publish];
        Assert.Contains("continue-on-error: true", job, StringComparison.Ordinal);
        Assert.Contains("python3 scripts/ios-signing.py --check-dev", job, StringComparison.Ordinal);
        Assert.Contains("if: steps.signing.outputs.sign-dev == 'true'", job, StringComparison.Ordinal);
        Assert.Contains("-p:GroupLabDev=true", job, StringComparison.Ordinal);
        Assert.Contains("-p:EnableCodeSigning=false", job, StringComparison.Ordinal);
        Assert.Contains("IOS_DEV_PROFILE: ${{ secrets.IOS_DEV_PROFILE }}", job, StringComparison.Ordinal);
        Assert.Contains("exit 0", job, StringComparison.Ordinal);
        string needs = nightly[publish..].Split('\n').First(l => l.TrimStart().StartsWith("needs:", StringComparison.Ordinal));
        Assert.DoesNotContain("ios", needs, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSimulatorWorkflowRunsAScenarioInGroupLabDevAndChecksGroupLabHasNoTools()
    {
        string workflow = Text(".github", "workflows", "ios-app.yml");
        int dev = workflow.IndexOf("\n  dev-simulator:\n", StringComparison.Ordinal);
        Assert.True(dev > 0);
        string job = workflow[dev..];
        Assert.Contains("-p:GroupLabDev=true", job, StringComparison.Ordinal);
        Assert.Contains("scenario.json", job, StringComparison.Ordinal);
        Assert.Contains("GroupLab carries GroupLab Dev's developer tools", workflow[..dev], StringComparison.Ordinal);
    }
}
