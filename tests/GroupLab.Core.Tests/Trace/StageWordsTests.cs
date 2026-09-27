using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Tests.Trace;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 243 section 3.2: the progress line's words follow the pipeline's own stages, so a stage renamed without the
/// words would leave a person looking at a line that never moves. Every stage the words name is one a real run files, in the order given.
/// </summary>
public class StageWordsTests
{
    [Fact]
    public void EveryStageTheProgressLineNamesIsOneARealRunFiles()
    {
        string sample = Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png");
        var (grey, metadata) = ImageLoader.Load(sample, 8);
        var (value, _) = ImageLoader.LoadMaxChannel(sample, 8);
        var trace = new TraceRecorder();
        var definition = GltdJsonReader.ReadFile(Repo.PathTo("targets", "GL-CF25-LTR-D.gltd.json")).Definition!;
        var result = AutomaticMarking.Run(grey, value, metadata, definition, new OpenCvSharpBackend(), trace);
        Assert.Null(result.Failure);
        var filed = trace.Records.Select(r => r.Stage).ToList();
        foreach (string stage in new[] { "S3.register", "P0.bulls", "S5-S8.holes" })
        {
            Assert.Contains(stage, filed);
            Assert.NotNull(StageWords.After(stage));
        }

        Assert.True(filed.IndexOf("S3.register") < filed.IndexOf("P0.bulls") && filed.IndexOf("P0.bulls") < filed.IndexOf("S5-S8.holes"), string.Join(", ", filed));
    }
}
