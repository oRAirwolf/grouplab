using GroupLab.Core.Publication;

namespace GroupLab.Core.Tests.Publication;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 35 section 1: a file that is not a camera original is held by default. The names screenshot tools and
/// messaging apps write are caught under any prefix the upload page adds, and a file with no camera make is caught whatever its name.
/// An edited phone photograph that keeps its camera metadata is not caught.
/// </summary>
public class CameraOriginalTests
{
    [Theory]
    [InlineData("Screenshot_20231029-170033.jpg", "a screenshot tool")]
    [InlineData("002_Screen Shot 2024-01-02 at 10.00.00.jpg", "a screenshot tool")]
    [InlineData("signal-2023-07-25-20-37-40-354-1.jpg", "the Signal messenger")]
    [InlineData("001_IMG-20230725-WA0003.jpg", "WhatsApp")]
    [InlineData("FB_IMG_1690000000000.jpg", "Facebook")]
    public void ANameAScreenshotToolOrMessagingAppWritesIsHeldEvenWithCameraMetadata(string name, string source)
    {
        string? problem = CameraOriginal.Problem(PhoneImages.Jpeg(), name);
        Assert.NotNull(problem);
        Assert.Contains(source, problem, StringComparison.Ordinal);
    }

    [Fact]
    public void AFileWithNoCameraMakeIsHeldWhateverItsName()
    {
        Assert.Contains("no camera make", CameraOriginal.Problem(PhoneImages.Png(), "target.png"), StringComparison.Ordinal);
    }

    [Fact]
    public void ACameraOriginalIsNotHeldIncludingAnEditedCopyThatKeptItsMetadata()
    {
        Assert.Null(CameraOriginal.Problem(PhoneImages.Jpeg(), "PXL_20250324_004445754.MP~2.jpg", "001_PXL_20250324_004445754.MP-2.jpg"));
        Assert.Null(CameraOriginal.Problem(PhoneImages.Jpeg(), null, "20260329_183028.jpg"));
    }
}
