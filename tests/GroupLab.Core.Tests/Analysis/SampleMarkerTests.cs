using GroupLab.Cli;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Analysis;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 164 section 2: the published sample reads 33 of its 34 markers, and the one it misses is known.
/// <para>
/// The first macOS report read 33 of 34 on the package's own self-test sheet. Windows reads the same from the same file, so it is not a
/// platform difference in decoding or arithmetic. The missed one is marker 28, at the lower left above the load block, printed with
/// horizontal white streaks across it by the printer's banding, so its border is not a closed square. No hole is near it.
/// </para>
/// <para>
/// CI runs this on Windows, Linux and macOS, which is the three-platform comparison the section asked for, repeated on every push: were
/// one platform ever to read 34 or 32 from the same pixels, that would be a real defect and this is where it would show.
/// </para>
/// </summary>
public class SampleMarkerTests
{
    [Fact]
    public void TheSampleReadsThirtyThreeOfItsThirtyFourMarkersAndMissesMarkerTwentyEight()
    {
        string path = Path.Combine(Repo.PathTo("samples"), "gl-cf25-ltr-d-25-shots-600-dpi.png");
        var result = AnalyzeVerb.Analyze(GroupLab.Tests.Support.Temp.Readable(path), null, out string? failure, [Repo.PathTo("targets")]);
        Assert.Null(failure);
        var automatic = result!.Automatic;

        Assert.Equal(34, automatic.Scale!.MarkersExpected);
        Assert.Equal(33, automatic.Scale.MarkersFound);

        // The one not read is marker 28's, 0.512 in from the left and 9.142 in down, within a hundredth of an inch.
        var missing = Assert.Single(automatic.MissingMarkers);
        var page = automatic.Scale.Mapping.ToPage(missing);
        Assert.InRange(page.X / 254, 0.50, 0.53);
        Assert.InRange(page.Y / 254, 9.13, 9.16);

        Assert.True(automatic.Measurement.Registration!.RmsResidual / 254 < 0.003, "the sample registers at 0.0026 in on 33 markers");
    }
}
