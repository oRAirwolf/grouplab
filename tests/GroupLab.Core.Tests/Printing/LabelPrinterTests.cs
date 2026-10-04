using System.Text;
using GroupLab.Core.Printing.Labels;
using GroupLab.Core.Printing.Thermal;

namespace GroupLab.Core.Tests.Printing;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 358 section 4: the framework for label printers. Every shipped profile reads and names an encoder that
/// exists; each encoder writes its language's commands around the dots, black the right way round; a printer is recognised by its services
/// before its name; and a print goes in the profile's chunks with a pause between them. No real printer is reached: entry 359 keeps every
/// test on bytes until a printer has been recorded.
/// </summary>
public class LabelPrinterTests
{
    /// <summary>A label 12 dots across and 2 down: the first dot of each row black, and the twelfth dot of the second.</summary>
    private static DotImage Dots()
    {
        var image = new DotImage(12, 2);
        image[0, 0] = true;
        image[0, 1] = true;
        image[11, 1] = true;
        return image;
    }

    private static PrinterProfile Profile(string encoder) => new()
    {
        Id = "test-" + encoder,
        Name = "a test printer",
        Encoder = encoder,
        Transports = [PrinterTransport.BluetoothLe],
        DotsPerInch = 203.2,
        HeadDots = 832,
        ChunkBytes = 4,
        PacingMs = 20,
        Density = new SettingRange(0, 15, 10),
        Speed = new SettingRange(1, 5, 3),
        Media = new Dictionary<string, int> { ["gaps"] = 0x0A, ["continuous"] = 0x0B },
    };

    [Fact]
    public void EveryShippedProfileReadsAndNamesAnEncoderThatExists()
    {
        Assert.NotEmpty(PrinterProfiles.All);
        foreach (var profile in PrinterProfiles.All)
        {
            Assert.NotNull(PrinterEncoders.For(profile));
            Assert.True(profile.DotsPerInch > 0 && profile.HeadDots > 0, profile.Id);
            Assert.NotEmpty(profile.Transports);
            Assert.False(profile.Tested, $"{profile.Id} says tested, and no label printer has printed GroupLab's check page yet");
            Assert.False(string.IsNullOrWhiteSpace(profile.Source), $"{profile.Id} does not say where its facts come from");
        }

        Assert.Equal(PrinterProfiles.All.Count, PrinterProfiles.All.Select(p => p.Id).Distinct().Count());
    }

    [Fact]
    public void TsplWritesTheLabelsSizeItsSettingsAndTheDotsTurnedOver()
    {
        var bytes = new TsplEncoder().Encode(new LabelJob(Dots(), 101.6, 152.4, Density: 99, Speed: 2), Profile("tspl"));
        string text = Encoding.ASCII.GetString(bytes);
        Assert.StartsWith("SIZE 101.6 mm,152.4 mm\r\nGAP 2 mm,0 mm\r\nDENSITY 15\r\nSPEED 2\r\nDIRECTION 0\r\nCLS\r\nBITMAP 0,0,2,2,0,", text, StringComparison.Ordinal);
        Assert.EndsWith("\r\nPRINT 1,1\r\n", text, StringComparison.Ordinal);
        int at = text.IndexOf("BITMAP 0,0,2,2,0,", StringComparison.Ordinal) + "BITMAP 0,0,2,2,0,".Length;
        // A set bit is paper in TSPL: the black first dot is a clear top bit, and the dots past the twelfth stay paper.
        Assert.Equal(new byte[] { 0x7F, 0xFF, 0x7F, 0xEF }, bytes[at..(at + 4)]);
    }

    [Fact]
    public void ThePhomemoFamilyWritesSpeedDensityMediaTheRasterAndTheEnd()
    {
        var bytes = new PhomemoEscEncoder().Encode(new LabelJob(Dots(), 50, 30, Density: 12, Speed: 9, Media: "continuous"), Profile("phomemo-esc"));
        byte[] expected =
        [
            0x1B, 0x4E, 0x0D, 0x05,
            0x1B, 0x4E, 0x04, 0x0C,
            0x1F, 0x11, 0x0B,
            0x1D, 0x76, 0x30, 0x00, 0x02, 0x00, 0x02, 0x00,
            0x80, 0x00, 0x80, 0x10,
            0x1F, 0xF0, 0x05, 0x00, 0x1F, 0xF0, 0x03, 0x00,
        ];
        Assert.Equal(expected, bytes);
    }

