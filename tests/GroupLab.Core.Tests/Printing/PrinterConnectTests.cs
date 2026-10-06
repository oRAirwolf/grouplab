using GroupLab.Core.Printing.Labels;

namespace GroupLab.Core.Tests.Printing;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 377: a connect that never answers must end, be said, and give way to the next way of connecting, and
/// Cancel must end it at once. Each fake blocks as Android's connect does, until its abort closes it.
/// </summary>
public class PrinterConnectTests
{
    /// <summary>A connect that hangs until aborted, then throws as a closed socket does; or one that opens at once.</summary>
    private static ConnectAttempt Fake(string name, bool answers, List<string> aborted)
    {
        var closed = new ManualResetEventSlim();
        return new ConnectAttempt(name, () =>
        {
            if (answers)
            {
                return;
            }

            closed.Wait();
            throw new IOException("socket closed");
        }, () =>
        {
            aborted.Add(name);
            closed.Set();
        });
    }

    [Fact]
    public async Task AConnectThatNeverAnswersIsEndedAndTheNextWayIsTried()
    {
        var aborted = new List<string>();
        var said = new List<(string Name, string Outcome)>();
        string? connected = await PrinterConnect.FirstThatConnects(
            [Fake("serial port", false, aborted), Fake("channel 1", true, aborted)], TimeSpan.FromMilliseconds(200), (n, o, _) => said.Add((n, o)), CancellationToken.None);

        Assert.Equal("channel 1", connected);
        Assert.Equal(["serial port"], aborted);
        Assert.Equal([("serial port", "timed out"), ("channel 1", "connected")], said);
    }

    [Fact]
    public async Task WhenNoWayAnswersItSaysSoRatherThanWaiting()
    {
        var aborted = new List<string>();
        var said = new List<string>();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        string? connected = await PrinterConnect.FirstThatConnects(
            [Fake("a", false, aborted), Fake("b", false, aborted)], TimeSpan.FromMilliseconds(150), (n, o, _) => said.Add(n + " " + o), CancellationToken.None);

        Assert.Null(connected);
        Assert.Equal(["a timed out", "b timed out"], said);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(5), $"it took {clock.Elapsed}");
    }

    [Fact]
    public async Task CancelEndsTheConnectUnderWay()
    {
        var aborted = new List<string>();
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => PrinterConnect.FirstThatConnects(
            [Fake("a", false, aborted), Fake("b", true, aborted)], TimeSpan.FromSeconds(30), (_, _, _) => { }, cancel.Token));
        Assert.Equal(["a"], aborted);
    }
}
