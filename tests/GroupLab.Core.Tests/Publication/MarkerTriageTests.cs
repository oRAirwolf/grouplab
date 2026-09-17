using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Publication;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 76 section 4: intake's marker count finds the markers of a sheet photographed from a distance. A rendered
/// GL-CF25-LTR with markers about 36 px across, placed on a 6000 by 4500 frame, is the shape of submission 3a493942's wide frames, whose
/// markers were 33 to 53 px on a 5712 px side and which the old three guesses read as 0 to 3 markers.
/// </summary>
public class MarkerTriageTests
{
    [Fact]
    public void ASheetSmallInTheFrameIsCountedFromItsOwnMarkers()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var sheet = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], 228);
        const int width = 6000, height = 4500;
        var pixels = new byte[width * height];
        Array.Fill(pixels, (byte)255);
        int left = 1200, top = (height - sheet.Height) / 2;
        for (int y = 0; y < sheet.Height; y++)
        {
            Array.Copy(sheet.Pixels, y * sheet.Width, pixels, ((top + y) * width) + left, sheet.Width);
        }

        var frame = new GrayImage(width, height, pixels);
        int count = MarkerTriage.Count(frame, new OpenCvSharpBackend());

        int printed = PageRegistration.ExpectedMarkers(definition, 0).Count;
        Assert.True(count >= printed - 2, $"{count} of {printed} markers counted");
    }
}
