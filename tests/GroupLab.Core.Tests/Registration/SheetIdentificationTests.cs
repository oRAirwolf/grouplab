using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Tests.Registration;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 35 section 6 item 3: a sheet names its own definition through the GLTD-B frame in its printed codes, read
/// off printed scans, a photograph and fresh renders on every platform CI runs, and nothing is chosen when the codes do not settle it.
/// </summary>
public class SheetIdentificationTests
{
    private static readonly IReadOnlyList<TargetDefinition> Library = SheetIdentification.Candidates([Repo.PathTo("targets")]);

    private static string Id(TargetDefinition definition) => GltdBinary.Encode(definition).Encoding!.DefinitionId;

    private static byte[] Frame(string file, byte tile = 0) => GltdBinary.ReplicatedFrame(GltdBinary.Encode(BuiltIns.Load(file)).Encoding!, tile);

    [Theory]
    [InlineData("gl-cf25-ltr-1-300-dpi.png", "GL-YCSK-DZZ1-R0VJ-4T5Y", 0)]
    [InlineData("gl-cf25-ltr-d-blank-600-dpi.png", "GL-R0T0-384Z-HRBE-M0EW", 0)]
    [InlineData("gl-lr300-t-3-300-dpi.png", "GL-G8JP-FF4D-AE0T-GPMN", 2)]
    [InlineData("main_flat1.jpg", "GL-YCSK-DZZ1-R0VJ-4T5Y", 0)]
    public void APrintedSheetNamesTheFrozenDefinitionItWasPrintedFrom(string file, string id, int tile)
    {
        var (grey, _) = Cli.Imaging.ImageLoader.Load(Repo.PathTo("scans", "phase0", file));
        var trace = new TraceRecorder();
        var identity = SheetIdentification.Identify(grey, Library, new OpenCvSharpBackend(), trace);

        Assert.True(identity.Failure is null, identity.Failure is null ? null : identity.Failure + CodeDiagnostics.Describe(grey));
        Assert.Equal(id, identity.DefinitionId);
        Assert.Equal(id, Id(identity.Definition!));
        Assert.Equal(tile, identity.TileIndex);
        Assert.Contains(trace.Records, r => r.Stage == "S0.identify" && r.Status == StageStatus.Ok);
    }

    [Theory]
    [InlineData("GL-CF25-LTR.gltd.json", 0)]
    [InlineData("GL-RF25-A4.gltd.json", 0)]
    [InlineData("GL-ZERO-MIL-100M.gltd.json", 0)]
    [InlineData("GL-LR300-T.gltd.json", 3)]
    public void ARenderedBuiltInNamesItselfAndItsTile(string file, int page)
    {
        var definition = BuiltIns.Load(file);
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[page], 300);
        var identity = SheetIdentification.Identify(render, Library, new OpenCvSharpBackend(), new TraceRecorder());

