using System.Globalization;
using System.Text;
using GroupLab.Core.Printing.Thermal;

namespace GroupLab.Core.Printing.Labels;

/// <summary>What one print asks of the printer beyond the dots: darkness, speed, media, the label's size and how many.</summary>
/// <param name="FeedAfterMm">Paper fed after the page, as white rows, so its end clears the tear bar (entry 382); 0 feeds nothing.</param>
public sealed record LabelJob(DotImage Image, double WidthMm, double HeightMm, int? Density = null, int? Speed = null, string? Media = null, int Copies = 1, double FeedAfterMm = 0);

/// <summary>How the paper comes into the printer: a continuous roll, or fanfold sheets with a perforation between them.</summary>
public enum PaperForm
{
    Roll,
    Fanfold,
}

/// <summary>
/// Entry 382: GroupLab's true-size Letter page on the M834 ended with its last 15 mm behind the tear bar, where the Phomemo app's page,
/// printed at 94.7 percent and top aligned, left about 15 mm of white and came all the way out. Alan measured 0.61 in (15.5 mm) from the
/// end of the print head to the tear bar on 2026-10-07. On a roll GroupLab now feeds that much paper after the page, as white rows,
/// which the printer prints like any other (no feed command of its is known). On fanfold a true-size 11 inch page already ends on the
/// perforation, so nothing is fed until a print on fanfold shows whether the printer finds the fold by itself.
/// </summary>
public static class TearBar
{
    public const double M834Mm = 15.5;

    public static double FeedAfterMm(PaperForm paper) => paper == PaperForm.Roll ? M834Mm : 0;

    /// <summary>The white rows <paramref name="mm"/> of paper is on a head of <paramref name="dotsPerInch"/>.</summary>
    public static int Rows(double mm, double dotsPerInch) => mm <= 0 ? 0 : (int)Math.Ceiling(mm / 25.4 * dotsPerInch);
}

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
    public static IReadOnlyList<IPrinterEncoder> All { get; } = [new TsplEncoder(), new PhomemoEscEncoder(), new PhomemoLzoEncoder(), new ZplEncoder(), new EscPosEncoder()];

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
/// The Phomemo M834 (request 73's recording, 2026-10-05): the settings the Phomemo app sent before its page, sent here as they were, then
/// ESC/POS's raster <c>1D 76 30 00 wL wH hL hH</c> whose rows do not follow raw but as LZO1X blocks, each the next 4096 bytes of the page
/// (the last shorter) with its packed length in three bytes, low first. The app's page was 316 bytes (2528 dots) across, so an image of
/// any other width is centred on the head, clipped or padded with paper. Which of the settings is darkness, paper or speed is not known
/// yet, so none of them follows the profile until the darkness test says what they do. Paper to feed after the page is sent as white
/// rows at its end (entry 382).
/// </summary>
public sealed class PhomemoLzoEncoder : IPrinterEncoder
{
    public const int Piece = 4096;

    /// <summary>What the app sent before the raster, in order: status questions (1F 11 n, 1A ...), reset 1B 40, then its settings.</summary>
    public static readonly byte[] Settings =
    [
        0x1F, 0x11, 0x38, 0x1F, 0x11, 0x07, 0x1F, 0x11, 0x09, 0x1F, 0x11, 0x08, 0x1F, 0x11, 0x0E, 0x1F, 0x11, 0x63, 0x1F, 0x11, 0x5E,
        0x1F, 0x11, 0x56, 0x1F, 0x11, 0x51, 0x1B, 0x4E, 0x1C, 0x02, 0x1A, 0x0A, 0x05, 0x01, 0x00, 0x06, 0x1F, 0x11, 0x12, 0x1F, 0x11, 0x11,
        0x1F, 0x11, 0x08, 0x1F, 0x11, 0x7B, 0x1F, 0x11, 0x08, 0x1B, 0x40, 0x1F, 0x11, 0x02, 0x04, 0x1F, 0x11, 0x37, 0x64, 0x1F, 0x11, 0x0B,
        0x1F, 0x11, 0x35, 0x01, 0x1F, 0x11, 0x3C, 0x02,
    ];

    public string Id => "phomemo-lzo";

    public byte[] Encode(LabelJob job, PrinterProfile profile)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(profile);
        int across = (profile.HeadDots + 7) / 8, feed = TearBar.Rows(job.FeedAfterMm, profile.DotsPerInch), height = job.Image.Height + feed;
        byte[] rows = OnHead(job.Image, across);
        if (feed > 0)
        {
            // Entry 382: the paper after the page, white rows the printer feeds out like the rest of the page.
            Array.Resize(ref rows, across * height);
        }

        var bytes = new List<byte>(Settings);
        for (int copy = 0; copy < Math.Max(1, job.Copies); copy++)
        {
            bytes.AddRange([0x1D, 0x76, 0x30, 0x00, (byte)(across & 0xFF), (byte)(across >> 8), (byte)(height & 0xFF), (byte)(height >> 8)]);
            for (int at = 0; at < rows.Length; at += Piece)
            {
                byte[] packed = Lzo1x.Compress(rows.AsSpan(at, Math.Min(Piece, rows.Length - at)));
                bytes.AddRange([(byte)(packed.Length & 0xFF), (byte)((packed.Length >> 8) & 0xFF), (byte)(packed.Length >> 16)]);
                bytes.AddRange(packed);
            }
        }

        return [.. bytes];
    }

    /// <summary>The image's rows centred on a head <paramref name="across"/> bytes wide, a whole number of bytes either side.</summary>
    internal static byte[] OnHead(DotImage image, int across)
    {
        if (image.RowBytes == across)
        {
            return image.Bits;
        }

        var rows = new byte[across * image.Height];
        int shift = (across - image.RowBytes) / 2;
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < across; x++)
            {
                int from = x - shift;
                if (from >= 0 && from < image.RowBytes)
                {
                    rows[(y * across) + x] = image.Bits[(y * image.RowBytes) + from];
                }
            }
        }

        return rows;
    }

    /// <summary>The page back from a stream this encoder wrote, or the app's: the raster's size and its rows unpacked, for the tests and the recording reader.</summary>
    public static (int Across, int Height, byte[] Rows) Read(ReadOnlySpan<byte> stream)
    {
        int at = stream.IndexOf([(byte)0x1D, (byte)0x76, (byte)0x30]);
        if (at < 0 || at + 8 > stream.Length)
        {
            throw new InvalidDataException("no raster in it");
        }

        int across = stream[at + 4] | (stream[at + 5] << 8), height = stream[at + 6] | (stream[at + 7] << 8);
        var rows = new List<byte>(across * height);
        at += 8;
        while (rows.Count < across * height)
        {
            int n = stream[at] | (stream[at + 1] << 8) | (stream[at + 2] << 16);
            rows.AddRange(Lzo1x.Decompress(stream.Slice(at + 3, n)));
            at += 3 + n;
        }

        return (across, height, [.. rows]);
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
