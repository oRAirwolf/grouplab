using System.Text.Json.Nodes;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Evaluation;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Analysis;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 354: two phone photographs of GroupLab sheets shot with .22 LR at 50 yd, sent with consent to publish, read
/// as the desktop reads a phone photograph with the calibre given as 0.223 in. Each is held to its 25 holes marked by eye.
/// <para>
/// <b>The load sheet</b> was stapled crinkled to a board, its top corners torn and its bottom edge lifted. Before entry 354 the photograph
/// read 32 marks, 24 of them holes: the torn corner, the curled top edge and the print instruction at the lifted bottom edge, where the
/// board showed through, and two bulls' printed numbers, thickened by the photograph's blur. Now it reads the 24 holes and nothing else.
/// Bull 23's hole touches the marker below it and is refused with the marker, as a hole centred in a marker's zone always has been (the
/// 6.5 sheet of <see cref="PhotoAgainstScanTests"/> loses one the same way): keeping the part outside the zone found it here, and on
/// photographs of several sheets at a range it kept the board beside a sheet's edge too, so it was not kept.
/// </para>
/// <para>
/// <b>The diamond sheet</b> was made by the target generator and printed without being saved, so no definition here has its identifier, and
/// GroupLab refused it outright although its codes read perfectly. Its definition is now read from the codes themselves.
/// </para>
/// </summary>
public class TwentyTwoPhotographTests
{
    /// <summary>Where the photographs were measured, on the machine they were measured on.</summary>
    private const string MeasuredIn = @"C:\Dev\grouplab-submissions\2026-10-03_36d3e498";

    public static TheoryData<string, int, int> Photos => new()
    {
        // The photograph, the holes it must find of its 25, and the most false marks it may make.
        { "photo-22lr-load-sheet-2026-10-03.jpg", 24, 0 },
        // Four bulls' white centres and the edge of one black diamond, on the crinkled left of the sheet, are still read as marks.
        { "photo-22lr-diamond-2026-10-03.jpg", 25, 5 },
    };

    [Theory]
    [MemberData(nameof(Photos))]
    public void APhotographOfA22SheetReadsItsHoles(string file, int found, int falseMarks)
    {
        string path = TestData.Path(file, MeasuredIn);
        if (!File.Exists(path))
        {
            Assert.True(true, $"skipped: {file} is not on this machine");
            return;
        }

        var fixture = JsonNode.Parse(File.ReadAllText(Repo.PathTo("tests", "GroupLab.Core.Tests", "Fixtures", "twenty-two-photographs-2026-10-03.json")))!;
        var sheet = fixture["sheets"]!.AsArray().Single(s => (string?)s!["photo"] == file)!;
        var truth = sheet["shots"]!.AsArray().Select(p => new PointD((double)p![0]! * 254, (double)p[1]! * 254)).ToList();
        var (grey, metadata) = ImageLoader.Load(path);
        var (value, _) = ImageLoader.LoadMaxChannel(path);
        metadata = metadata with { FocalLengthMm = (double)fixture["focalLengthMm"]!, FocalLength35mm = (int)fixture["focalLength35mm"]! };

        // The sheet names itself, as the desktop finds it: from the library, or for the generated sheet from its own codes.
        var backend = new OpenCvSharpBackend();
        var identity = SheetIdentification.Identify(grey, SheetIdentification.Candidates([Repo.PathTo("targets")]), backend, new GroupLab.Core.Trace.TraceRecorder());
        Assert.True(identity.Definition is not null, identity.Failure);
        Assert.Equal(sheet["sheet"] is null, identity.FromItsCodes);

        var result = AutomaticMarking.Run(grey, value, metadata, identity.Definition!, backend, calibre: Calibre.Of((double)fixture["calibre"]!));
        Assert.Null(result.Failure);
        var marks = result.Detections.Select(d => result.Scale!.Mapping.ToPage(d.Image)).ToList();
        var (hits, extra, _) = Scoreboard.Match(truth, marks, Scoreboard.FoundWithinInches * 254);
        Assert.True(hits >= found, $"{file}: {hits} of 25 holes found, at least {found} expected");
        Assert.True(extra <= falseMarks, $"{file}: {extra} false marks, at most {falseMarks} expected");
    }
}
