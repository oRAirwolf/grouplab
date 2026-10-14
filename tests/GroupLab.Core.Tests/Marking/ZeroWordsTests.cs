using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 424, Alan on a pooled 20 shot set: the zero correction said nothing about an axis it left alone. It now says
/// "leave it" and why, or with the setting gives the click anyway, marked; and a pool's centre agrees with the Shots list it is shown beside.
/// </summary>
public class ZeroWordsTests
{
    private const double Yards100 = 3600;

    private static readonly Rifle MilRifle = new("Test rifle", 0.1, AngularUnit.Mrad);

    /// <summary>Twenty shots round a ring centred <paramref name="rightInches"/> right of the aim, with the sigma asked for, at 100 yd.</summary>
    private static MarkingState Group(double rightInches, double sigmaInches, int shots = 20)
    {
        var session = new MarkingSession();
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        session.SetPointOfAim(new PointD(0, 0));
        double radius = sigmaInches * Math.Sqrt(2);
        for (int k = 0; k < shots; k++)
        {
            double angle = 2 * Math.PI * k / shots;
            session.AddShot(new PointD(100 * (rightInches + (radius * Math.Cos(angle))), 100 * radius * Math.Sin(angle)));
        }

        session.SetShotDistance(Yards100);
        session.SetEquipment(MilRifle, null, null);
        return session.State;
    }

    /// <summary>0.08 mil at 100 yd, Alan's elevation, here on the windage: 0.288 in.</summary>
    private const double PointZeroEightMil = 0.08e-3 * Yards100;

    /// <summary>The sigma that puts the 95 percent half-width at <paramref name="times"/> the offset, for twenty shots in a round group.</summary>
    private static double SigmaFor(double times) => times * PointZeroEightMil / Zeroing.DetectableMultiple(20, circular: true);

    [Fact]
    public void JustInsideTheScatterTheAxisIsNamedLeaveItWithWhy()
    {
        var state = Group(PointZeroEightMil, SigmaFor(1.05));
        var zero = Zeroing.For(state)!;
        Assert.False(zero.Windage.Distinguishable);

        var said = ZeroWords.For(zero, state, UnitSettings.Imperial);
        Assert.Contains("Windage: leave it. 0.08 mil right is within what these 20 shots can tell apart (about 0.09 mil either way). Elevation: leave it. It is on the aim, as near as these 20 shots can tell (about 0.09 mil either way).", said.Verdict, StringComparison.Ordinal);
        Assert.Contains(said.Why, w => w.StartsWith("Windage got no click: 0.08 mil right is within", StringComparison.Ordinal));
        Assert.Contains(said.Why, w => w.Contains(" in (0.09 mil) across", StringComparison.Ordinal));
        Assert.Equal("Worked out from both axes together from 20 shots, because the group is about as wide as it is tall.", said.Why[^1]);
    }

    [Fact]
    public void JustOutsideTheScatterTheAxisIsDialledWithNoLeaveIt()
    {
        var state = Group(PointZeroEightMil, SigmaFor(0.95));
        var zero = Zeroing.For(state)!;
        Assert.True(zero.Windage.Distinguishable);

        var said = ZeroWords.For(zero, state, UnitSettings.Imperial);
        Assert.StartsWith("Dial 1 click left, at 0.1 mil a click.", said.Verdict, StringComparison.Ordinal);
        Assert.DoesNotContain("Windage: leave it", said.Verdict, StringComparison.Ordinal);
        Assert.Contains("Elevation: leave it.", said.Verdict, StringComparison.Ordinal);
        Assert.True(said.Dial);
    }

