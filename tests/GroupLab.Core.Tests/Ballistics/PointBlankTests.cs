using System.Globalization;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;
using GroupLab.Core.Rendering;
using GroupLab.Core.Statistics;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Ballistics;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 455: maximum point blank range against the planning session's own point-mass G7 solve of the Test load
/// (140 gr, 2710 ft/s, SD 10, G7 0.326, sight 1.75 in, ICAO, an 8 in circle), which matched the Test load's table to 0.04 in. The entry
/// says to expect small differences from GroupLab's solver and that anything over a few yards is worth a look.
/// </summary>
public class PointBlankTests(ITestOutputHelper output)
{
    private static BallisticInput TestLoad(double zeroYards = 100) =>
        new(0.326, DragModel.G7, 2710, 140, SightHeightInches: 1.75, ZeroRangeYards: zeroYards, Reference: ReferenceAtmosphere.Icao);

    private static readonly PointBlankZone Eight = PointBlankZone.Circle(8);

    private static PointBlankModel Model(double meanRadiusMoa = 0.63, double sd = 10) => new(TestLoad(), meanRadiusMoa, sd);

    private void Say(string what) => output.WriteLine(what);

    [Fact]
    public void TheClassicFigureMatchesThePlanningSessionsSolve()
    {
        var classic = Model().Classic(Eight);
        Say(string.Create(CultureInfo.InvariantCulture,
            $"start {classic.StartYards:0.0} near {classic.NearZeroYards:0.0} peak {classic.PeakInches:0.00} at {classic.PeakYards} far {classic.FarZeroYards:0.0} end {classic.EndYards:0.0} at100 {classic.OffsetAt100Inches:0.00}"));

        // Planning: start 0, near zero 26 yd, peak +4.0 in at 148 yd, far zero 266 yd, end 312 yd, +3.38 in at 100 yd.
        Assert.Equal(0, classic.StartYards);
        Assert.InRange(classic.NearZeroYards, 23, 29);
        Assert.InRange(classic.PeakInches, 3.99, 4.01);
        Assert.InRange(classic.PeakYards, 143, 153);
        Assert.InRange(classic.FarZeroYards, 261, 271);
        Assert.InRange(classic.EndYards, 307, 317);
        Assert.InRange(classic.OffsetAt100Inches, 3.30, 3.46);
    }

    [Fact]
    public void TheCertaintyBandMatchesThePlanningSessionsSolve()
    {
        var model = Model();
        foreach (var (certainty, end) in new[] { (0.90, 262), (0.95, 252), (0.99, 228), (0.999, 200) })
        {
            var band = model.Longest(Eight, certainty);
            Say(string.Create(CultureInfo.InvariantCulture, $"{certainty:P1}: {band.StartYards} to {band.EndYards}, at 100 {model.Height(100, band.LaunchDeltaMoa):0.00} in"));
            Assert.True(band.Exists);
            Assert.Equal(0, band.StartYards);
            Assert.InRange(band.EndYards, end - 5, end + 5);
        }

        // Planning: at 90 %, +2.6 in at 100 yd.
        Assert.InRange(model.Height(100, model.Longest(Eight, 0.90).LaunchDeltaMoa), 2.4, 2.8);

        // The rifle's own 100 yd zero: 0 to 208 yd by the classic rule, and shorter once the spread is counted.
        var classic = model.ClassicBand(Eight, 0);
        var current = model.Band(Eight, 0.90, 0);
        Say($"current zero: classic {classic.StartYards} to {classic.EndYards}, at 90 % {current.StartYards} to {current.EndYards}");
        Assert.InRange(classic.EndYards, 203, 213);
        Assert.True(current.EndYards < classic.EndYards);
    }

