using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Analysis;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 229 section 4: Alan's 2026-09-26 6.5 Creedmoor sheet, every shot about 0.8 in high of its own bull with bulls
/// 1.5 in apart, so each lands nearer the bull above it. One-to-one matching gave 5 of the 23 found shots to the wrong bull and a mean radius
/// of 0.591 in. Every shot off by the same amount is the rifle's zero, so the sheet is assigned in the frame moved back by it: every shot on
/// its own bull, a mean radius near 0.21 in, the offset said, and nearest bull as the undo.
/// </summary>
public class WholeSheetWrongBullTests
{
    private const string File = "load-sheet-6.5-wrong-bull-2026-09-26.png";
    private const string Rebuilt = "1f43bab71b79c3005e4213a1e4b9b9eb1f015459a982fdbf4a2d2878fdc985fe";

    [Fact]
    public void EveryShotOffByTheSameAmountGoesToTheBullItWasFiredAt()
    {
        string path = TestData.Path(File, @"C:\Dev\grouplab-originals\range-2026-09-26");
        if (!System.IO.File.Exists(path))
        {
            path = @"C:\Dev\grouplab-originals\range-2026-09-26\6.5.creedmoor09262026.png";
            if (!System.IO.File.Exists(path))
            {
                Assert.True(true, "skipped: the 2026-09-26 6.5 sheet is not on this machine");
                return;
            }
        }
        else
        {
            Assert.True(TestData.Matches(path, Rebuilt), $"{path} is not the published copy of the 6.5 sheet");
        }

        var definition = GltdJsonReader.ReadFile(Repo.PathTo("targets", "GL-CF25-LTR-D.gltd.json")).Definition!;
        var (grey, metadata) = ImageLoader.Load(path);
        var (value, _) = ImageLoader.LoadMaxChannel(path);
        var result = AutomaticMarking.Run(grey, value, metadata, definition, new OpenCvSharpBackend(), calibre: Calibre.Of(0.264));
        Assert.Null(result.Failure);
        Assert.StartsWith("All shots are about 0.7", result.Assignment!.Reason, StringComparison.Ordinal);

        var session = new MarkingSession();
        session.LoadDetections(result.Scale!, result.Bulls, result.Detections, result.Assignment, result.Rejected ?? [], result.Summary);
        var offsets = GroupAnalysis.CompositeOffsets(session.State, session.State.Shots.Where(s => s.IsShot).ToList());
        Assert.All(offsets, o => Assert.True(o.Y < -0.3 && o.Y > -1.2, $"a shot {o.Y:0.00} in from its bull up and down"));
        var figures = GroupAnalysis.Analyse(session.State).AllShots!;
        Assert.InRange(figures.MeanRadius!.Value, 0.15, 0.3);
        Assert.NotNull(session.WholeSheetShift());

        // The undo: nearest bull puts them back where one-to-one matching had them.
        session.SetAssignmentRule(AssignmentRule.Nearest);
        Assert.Null(session.WholeSheetShift());
        Assert.True(GroupAnalysis.Analyse(session.State).AllShots!.MeanRadius!.Value > 0.4);
    }
}
