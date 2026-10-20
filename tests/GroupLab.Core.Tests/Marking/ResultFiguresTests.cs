using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 259 screen 1: the phone's full figures, from the shared computation, as Alan's choice A lays them out:
/// four tiles, then "All figures" open, "Advanced", "Bull by bull" and the full CEP table; every figure explains itself, and those with a
/// range say what the shots can say about it.
/// </summary>
public class ResultFiguresTests
{
    private static MarkingState Group(int shots = 20)
    {
        var session = new MarkingSession(MarkingState.Empty with
        {
            Scale = new LengthReference(new PointD(0, 0), new PointD(100, 0), 1),
            PointOfAim = new PointD(500, 500),
            ShotDistanceInches = 3600,
        });
        var random = new Random(12);
        for (int k = 0; k < shots; k++)
        {
            session.AddShot(new PointD(530 + (random.NextDouble() * 40), 540 + (random.NextDouble() * 40)));
        }

        return session.State;
    }

    [Fact]
    public void TheResultIsFourTilesThenTheSectionsAlanChose()
    {
        var (tiles, sections) = ResultFigures.Build(Group(), UnitSettings.Imperial);
        Assert.Equal(["Mean radius", "Extreme spread", "CEP 50", "Center from aim"], tiles.Select(t => t.Label));
        Assert.Single(tiles, t => t.Headline);
        Assert.Equal(["all", "advanced", "bulls", "table"], sections.Select(s => s.Key));
        Assert.True(sections[0].Open);
        Assert.All(sections.Skip(1), s => Assert.False(s.Open));
        Assert.Contains(sections[0].Figures, f => f.Label == "CEP 99" && f.Range is not null);
        Assert.Contains(sections[0].Figures, f => f.Key == "zero");
    }

    /// <summary>The same figure as the desktop's: the mean radius tile is the shared analysis's own value, in the chosen unit.</summary>
    [Fact]
    public void TheNumbersAreTheSharedAnalysisOwn()
    {
        var state = Group();
        var units = UnitSettings.Imperial;
        var mr = GroupAnalysis.Analyse(state).AllShots!.MeanRadius!.Value;
        var (inches, _) = ResultFigures.Build(state, units);
        Assert.Equal(units.Length(mr), inches[0].Value);
        var (moa, _) = ResultFigures.Build(state, units, AngularUnit.Moa);
        Assert.EndsWith("MOA", moa[0].Value, StringComparison.Ordinal);
        Assert.Equal(units.Length(mr) + " on the paper", moa[0].Beneath);
    }

    /// <summary>Every figure with a range says what its shots can say, and what half as many would have done.</summary>
    [Fact]
    public void AFigureWithARangeSaysWhatTheShotsCanSay()
    {
        var (tiles, sections) = ResultFigures.Build(Group(), UnitSettings.Imperial);
        var withRange = tiles.Concat(sections.SelectMany(s => s.Figures)).Where(f => f.Range is not null).ToList();
        Assert.NotEmpty(withRange);
        Assert.All(withRange, f =>
        {
            Assert.StartsWith("From 20 shots the true value lies between", f.ShotsCanSay, StringComparison.Ordinal);
            Assert.Contains("With 10 shots the range would be", f.ShotsCanSay, StringComparison.Ordinal);
        });
        Assert.All(tiles.Concat(sections[0].Figures).Where(f => f.Key is not ("groupSize" or "zero")), f => Assert.False(string.IsNullOrWhiteSpace(f.Explanation), $"{f.Label} explains nothing"));
    }

    /// <summary>
    /// Request 103: where the rifle and load are known, Advanced carries Top Gun and, beneath it, these shots on its basis, the average
    /// five-shot group their sigma implies, the same number <c>TopGun.OnItsBasis</c> gives from the analysis's own sigma.
    /// </summary>
    [Fact]
    public void AdvancedSetsTheGroupBesideTopGunOnItsBasis()
    {
        var state = Group() with { Rifle = new Rifle("Heavy", 0.25, AngularUnit.Moa) { WeightAsFiredLb = 11 } };
        var load = new Load("Test load", null) { BulletWeightGrains = 140, MuzzleVelocityFps = 2710 };
        var (_, sections) = ResultFigures.Build(state, UnitSettings.Imperial, load: load);
        var advanced = sections.Single(s => s.Key == "advanced").Figures;
        Assert.Contains(advanced, f => f.Key == "topGun" && f.Value == "1.04 MOA average five-shot group");
        var sigma = GroupAnalysis.Analyse(state).Counted!.Sigma!;
        var yours = GroupLab.Core.Ballistics.TopGun.OnItsBasis(sigma.Value, sigma.Lower, sigma.Upper, 3600)!.Value;
        var figure = Assert.Single(advanced, f => f.Key == "topGunYours");
        Assert.Equal(GroupLab.Core.Ballistics.TopGun.Compared(GroupLab.Core.Ballistics.TopGun.GroupMoa(140, 2710, 11), yours, 20), figure.Explanation);
        Assert.StartsWith(yours.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " MOA", figure.Value, StringComparison.Ordinal);

        var (_, noDistance) = ResultFigures.Build(state with { ShotDistanceInches = null }, UnitSettings.Imperial, load: load);
        Assert.DoesNotContain(noDistance.Single(s => s.Key == "advanced").Figures, f => f.Key == "topGunYours");
    }
}
