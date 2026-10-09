using System.Text;
using GroupLab.Core.Printing.Labels;
using GroupLab.Core.Printing.Thermal;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Printing;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 389 section 1: the M834 from the computer. The port is found by the printer's Bluetooth name among the
/// serial ports Windows made for paired devices, what stops it is said in plain words, and the page is the phone's page byte for byte. No
/// test opens a real port.
/// </summary>
public sealed class M834ComputerTests
{
    [Fact]
    public void ThePortIsFoundByTheDevicesNameAndOnlyTheOutgoingOne()
    {
        Assert.Equal("A1B2C3D4E5F6", BluetoothPorts.AddressOf("8&1a2b3c4d&0&a1b2c3d4e5f6_C00000000"));
        Assert.Equal("000000000000", BluetoothPorts.AddressOf("8&1a2b3c4d&0&000000000000_Microsoft"));
        Assert.Null(BluetoothPorts.AddressOf("8&1a2b3c4d&0&not-an-address_C00000000"));
        Assert.Equal("M834", BluetoothPorts.NameOf([.. Encoding.UTF8.GetBytes("M834"), 0, 0x41]));

        BluetoothPort[] ports =
        [
            new("COM3", "Xbox Wireless Controller", true),
            new("COM4", "", false),
            new("COM5", "M834", false),
            new("COM6", "M834", true),
        ];
        Assert.Equal("COM6", Assert.Single(BluetoothPorts.Named(ports, M834Print.NameHint)).Port);
        Assert.Empty(BluetoothPorts.Named(ports, "M220"));
    }

    [Fact]
    public void WhatStopsItIsSaidInPlainWords()
    {
        Assert.Contains("is paired with this computer", SerialPrinterWords.ForError("M834", 2), StringComparison.Ordinal);
        Assert.Contains("Add device", SerialPrinterWords.ForError("M834", 2), StringComparison.Ordinal);
        Assert.Contains("in use by another program", SerialPrinterWords.ForError("M834", 5), StringComparison.Ordinal);
        // No answer is a printer that is off or busy with a phone, which is said.
        string none = SerialPrinterWords.ForError("M834", 121);
        Assert.Contains("Check it is on", none, StringComparison.Ordinal);
        Assert.Contains("no phone is connected to it", none, StringComparison.Ordinal);
        Assert.Contains("Windows only so far", SerialPrinterWords.NotHere("M834"), StringComparison.Ordinal);
    }

    /// <summary>The shared encoding is what the phone did before it was shared: the page drawn for the head, the roll's feed, the LZO encoder.</summary>
    [Theory]
    [InlineData(PaperForm.Roll)]
    [InlineData(PaperForm.Fanfold)]
    public void ThePageIsThePhonesPage(PaperForm paper)
    {
        var page = Assert.Single(SceneBuilder.Build(BuiltIns.Load("GL-SCALE-LTR-1.gltd.json"), new RenderOptions(PrintNote: SceneBuilder.ActualSizeNote)).Pages);
        var profile = PrinterProfiles.All.Single(p => p.Id == "phomemo-m834");
        var dots = ThermalRaster.Render(page, M834Print.Head);
        var job = new LabelJob(dots.Image, page.Width / (10.0 * Scene.UnitsPerDmm), page.Height / (10.0 * Scene.UnitsPerDmm), FeedAfterMm: TearBar.FeedAfterMm(paper));
        Assert.Equal(PrinterEncoders.For(profile).Encode(job, profile), M834Print.Encode(page, paper));
    }

    [Fact]
    public async Task EveryPageIsSentWholeAndTheLinkWaitsForThePrinterToSayItPrinted()
    {
        byte[][] pages = [[.. Enumerable.Range(0, 2500).Select(i => (byte)i)], [1, 2, 3]];
        var link = new AnsweringLink();
        var heard = new List<M834Progress>();
        bool finished = await M834Print.SendAsync(link, pages, heard.Add, (_, _) => Task.CompletedTask, CancellationToken.None);

        Assert.True(finished);
        Assert.Equal(pages.SelectMany(p => p), link.Written);
        Assert.Equal(990, link.Chunks.Max());
        Assert.Equal([M834Stage.Sending, M834Stage.Sent, M834Stage.Printing, M834Stage.Printed], heard.Where(h => h.Page == 1).Select(h => h.Stage));
        Assert.All(heard.Where(h => h.Stage == M834Stage.Printed), h => Assert.True(h.Said));
        Assert.Equal("The M834 printed 2 pages. Measure a ruler line: it should be true to size.", M834Print.Done(2, finished, "send the log"));
        Assert.Contains("send the log", M834Print.Done(1, false, "send the log"), StringComparison.Ordinal);
    }

    /// <summary>A port that takes every chunk and answers <c>1A 0F 0C</c> once the page has been written.</summary>
    private sealed class AnsweringLink : IPrinterLink
    {
        private bool owed;

        public List<byte> Written { get; } = [];

        public List<int> Chunks { get; } = [];

        public Task WriteAsync(ReadOnlyMemory<byte> chunk, CancellationToken token)
        {
            Written.AddRange(chunk.ToArray());
            Chunks.Add(chunk.Length);
            owed = true;
            return Task.CompletedTask;
        }

        public IReadOnlyList<byte[]> TakeAnswers()
        {
            if (!owed)
            {
                return [];
            }

            owed = false;
            return [PrinterFinish.M834Printed];
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
