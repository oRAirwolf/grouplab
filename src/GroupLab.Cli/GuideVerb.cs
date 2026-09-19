using GroupLab.Core.Reporting;
using OpenCvSharp;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab user-guide</c>, NOTES-FROM-PLANNING.md entry 113 section 6: docs/USER-GUIDE.pdf from docs/USER-GUIDE.md, the Markdown the
/// source, with every picture it names read from the committed renders, scaled to at most 1400 pixels across and set as JPEG. Run it after the
/// renders are regenerated, and commit both files.
/// </summary>
public static class GuideVerb
{
    public const string Usage = "grouplab user-guide [<docs directory>]";

    public static int Run(string docs, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        string source = Path.Combine(docs, "USER-GUIDE.md");
        if (!File.Exists(source))
        {
            error.WriteLine($"user-guide: there is no {source}");
            return 2;
        }

        string markdown = File.ReadAllText(source);
        var missing = DocumentPdf.Pictures(markdown).Where(p => !File.Exists(Path.Combine(docs, p))).ToList();
        if (missing.Count > 0)
        {
            error.WriteLine($"user-guide: the guide names pictures that are not there: {string.Join(", ", missing)}");
            return 1;
        }

        byte[] pdf = DocumentPdf.Write(markdown, path => Picture(Path.Combine(docs, path)));
        string target = Path.Combine(docs, "USER-GUIDE.pdf");
        File.WriteAllBytes(target, pdf);
        output.WriteLine($"Wrote {target}, {pdf.Length / 1024} KB.");
        return 0;
    }

    /// <summary>A render as a JPEG no wider than 1400 pixels.</summary>
    public static DocumentImage? Picture(string path)
    {
        using var image = Cv2.ImRead(path, ImreadModes.Color);
        if (image.Empty())
        {
            return null;
        }

        using var scaled = image.Width > 1400 ? image.Resize(new Size(1400, image.Height * 1400 / image.Width), 0, 0, InterpolationFlags.Area) : image.Clone();
        Cv2.ImEncode(".jpg", scaled, out byte[] jpeg, new ImageEncodingParam(ImwriteFlags.JpegQuality, 85));
        return new DocumentImage(jpeg, scaled.Width, scaled.Height);
    }
}
