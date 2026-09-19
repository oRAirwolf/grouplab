using System.Buffers.Binary;
using System.Globalization;
using System.Xml.Linq;
using OpenCvSharp;

namespace GroupLab.Cli;

/// <summary>
/// The application's icons from its mark, NOTES-FROM-PLANNING.md entry 105 section 5: <c>grouplab icons &lt;mark.svg&gt; &lt;directory&gt;</c>
/// writes a Windows <c>.ico</c>, a Linux PNG set and a macOS <c>.icns</c>, so the mark is changed in one place and the rasters follow.
/// <para>
/// <b>Each size is drawn at its size.</b> The circles are drawn eight times over at the size wanted and averaged down to it, which is
/// anti-aliasing at that size, not a shrink of the largest image; the 16 pixel image is the one Windows shows in the taskbar and title bar.
/// The mark is circles only, filled or stroked, which is all this reads. The dark variant is used, whose grey and amber hold on a light
/// taskbar as well as a dark one.
/// </para>
/// </summary>
public static class IconSet
{
    /// <summary>The Windows sizes, as the entry lists them.</summary>
    public static readonly int[] WindowsSizes = [16, 24, 32, 48, 64, 256];

    /// <summary>The Linux hicolor sizes.</summary>
    public static readonly int[] LinuxSizes = [16, 24, 32, 48, 64, 128, 256, 512];

    /// <summary>The macOS sizes, with the type codes an <c>.icns</c> file names them by.</summary>
    public static readonly (int Size, string Type)[] MacSizes = [(16, "icp4"), (32, "icp5"), (64, "icp6"), (128, "ic07"), (256, "ic08"), (512, "ic09"), (1024, "ic10")];

    private const int Supersample = 8;

    public static int Write(string svg, string directory, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var circles = Read(svg);
        Directory.CreateDirectory(Path.Combine(directory, "linux"));
        var png = new Dictionary<int, byte[]>();
        byte[] Png(int size) => png.TryGetValue(size, out var bytes) ? bytes : png[size] = Draw(circles, size);

        File.WriteAllBytes(Path.Combine(directory, "grouplab.ico"), Ico([.. WindowsSizes.Select(s => (s, Png(s)))]));
        foreach (int size in LinuxSizes)
        {
            File.WriteAllBytes(Path.Combine(directory, "linux", $"grouplab-{size}.png"), Png(size));
        }

        File.WriteAllBytes(Path.Combine(directory, "grouplab.icns"), Icns([.. MacSizes.Select(m => (m.Type, Png(m.Size)))]));
        output.WriteLine($"icons from {svg}: grouplab.ico at {string.Join(", ", WindowsSizes)} px, linux PNGs at {string.Join(", ", LinuxSizes)} px, grouplab.icns at {string.Join(", ", MacSizes.Select(m => m.Size))} px, in {directory}");
        return 0;
    }

    /// <summary>One circle of the mark, in the SVG's units, with its fill or its stroke.</summary>
    public sealed record Circle(double X, double Y, double Radius, (byte R, byte G, byte B)? Fill, (byte R, byte G, byte B)? Stroke, double StrokeWidth);

    public static (double Width, double Height, IReadOnlyList<Circle> Circles) Read(string svg)
    {
        var root = XDocument.Load(svg).Root ?? throw new InvalidDataException($"{svg} is empty");
        double[] box = [.. ((string?)root.Attribute("viewBox") ?? "0 0 100 100").Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Number)];
        var circles = root.Elements().Where(e => e.Name.LocalName == "circle").Select(e => new Circle(
            Number((string)e.Attribute("cx")!) - box[0],
            Number((string)e.Attribute("cy")!) - box[1],
            Number((string)e.Attribute("r")!),
            Colour((string?)e.Attribute("fill")),
            Colour((string?)e.Attribute("stroke")),
            e.Attribute("stroke-width") is { } w ? Number(w.Value) : 1)).ToList();
        if (root.Elements().Any(e => e.Name.LocalName is "path" or "rect" or "polygon"))
        {
            throw new InvalidDataException($"{svg} has shapes other than circles; the icon is drawn from the mark alone, which is circles");
        }

        return (box[2], box[3], circles);
    }

    /// <summary>The mark at one size, as a PNG with a transparent background.</summary>
    public static byte[] Draw((double Width, double Height, IReadOnlyList<Circle> Circles) mark, int size)
    {
        int big = size * Supersample;
        double scale = big / Math.Max(mark.Width, mark.Height);
        using var canvas = new Mat(big, big, MatType.CV_8UC4, Scalar.All(0));
        foreach (var c in mark.Circles)
        {
            var centre = new Point((int)Math.Round(c.X * scale), (int)Math.Round(c.Y * scale));
            if (c.Fill is { } fill)
            {
                Cv2.Circle(canvas, centre, (int)Math.Round(c.Radius * scale), new Scalar(fill.B, fill.G, fill.R, 255), -1, LineTypes.AntiAlias);
            }

            if (c.Stroke is { } stroke)
            {
                Cv2.Circle(canvas, centre, (int)Math.Round(c.Radius * scale), new Scalar(stroke.B, stroke.G, stroke.R, 255), Math.Max(1, (int)Math.Round(c.StrokeWidth * scale)), LineTypes.AntiAlias);
            }
        }

        using var small = new Mat();
        Cv2.Resize(canvas, small, new Size(size, size), 0, 0, InterpolationFlags.Area);
        Cv2.ImEncode(".png", small, out byte[] bytes);
        return bytes;
    }

    /// <summary>A Windows icon file of PNG images, which Windows reads from Vista on.</summary>
    public static byte[] Ico(IReadOnlyList<(int Size, byte[] Png)> images)
    {
        ArgumentNullException.ThrowIfNull(images);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)images.Count);
        int offset = 6 + (16 * images.Count);
        foreach (var (size, png) in images)
        {
            writer.Write((byte)(size >= 256 ? 0 : size));
            writer.Write((byte)(size >= 256 ? 0 : size));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(png.Length);
            writer.Write(offset);
            offset += png.Length;
        }

        foreach (var (_, png) in images)
        {
            writer.Write(png);
        }

        return stream.ToArray();
    }

    /// <summary>A macOS icon file: a big-endian header and one PNG per type code.</summary>
    public static byte[] Icns(IReadOnlyList<(string Type, byte[] Png)> images)
    {
        ArgumentNullException.ThrowIfNull(images);
        int length = 8 + images.Sum(i => 8 + i.Png.Length);
        var bytes = new byte[length];
        "icns"u8.CopyTo(bytes);
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(4), length);
        int at = 8;
        foreach (var (type, png) in images)
        {
            System.Text.Encoding.ASCII.GetBytes(type).CopyTo(bytes, at);
            BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(at + 4), 8 + png.Length);
            png.CopyTo(bytes, at + 8);
            at += 8 + png.Length;
        }

        return bytes;
    }

    private static double Number(string text) => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static (byte, byte, byte)? Colour(string? value)
    {
        if (value is null || value == "none" || !value.StartsWith('#') || value.Length != 7)
        {
            return null;
        }

        int rgb = int.Parse(value[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return ((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }
}
