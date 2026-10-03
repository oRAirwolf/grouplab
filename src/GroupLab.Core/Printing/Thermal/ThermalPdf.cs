using System.Globalization;
using System.IO.Compression;
using System.Text;
using GroupLab.Core.Rendering;

namespace GroupLab.Core.Printing.Thermal;

/// <summary>
/// The thermal print as a PDF, NOTES-FROM-PLANNING.md entry 358 section 4: the fallback for a thermal printer GroupLab cannot reach itself,
/// through the system's own print dialog. Each page holds the one-bit image the head will print, at actual size and one image pixel to a dot,
/// so a driver at the printer's own resolution has nothing to resample, and the viewer is asked for no scaling as every GroupLab PDF is.
/// </summary>
public static class ThermalPdf
{
    public static byte[] Write(IReadOnlyList<(Scene Page, ThermalPage Print)> pages, PrintHead head)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(head);
        if (pages.Count == 0)
        {
            throw new ArgumentException("A PDF needs a page.", nameof(pages));
        }

        var inv = CultureInfo.InvariantCulture;
        using var output = new MemoryStream();
        var offsets = new List<long>();
        void Text(string s) => output.Write(Encoding.ASCII.GetBytes(s));
        void Begin(int number)
        {
            while (offsets.Count < number)
            {
                offsets.Add(0);
            }

            offsets[number - 1] = output.Position;
            Text(string.Create(inv, $"{number} 0 obj\n"));
        }

        Text("%PDF-1.7\n");
        output.Write([0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A]);
        Begin(1);
        Text("<< /Type /Catalog /Pages 2 0 R /ViewerPreferences << /PrintScaling /None >> >>\nendobj\n");
        Begin(2);
        Text("<< /Type /Pages /Kids [" + string.Join(" ", Enumerable.Range(0, pages.Count).Select(i => string.Create(inv, $"{3 + (3 * i)} 0 R"))) +
            string.Create(inv, $"] /Count {pages.Count} >>\nendobj\n"));

        for (int i = 0; i < pages.Count; i++)
        {
            var (page, print) = pages[i];
            int pageObject = 3 + (3 * i), content = pageObject + 1, image = pageObject + 2;
            double widthPt = page.Width / PrintFit.UnitsPerInch * 72, heightPt = page.Height / PrintFit.UnitsPerInch * 72;
            double imageWidth = print.Image.Width / head.DotsPerInch * 72, imageHeight = print.Image.Height / head.DotsPerInch * 72;
            double left = print.CutLeft / head.DotsPerInch * 72;
            Begin(pageObject);
            Text(string.Create(inv, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {widthPt:0.####} {heightPt:0.####}] /Resources << /XObject << /Dots {image} 0 R >> >> /Contents {content} 0 R >>\nendobj\n"));

            // The image is placed so that its top edge is the page's: the head prints from the top of the label.
            string draw = string.Create(inv, $"q {imageWidth:0.####} 0 0 {imageHeight:0.####} {left:0.####} {heightPt - imageHeight:0.####} cm /Dots Do Q\n");
            Begin(content);
            Text(string.Create(inv, $"<< /Length {draw.Length} >>\nstream\n{draw}endstream\nendobj\n"));

            byte[] packed = Deflate(print.Image.Bits);
            Begin(image);
            Text(string.Create(inv, $"<< /Type /XObject /Subtype /Image /Width {print.Image.Width} /Height {print.Image.Height} /ColorSpace /DeviceGray /BitsPerComponent 1 /Decode [1 0] /Interpolate false /Filter /FlateDecode /Length {packed.Length} >>\nstream\n"));
            output.Write(packed);
            Text("\nendstream\nendobj\n");
        }

        long xref = output.Position;
        var table = new StringBuilder(string.Create(inv, $"xref\n0 {offsets.Count + 1}\n0000000000 65535 f \n"));
        foreach (long offset in offsets)
        {
            table.Append(string.Create(inv, $"{offset:0000000000} 00000 n \n"));
        }

        table.Append(string.Create(inv, $"trailer\n<< /Size {offsets.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n"));
        Text(table.ToString());
        return output.ToArray();
    }

    private static byte[] Deflate(byte[] data)
    {
        using var packed = new MemoryStream();
        using (var z = new ZLibStream(packed, CompressionLevel.Optimal, leaveOpen: true))
        {
            z.Write(data);
        }

        return packed.ToArray();
    }
}