    [Fact]
    public void TheMostLikelyDistanceAndTheWindowMatchThePlanningSessionsSolve()
    {
        var model = Model();
        var likely = model.MostLikely(Eight, 0.90, 200);
        Say(string.Create(CultureInfo.InvariantCulture, $"most likely 200: at 100 {model.Height(100, likely.LaunchDeltaMoa):0.00} in, band {likely.StartYards} to {likely.EndYards}, 200 yd {model.Chance(Eight, 200, likely.LaunchDeltaMoa):P2}"));

        // Planning: +1.77 in at 100 yd, 99.9 % at 200 yd, the 90 % band 0 to 236 yd.
        Assert.InRange(model.Height(100, likely.LaunchDeltaMoa), 1.65, 1.89);
        Assert.InRange(Math.Abs(model.Height(200, likely.LaunchDeltaMoa)), 0, 0.01);
        Assert.True(model.Chance(Eight, 200, likely.LaunchDeltaMoa) > 0.998);
        Assert.InRange(likely.EndYards, 231, 241);

        // Planning: the window 150 to 300 yd, a best worst case of 56 %, below 9 in 10, so said.
        var window = model.Window(Eight, 0.90, 150, 300);
        Say(string.Create(CultureInfo.InvariantCulture, $"window: worst {window.WorstChance:P1} at {window.WorstYards:0}"));
        Assert.InRange(window.WorstChance, 0.50, 0.62);
        Assert.False(window.Reaches);
    }

    [Fact]
    public void MoreCertaintyOrAWiderGroupNeverLengthensTheBand()
    {
        var model = Model();
        double previous = double.MaxValue;
        foreach (double certainty in new[] { 0.5, 0.8, 0.9, 0.95, 0.99, 0.999 })
        {
            double end = model.Longest(Eight, certainty).EndYards;
            Assert.True(end <= previous, $"{certainty} reached {end}, past {previous}");
            previous = end;
        }

        previous = double.MaxValue;
        foreach (double mr in new[] { 0.25, 0.5, 1, 2, 3 })
        {
            double end = Model(mr).Longest(Eight, 0.9).EndYards;
            Assert.True(end <= previous, $"a mean radius of {mr} MOA reached {end}, past {previous}");
            previous = end;
        }
    }

    [Fact]
    public void AZoneSmallerThanTheSightHeightStartsOutFromTheMuzzle()
    {
        // Planning: a prairie dog's 2 in with the Test load starts at about 16 yd.
        var classic = Model().Classic(PointBlankZone.Circle(2));
        Say(string.Create(CultureInfo.InvariantCulture, $"prairie dog classic start {classic.StartYards:0.0}"));
        Assert.InRange(classic.StartYards, 12, 20);
        Assert.True(Model(0.3).Longest(PointBlankZone.Circle(2), 0.5).StartYards > 0);
    }

    [Fact]
    public void ARectangleKeepsTheSameRulesAndATallerOneReachesFarther()
    {
        var model = Model();
        var square = model.Longest(PointBlankZone.Rectangle(8, 8), 0.9);
        var tall = model.Longest(PointBlankZone.Rectangle(8, 16), 0.9);
        Assert.True(square.Exists && tall.Exists);
        Assert.True(tall.EndYards > square.EndYards);

        // A square holds a circle inside it, so it reaches at least as far.
        Assert.True(square.EndYards >= model.Longest(Eight, 0.9).EndYards);
    }

    [Fact]
    public void ACertaintyOfAHundredIsRefusedInWords()
    {
        Assert.Equal(PointBlankModel.NoPromise, PointBlankModel.Certainty(100).Refusal);
        Assert.Null(PointBlankModel.Certainty(100).Share);
        Assert.Equal(0.999, PointBlankModel.Certainty(99.9).Share!.Value, 9);
        Assert.Equal(0.9, PointBlankModel.Certainty(90).Share!.Value, 9);
    }

    [Fact]
    public void TheEnergyFloorAndTheExpansionVelocityEndTheBandWhenTheyComeFirst()
    {
        var model = Model();
        var band = model.Longest(PointBlankZone.Circle(14.5), 0.9);
        var plain = model.Ending(band, null, null);
        Assert.Equal(PointBlankEnd.Certainty, plain.Why);

        var energy = model.Ending(band, 2000, null);
        Assert.Equal(PointBlankEnd.Energy, energy.Why);
        Assert.True(model.EnergyAt(energy.EndYards) >= 2000 && model.EnergyAt(energy.EndYards + 1) < 2000);

        var expansion = model.Ending(band, null, 2400);
        Assert.Equal(PointBlankEnd.Expansion, expansion.Why);
        Assert.True(model.VelocityAt(expansion.EndYards) >= 2400);
    }