        Assert.True(identity.Failure is null, identity.Failure is null ? null : identity.Failure + CodeDiagnostics.Describe(render));
        Assert.Equal(Id(definition), identity.DefinitionId);
        Assert.Equal(page, identity.TileIndex);
    }

    [Fact]
    public void CodesThatNameTwoDefinitionsOrTwoTilesAreRefusedRatherThanChosenBetween()
    {
        var blank = new GrayImage(8, 8, new byte[64]);
        var two = SheetIdentification.Identify(blank, Library, new Codes(Frame("GL-CF25-LTR.gltd.json"), Frame("GL-RF25-LTR.gltd.json")), new TraceRecorder());
        Assert.Null(two.Definition);
        Assert.Contains(Id(BuiltIns.Load("GL-CF25-LTR.gltd.json")), two.Failure, StringComparison.Ordinal);
        Assert.Contains(Id(BuiltIns.Load("GL-RF25-LTR.gltd.json")), two.Failure, StringComparison.Ordinal);

        var tiles = SheetIdentification.Identify(blank, Library, new Codes(Frame("GL-LR300-T.gltd.json", 0), Frame("GL-LR300-T.gltd.json", 1)), new TraceRecorder());
        Assert.Null(tiles.Definition);
        Assert.Contains("more than one tile", tiles.Failure, StringComparison.Ordinal);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 354 section 2: a definition on no list here is the one the codes carry, read from them, and never one
    /// that is on the list. the submitted generated diamond sheet was refused this way on the desktop although its codes read perfectly: a sheet
    /// made by the generator and printed is on no list unless it was saved, and its codes are the only copy of it.
    /// </summary>
    [Fact]
    public void ADefinitionNotAmongTheCandidatesIsReadFromItsCodesAndNotReplacedByOneThatIs()
    {
        var blank = new GrayImage(8, 8, new byte[64]);
        var candidates = new[] { BuiltIns.Load("GL-RF25-LTR.gltd.json") };
        var trace = new TraceRecorder();
        var identity = SheetIdentification.Identify(blank, candidates, new Codes(Frame("GL-CF25-LTR.gltd.json")), trace);

        string id = Id(BuiltIns.Load("GL-CF25-LTR.gltd.json"));
        Assert.Null(identity.Failure);
        Assert.True(identity.FromItsCodes);
        Assert.Equal(id, identity.DefinitionId);
        Assert.Equal(id, Id(identity.Definition!));
        Assert.Equal(BuiltIns.Load("GL-CF25-LTR.gltd.json").Bulls.Count, identity.Definition!.Bulls.Count);
        Assert.Contains(trace.Records, r => r.Stage == "S0.identify" && r.Summary.Contains("read from the codes themselves", StringComparison.Ordinal));

        // Where the library has it, the library's own definition is used, with its name and its words, as before.
        var listed = SheetIdentification.Identify(blank, Library, new Codes(Frame("GL-CF25-LTR.gltd.json")), new TraceRecorder());
        Assert.False(listed.FromItsCodes);
        Assert.Same(Library.First(d => Id(d) == id), listed.Definition);
    }

    [Fact]
    public void ADamagedFrameIsNotReadAsASheetAndEveryResolutionIsTriedFirst()
    {
        var blank = new GrayImage(8, 8, new byte[64]);
        byte[] damaged = Frame("GL-CF25-LTR.gltd.json");
        damaged[^1] ^= 0x01;
        var trace = new TraceRecorder();
        var identity = SheetIdentification.Identify(blank, Library, new Codes(damaged), trace, wholeOnlyWithoutMarkers: false);

        Assert.Null(identity.Definition);
        Assert.Equal("no code on the sheet held a valid GroupLab frame", identity.Failure);
        var stage = Assert.Single(trace.Records);
        Assert.Equal(StageStatus.Failed, stage.Status);
        // Every resolution, then entry 282's codes cut out where the markers put them, which on a picture with no markers is nothing.
        Assert.Equal(SheetIdentification.Scales.Count + 1, stage.Details.Count);

        var none = SheetIdentification.Identify(blank, Library, new Codes(), new TraceRecorder(), wholeOnlyWithoutMarkers: false);
        Assert.Equal("no code on the sheet could be read", none.Failure);
    }

    /// <summary>
    /// Entry 386 section 2, question 83 (b): with no GroupLab marker in the picture, it is read once, whole, at full size, and a code found
    /// there still names the sheet.
    /// </summary>
    [Fact]
    public void APictureWithNoMarkerIsReadOnceWholeAndItsCodeStillNamesTheSheet()
    {
        var blank = new GrayImage(8, 8, new byte[64]);
        var trace = new TraceRecorder();
        var none = SheetIdentification.Identify(blank, Library, new Codes(), trace);
        Assert.Equal("no GroupLab marker was found and no code could be read", none.Failure);
        var stage = Assert.Single(trace.Records);
        Assert.Equal(2, stage.Details.Count);
        Assert.Contains("read whole at full size only", stage.Details[0], StringComparison.Ordinal);
        Assert.StartsWith("at 1 times full resolution", stage.Details[1], StringComparison.Ordinal);

        var named = SheetIdentification.Identify(blank, Library, new Codes(Frame("GL-CF25-LTR.gltd.json")), new TraceRecorder());
        Assert.NotNull(named.Definition);
    }

    /// <summary>A backend whose only ability is to return the given code payloads at every resolution; it sees no markers.</summary>
    private sealed class Codes(params byte[][] payloads) : IImagingBackend
    {
        public IReadOnlyList<byte[]> ReadCodes(GrayImage image, double scale) => payloads;

        // No markers: entry 282's second look, codes cut out where the markers put them, finds nothing to cut.
        public MarkerDetection DetectMarkers(GrayImage image, MarkerDetectionOptions options) => new([], [], []);

        public HomographyFit FindHomography(IReadOnlyList<PointD> source, IReadOnlyList<PointD> destination, double ransacThreshold) => throw new NotSupportedException();

        public GrayImage WarpPerspective(GrayImage image, Homography transform, int width, int height) => throw new NotSupportedException();

        public GrayImage Morphology(GrayImage image, MorphologyOperation operation, int radius) => throw new NotSupportedException();

        public IReadOnlyList<ImageBlob> FilledBlobs(GrayImage binary) => throw new NotSupportedException();

        public (PointD Shift, double Response) PhaseCorrelate(GrayImage reference, GrayImage moved) => throw new NotSupportedException();
    }
}
