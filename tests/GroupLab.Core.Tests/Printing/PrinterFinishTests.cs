using GroupLab.Core.Printing.Labels;

namespace GroupLab.Core.Tests.Printing;

/// <summary>
/// Entry 381: GroupLab closed the link to the M834 2 s after writing the page, while the printer was still drawing it across, and every
/// print was a strip of a few millimetres. The print now waits for the printer's "printed" answer, or for a limit that grows with the page.
/// </summary>
public class PrinterFinishTests
{
    /// <summary>A link whose printer answers on a script: each poll hands back the next list of answers.</summary>
    private sealed class AnsweringLink(params byte[][][] script) : IPrinterLink
    {
        private int poll;

        public int Polls => poll;

        public Task WriteAsync(ReadOnlyMemory<byte> chunk, CancellationToken token) => Task.CompletedTask;

        public IReadOnlyList<byte[]> TakeAnswers()
        {
            poll++;
            return poll <= script.Length ? script[poll - 1] : [];
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private static Task NoWait(TimeSpan _, CancellationToken token) => Task.CompletedTask;

    [Fact]
    public async Task ThePrintWaitsUntilThePrinterSaysItHasPrinted()
    {
        // As in request 73's recording: a battery answer first, then nothing for a while, then 1A 0F 0C.
        var link = new AnsweringLink([[0x1A, 0x04, 0x62]], [], [], [[0x1A, 0x0F, 0x0C]], [[0x1A, 0x04, 0x62]]);
        var heard = new List<string>();
        bool done = await PrinterFinish.WaitAsync(link, PrinterFinish.M834Printed, TimeSpan.FromMinutes(1), NoWait, a => heard.Add(Convert.ToHexString(a)), CancellationToken.None);
        Assert.True(done);
        Assert.Equal(["1A0462", "1A0F0C"], heard);
        Assert.Equal(4, link.Polls);
    }

    [Fact]
    public async Task AnAnswerSplitAcrossTwoReadsStillCounts()
    {
        var link = new AnsweringLink([[0x1A, 0x0F]], [[0x0C]]);
        Assert.True(await PrinterFinish.WaitAsync(link, PrinterFinish.M834Printed, TimeSpan.FromMinutes(1), NoWait, _ => { }, CancellationToken.None));
    }

    [Fact]
    public async Task APrinterThatNeverAnswersIsWaitedForOnlyUntilTheLimit()
    {
        var link = new AnsweringLink();
        var waited = TimeSpan.Zero;
        bool done = await PrinterFinish.WaitAsync(link, PrinterFinish.M834Printed, TimeSpan.FromSeconds(5), (t, _) =>
        {
            waited += t;
            return Task.CompletedTask;
        }, _ => { }, CancellationToken.None);
        Assert.False(done);
        Assert.Equal(TimeSpan.FromSeconds(5), waited);
    }

    [Fact]
    public async Task CancelEndsTheWait()
    {
        using var cancel = new CancellationTokenSource();
        var link = new AnsweringLink();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => PrinterFinish.WaitAsync(link, PrinterFinish.M834Printed, TimeSpan.FromMinutes(1),
            (_, token) =>
            {
                cancel.Cancel();
                token.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            }, _ => { }, cancel.Token));
    }

    [Fact]
    public void ThePageIsGivenFarLongerThanTheTwoSecondsThatCutItOff()
    {
        // Alan's page was 146,525 bytes; the recorded one crossed and printed in 45 s.
        var limit = PrinterFinish.Limit(146_525);
        Assert.InRange(limit.TotalSeconds, 85, 95);
        Assert.Equal(TimeSpan.FromSeconds(30), PrinterFinish.Limit(0));
    }
}
