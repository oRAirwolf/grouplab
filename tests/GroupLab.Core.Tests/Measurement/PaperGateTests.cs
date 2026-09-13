using GroupLab.Cli.Imaging;
using GroupLab.Cli.Spike;
using GroupLab.Core.Measurement;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Measurement;

/// <summary>
/// The paper gate of PHASE0-SPIKE-BRIEF.md section 2 and DESIGN.md section 21: on the 600 DPI scan of each of the ten
/// printed sheets, every bull is located, the tile is the one the file names, and the worst bull-centre error is under
/// 0.005 in with the locator the pipeline ships.
/// </summary>
public class PaperGateTests(ITestOutputHelper output)
{
    public static TheoryData<string> GatedScans()
    {
        var data = new TheoryData<string>();
        foreach (var sample in SampleSet.All.Where(s => s.Kind == SampleSet.SampleKind.Scan && s.Gated))
        {
            data.Add(sample.File);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(GatedScans))]
    public void TheWorstBullIsWithinFiveThousandthsOfAnInch(string file)
    {
        var sample = SampleSet.All.Single(s => s.File == file);
        var shipped = new MeasureOptions();

        var result = Phase0Spike.Measure(Repo.PathTo("scans", "phase0"), Repo.PathTo("targets"), sample, new OpenCvSharpBackend(), shipped);
        var bulls = shipped.Locator == BullLocatorKind.Centroid ? result.Centroid : result.EdgeFit;
        var (mean, worst, missing) = Phase0Spike.Stats(bulls);
        output.WriteLine($"{file}: {result.Fiducials.Matches.Count}/{result.Fiducials.Expected} markers, mean {mean / 254:0.00000} in, worst {worst / 254:0.00000} in");

        Assert.Null(result.Failure);
        Assert.Equal(sample.Tile, result.Fiducials.TileIndex);
        Assert.Equal(0, missing);
        Assert.True(worst < Phase0Spike.PaperGate, $"{file}: worst bull {worst / 254:0.00000} in.");
    }
}
