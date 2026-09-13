using System.Globalization;
using System.Text;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Pdf;
using PDFtoImage;
using SkiaSharp;

namespace GroupLab.Core.Tests.Support;

/// <summary>PDFium is not thread-safe, so every test that rasterises runs in this one serialised collection.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PdfiumCollection
{
    public const string Name = "PDFium";
}

internal static class BuiltIns
{
    public static IEnumerable<string> Files =>
        Directory.EnumerateFiles(Repo.PathTo("targets"), "*.gltd.json").Select(Path.GetFileName).Order()!;

    public static TargetDefinition Load(string file)
    {
        var read = GltdJsonReader.Read(File.ReadAllBytes(Repo.PathTo("targets", file)));
        Assert.Empty(read.Diagnostics);
        return read.Definition!;
    }
}

/// <summary>A greyscale raster of a square region of a rendered page, sampled in dmm around a page point.</summary>
internal sealed class Raster
{
    private readonly float[] _luminance;

    private Raster(float[] luminance, int width, int height, double originX, double originY, double pixelsPerDmm)
    {
        _luminance = luminance;
        Width = width;
        Height = height;
        OriginX = originX;
        OriginY = originY;
        PixelsPerDmm = pixelsPerDmm;
    }

    public int Width { get; }

    public int Height { get; }

    public double OriginX { get; }

    public double OriginY { get; }

    public double PixelsPerDmm { get; }

    /// <summary>
    /// Rasterises at least the square of side <paramref name="sideDmm"/> centred on (<paramref name="centreX"/>,
    /// <paramref name="centreY"/>) dmm of <paramref name="page"/>, at <paramref name="dpi"/>, widened outward to whole inches.
    /// </summary>
    /// <remarks>
    /// Two PDFium behaviours were measured and are avoided. PDFtoImage's <c>Bounds</c> crop placed the raster between 0
    /// and 1.3 px off, varying with the crop origin; and a MediaBox whose edges are not whole points renders up to half a
    /// pixel off. A whole-page render of the same bytes put every bull within 0.02 px. So the crop is made the PDF way, the
    /// page's own content stream exactly as <see cref="PdfWriter"/> writes it under a MediaBox that is the crop, with every
    /// edge on a whole inch: a whole number of points, and of device pixels at any integer DPI. A whole page at 600 DPI is
    /// not an option on the roll sheets, at over a gigabyte of bitmap.
    /// </remarks>
    public static Raster Render(Scene page, double centreX, double centreY, double sideDmm, int dpi, SKColor? background = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        const double dmmPerInch = 254.0;
        double pageHeight = page.Height / 508.0;

        // PDF space runs up from the bottom edge of the page, in inches here.
        int left = (int)Math.Floor((centreX - (sideDmm / 2)) / dmmPerInch);
        int bottom = (int)Math.Floor(pageHeight - ((centreY + (sideDmm / 2)) / dmmPerInch));
        int side = (int)Math.Ceiling(Math.Max(
            ((centreX + (sideDmm / 2)) / dmmPerInch) - left,
            pageHeight - ((centreY - (sideDmm / 2)) / dmmPerInch) - bottom));

        var options = new PDFtoImage.RenderOptions { Dpi = dpi, BackgroundColor = background ?? SKColors.White };
        using var bitmap = Conversion.ToImage(CroppedPdf(page, left, bottom, side), 0, options: options);
        Assert.True(Math.Abs(bitmap.Width - (side * dpi)) <= 1 && Math.Abs(bitmap.Height - (side * dpi)) <= 1,
            $"Raster is {bitmap.Width} by {bitmap.Height} px, expected {side * dpi} square.");

        var pixels = bitmap.Pixels;
        var luminance = new float[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            luminance[i] = (0.299f * pixels[i].Red) + (0.587f * pixels[i].Green) + (0.114f * pixels[i].Blue);
        }

        double originY = (pageHeight - bottom - side) * dmmPerInch;
        return new Raster(luminance, bitmap.Width, bitmap.Height, left * dmmPerInch, originY, dpi / dmmPerInch)
        {
            Colours = pixels,
        };
    }