    [Fact]
    public void ZplWritesOneGraphicFieldWithBlackAsASetBit()
    {
        string text = Encoding.ASCII.GetString(new ZplEncoder().Encode(new LabelJob(Dots(), 50, 30, Copies: 2), Profile("zpl")));
        Assert.Equal("^XA^PW12^LL2^LH0,0~SD10^PR3^FO0,0^GFA,4,4,2,80008010^FS^PQ2^XZ", text);
    }

    [Fact]
    public void EscPosWritesTheRasterAndAFeed()
    {
        var bytes = new EscPosEncoder().Encode(new LabelJob(Dots(), 50, 30), Profile("escpos"));
        Assert.Equal(new byte[] { 0x1B, 0x40, 0x1D, 0x76, 0x30, 0x00, 0x02, 0x00, 0x02, 0x00, 0x80, 0x00, 0x80, 0x10, 0x1B, 0x64, 0x03 }, bytes);
    }

    [Fact]
    public void APrinterIsRecognisedByItsServicesBeforeItsName()
    {
        var a = Profile("tspl") with { Id = "a", Services = ["0000ff00-0000-1000-8000-00805f9b34fb"], NameHints = ["Q"] };
        var b = Profile("escpos") with { Id = "b", Services = ["000018f0-0000-1000-8000-00805f9b34fb"], NameHints = ["Q"] };
        var c = Profile("zpl") with { Id = "c", Services = ["000018f0-0000-1000-8000-00805f9b34fb"], NameHints = ["ZD"] };
        PrinterProfile[] all = [a, b, c];

        // Its service decides, whatever it calls itself.
        Assert.Equal("a", PrinterRecognition.Match(new FoundPrinter("1", "ZD420", PrinterTransport.BluetoothLe, ["0000FF00-0000-1000-8000-00805F9B34FB"]), all)?.Id);
        // A service two makers share: the name breaks the tie.
        Assert.Equal("c", PrinterRecognition.Match(new FoundPrinter("2", "ZD420", PrinterTransport.BluetoothLe, ["000018f0-0000-1000-8000-00805f9b34fb"]), all)?.Id);
        // Nothing known about it.
        Assert.Null(PrinterRecognition.Match(new FoundPrinter("3", "Speaker", PrinterTransport.BluetoothLe, ["0000abcd-0000-1000-8000-00805f9b34fb"]), all));
        // Seen over a transport the profile does not use.
        Assert.Null(PrinterRecognition.Match(new FoundPrinter("4", "Q155", PrinterTransport.UsbSerial, ["0000ff00-0000-1000-8000-00805f9b34fb"]), all));
    }

    private sealed class RecordingLink : IPrinterLink
    {
        public List<byte[]> Chunks { get; } = [];

        public Task WriteAsync(ReadOnlyMemory<byte> chunk, CancellationToken token)
        {
            Chunks.Add(chunk.ToArray());
            return Task.CompletedTask;
        }

        public IReadOnlyList<byte[]> TakeAnswers() => [];

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task APrintGoesInTheProfilesChunksWithAPauseBetweenEach()
    {
        var link = new RecordingLink();
        var pauses = new List<TimeSpan>();
        byte[] bytes = [.. Enumerable.Range(0, 10).Select(i => (byte)i)];
        int chunks = await PrinterJob.SendAsync(link, Profile("tspl"), bytes, (t, _) =>
        {
            pauses.Add(t);
            return Task.CompletedTask;
        }, CancellationToken.None);

        Assert.Equal(3, chunks);
        Assert.Equal(new[] { 4, 4, 2 }, link.Chunks.Select(c => c.Length));
        Assert.Equal(bytes, link.Chunks.SelectMany(c => c));
        Assert.Equal(new[] { TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(20) }, pauses);
    }

