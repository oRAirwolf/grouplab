using System.IO.Compression;
using System.Text;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.StoreTargets;

/// <summary>
/// The colour layout of a store-bought target, NOTES-FROM-PLANNING.md entry 332's trial (docs/notes/fingerprint-trial.md): the smoothed
/// colour of each half inch cell of the printing, in OpenCV's 8-bit Lab, three bytes a cell, row by row from the top left cell at
/// (<paramref name="X0"/>, <paramref name="Y0"/>) inches. It is what tells apart two products whose local features agree; at two points an
/// inch no usable picture of the maker's printing can be made from it.
/// </summary>
public sealed record ColourLayout(double X0, double Y0, int Cols, int Rows, byte[] Lab)
{
    /// <summary>The side of one cell, inches.</summary>
    public const double Cell = 0.5;

    /// <summary>The printed area the layout covers, inches across and down.</summary>
    public double Width => Cols * Cell;

    public double Height => Rows * Cell;
}

/// <summary>
/// A store-bought target's fingerprint, entry 340 section 1: what GroupLab keeps of another maker's target so a picture of one is
/// recognized, its bulls placed and its printed size used as the scale. It holds local features (ORB, 32 byte descriptors, positions in
/// inches on the target), the aim points in the same inches, and the <see cref="ColourLayout"/>. It never holds a picture of the target:
/// the scans it was made from stay on the computer they were made on (samples/PROVENANCE.md, "Store-bought target fingerprints").
/// </summary>
public sealed class TargetFingerprint
{
    /// <summary>The bytes that begin every fingerprint file, after the gzip wrapper is taken off.</summary>
    public const string Magic = "GLFP2";

    public TargetFingerprint(string product, IReadOnlyList<PointD> points, byte[] descriptors, int descriptorBytes, IReadOnlyList<PointD> bulls, ColourLayout layout)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(descriptors);
        ArgumentNullException.ThrowIfNull(bulls);
        ArgumentNullException.ThrowIfNull(layout);
        if (descriptors.Length != points.Count * descriptorBytes)
        {
            throw new ArgumentException("one descriptor a feature", nameof(descriptors));
        }

        Product = product;
        Points = [.. points];
        Descriptors = descriptors;
        DescriptorBytes = descriptorBytes;
        Bulls = [.. bulls];
        Layout = layout;
        Centre = Points.Count == 0 ? default : new PointD(Points.Average(p => p.X), Points.Average(p => p.Y));
    }

    /// <summary>The product's identifier in <see cref="StoreTargetLibrary"/>.</summary>
    public string Product { get; }

    /// <summary>Each feature's position on the target, inches from the scan's top left corner.</summary>
    public IReadOnlyList<PointD> Points { get; }

    /// <summary>The features' descriptors, <see cref="DescriptorBytes"/> bytes each, in the order of <see cref="Points"/>.</summary>
    public byte[] Descriptors { get; }

    public int DescriptorBytes { get; }

    /// <summary>The aim points, inches, top to bottom and left to right.</summary>
    public IReadOnlyList<PointD> Bulls { get; }

    public ColourLayout Layout { get; }

    /// <summary>The mean of the features' positions, where a fit's scale and stretch are judged.</summary>
    public PointD Centre { get; }

    /// <summary>The fingerprint as it is stored: gzip around the plain format.</summary>
    public byte[] ToBytes()
    {
        using var packed = new MemoryStream();
        using (var zip = new GZipStream(packed, CompressionLevel.SmallestSize, leaveOpen: true))
        using (var w = new BinaryWriter(zip, Encoding.UTF8, leaveOpen: true))
        {
            w.Write(Magic);
            w.Write(Product);
            w.Write(Bulls.Count);
            foreach (var b in Bulls)
            {
                w.Write((float)b.X);
                w.Write((float)b.Y);
            }

            w.Write(Layout.X0);
            w.Write(Layout.Y0);
            w.Write(Layout.Cols);
            w.Write(Layout.Rows);
            w.Write(Layout.Lab);
            w.Write(Points.Count);
            w.Write(DescriptorBytes);
            for (int i = 0; i < Points.Count; i++)
            {
                w.Write((float)Points[i].X);
                w.Write((float)Points[i].Y);
                w.Write(Descriptors, i * DescriptorBytes, DescriptorBytes);
            }
        }

        return packed.ToArray();
    }

    /// <summary>Reads a fingerprint written by <see cref="ToBytes"/>.</summary>
    public static TargetFingerprint Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var zip = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
        using var r = new BinaryReader(zip, Encoding.UTF8, leaveOpen: true);
        if (r.ReadString() != Magic)
        {
            throw new InvalidDataException("not a GroupLab target fingerprint");
        }

        string product = r.ReadString();
        var bulls = new PointD[r.ReadInt32()];
        for (int i = 0; i < bulls.Length; i++)
        {
            bulls[i] = new PointD(r.ReadSingle(), r.ReadSingle());
        }

        double x0 = r.ReadDouble(), y0 = r.ReadDouble();
        int cols = r.ReadInt32(), rows = r.ReadInt32();
        var layout = new ColourLayout(x0, y0, cols, rows, r.ReadBytes(cols * rows * 3));
        int n = r.ReadInt32(), size = r.ReadInt32();
        var points = new PointD[n];
        var descriptors = new byte[n * size];
        for (int i = 0; i < n; i++)
        {
            points[i] = new PointD(r.ReadSingle(), r.ReadSingle());
            byte[] one = r.ReadBytes(size);
            if (one.Length != size)
            {
                throw new InvalidDataException("the fingerprint ends early");
            }

            Array.Copy(one, 0, descriptors, i * size, size);
        }

        return new TargetFingerprint(product, points, descriptors, size, bulls, layout);
    }
}
