using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GroupLab.Core.Printing.Labels;

/// <summary>How GroupLab reaches a printer, NOTES-FROM-PLANNING.md entry 358 section 4, in the order it tries them.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PrinterTransport>))]
public enum PrinterTransport
{
    /// <summary>Bluetooth LE: the only kind an iPhone allows a printer without Apple's accessory programme, so it comes first.</summary>
    BluetoothLe,

    /// <summary>Classic Bluetooth's serial port, where the platform allows it (not an iPhone).</summary>
    BluetoothClassic,

    /// <summary>A USB serial port.</summary>
    UsbSerial,

    /// <summary>The operating system's own print dialog, the fallback for any printer with a driver.</summary>
    SystemDialog,
}

/// <summary>A setting a printer takes as a number, density or speed: its range, its default, and which way is darker or faster.</summary>
public sealed record SettingRange(int Min, int Max, int Default, bool HigherIsMore = true)
{
    /// <summary>A value kept inside the range; the default where none is given.</summary>
    public int Clamp(int? value) => Math.Clamp(value ?? Default, Math.Min(Min, Max), Math.Max(Min, Max));
}

/// <summary>
/// One kind of label printer, as data, NOTES-FROM-PLANNING.md entry 358 section 4: "a new printer is a data file plus, at most, one small
/// encoder". How it is recognised (its GATT services after connecting; its advertised name is a hint only, since the M220 advertises as
/// "Q155" and a serial number), how it is reached, its head, the page widths it takes, how bytes are paced to it, its density and speed and
/// the commands' ranges, its media codes, and which encoder speaks its language. <see cref="Tested"/> says whether a real one has printed
/// GroupLab's check page; every other profile is "should work".
/// </summary>
public sealed record PrinterProfile
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    /// <summary>The <see cref="IPrinterEncoder.Id"/> of the language it speaks.</summary>
    public required string Encoder { get; init; }

    /// <summary>GATT service UUIDs, lower case, any one of which identifies it after connecting.</summary>
    public IReadOnlyList<string> Services { get; init; } = [];

    /// <summary>Advertised name prefixes: a hint before connecting, never proof.</summary>
    public IReadOnlyList<string> NameHints { get; init; } = [];

    public IReadOnlyList<PrinterTransport> Transports { get; init; } = [];

    /// <summary>The characteristic bytes are written to, and those it answers on.</summary>
    public string? Write { get; init; }

    public IReadOnlyList<string> Notify { get; init; } = [];

    public double DotsPerInch { get; init; }

    public int HeadDots { get; init; }

    /// <summary>The page widths it takes, millimetres.</summary>
    public IReadOnlyList<double> PageWidthsMm { get; init; } = [];

    /// <summary>Bytes in one write, and the pause after each, milliseconds.</summary>
    public int ChunkBytes { get; init; } = 128;

    public int PacingMs { get; init; } = 20;

    public SettingRange? Density { get; init; }

    public SettingRange? Speed { get; init; }

    /// <summary>Media type codes by name: "gaps", "continuous", "marks".</summary>
    public IReadOnlyDictionary<string, int> Media { get; init; } = new Dictionary<string, int>();

    public bool Tested { get; init; }

    /// <summary>Where the profile's facts come from, for the person reading the file.</summary>
    public string? Source { get; init; }

    /// <summary>The head as the thermal raster draws for it.</summary>
    public Thermal.PrintHead Head => new(DotsPerInch, HeadDots);
}

/// <summary>The printer profiles GroupLab ships, read once from the data file built into it (<c>Printing/Labels/printers.json</c>).</summary>
public static class PrinterProfiles
{
    private const string ResourceName = "GroupLab.Core.Printing.Labels.printers.json";

    private static readonly Lazy<IReadOnlyList<PrinterProfile>> Shipped = new(() =>
    {
        using var stream = typeof(PrinterProfiles).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(ResourceName + " is not built into GroupLab.Core");
        return Read(stream);
    });

    public static IReadOnlyList<PrinterProfile> All => Shipped.Value;

    public static JsonSerializerOptions Options { get; } = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, ReadCommentHandling = JsonCommentHandling.Skip };

    /// <summary>Profiles from a data file shaped as <c>printers.json</c> is: { "printers": [ ... ] }.</summary>
    public static IReadOnlyList<PrinterProfile> Read(Stream json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var file = JsonSerializer.Deserialize<ProfileFile>(json, Options) ?? throw new InvalidDataException("not a printer profile file");
        return file.Printers;
    }

    private sealed record ProfileFile(IReadOnlyList<PrinterProfile> Printers);
}
