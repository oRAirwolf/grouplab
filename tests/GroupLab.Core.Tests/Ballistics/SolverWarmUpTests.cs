using System.Diagnostics;
using GroupLab.Core.Ballistics;

namespace GroupLab.Core.Tests.Ballistics;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 414 section 1.2: the solver is warmed on a thread of its own once the window is up, so the call returns at
/// once, whoever calls it and however often, and a table flown while it warms is the table flown without it.
/// </summary>
public class SolverWarmUpTests
{
    [Fact]
    public void WarmingReturnsAtOnceAndChangesNoFigure()
    {
        var input = new BallisticInput(0.308, DragModel.G1, 2650, 175, 1.5, 100, 70, null, 0, 40, 10, 0, ReferenceAtmosphere.Icao, 10, 1, 0.308, 1.24);
        var before = SolverUse.Dope(input, 1000, 25);
        var clock = Stopwatch.StartNew();
        SolverUse.WarmInBackground();
        SolverUse.WarmInBackground();
        // Warming here, on the caller's thread, would be eight flights of about 100 ms the first time; starting it in the background is not.
        // 600 ms tells the two apart, where 200 ms failed on a busy macOS runner at 211 (entry 416's CI) and once on this desktop.
        Assert.True(clock.ElapsedMilliseconds < 600, $"warming took {clock.ElapsedMilliseconds} ms on the caller's thread");

        var during = SolverUse.Dope(input, 1000, 25);
        Assert.Equal(before.Points, during.Points);
        Assert.True(SolverUse.WarmFlights >= 4, "the measured flights became fast at the fourth");
    }
}
