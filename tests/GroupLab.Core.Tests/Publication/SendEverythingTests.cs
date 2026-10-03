using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Core.Publication;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Publication;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 357 section 1, the shared half: the switches come from limits.json as the application reads it; a package
/// without a submission block is exactly what it was before the entry; one with it carries the state and the picture code; and the code is
/// the same for two versions of one picture and different for another installation.
/// </summary>
public class SendEverythingTests
{
    [Fact]
    public void TheSwitchesAreTheOnesLimitsJsonHolds()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Repo.PathTo("website", "api", "limits.json")));
        var root = doc.RootElement;
        Assert.Equal(root.GetProperty("sendEverythingOpen").GetBoolean(), ReceiverTerms.Current.SendEverythingOpen);
        Assert.Equal(root.GetProperty("fullLogErrorReports").GetBoolean(), ReceiverTerms.Current.FullLogErrorReports);
        Assert.Equal(root.GetProperty("crashReceiver").GetString(), ReceiverTerms.Current.CrashReceiver);
    }

    [Fact]
    public void APackageWithoutAStateIsAsBeforeAndOneWithItCarriesIt()
    {
        var terms = ReceiverTerms.Current;
        var before = JsonNode.Parse(TargetPackages.Build([1, 2, 3], "target.png", [], [], [], [], [], "log", ConsentLevel.Testing, terms).Json)!.AsObject();
        Assert.False(before.ContainsKey("submission"));
        Assert.Equal(["schema", "consent", "manifest", "detected", "corrected", "told", "analysis", "environment", "log"], before.Select(p => p.Key).ToArray());

        var block = new JsonObject { ["state"] = "unread", ["picture"] = new string('a', 32) };
        var after = JsonNode.Parse(TargetPackages.Build([1, 2, 3], "target.png", [], [], [], [], [], "log", ConsentLevel.Testing, terms, block).Json)!;
        Assert.Equal("unread", (string?)after["submission"]!["state"]);
        Assert.Contains((string?)after["submission"]!["state"], TargetPackages.States);
    }

    [Fact]
    public void TwoVersionsOfOnePictureShareACodeThatSaysNothingElse()
    {
        byte[] picture = [9, 8, 7, 6];
        string code = TargetPackages.PictureCode("salt-one", picture);
        Assert.Matches("^[0-9a-f]{32}$", code);
        Assert.Equal(code, TargetPackages.PictureCode("salt-one", [.. picture]));
        Assert.NotEqual(code, TargetPackages.PictureCode("salt-two", picture));
        Assert.NotEqual(code, TargetPackages.PictureCode("salt-one", [9, 8, 7, 5]));
        Assert.DoesNotContain(Intake.Sha256(picture)[..16], code, StringComparison.Ordinal);
    }
}
