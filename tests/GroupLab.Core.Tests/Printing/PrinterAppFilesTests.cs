using System.Text;
using System.Text.RegularExpressions;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Printing.Thermal;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using OpenCvSharp;

namespace GroupLab.Core.Tests.Printing;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 363 section 2a: a target handed to a thermal printer's own app. The picture is exactly the page at the
/// printer's dots, black and white only, and says its resolution; the PDF's page is the sheet's true size with one image the size of the page;
/// the darkness test is a whole Letter page. An app that honours the resolution or the page size prints it one to one.
/// </summary>
public class PrinterAppFilesTests
{
    private static Scene Page(string file) =>
        SceneBuilder.Build(GltdJsonReader.ReadFile(Repo.PathTo("targets", file)).Definition!, new RenderOptions(PrintNote: SceneBuilder.ActualSizeNote)).Pages[0];

    [Theory]
    [InlineData("GL-CF25-LTR.gltd.json", 8.5, 11)]
    [InlineData("GL-SCALE-LTR-1.gltd.json", 8.5, 11)]
    [InlineData("GL-X6-4X6.gltd.json", 4, 6)]
    public void ThePictureIsThePageAtThePrintersDotsAndSaysItsResolution(string file, double widthInches, double heightInches)
    {
        var (pngs, pdf) = PrinterAppFiles.Make([Page(file)]);
        Assert.Single(pngs);
        Assert.Equal(300, ThermalPng.Dpi(pngs[0])!.Value, 1);
        using var image = Cv2.ImDecode(pngs[0], ImreadModes.Unchanged);
        Assert.Equal(((int)Math.Round(widthInches * 300), (int)Math.Round(heightInches * 300)), (image.Width, image.Height));
        Assert.Equal(MatType.CV_8UC1, image.Type());
        image.GetArray(out byte[] pixels);
        Assert.All(pixels.Distinct(), v => Assert.True(v is 0 or 255, $"a pixel is {v}, neither black nor white"));
        Assert.Contains((byte)0, pixels);

        // The PDF's page is the sheet's true size in points, and its one image is that many dots.
        string text = Encoding.Latin1.GetString(pdf);
        var box = Regex.Match(text, @"/MediaBox \[0 0 ([\d.]+) ([\d.]+)\]");
        Assert.Equal(widthInches * 72, double.Parse(box.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), 1);
        Assert.Equal(heightInches * 72, double.Parse(box.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture), 1);
        Assert.Contains(FormattableString.Invariant($"/Width {image.Width} /Height {image.Height}"), text, StringComparison.Ordinal);
        Assert.Equal("thermal-300dpi", PrinterAppFiles.Suffix(300));
    }

    [Fact]
    public void TheDarknessTestIsAWholeLetterPageWithFiveStrips()
    {
        var (pngs, pdf) = PrinterAppFiles.DarknessPage();
        using var image = Cv2.ImDecode(pngs[0], ImreadModes.Unchanged);
        Assert.Equal((2550, 3300), (image.Width, image.Height));
        Assert.Equal(300, ThermalPng.Dpi(pngs[0])!.Value, 1);
        Assert.Contains("/MediaBox [0 0 612 792]", Encoding.Latin1.GetString(pdf), StringComparison.Ordinal);

        // Five strips down the page: rows with black in them come in five separate bands.
        int bands = 0, width = image.Width, height = image.Height;
        bool inside = false;
        image.GetArray(out byte[] pixels);
        for (int y = 0; y < height; y++)
        {
            bool black = false;
            for (int x = 0; x < 400 && !black; x++)
            {
                black = pixels[(y * width) + x] == 0;
            }

            if (black && !inside)
            {
                bands++;
            }

            inside = black;
        }

        Assert.Equal(5, bands);
    }
}
