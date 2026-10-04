using System.Globalization;
using System.Text;
using GroupLab.Core.Printing.Thermal;

namespace GroupLab.Core.Printing.Labels;

/// <summary>What one print asks of the printer beyond the dots: darkness, speed, media, the label's size and how many.</summary>
public sealed record LabelJob(DotImage Image, double WidthMm, double HeightMm, int? Density = null, int? Speed = null, string? Media = null, int Copies = 1);

/// <summary>
/// One printer language, NOTES-FROM-PLANNING.md entry 358 section 4: turns a page of dots and its settings into the bytes the printer takes.
/// Written here; phomemo-tools (GPL-3.0) is compatible and was read, and the MIT and Apache projects were references only.
/// </summary>
public interface IPrinterEncoder
{
    string Id { get; }

    byte[] Encode(LabelJob job, PrinterProfile profile);
}

/// <summary>The encoders GroupLab ships, by <see cref="IPrinterEncoder.Id"/>.</summary>
public static class PrinterEncoders
{
    public static IReadOnlyList<IPrinterEncoder> All { get; } = [new TsplEncoder(), new PhomemoEscEncoder(), new ZplEncoder(), new EscPosEncoder()];

    public static IPrinterEncoder For(PrinterProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return All.FirstOrDefault(e => e.Id == profile.Encoder) ?? throw new InvalidOperationException($"no encoder named {profile.Encoder}");
    }

    internal static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    internal static string N(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}

/// <summary>
/// TSPL, which most 4x6 shipping label printers speak (the Labeer Y43BT among them, as the open tspl-cups-driver confirms), from TSC's
/// published TSPL/TSPL2 manual: SIZE, GAP, DENSITY 0 to 15, SPEED, CLS, BITMAP in overwrite mode and PRINT. Its BITMAP takes a set bit as
/// paper, the opposite of <see cref="DotImage"/>, so the rows are turned over.
/// </summary>
public sealed class TsplEncoder : IPrinterEncoder
{
    public string Id => "tspl";

    public byte[] Encode(LabelJob job, PrinterProfile profile)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(profile);
        var image = job.Image;
        var head = new StringBuilder();
        head.Append(CultureInfo.InvariantCulture, $"SIZE {PrinterEncoders.N(job.WidthMm)} mm,{PrinterEncoders.N(job.HeightMm)} mm\r\n");
        head.Append(job.Media == "continuous" ? "GAP 0 mm,0 mm\r\n" : "GAP 2 mm,0 mm\r\n");
        if (profile.Density is { } density)
        {
            head.Append(CultureInfo.InvariantCulture, $"DENSITY {density.Clamp(job.Density)}\r\n");
        }

        if (profile.Speed is { } speed)
        {
            head.Append(CultureInfo.InvariantCulture, $"SPEED {speed.Clamp(job.Speed)}\r\n");
        }

        head.Append("DIRECTION 0\r\nCLS\r\n");
        head.Append(CultureInfo.InvariantCulture, $"BITMAP 0,0,{image.RowBytes},{image.Height},0,");
        var tail = PrinterEncoders.Ascii(string.Create(CultureInfo.InvariantCulture, $"\r\nPRINT 1,{Math.Max(1, job.Copies)}\r\n"));
        return [.. PrinterEncoders.Ascii(head.ToString()), .. image.Inverted(), .. tail];
    }
}

/// <summary>
/// The Phomemo ESC family as the M110, M120 and M220 speak it (phomemo-tools' filter, and entry 358 section 5's direct print): speed
/// <c>1B 4E 0D n</c>, density <c>1B 4E 04 n</c>, media <c>1F 11 m</c>, the raster <c>1D 76 30 00 wL wH hL hH</c> with a set bit black, most
/// significant bit first, then <c>1F F0 05 00</c> and <c>1F F0 03 00</c>. A raster block holds at most 65535 rows.
/// </summary>
public sealed class PhomemoEscEncoder : IPrinterEncoder
{
    public string Id => "phomemo-esc";

