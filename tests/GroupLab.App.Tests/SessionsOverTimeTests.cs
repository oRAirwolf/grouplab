using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 5.2.4: is this load getting better or worse?
/// <para>
/// <b>The test that matters is <see cref="SessionsThatWanderAreNotCalledATrend"/>.</b> Any handful of sessions plotted against the date
/// climbs or falls, and a shooter reads a barrel wearing or a batch of powder going off into it. So what is held here is not that a real
/// improvement is found; it is that the caption never announces one the sessions cannot support.
/// </para>
/// </summary>
public class SessionsOverTimeTests
{
    private static SessionPoint At(int day, double radius, double? lower = null, double? upper = null, int shots = 10) =>
        new(new DateTime(2026, 3, day, 0, 0, 0, DateTimeKind.Local), radius, lower, upper, shots);

    [Fact]
    public void WithNoSessionsItSaysSoAndNamesTheLoad()
    {
        var chart = new SessionsOverTime { Load = "Varget 41.5" };

        Assert.Equal("No sessions of Varget 41.5 yet.", chart.Description);
    }

    [Fact]
    public void OneSessionIsNotAComparisonAndSaysSo()
    {
        var chart = new SessionsOverTime { Load = "Varget 41.5", Points = [At(1, 0.42)] };

        Assert.Contains("Two or more are needed", chart.Description, StringComparison.Ordinal);
        Assert.Null(chart.Trend);
    }

    /// <summary>Between two and four sessions there is a picture but no test, and it says the different thing.</summary>
    [Fact]
    public void TooFewSessionsForTheTestSaysThatRatherThanNothing()
    {
        var chart = new SessionsOverTime { Load = "Varget 41.5", Points = [At(1, 0.52), At(8, 0.46), At(15, 0.40)] };

        Assert.Contains("3 sessions of Varget 41.5", chart.Description, StringComparison.Ordinal);
        Assert.Contains("cannot show a trend at all", chart.Description, StringComparison.Ordinal);
        Assert.DoesNotContain("enough to say so", chart.Description, StringComparison.Ordinal);
    }

    /// <summary>A load that really did tighten, session after session, says so and says how sure that is.</summary>
    [Fact]
    public void ALoadThatTightensEverySessionSaysSo()
    {
        var chart = new SessionsOverTime
        {
            Load = "Varget 41.5",
            Points = [At(1, 0.62), At(4, 0.55), At(7, 0.49), At(10, 0.44), At(13, 0.38), At(16, 0.31)],
        };

        Assert.Contains("tightening as the sessions go on", chart.Description, StringComparison.Ordinal);
        Assert.Contains("6 sessions are enough to say so", chart.Description, StringComparison.Ordinal);
    }

    /// <summary>And the case that happens far more often: sessions that go up and down, which is what every load does.</summary>
    [Fact]
    public void SessionsThatWanderAreNotCalledATrend()
    {
        var chart = new SessionsOverTime
        {
            Load = "Varget 41.5",
            Points = [At(1, 0.44), At(4, 0.51), At(7, 0.41), At(10, 0.55), At(13, 0.39), At(16, 0.48)],
        };

        Assert.NotNull(chart.Trend);
        Assert.True(chart.Trend!.PValue > 0.05, $"a wandering load was called a trend at p = {chart.Trend.PValue}");
        Assert.Contains("cannot tell that from chance", chart.Description, StringComparison.Ordinal);
        Assert.DoesNotContain("enough to say so", chart.Description, StringComparison.Ordinal);
    }

    /// <summary>
    /// The strongest thing this chart can say, and the one a table of mean radii hides completely: the sessions differ, and every one of
    /// their intervals covers every other, so the differences are worth nothing.
    /// </summary>
    [Fact]
    public void SessionsWhoseIntervalsAllOverlapSayThatInstead()
    {
        var chart = new SessionsOverTime
        {
            Load = "Varget 41.5",
            Points =
            [
                At(1, 0.44, 0.30, 0.62), At(4, 0.51, 0.34, 0.71), At(7, 0.41, 0.28, 0.58),
                At(10, 0.55, 0.36, 0.76), At(13, 0.39, 0.26, 0.56), At(16, 0.48, 0.33, 0.67),
            ],
        };

        Assert.True(chart.AllOverlap);
        Assert.Contains("Every session's interval overlaps every other", chart.Description, StringComparison.Ordinal);
    }

    /// <summary>A rising load is not described as an improving one, because the two readings are opposite.</summary>
    [Fact]
    public void ALoadThatOpensUpEverySessionSaysThat()
    {
        var chart = new SessionsOverTime
        {
            Load = "Varget 41.5",
            Points = [At(1, 0.31), At(4, 0.38), At(7, 0.44), At(10, 0.49), At(13, 0.55), At(16, 0.62)],
        };

        Assert.Contains("opening up as the sessions go on", chart.Description, StringComparison.Ordinal);
    }

    /// <summary>The figures are in the person's own units, which is what the window passes in.</summary>
    [Fact]
    public void TheSpreadIsShownInTheUnitsInForce()
    {
        var chart = new SessionsOverTime
        {
            Load = "Varget 41.5",
            Points = [At(1, 0.40), At(8, 0.50)],
            Length = inches => $"{inches * 25.4:0.0} mm",
        };

        Assert.Contains("from 10.2 mm to 12.7 mm mean radius", chart.Description, StringComparison.Ordinal);
    }

    /// <summary>Drawing it must not throw on any of these, including sessions all measuring the same and sessions all on one day.</summary>
    [AvaloniaTheory]
    [InlineData(0.42, 0.42)]
    [InlineData(0.30, 0.70)]
    public void ItDrawsWithoutFailing(double firstRadius, double lastRadius)
    {
        var chart = new SessionsOverTime
        {
            Load = "Varget 41.5",
            Points = [At(1, firstRadius, firstRadius - 0.08, firstRadius + 0.08), At(1, lastRadius), At(9, lastRadius, lastRadius - 0.1, lastRadius + 0.1)],
        };

        var window = new Avalonia.Controls.Window { Width = 900, Height = 400, Content = chart };
        window.Show();
        Avalonia.Headless.AvaloniaHeadlessPlatform.ForceRenderTimerTick();

        Assert.NotEqual(0, chart.Bounds.Width);
    }
}