    [Fact]
    public void TheEstimatesSayTheyAreEstimatesAndEverySourcedRowNamesItsSource()
    {
        Assert.All(PointBlankTargets.All.Where(t => t.IsEstimate), t => Assert.StartsWith("Estimate, no published size found", t.Source, StringComparison.Ordinal));
        Assert.All(PointBlankTargets.All.Where(t => !t.IsEstimate), t => Assert.DoesNotContain("Estimate", t.Source, StringComparison.Ordinal));
        Assert.Equal(new[] { "European wild boar", "Impala", "Kudu", "Red stag", "Wild hog" }, PointBlankTargets.All.Where(t => t.IsEstimate).Select(t => t.Name).Order());
        Assert.Equal(1500, PointBlankTargets.All.Single(t => t.Name == "Elk").EnergyFloorFtLb);
        Assert.Equal(1000, PointBlankTargets.All.Single(t => t.Name == "Whitetail deer").EnergyFloorFtLb);
        Assert.Null(PointBlankTargets.All.Single(t => t.Name == "Coyote").EnergyFloorFtLb);
    }

    [Fact]
    public void TheChanceIsTheSimulatedShareOfShots()
    {
        // The closed form against shots drawn, at 250 yd with the 90 % zero and no velocity spread.
        var model = Model(sd: 0);
        double delta = model.Longest(Eight, 0.9).LaunchDeltaMoa;
        double sigma = 0.63 / Math.Sqrt(Math.PI / 2) * PointBlankModel.InchesPerMoaPerYard * 250;
        double mu = model.Height(250, delta);
        var random = new Random(455);
        int hits = 0, n = 200_000;
        for (int i = 0; i < n; i++)
        {
            double x = Normal(random) * sigma, y = mu + (Normal(random) * sigma);
            hits += (x * x) + (y * y) <= 16 ? 1 : 0;
        }

        double share = (double)hits / n;
        Assert.InRange(model.Chance(Eight, 250, delta), share - 0.005, share + 0.005);

        // And the velocity spread only ever lowers it.
        Assert.True(Model().Chance(Eight, 250, delta) <= model.Chance(Eight, 250, delta) + 1e-9);
    }

    private static Rifle TestRifle => new("Test rifle", 0.1, AngularUnit.Mrad) { SightHeightInches = 1.75, ZeroDistanceYards = 100 };

    private static Load TestLoadRecord => new("Test load", null)
    {
        MuzzleVelocityFps = 2710, BallisticCoefficient = 0.326, DragModel = DragModel.G7, BulletWeightGrains = 140, BulletName = "ELD-M",
    };

    [Fact]
    public void TheReportSaysHowToSetItInTheScopesClicks()
    {
        var request = new PointBlankRequest(Eight, "Whitetail deer", 0.9, EnergyFloorFtLb: 1000);
        var result = PointBlankReport.Work(Model(), request, TestRifle, UnitSettings.Imperial);
        Say(result.Headline + " | " + result.HowToSet + " | " + result.Ends + " | " + result.CurrentZero + " | " + result.Wind);

        // Planning: +2.6 in at 100 yd, 0.72 mil, so 7 clicks of 0.1 mil.
        Assert.Equal("Dead on 0 to 263 yd", result.Headline);
        Assert.StartsWith("Zero 2.6 in high at 100 yd: 0.7", result.HowToSet, StringComparison.Ordinal);
        Assert.EndsWith("7 clicks up from your 100 yd zero.", result.HowToSet, StringComparison.Ordinal);
        Assert.StartsWith("9 in 10 shots in an 8", result.Certainty, StringComparison.Ordinal);
        Assert.Equal(PointBlankReport.ExpansionNotEntered, result.Expansion);
        Assert.StartsWith("Your rifle's 100 yd zero: dead on 0 to 184 yd at 9 in 10", result.CurrentZero, StringComparison.Ordinal);
        Assert.NotNull(result.FarZeroYards);
        Assert.InRange(result.FarZeroYards!.Value, 200, 300);

        // The window that cannot be held says so, with what would help.
        var window = PointBlankReport.Work(Model(), request with { Mode = PointBlankMode.Window, WindowFromYards = 150, WindowToYards = 300 }, TestRifle, UnitSettings.Imperial);
        Assert.StartsWith("No single zero keeps 9 in 10 across 150 to 300 yd.", window.WindowSays, StringComparison.Ordinal);

        // The most likely distance lists the holds past its band.
        var likely = PointBlankReport.Work(Model(), request with { Mode = PointBlankMode.MostLikely, LikelyYards = 200 }, TestRifle, UnitSettings.Imperial);
        Assert.NotEmpty(likely.Holds);
        Assert.All(likely.Holds, h => Assert.True(h.Yards > likely.Ending.EndYards));
    }