    public byte[] Encode(LabelJob job, PrinterProfile profile)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(profile);
        var image = job.Image;
        var bytes = new List<byte>();
        if (profile.Speed is { } speed)
        {
            bytes.AddRange([0x1B, 0x4E, 0x0D, (byte)speed.Clamp(job.Speed)]);
        }

        if (profile.Density is { } density)
        {
            bytes.AddRange([0x1B, 0x4E, 0x04, (byte)density.Clamp(job.Density)]);
        }

        if (profile.Media.TryGetValue(job.Media ?? "gaps", out int media))
        {
            bytes.AddRange([0x1F, 0x11, (byte)media]);
        }

        for (int copy = 0; copy < Math.Max(1, job.Copies); copy++)
        {
            bytes.AddRange([0x1D, 0x76, 0x30, 0x00, (byte)(image.RowBytes & 0xFF), (byte)(image.RowBytes >> 8), (byte)(image.Height & 0xFF), (byte)(image.Height >> 8)]);
            bytes.AddRange(image.Bits);
        }

        bytes.AddRange([0x1F, 0xF0, 0x05, 0x00, 0x1F, 0xF0, 0x03, 0x00]);
        return [.. bytes];
    }
}

/// <summary>
/// ZPL II, from Zebra's published programming guide: the page width <c>^PW</c> and length <c>^LL</c> in dots, darkness <c>~SD</c> 0 to 30,
/// speed <c>^PR</c>, one graphic field <c>^GFA</c> in hexadecimal with a set bit black, and <c>^PQ</c> copies. Zebra's SDK is never bundled.
/// </summary>
public sealed class ZplEncoder : IPrinterEncoder
{
    public string Id => "zpl";

    public byte[] Encode(LabelJob job, PrinterProfile profile)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(profile);
        var image = job.Image;
        var text = new StringBuilder("^XA");
        text.Append(CultureInfo.InvariantCulture, $"^PW{image.Width}^LL{image.Height}^LH0,0");
        if (profile.Density is { } density)
        {
            text.Append(CultureInfo.InvariantCulture, $"~SD{density.Clamp(job.Density):00}");
        }

        if (profile.Speed is { } speed)
        {
            text.Append(CultureInfo.InvariantCulture, $"^PR{speed.Clamp(job.Speed)}");
        }

        text.Append(CultureInfo.InvariantCulture, $"^FO0,0^GFA,{image.Bits.Length},{image.Bits.Length},{image.RowBytes},");
        text.Append(Convert.ToHexString(image.Bits));
        text.Append(CultureInfo.InvariantCulture, $"^FS^PQ{Math.Max(1, job.Copies)}^XZ");
        return PrinterEncoders.Ascii(text.ToString());
    }
}

/// <summary>
/// Plain ESC/POS, as receipt and many small label printers take it: initialise <c>1B 40</c>, the raster <c>1D 76 30 00 xL xH yL yH</c> with a
/// set bit black, then feed <c>1B 64 n</c>. It has no standard darkness or speed command, so a profile for it names neither.
/// </summary>
public sealed class EscPosEncoder : IPrinterEncoder
{
    public string Id => "escpos";

    public byte[] Encode(LabelJob job, PrinterProfile profile)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(profile);
        var image = job.Image;
        var bytes = new List<byte> { 0x1B, 0x40 };
        for (int copy = 0; copy < Math.Max(1, job.Copies); copy++)
        {
            bytes.AddRange([0x1D, 0x76, 0x30, 0x00, (byte)(image.RowBytes & 0xFF), (byte)(image.RowBytes >> 8), (byte)(image.Height & 0xFF), (byte)(image.Height >> 8)]);
            bytes.AddRange(image.Bits);
            bytes.AddRange([0x1B, 0x64, 0x03]);
        }

        return [.. bytes];
    }
}
