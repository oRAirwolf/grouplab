using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Rendering.Markers;

namespace GroupLab.Core.Tests.Imaging;

/// <summary>
/// The embedded tag36h11 table, transcribed from libapriltag, against OpenCV's own DICT_APRILTAG_36h11: all 587 codes
/// drawn from <see cref="Tag36h11"/> must decode to their own ids with corners in printed order, top-left first. A
/// table in another bit order or rotation could still decode, but to the wrong ids or with the corners turned, and
/// registration against the definition would then be wrong without failing. This test is how OpenCV's dictionary was
/// found to hold every code turned 180 degrees from libapriltag's, which the backend corrects.
/// </summary>
public class OpenCvMarkerTableTests
{
    [Fact]
    public void EveryTag36h11CodeDecodesToItsOwnIdWithCornersInPrintedOrder()
    {
        const int module = 10, cell = 12 * module, columns = 25;
        int rows = (Tag36h11.CodeCount + columns - 1) / columns;
        int width = columns * cell, height = rows * cell;
        var pixels = new byte[width * height];
        Array.Fill(pixels, (byte)255);
        for (int id = 0; id < Tag36h11.CodeCount; id++)
        {
            var (left, top) = Origin(id);
            var inked = Tag36h11.InkedModules(id);
            for (int y = 0; y < 8 * module; y++)
            {
                for (int x = 0; x < 8 * module; x++)
                {
                    if (inked[y / module, x / module])
                    {
                        pixels[((top + y) * width) + left + x] = 0;
                    }
                }
            }
        }

        var detected = new OpenCvSharpBackend().DetectMarkers(new GrayImage(width, height, pixels), new MarkerDetectionOptions(MarkerFamily.AprilTag36h11, 8 * module)).Markers;

        Assert.Equal(Enumerable.Range(0, Tag36h11.CodeCount), detected.Select(m => m.Id).Order());
        foreach (var marker in detected)
        {
            // OpenCV puts pixel centres on integers, so the square's outer edge is half a pixel before its first pixel.
            var (left, top) = Origin(marker.Id);
            double l = left - 0.5, t = top - 0.5, s = 8 * module;
            PointD[] expected = [new(l, t), new(l + s, t), new(l + s, t + s), new(l, t + s)];
            for (int k = 0; k < 4; k++)
            {
                double error = Math.Sqrt(Math.Pow(marker.Corners[k].X - expected[k].X, 2) + Math.Pow(marker.Corners[k].Y - expected[k].Y, 2));
                Assert.True(error < 1.0, $"Marker {marker.Id} corner {k} is at {marker.Corners[k]}, expected {expected[k]}.");
            }
        }

        static (int Left, int Top) Origin(int id) => ((id % columns * cell) + (2 * module), (id / columns * cell) + (2 * module));
    }
}