    [Fact]
    public void WithTheSettingAnAxisInsideTheScatterIsRoundedToAWholeClickAndMarked()
    {
        // 0.08 mil at a 0.1 mil click is one click, given anyway and marked; the elevation, on the aim, says no click.
        var state = Group(PointZeroEightMil, SigmaFor(1.05));
        var zero = Zeroing.For(state)!;
        Assert.Equal(1, zero.Windage.Rounded!.Count);
        Assert.Null(zero.Windage.Clicks);

        var said = ZeroWords.For(zero, state, UnitSettings.Imperial, InsideScatter.ClicksAnyway);
        Assert.Equal("Dial 1 click left, at 0.1 mil a click. Elevation: no click. The left click is within the scatter; more shots would tell whether it is needed.", said.Verdict);
        Assert.Equal(["no click", "1 click left, within the scatter"], said.Axes);
        Assert.Contains(said.Why, w => w.StartsWith("Windage was given its click anyway, as Settings asks", StringComparison.Ordinal));

        // Just outside, the same click is simply dialled.
        var outside = Group(PointZeroEightMil, SigmaFor(0.95));
        var plain = ZeroWords.For(Zeroing.For(outside)!, outside, UnitSettings.Imperial, InsideScatter.ClicksAnyway);
        Assert.Equal("Dial 1 click left, at 0.1 mil a click. Elevation: no click.", plain.Verdict);
    }

    [Fact]
    public void NeitherAxisDistinguishableKeepsTheVerdictAndNamesEachAxis()
    {
        var state = Group(0.05, 0.5);
        var said = ZeroWords.For(Zeroing.For(state)!, state, UnitSettings.Imperial);
        Assert.StartsWith("Not distinguishable from zero at 20 shots.", said.Verdict, StringComparison.Ordinal);
        Assert.Contains("Windage: leave it.", said.Verdict, StringComparison.Ordinal);
        Assert.Contains("Elevation: leave it.", said.Verdict, StringComparison.Ordinal);
        Assert.False(said.Dial);
    }

    [Fact]
    public void AShotMarkedAsASighterIsLeftOutOfTheZeroAsItIsOutOfTheGroup()
    {
        var session = new MarkingSession();
        session.Load(Group(0.5, 0.1));
        int far = session.AddShot(new PointD(100 * 3, 0));
        session.SetSighter(far, true);
        var zero = Zeroing.For(session.State)!;
        var group = GroupAnalysis.Analyse(session.State);
        Assert.Equal(20, zero.Shots);
        Assert.Equal(group.AllShots!.CentreFromAim!.Value.X, zero.Windage.OffsetInches, 9);
    }

    /// <summary>
    /// Section 3: a pool's centre from aim is the mean of its Shots list on both axes, over the shots the list shows as counted. A shot left
    /// out is listed, struck through, and not in the centre; that, not the pool, is what makes the list's own average differ.
    /// </summary>
    [Fact]
    public void APoolsCentreIsTheMeanOfItsShotsListOverTheCountedShots()
    {
        var set = GltdJsonReader.ReadFile(Repo.PathTo("targets", "GL-CF9-T.gltd.json")).Definition!;
        var random = new Random(424);
        var sheets = new List<MarkingState>();
        for (int k = 0; k < 2; k++)
        {
            var session = new MarkingSession();
            session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
            int left = session.AddBull(new PointD(0, 0)), right = session.AddBull(new PointD(600, 0));
            foreach (int bull in new[] { left, right })
            {
                var at = session.State.Bulls.Single(b => b.Index == bull).Image;
                for (int i = 0; i < 5; i++)
                {
                    session.AddShot(new PointD(at.X + 40 + (random.NextDouble() * 30), at.Y + 25 + (random.NextDouble() * 20)), bull);
                }
            }

            session.SetSetSheet(k);
            sheets.Add(session.State);
        }

        var excluded = new MarkingSession();
        excluded.Load(sheets[1]);
        excluded.SetExclusion(excluded.State.Shots[0].Id, ExclusionReason.ByShooter);
        sheets[1] = excluded.State;

        var pooled = SetPool.Composite(set, sheets);
        var zero = Zeroing.For(pooled)!;
        var rows = ShotOffsets.Table(pooled);
        var counted = rows.Where(r => !r.LeftOut).ToList();
        Assert.Equal(20, rows.Count);
        Assert.Equal(19, counted.Count);
        Assert.Equal(counted.Average(r => r.AcrossInches), zero.Windage.OffsetInches, 9);
        Assert.Equal(-counted.Average(r => r.UpInches), zero.Elevation.OffsetInches, 9);
        Assert.NotEqual(rows.Average(r => r.AcrossInches), zero.Windage.OffsetInches, 3);
    }
}
