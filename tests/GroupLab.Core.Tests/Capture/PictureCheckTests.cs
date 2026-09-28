using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Capture;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 260, after the Fold 7's camera test of 2026-09-28: a white sheet on an off-white counter is judged from its
/// own markers, the sheet is found from its markers when its codes are too small to read, and every picture is checked afterwards, a retake
/// asked for only where GroupLab cannot measure.
/// </summary>
public class PictureCheckTests
{
    private static readonly ImageMetadata Camera = new("JPEG", 1, 1, null, null, "TestMake", "TestPhone", 1, 6.25, 24);

    /// <summary>GL-CF25-LTR at <paramref name="dpi"/>, its paper brought to a photograph's 225, on a board of <paramref name="board"/>.</summary>
    private static GrayImage OnBoard(double dpi, byte board, int border = 120)
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        int w = render.Width + (2 * border), h = render.Height + (2 * border);
        var pixels = new byte[w * h];
        Array.Fill(pixels, board);
        for (int y = 0; y < render.Height; y++)
        {
            for (int x = 0; x < render.Width; x++)
            {
                pixels[((y + border) * w) + x + border] = (byte)(render.Pixels[(y * render.Width) + x] * 225 / 255);
            }
        }

        return new GrayImage(w, h, pixels);
    }

    /// <summary>
    /// The Fold 7's failure, reproduced: the paper at 225 on a counter at 205, which the outline search cannot tell apart. The frame is judged
    /// from the markers, and the sheet, whole and in view, is ready; the screen said "Move back" to it for five minutes.
    /// </summary>
    [Fact]
    public void ASheetOnAnOffWhiteCounterIsNotToldToMoveBack()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var frame = OnBoard(150, 205);
        var verdict = CaptureGuidance.JudgeFrame(frame, Camera with { Width = frame.Width, Height = frame.Height }, definition, new OpenCvSharpBackend(), codesRead: 2);
        Assert.NotEqual(Instruction.MoveBack, verdict.Say);
        Assert.Equal(Instruction.Ready, verdict.Say);
        Assert.True(verdict.MarkersRead >= 0.9 * verdict.MarkersExpected!.Value, $"{verdict.MarkersRead} of {verdict.MarkersExpected} markers");
        Assert.Equal(definition.Codes!.Positions.Count, verdict.CodesExpected);
        Assert.True(verdict.Evenness > 0.95, $"evenness {verdict.Evenness}");
    }

    /// <summary>
    /// A frame too small for the codes: at 100 dpi a code's module is 1.6 pixels and nothing reads it, but the markers still name a sheet
    /// with the right layout, so the guidance can go on; and when they are too small to read the codes from, it says to move closer.
    /// </summary>
    [Fact]
    public void TheSheetIsFoundFromItsMarkersWhenTheCodesAreTooSmall()
    {
        var library = BuiltIns.Files.Select(BuiltIns.Load).ToList();
        var frame = OnBoard(100, 60, 40);
        var search = LiveSheet.Find(frame, library, new OpenCvSharpBackend());
        Assert.True(search.MarkersFound >= LiveSheet.LeastMarkers, $"{search.MarkersFound} markers");
        Assert.NotNull(search.Definition);
        Assert.False(search.FromCodes);
        Assert.Equal(BuiltIns.Load("GL-CF25-LTR.gltd.json").Page, search.Definition!.Page);

        var small = CaptureGuidance.Search(search with { Definition = null, MedianSidePixels = LiveSheet.ReadableSidePixels - 1 }, null, SheetOutline.OutOfFrame);
        Assert.Equal(Instruction.MoveCloser, small.Say);
        var none = CaptureGuidance.Search(new LiveSearch(null, false, 0, 0, 0), null, SheetOutline.OutOfFrame);
        Assert.Equal(Instruction.MoveBack, none.Say);
        Assert.Contains("whole sheet", none.Words, StringComparison.Ordinal);
    }

    private static PictureVerdict Check(GrayImage image, bool torch = false)
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var metadata = Camera with { Width = image.Width, Height = image.Height };
        var result = AutomaticMarking.Run(image, image, metadata, definition, new OpenCvSharpBackend());
        return PictureCheck.Of(image, definition, result, definition.Codes!.Positions.Count, torch);
    }

    /// <summary>A clean picture is good, with nothing to say but what was fine.</summary>
    [Fact]
    public void ACleanPictureIsGood()
    {
        var verdict = Check(OnBoard(150, 60));
        Assert.True(verdict.CanMeasure, verdict.Describe());
        Assert.Equal(PictureBand.Good, verdict.Band);
        Assert.Contains(verdict.Fine, f => f.EndsWith("codes read", StringComparison.Ordinal) && f.Split(" of ")[0] == f.Split(" of ")[1].Split()[0]);
        Assert.Contains(verdict.Fine, f => f.EndsWith("tags read", StringComparison.Ordinal));
        Assert.Contains("no torch", verdict.Fine);
    }

    /// <summary>
    /// A shadow across the bottom row, bulls 21 to 25, dims their paper to 60 percent: the picture is still measured, the note names those
    /// bulls and says it was evened out, and the band is amber or green, never red, because retaking is the last resort.
    /// </summary>
    [Fact]
    public void AShadowAcrossARowIsNamedAndNotARetake()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var image = OnBoard(150, 60);
        const int border = 120;
        double perDmm = 150 / 254.0;
        int top = border + (int)((definition.Bulls.Where(b => b.Scoring).Skip(20).Min(b => b.Y) - 220) * perDmm);
        int bottom = border + (int)((definition.Bulls.Where(b => b.Scoring).Skip(20).Max(b => b.Y) + 220) * perDmm);
        for (int y = top; y < bottom; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                image.Pixels[(y * image.Width) + x] = (byte)(image.Pixels[(y * image.Width) + x] * 6 / 10);
            }
        }

        var verdict = Check(image);
        Assert.True(verdict.CanMeasure, verdict.Describe());
        Assert.NotEqual(PictureBand.Retake, verdict.Band);
        Assert.True(verdict.Score >= 40, verdict.Describe());
        var note = Assert.Single(verdict.Notes, n => n.Words.StartsWith("A shadow falls across bulls", StringComparison.Ordinal));
        Assert.Contains("21 to 25", note.Words, StringComparison.Ordinal);
        Assert.Contains("evened out", note.Words, StringComparison.Ordinal);
        Assert.NotNull(note.Outline);
        Assert.DoesNotContain("light even", verdict.Fine);
        Assert.Equal("Good with notes", verdict.Verdict);
    }

    /// <summary>With no sheet named, the only verdict is a retake, in red, and it says what to do.</summary>
    [Fact]
    public void AnUnreadSheetIsARetakeInRed()
    {
        var verdict = PictureCheck.Of(OnBoard(40, 60, 20), null, null, 0, torch: true);
        Assert.False(verdict.CanMeasure);
        Assert.Equal(PictureBand.Retake, verdict.Band);
        Assert.True(verdict.Score < 40);
        Assert.Equal("Retake", verdict.Verdict);
        Assert.Equal("Retake", verdict.BandWord);
        Assert.Contains("square codes", verdict.Notes[0].Words, StringComparison.Ordinal);
        Assert.Contains("torch on", verdict.Fine);
    }

    [Fact]
    public void BullNumbersReadAsAPersonSaysThem()
    {
        Assert.Equal("11 to 15", PictureCheck.Numbers([15, 11, 12, 13, 14]));
        Assert.Equal("3", PictureCheck.Numbers([3]));
        Assert.Equal("3, 4 and 9", PictureCheck.Numbers([3, 4, 9]));
        Assert.Equal("1 to 3 and 7 to 9", PictureCheck.Numbers([1, 2, 3, 7, 8, 9]));
    }
}