    [Fact]
    public void TheShareIsSaidInWords()
    {
        Assert.Equal("9 in 10", PointBlankReport.Share(0.9));
        Assert.Equal("19 in 20", PointBlankReport.Share(0.95));
        Assert.Equal("99 in 100", PointBlankReport.Share(0.99));
        Assert.Equal("999 in 1000", PointBlankReport.Share(0.999));
        Assert.Equal("87.3 %", PointBlankReport.Share(0.873));
    }

    [Fact]
    public void TheCardIsFiftyByThirtyAndEveryLineFits()
    {
        var request = new PointBlankRequest(Eight, "Whitetail deer", 0.9);
        var result = PointBlankReport.Work(Model(), request, TestRifle, UnitSettings.Imperial);
        var lines = PointBlankCard.Lines(TestRifle, TestLoadRecord, result, request, UnitSettings.Imperial, new DateOnly(2026, 10, 19));
        Say(string.Join(" / ", lines));
        Assert.Equal(6, lines.Count);
        Assert.Equal("DEAD ON 0 TO 263 YD", lines[2]);
        Assert.Equal("Whitetail deer, 9 in 10 shots", lines[3]);
        Assert.StartsWith("Zero 2.6 in high at 100 yd", lines[4], StringComparison.Ordinal);
        Assert.Contains("140 gr ELD-M", lines[1], StringComparison.Ordinal);

        var page = PointBlankCard.Page(lines);
        Assert.Equal(1000, page.Width);
        Assert.Equal(600, page.Height);
        var runs = page.Items.OfType<TextRun>().ToList();
        Assert.Equal(6, runs.Count);
        Assert.All(runs, r => Assert.True(r.Baseline < page.Height && r.Baseline > 0, $"{r.Text} sits off the card"));
        Assert.All(runs, r => Assert.True(r.Text.Length * r.FontSize * 0.56 <= 46 * 20 + 1, $"{r.Text} runs off the card"));
        Assert.Equal("grouplab-point-blank-test-rifle-50x30mm", PointBlankCard.FileName(TestRifle));
    }

    [Fact]
    public void KeepingAZeroOnTheRifleKeepsTheOldOneToPutBack()
    {
        var rifle = TestRifle.WithZero(232, "point blank, 9 in 10 in an 8 in circle", new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));
        Assert.Equal(232, rifle.ZeroDistanceYards);
        var previous = Assert.Single(rifle.PreviousZeros);
        Assert.Equal(100, previous.ZeroDistanceYards);
        Assert.Equal("2026-10-19T12:00:00Z", previous.ReplacedUtc);
        var back = rifle.UndoZero();
        Assert.Equal(100, back.ZeroDistanceYards);
        Assert.Empty(back.PreviousZeros);
        Assert.Equal(TestRifle, back);
    }

    /// <summary>
    /// The figures the research article "Point blank range, honestly" quotes beyond the planning session's (2026-10-19): the 90 % band for
    /// five mean radii, the share of shots the classic zero keeps in the circle at its peak and far end, from the bench and from a field
    /// position, and the window that can be held.
    /// </summary>
    [Fact]
    public void TheArticlesFiguresHold()
    {
        foreach (var (mr, end) in new[] { (0.5, 275), (0.63, 263), (1.0, 221), (1.5, 148), (2.0, 111) })
        {
            Assert.InRange(Model(mr).Longest(Eight, 0.9).EndYards, end - 1, end + 1);
        }

        var bench = Model();
        var classic = bench.Classic(Eight);
        Assert.InRange(bench.Chance(Eight, classic.PeakYards, classic.LaunchDeltaMoa), 0.455, 0.467);
        Assert.InRange(bench.Chance(Eight, classic.EndYards, classic.LaunchDeltaMoa), 0.41, 0.422);
        Assert.InRange(bench.Chance(Eight, classic.FarZeroYards, classic.LaunchDeltaMoa), 0.978, 0.988);

        var field = Model(1.5);
        var wide = field.Classic(Eight);
        Assert.InRange(field.Chance(Eight, wide.PeakYards, wide.LaunchDeltaMoa), 0.40, 0.41);
        Assert.InRange(field.Chance(Eight, wide.EndYards, wide.LaunchDeltaMoa), 0.267, 0.277);

        var held = bench.Window(Eight, 0.9, 150, 250);
        Assert.True(held.Reaches);
        Assert.InRange(held.WorstChance, 0.95, 0.96);
    }

    private static double Normal(Random random) => Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble());
}
