using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Tests.Registration;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 356 section 6: "Try again, reading harder" does more than the first reading. A code located but not read
/// is squared from its own corners and read; a sheet whose codes are gone altogether is named by its printed identifier among
/// the sheets its markers fit; and it says each step as it starts and stops when asked.
/// </summary>
public class ReadingHarderTests
{
    private static readonly IReadOnlyList<GroupLab.Core.Gltd.Model.TargetDefinition> Library = SheetIdentification.Candidates([Repo.PathTo("targets")]);

    private static string Id(GroupLab.Core.Gltd.Model.TargetDefinition d) => GltdBinary.Encode(d).Encoding!.DefinitionId;

    /// <summary>
    /// A code the whole picture's reading cannot find, standing for one in glare or curled: the backend here reads a code only from a view
    /// cut square around it. The first reading's cut-outs come only from a layout the markers fit, and this picture has none, so it reads
    /// nothing; the harder reading squares each code it locates from its own four corners, and reads it.
    /// </summary>
    [Fact]
    public void ACodeLocatedButNotReadIsSquaredFromItsCornersAndRead()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var frame = GltdBinary.ReplicatedFrame(GltdBinary.Encode(definition).Encoding!);
        var picture = new GrayImage(900, 900, Enumerable.Repeat((byte)230, 810_000).ToArray());
        IReadOnlyList<PointD> box = [new(600, 610), new(780, 640), new(760, 820), new(590, 790)];
        var backend = new OnlySquared(frame, box);

        Assert.Null(SheetIdentification.Identify(picture, Library, backend, new TraceRecorder()).Definition);

        var steps = new List<int>();
        var harder = SheetIdentification.IdentifyHarder(picture, Library, backend, new TraceRecorder(), steps.Add);
        Assert.True(harder.Definition is not null, harder.Failure);
        Assert.Equal(Id(definition), harder.DefinitionId);
        Assert.True(harder.ReadHarder);
        Assert.Equal([0, 1], steps);
        Assert.True(backend.SquaredViews > 0);
    }

    /// <summary>Reads nothing from a whole picture; locates one code; reads it only from a square view about its size, as squaring makes.</summary>
    private sealed class OnlySquared(byte[] frame, IReadOnlyList<PointD> box) : IImagingBackend
    {
        public int SquaredViews { get; private set; }

        public MarkerDetection DetectMarkers(GrayImage image, MarkerDetectionOptions options) => new([], [], []);

        public HomographyFit FindHomography(IReadOnlyList<PointD> source, IReadOnlyList<PointD> destination, double ransacThreshold) => throw new NotSupportedException();

        public GrayImage WarpPerspective(GrayImage image, Homography transform, int width, int height) => PortableImaging.WarpPerspective(image, transform, width, height);

        public GrayImage Morphology(GrayImage image, MorphologyOperation operation, int radius) => image;

        public IReadOnlyList<ImageBlob> FilledBlobs(GrayImage binary) => [];

        public (PointD Shift, double Response) PhaseCorrelate(GrayImage reference, GrayImage moved) => (new PointD(0, 0), 0);

        public IReadOnlyList<byte[]> ReadCodes(GrayImage image, double scale) => [];

        public IReadOnlyList<byte[]> ReadCutOut(GrayImage image, double scale)
        {
            // A squared code's view is a square a third again as wide as the code, about 240 pixels here; a corner of the picture is 300.
            if (image.Width == image.Height && image.Width < 290)
            {
                SquaredViews++;
                return [frame];
            }

            return [];
        }

        public IReadOnlyList<IReadOnlyList<PointD>> LocateCodes(GrayImage image) => [box];
    }

    /// <summary>
    /// No codes printed at all, the words printed as the sheet prints them: the markers fit several library sheets, and the printed identifier
    /// says which.
    /// </summary>
    [Fact]
    public void ASheetWithNoCodesIsNamedByItsPrintedName()
    {
        var definition = BuiltIns.Load("GL-RF25-LTR.gltd.json");
        var page = SceneBuilder.Build(definition).Pages[0];
        var codeless = page with { Items = [.. page.Items.Where(i => i.Layer != SceneLayer.Codes)] };
        var render = SceneRasterizer.Rasterize(codeless, 200, words: true);
        var backend = new OpenCvSharpBackend();
        Assert.Null(SheetIdentification.Identify(render, Library, backend, new TraceRecorder()).Definition);

        var harder = SheetIdentification.IdentifyHarder(render, Library, backend, new TraceRecorder());
        Assert.True(harder.Definition is not null, harder.Failure);
        Assert.True(harder.ByPrintedName);
        Assert.Equal(Id(definition), Id(harder.Definition!));
    }

    [Fact]
    public void ItStopsWhenAsked()
    {
        var blank = new GrayImage(400, 400, Enumerable.Repeat((byte)230, 160_000).ToArray());
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        Assert.Throws<OperationCanceledException>(() => SheetIdentification.IdentifyHarder(blank, Library, new OpenCvSharpBackend(), new TraceRecorder(), cancellation: cancel.Token));
    }
}