    /// <summary>Wraps a grey image whose pixel (0, 0) starts at (<paramref name="originX"/>, <paramref name="originY"/>) dmm.</summary>
    public static Raster FromImage(GrayImage image, double originX, double originY, double pixelsPerDmm)
    {
        ArgumentNullException.ThrowIfNull(image);
        return new Raster([.. image.Pixels.Select(p => (float)p)], image.Width, image.Height, originX, originY, pixelsPerDmm);
    }

    /// <summary>A whole page of a PDF rasterised by PDFium, as grey. Every built-in ink is black, so one channel carries it.</summary>
    public static GrayImage WholePage(byte[] pdf, int page, int dpi)
    {
        using var bitmap = Conversion.ToImage(pdf, page, options: new PDFtoImage.RenderOptions { Dpi = dpi, BackgroundColor = SKColors.White });
        var source = bitmap.GetPixelSpan();
        int channels = bitmap.BytesPerPixel;
        var grey = new byte[bitmap.Width * bitmap.Height];
        for (int i = 0; i < grey.Length; i++)
        {
            grey[i] = source[(i * channels) + 1];
        }

        return new GrayImage(bitmap.Width, bitmap.Height, grey);
    }

    public SKColor[] Colours { get; private init; } = [];

    /// <summary>Ink coverage, 0 for white and 1 for black, at a page point in dmm, bilinear between pixel centres.</summary>
    public double Ink(double x, double y)
    {
        double u = ((x - OriginX) * PixelsPerDmm) - 0.5, v = ((y - OriginY) * PixelsPerDmm) - 0.5;
        int u0 = (int)Math.Floor(u), v0 = (int)Math.Floor(v);
        double fu = u - u0, fv = v - v0;
        double l = ((1 - fu) * (1 - fv) * At(u0, v0)) + (fu * (1 - fv) * At(u0 + 1, v0)) + ((1 - fu) * fv * At(u0, v0 + 1)) + (fu * fv * At(u0 + 1, v0 + 1));
        return 1 - (l / 255.0);
    }

    public SKColor ColourAt(double x, double y)
    {
        int u = (int)Math.Floor((x - OriginX) * PixelsPerDmm), v = (int)Math.Floor((y - OriginY) * PixelsPerDmm);
        return Colours[(Math.Clamp(v, 0, Height - 1) * Width) + Math.Clamp(u, 0, Width - 1)];
    }

    /// <summary>A one-page PDF of <paramref name="page"/>'s content stream whose MediaBox is the given square, in whole inches of PDF space.</summary>
    private static byte[] CroppedPdf(Scene page, int left, int bottom, int side)
    {
        string content = PdfWriter.Content(page);
        string box = string.Create(CultureInfo.InvariantCulture, $"{left * 72} {bottom * 72} {(left + side) * 72} {(bottom + side) * 72}");
        string[] objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [4 0 R] /Count 1 >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            $"<< /Type /Page /Parent 2 0 R /MediaBox [{box}] /Resources << /Font << /F1 3 0 R >> >> /Contents 5 0 R >>",
            $"<< /Length {Encoding.Latin1.GetByteCount(content)} >>\nstream\n{content}\nendstream",
        ];

        var pdf = new StringBuilder("%PDF-1.7\n");
        var offsets = new List<int>();
        for (int i = 0; i < objects.Length; i++)
        {
            offsets.Add(Encoding.Latin1.GetByteCount(pdf.ToString()));
            pdf.Append(CultureInfo.InvariantCulture, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        int xref = Encoding.Latin1.GetByteCount(pdf.ToString());
        pdf.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (int offset in offsets)
        {
            pdf.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        }

        pdf.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.Latin1.GetBytes(pdf.ToString());
    }

    private float At(int u, int v) => _luminance[(Math.Clamp(v, 0, Height - 1) * Width) + Math.Clamp(u, 0, Width - 1)];
}
