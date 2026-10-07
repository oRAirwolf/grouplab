using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>NOTES-FROM-PLANNING.md entry 383: a marking names its photo by file name only, finds it beside itself, and sends no folder anywhere.</summary>
public sealed class MarkingFilePathTests
{
    [Theory]
    [InlineData("/Users/someone/Range photos/PXL_1.jpg", "PXL_1.jpg")]
    [InlineData(@"C:\Users\someone\Pictures\scan 1.png", "scan 1.png")]
    [InlineData("PXL_1.jpg", "PXL_1.jpg")]
    public void APathLosesItsFoldersWhicheverDeviceWroteIt(string path, string name)
    {
        Assert.Equal(name, MarkingFile.ImageName(path));
        Assert.Contains($"\"image\": \"{name}\"", MarkingFile.Write(MarkingState.Empty with { ImagePath = path }), StringComparison.Ordinal);
        Assert.DoesNotContain("someone", MarkingFile.WithoutFolders(MarkingFile.Write(MarkingState.Empty) .Replace("\"image\": null", $"\"image\": {System.Text.Json.JsonSerializer.Serialize(path)}", StringComparison.Ordinal)), StringComparison.Ordinal);
    }

    [Fact]
    public void ThePhotoIsLookedForBesideTheMarkingThenWhereAnOldFileSaysThenNowhere()
    {
        string marking = Path.Combine(Path.GetTempPath(), "moved", "target.grouplab.json");
        string beside = Path.Combine(Path.GetTempPath(), "moved", "PXL_1.jpg");
        const string old = "/Users/someone/Range photos/PXL_1.jpg";
        Assert.Equal(beside, MarkingFile.FindImage(marking, "PXL_1.jpg", p => p == beside));
        Assert.Equal(beside, MarkingFile.FindImage(marking, old, p => p == beside || p == old));
        Assert.Equal(old, MarkingFile.FindImage(marking, old, p => p == old));
        Assert.Null(MarkingFile.FindImage(marking, "PXL_1.jpg", _ => false));
        Assert.Null(MarkingFile.FindImage(marking, null, _ => true));
    }

    [Fact]
    public void SomethingThatIsNotAMarkingIsGivenBackAsItWas() =>
        Assert.Equal("not json", MarkingFile.WithoutFolders("not json"));
}