    [Fact]
    public async Task NoPlatformReachesAPrinterYet()
    {
        IOutsideWorldNothing world = new();
        Assert.Empty(await ((global::GroupLab.Core.Updates.IOutsideWorld)world).FindPrintersAsync(PrinterTransport.BluetoothLe, TimeSpan.Zero, CancellationToken.None));
    }

    /// <summary>An outside world that does nothing, to read the interface's own answers for printers.</summary>
    private sealed class IOutsideWorldNothing : global::GroupLab.Core.Updates.IOutsideWorld
    {
        public void OpenAddress(string address)
        {
        }

        public void OpenFile(string path)
        {
        }

        public void OpenFolder(string path)
        {
        }

        public Task<string?> GetTextAsync(string address, CancellationToken token) => Task.FromResult<string?>(null);

        public Task<global::GroupLab.Core.Updates.PostAnswer?> PostTargetAsync(string address, string package, byte[] image, string imageName, CancellationToken token) => Task.FromResult<global::GroupLab.Core.Updates.PostAnswer?>(null);

        public Task<global::GroupLab.Core.Updates.PostAnswer?> PostErrorReportAsync(string address, string report, CancellationToken token) => Task.FromResult<global::GroupLab.Core.Updates.PostAnswer?>(null);

        public Task<global::GroupLab.Core.Updates.PostAnswer?> PostReportPackageAsync(string address, byte[] zip, string version, CancellationToken token) => Task.FromResult<global::GroupLab.Core.Updates.PostAnswer?>(null);

        public Task<global::GroupLab.Core.Updates.PostAnswer?> PostSurveyAsync(string address, string report, CancellationToken token) => Task.FromResult<global::GroupLab.Core.Updates.PostAnswer?>(null);

        public Task<long?> DownloadAsync(string address, string into, IProgress<double>? progress, CancellationToken token) => Task.FromResult<long?>(null);

        public void StartInstaller(string path, string arguments)
        {
        }

        public Task<global::GroupLab.Core.Updates.ClipboardContents> ReadClipboardAsync(CancellationToken token) => throw new NotSupportedException();
    }
}

/// <summary>NOTES-FROM-PLANNING.md entry 358 section 6: the darkness test's pattern, and the rule that picks a setting from its photograph.</summary>
public class DarknessTestTests
{
    [Fact]
    public void EachSettingGetsAStripWithItsLinesDrawnAtTheirWidths()
    {
        var page = DarknessTest.Page(832, [4, 8, 12]);
        Assert.Equal(832, page.Width);
        Assert.Equal((3 * 64) + 8, page.Height);
        // In the first strip, along its middle row, the runs of black after the number are the square, then the four lines at 1 to 4 dots.
        int y = 8 + 24;
        var runs = new List<int>();
        int run = 0;
        for (int x = 0; x < page.Width; x++)
        {
            if (page[x, y])
            {
                run++;
            }
            else if (run > 0)
            {
                runs.Add(run);
                run = 0;
            }
        }

        Assert.Contains(40, runs);
        int square = runs.IndexOf(40);
        Assert.Equal(new[] { 1, 2, 3, 4 }, runs.Skip(square + 1).Take(4));
        Assert.Equal(new[] { 3, 4, 5, 6 }, runs.Skip(square + 5).Take(4));
    }

    [Fact]
    public void TheDarkestSettingThatKeepsFineDetailWithinHalfADotIsRecommended()
    {
        var measured = new Dictionary<int, IReadOnlyList<double>>
        {
            [4] = [0.6, 1.7, 2.8, 3.9],
            [8] = [1.4, 2.4, 3.3, 4.2],
            [12] = [1.9, 2.8, 3.7, 4.6],
        };
        Assert.Equal(8, DarknessTest.Recommend(measured));
        Assert.Equal(4, DarknessTest.Recommend(measured, higherIsDarker: false));
        Assert.Null(DarknessTest.Recommend(new Dictionary<int, IReadOnlyList<double>> { [15] = [2.0, 3.0, 4.0, 5.0] }));
    }
}
