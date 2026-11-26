using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// Request 121: Apple turned down Mac 1.0 for crashing on launch. Inside the App Sandbox, .NET started from its separate runtime libraries is
/// refused LaunchServices and AppKit stops it; the same program as one file is let through (mac-launch.yml). These hold the two things that
/// keep it fixed: the Mac App Store copy is published as one file, and mac-store.yml's sandbox check stops the run instead of being noted
/// and passed over, as it was for a week while every copy sent to Apple crashed.
/// </summary>
public class Request121Tests
{
    private static string Text(params string[] parts) => File.ReadAllText(Repo.PathTo(parts)).ReplaceLineEndings("\n");

    [Fact]
    public void TheMacAppStoreCopyIsPublishedAsOneFile()
    {
        string project = Text("src", "GroupLab.App", "GroupLab.App.csproj");

        Assert.Contains("<PublishSingleFile Condition=\"'$(GroupLabDistribution)' == 'store' And $(RuntimeIdentifier.StartsWith('osx'))\">true</PublishSingleFile>",
            project, StringComparison.Ordinal);
        // The store copy is made with exactly the setting the condition reads.
        Assert.Contains("-p:GroupLabDistribution=store", Text(".github", "workflows", "mac-store.yml"), StringComparison.Ordinal);
    }

    [Fact]
    public void ANightlyTaggedOnAnEarlierCommitStillGetsItsMacCopy()
    {
        Assert.Contains("select(.createdAt >= \"${{ github.event.workflow_run.created_at }}\")", Text(".github", "workflows", "mac-store.yml"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void AStoreCopyThatFailsInsideTheSandboxIsNeverSentToApple()
    {
        string workflow = Text(".github", "workflows", "mac-store.yml");
        int check = workflow.IndexOf("- name: Check it inside the sandbox", StringComparison.Ordinal);
        int sign = workflow.IndexOf("- name: Sign for the store and send to TestFlight", StringComparison.Ordinal);

        Assert.True(check > 0 && sign > check, "the sandbox check comes before the store signature and upload");
        Assert.DoesNotContain("continue-on-error", workflow[check..sign], StringComparison.Ordinal);
    }
}
