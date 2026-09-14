using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// docs/SCAN-MEASUREMENTS.md section 3.1's reasons for the primitive, on a drawn page at 600 DPI: a hole with a dark rim and
/// a grey core is kept, and printed strokes narrower than the opening disk, a ring and a straight rule, are erased.
/// </summary>
public class NeutralDarknessHoleDetectorTests
{
    [Fact]
    public void AHoleIsKeptAndPrintedStrokesAreErased()
    {
        const int size = 1200;
        const double dpi = 600;
        var pixels = new byte[size * size];
        Array.Fill(pixels, (byte)250);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double fromHole = Math.Sqrt(Math.Pow(x - 400, 2) + Math.Pow(y - 600, 2));
                double ring = Math.Sqrt(Math.Pow(x - 820, 2) + Math.Pow(y - 600, 2));

                // A printed ring stroke 30 px, 0.05 in, wide, and a printed rule 30 px wide, both black.
                if (Math.Abs(ring - 250) <= 15 || Math.Abs(y - 150) <= 15)
                {
                    pixels[(y * size) + x] = 20;
                }

                // A hole of 0.27 in: a dark rim 40 px thick around a grey core, as the survey measured them.
                if (fromHole <= 80)
                {
                    pixels[(y * size) + x] = fromHole >= 40 ? (byte)40 : (byte)200;
                }
            }
        }

        var detection = NeutralDarknessHoleDetector.Detect(new GrayImage(size, size, pixels), dpi, new OpenCvSharpBackend());

        var hole = Assert.Single(detection.Holes);
        Assert.True(Math.Abs(hole.X - 400) < 1 && Math.Abs(hole.Y - 600) < 1, $"centre ({hole.X}, {hole.Y})");
        Assert.True(Math.Abs(hole.DiameterInches - (160 / dpi)) < 0.01, $"diameter {hole.DiameterInches} in");
    }
}
