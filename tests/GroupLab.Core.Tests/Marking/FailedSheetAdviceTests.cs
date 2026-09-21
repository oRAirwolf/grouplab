using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 120 section 1, found on the second range day's blank sheet: a sheet chosen by name that then fails to
/// register said "0 of 34 markers found; registration needs 4", the pipeline's own words, instead of entry 115 section 4's sentence about
/// what to do next. The cause was that a failing result was built without the definition, so <see cref="DetectionAdvice"/> could not be
/// asked, and the screen fell back to the raw failure.
/// <para>
/// The material here is generated: a blank page of the sheet's size, which is what a sheet with no printing on it looks like to the
/// detector. Nothing from anybody's range day is in this test.
/// </para>
/// </summary>
public class FailedSheetAdviceTests
{
    [Fact]
    public void ASheetThatCannotBeRegisteredStillKnowsWhichSheetItWas()
    {
        var definition = GltdJsonReader.ReadFile(Repo.PathTo("targets", "GL-CF25-LTR.gltd.json")).Definition!;
        var blank = new GrayImage(2479, 3229, [.. Enumerable.Repeat((byte)242, 2479 * 3229)]);
        var result = AutomaticMarking.Run(blank, blank, ImageMetadata.ForScan(2479, 3229, 300), definition, new OpenCvSharpBackend(), new TraceRecorder());

        Assert.NotNull(result.Failure);
        Assert.Null(result.Scale);

        // The definition travels with the failure, because the sentence about what to do next names the sheet.
        Assert.NotNull(result.Definition);
        Assert.Equal(definition.Name, result.Definition!.Name);

        string advice = DetectionAdvice.Failure(result.Measurement, ImageMetadata.ForScan(2479, 3229, 300), result.Definition)!;
        Assert.NotNull(advice);
        Assert.EndsWith(".", advice, StringComparison.Ordinal);
        Assert.Contains(definition.Name, advice, StringComparison.Ordinal);

        // It says what to do, not what failed: none of the pipeline's own wording reaches a person.
        Assert.DoesNotContain("registration needs", advice, StringComparison.Ordinal);
        Assert.DoesNotContain("registration failed", advice, StringComparison.Ordinal);
    }

    /// <summary>
    /// The scanner crops a Letter sheet to 8.26 by 10.76 in, so a blank page arrives with its edges cut. It must still be the advice that
    /// is given, and the advice must still be a sentence.
    /// </summary>
    [Fact]
    public void TheAdviceIsOneFinishedSentenceWhateverWentWrong()
    {
        var definition = GltdJsonReader.ReadFile(Repo.PathTo("targets", "GL-CF25-LTR.gltd.json")).Definition!;
        foreach ((int width, int height, double dpi) in new[] { (4958, 6458, 600.0), (2479, 3229, 300.0), (600, 780, 72.0) })
        {
            var blank = new GrayImage(width, height, [.. Enumerable.Repeat((byte)242, width * height)]);
            var metadata = ImageMetadata.ForScan(width, height, dpi);
            var result = AutomaticMarking.Run(blank, blank, metadata, definition, new OpenCvSharpBackend(), new TraceRecorder());
            Assert.NotNull(result.Definition);
            string advice = DetectionAdvice.Failure(result.Measurement, metadata, result.Definition!)!;
            Assert.EndsWith(".", advice, StringComparison.Ordinal);
            Assert.DoesNotContain("  ", advice, StringComparison.Ordinal);
        }
    }
}
