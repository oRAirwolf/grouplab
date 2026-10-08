using GroupLab.Core.Printing.Labels;
using GroupLab.Core.Printing.Thermal;
using GroupLab.Core.ScaleMarkers;

namespace GroupLab.Core.Tests.Printing;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 390: Alan's first two scale labels from the phone on the M220 came out with the first right and the second
/// shifted about 60 dots left, wrapped round to the right. GroupLab's bytes for the labels are the same but for their codes and serial; the
/// second label's first block had followed the first label's end straight away. The fake printer here loses what arrives while it is still
/// feeding a finished label, which is what the second label's shift needs, and every label's rows must start at the same column.
/// </summary>
public sealed class M220LabelsTests
{
    private static PrinterProfile M220 => PrinterProfiles.All.Single(p => p.Id == "phomemo-m220");

    /// <summary>Three 70 by 80 mm labels encoded as the phone encodes them.</summary>
    private static List<byte[]> Labels(int count)
    {
        var profile = M220;
        int dots = (int)Math.Ceiling(70 / 25.4 * profile.DotsPerInch / 8) * 8;
        var head = new PrintHead(profile.DotsPerInch, dots);
        return [.. ScaleLabels.Pages(70, 80, 1, count, "M220")
            .Select(page => PrinterEncoders.For(profile).Encode(new LabelJob(ThermalRaster.Render(page, head).Image, 70, 80, Media: "gaps"), profile))];
    }

    [Fact]
    public void TheLabelsBytesDifferOnlyInTheirCodesAndSerial()
    {
        var labels = Labels(2);
        Assert.Equal(labels[0].Length, labels[1].Length);
        Assert.Equal(labels[0].AsSpan(0, 11).ToArray(), labels[1].AsSpan(0, 11).ToArray());
        Assert.Equal(labels[0][^8..], labels[1][^8..]);
        Assert.Equal(FirstInk(Printed(labels[0])[0]), FirstInk(Printed(labels[1])[0]));
    }

    [Fact]
    public async Task EveryLabelOfThreeStartsAtTheSameColumn()
    {
        var printer = new FeedingPrinter();
        var labels = Labels(3);
        var settled = new List<int>();
        await PrinterJob.SendLabelsAsync(printer, M220, labels, printer.Pause, (_, _) => { }, settled.Add, CancellationToken.None);

        Assert.Equal([1, 2, 3], settled);
        Assert.Equal(0, printer.Lost);
        var printed = Printed([.. printer.Took]);
        Assert.Equal(3, printed.Count);
        Assert.All(printed, rows => Assert.Equal(FirstInk(printed[0]), FirstInk(rows)));
    }

    /// <summary>The same labels sent back to back, as before entry 390: the fake printer loses the start of the second, and it shifts.</summary>
    [Fact]
    public async Task SentBackToBackTheSecondLabelShiftsAsAlansDid()
    {
        var printer = new FeedingPrinter();
        var labels = Labels(2);
        foreach (var label in labels)
        {
            await PrinterJob.SendAsync(printer, M220, label, printer.Pause, CancellationToken.None);
        }

        Assert.True(printer.Lost > 0);
        var printed = Printed([.. printer.Took]);
        Assert.NotEqual(FirstInk(printed[0]), FirstInk(printed[1]));
    }

    /// <summary>Each raster block in what the printer took, as its rows, read by the printer's own rule: the size in the header, then that many bytes.</summary>
    private static List<byte[][]> Printed(byte[] took)
    {
        var labels = new List<byte[][]>();
        for (int at = 0; at + 8 <= took.Length; at++)
        {
            if (took[at] == 0x1D && took[at + 1] == 0x76 && took[at + 2] == 0x30 && took[at + 3] == 0x00)
            {
                int across = took[at + 4] | (took[at + 5] << 8), down = took[at + 6] | (took[at + 7] << 8);
                int start = at + 8;
                labels.Add([.. Enumerable.Range(0, down).Select(y => took.Skip(start + (y * across)).Take(across).ToArray())]);
                at = start + (across * down) - 1;
            }
        }

        return labels;
    }

    /// <summary>The leftmost byte with ink in any row: where the label's left codes begin.</summary>
    private static int FirstInk(byte[][] rows) => rows.Where(r => r.Length > 0).Min(r => Array.FindIndex(r, b => b != 0) is var i and >= 0 ? i : int.MaxValue);

    /// <summary>
    /// A printer that, after a label's end, spends a second feeding it out, and loses the first eight bytes that arrive meanwhile: the shape
    /// of what Alan's second label showed, not a measurement of the M220.
    /// </summary>
    private sealed class FeedingPrinter : IPrinterLink
    {
        private static readonly byte[] End = [0x1F, 0xF0, 0x03, 0x00];
        private static readonly byte[] Raster = [0x1D, 0x76, 0x30, 0x00];
        private int? rowsFrom;
        private TimeSpan now;
        private TimeSpan? feedingUntil;

        public List<byte> Took { get; } = [];

        public int Lost { get; private set; }

        public Task Pause(TimeSpan time, CancellationToken token)
        {
            now += time;
            return Task.CompletedTask;
        }

        public Task WriteAsync(ReadOnlyMemory<byte> chunk, CancellationToken token)
        {
            foreach (byte b in chunk.Span)
            {
                // The commands before the rows are still read; it is the first of the rows that arrive while it feeds that are lost.
                if (feedingUntil is { } until && now < until && rowsFrom is { } from && Took.Count >= from && Lost < 8)
                {
                    Lost++;
                    continue;
                }

                Took.Add(b);
                if (Took.Count >= End.Length && Took[^End.Length..].SequenceEqual(End))
                {
                    feedingUntil = now + TimeSpan.FromSeconds(1);
                    rowsFrom = null;
                }
                else if (feedingUntil is not null && rowsFrom is null && Took.Count >= Raster.Length && Took[^Raster.Length..].SequenceEqual(Raster))
                {
                    rowsFrom = Took.Count + 4;
                }
            }

            return Task.CompletedTask;
        }

        public IReadOnlyList<byte[]> TakeAnswers() => [];

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
