using System.Globalization;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Ballistics;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 456: rail cant, the sight scale factor and Top Gun, from Alan's pissinhot.com calculators, against worked
/// examples by their formulas as the entry gives them.
/// </summary>
public class ScopeMountTests(ITestOutputHelper output)
{
    private static BallisticInput TestLoad => new(0.326, DragModel.G7, 2710, 140, SightHeightInches: 1.75, ZeroRangeYards: 100, Reference: ReferenceAtmosphere.Icao);

    private static Rifle Moa(double travel, double? cant, ScopeTravelKind kind = ScopeTravelKind.Total) =>
        new("Test rifle", 0.25, AngularUnit.Moa) { SightHeightInches = 1.75, ZeroDistanceYards = 100, ScopeTravel = travel, ScopeTravelKind = kind, RailCant = cant };

    /// <summary>
    /// His page's zero compensation is atan(sight height / zero range) plus atan(drop / zero range), drop = 0.5 g t squared from the time
    /// of flight. GroupLab's is the solver's launch angle. Over a 100 yd zero the two agree to a few hundredths of a minute; the rest is
    /// the same arithmetic.
    /// </summary>
    [Fact]
    public void RailCantFollowsThePageWithTheSolversZeroAngle()
    {
        var flight = BallisticSolver.Solve(TestLoad, ScopeMount.TravelLooked, 25);
        double t = BallisticSolver.Solve(TestLoad, 100, 100).Points[^1].TimeOfFlight;
        double drop = 0.5 * BallisticSolver.Gravity * t * t * 12;
        double page = (Math.Atan(1.75 / 3600) + Math.Atan(drop / 3600)) * BallisticSolver.MoaPerRadian;
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"page {page:0.000} MOA, solver {flight.ZeroAngleMoa:0.000} MOA"));
        Assert.InRange(flight.ZeroAngleMoa, page - 0.1, page + 0.1);

        // 60 MOA of travel and a 20 MOA rail: half travel 30, the most cant that zeroes 30 plus the zero's, and 30 + 20 less the zero's left.
        var figures = ScopeMount.Figures(Moa(60, 20), flight.ZeroAngleMoa, flight)!;
        Assert.Equal(30, figures.HalfTravelMoa, 9);
        Assert.Equal(30 + flight.ZeroAngleMoa, figures.MostRailCantMoa, 9);
        Assert.Equal(50 - flight.ZeroAngleMoa, figures.LeftAboveZeroMoa!.Value, 9);
        Assert.False(figures.CannotZero);

        // The farthest range is the last row whose elevation is within what is left, and the next row needs more.
        double far = figures.FarthestYards!.Value;
        output.WriteLine($"reaches {far} yd");
        Assert.InRange(far, 800, 2000);
        var next = flight.Points.First(p => p.RangeYards > far);
        Assert.True(next.DropMoa > figures.LeftAboveZeroMoa);
        Assert.True(flight.Points.Single(p => p.RangeYards == far).DropMoa <= figures.LeftAboveZeroMoa);
    }

    [Fact]
    public void MostUpFromTheZeroIsHalfTheTravelOnceTheZerosElevationIsAddedBack()
    {
        // His page's other mode: total = 2 x (most up from zero + ZC), so half travel = most up + ZC.
        var figures = ScopeMount.Figures(Moa(27, null, ScopeTravelKind.MostUpFromZero), 3.4)!;
        Assert.Equal(30.4, figures.HalfTravelMoa, 9);
        Assert.Equal(33.8, figures.MostRailCantMoa, 9);
        Assert.Null(figures.LeftAboveZeroMoa);
    }

    [Fact]
    public void ARailCantedPastTheMostThatZeroesIsSaidPlainly()
    {
        var figures = ScopeMount.Figures(Moa(40, 30), 3.4)!;
        Assert.True(figures.CannotZero);
        var lines = ScopeMount.Lines(Moa(40, 30), "Test load", figures, UnitSettings.Imperial);
        Assert.Contains(lines, l => l == "The 30.0 MOA rail is more than the 23.4 MOA that still zeroes: this rifle cannot be zeroed with it.");
    }

    [Fact]
    public void PastTheLastDialableRangeTheRestIsHeldInTheReticle()
    {
        var rifle = new Rifle("Mil rifle", 0.1, AngularUnit.Mrad) { ScopeTravel = 30, RailCant = 6 };
        var figures = ScopeMount.Figures(rifle, 1.0)!;
        double left = figures.LeftAboveZeroMoa!.Value;
        Assert.Null(ScopeMount.OutOfElevation(rifle, figures, left - 0.01));
        double over = left + (1.4 / BallisticSolver.MilPerMoa);
        Assert.Equal("out of elevation, hold 1.4 mil in the reticle", ScopeMount.OutOfElevation(rifle, figures, over));
        Assert.Null(ScopeMount.OutOfElevation(new Rifle("No travel", 0.1, AngularUnit.Mrad), null, 500));
    }

    [Fact]
    public void TheTrackingTestGivesTheFactorTheErrorAndItsBand()
    {
        var under = ScopeTracking.Work(20, 20.5);
        Assert.Equal(20 / 20.5, under.ScaleFactor, 12);
        Assert.Equal(2.5, under.ErrorPercent, 9);
        Assert.StartsWith("A significant tracking error", under.Verdict, StringComparison.Ordinal);
        Assert.EndsWith("Each click moves less than marked.", under.Verdict, StringComparison.Ordinal);

        Assert.StartsWith("Normal for a quality scope", ScopeTracking.Work(66, 66.66).Verdict, StringComparison.Ordinal);
        Assert.StartsWith("Essentially perfect", ScopeTracking.Work(20, 20.05).Verdict, StringComparison.Ordinal);
        Assert.Equal("SSF 0.980 applied", ScopeTracking.Applied(0.98));
        Assert.Null(ScopeTracking.Applied(1.0));
        Assert.Null(ScopeTracking.Applied(null));
    }

    [Fact]
    public void EveryClickIsTheTrueAngleOverTheScaleFactor()
    {
        // 5 mil at 100 yd: 50 clicks of 0.1 mil on a true scope, 51 on one that under-tracks by 2 percent.
        double inches = Math.Tan(5.0 / 1000) * 3600;
        var plain = new Rifle("Plain", 0.1, AngularUnit.Mrad);
        Assert.Equal(50, Clicks.For(inches, 3600, plain, "up").Count);
        var scaled = plain with { SightScaleFactor = 0.98 };
        var clicks = Clicks.For(inches, 3600, scaled, "up");
        Assert.Equal(51, clicks.Count);
        Assert.Equal(0.98, clicks.ScaleFactor, 12);

        // The analysis's own clicks and Shots Needed to Zero follow the same rule.
        Assert.Equal("51 clicks at 0.1 mil", FourUnits.Clicks(inches, 100, "mil", 0.1, 0.98));
        Assert.Equal(ShotsToZero.SigmaClicks(1, 3600, AngularUnit.Mrad, 0.1) / 0.98, ShotsToZero.SigmaClicks(1, 3600, AngularUnit.Mrad, 0.1, 0.98), 12);
    }

    [Fact]
    public void TopGunIsLitzsArithmetic()
    {
        // 140 gr at 2710 ft/s is 2284 ft lb; in an 11 lb rifle that is 207.6 ft lb a pound, over 200 is 1.04 MOA.
        Assert.Equal(140 * 2710.0 * 2710 / 450240, TopGun.MuzzleEnergyFtLb(140, 2710), 9);
        Assert.Equal(2283.6, TopGun.MuzzleEnergyFtLb(140, 2710), 1);
        Assert.Equal(1.038, TopGun.GroupMoa(140, 2710, 11), 3);
        var rifle = new Rifle("Heavy", 0.1, AngularUnit.Mrad) { WeightAsFiredLb = 11 };
        var load = new Load("Test load", null) { BulletWeightGrains = 140, MuzzleVelocityFps = 2710 };
        Assert.Equal("Top Gun expects about 1.04 MOA average five-shot group for this rifle and load (Bryan Litz's trend, not a limit).", TopGun.Says(TopGun.For(rifle, load)!.Value));
        Assert.Null(TopGun.For(rifle with { WeightAsFiredLb = null }, load));
        Assert.Null(TopGun.For(rifle, load with { MuzzleVelocityFps = null }));

        var book = RecordBook.Empty.With(rifle).With(load);
        Assert.Equal(["Top Gun expects about 1.04 MOA average five-shot group for Test load in Heavy (Bryan Litz's trend, not a limit)."],
            TopGun.Lines([("Heavy", "Test load", "First", []), ("Heavy", "Test load", "Second", []), ("Heavy", "Unknown", "Third", [])], book, 3600));
    }

    /// <summary>
    /// Request 103 (Alan, 2026-10-20): Top Gun's group is the extreme spread of five shots, averaged. A group's own figure on that basis is
    /// the average five-shot extreme spread its sigma implies, so drawing many five-shot groups at a known sigma and averaging their spreads
    /// has to land on it, and its interval is the sigma's carried through.
    /// </summary>
    [Fact]
    public void YourGroupOnTopGunsBasisIsTheAverageFiveShotSpread()
    {
        var random = new Random(103);
        double Normal() => Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble());
        const double sigma = 0.5;
        double total = 0;
        const int groups = 40000;
        for (int g = 0; g < groups; g++)
        {
            total += FuddBuster.ExtremeSpread([.. Enumerable.Range(0, TopGun.GroupShots).Select(_ => new GroupLab.Core.Imaging.PointD(sigma * Normal(), sigma * Normal()))]);
        }

        double drawnMoa = UnitSettings.AngleIn(total / groups, 3600, AngularUnit.Moa)!.Value;
        var yours = TopGun.OnItsBasis(sigma, 0.4, 0.7, 3600)!.Value;
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"drawn {drawnMoa:0.0000} MOA, on its basis {yours.Value:0.0000}"));
        Assert.Equal(drawnMoa, yours.Value, 0.01);
        Assert.Equal(yours.Value * 0.4 / sigma, yours.Lower, 6);
        Assert.Equal(yours.Value * 0.7 / sigma, yours.Upper, 6);
        Assert.Null(TopGun.OnItsBasis(sigma, 0.4, 0.7, null));
        Assert.True(double.IsNaN(TopGun.OnItsBasis(sigma, null, 0.7, 3600)!.Value.Lower));

        Assert.Equal("Your shots on the same basis: about 1.00 MOA average five-shot group (0.80 to 1.20), from 20 shots. Not clearly different from the trend at this many shots.",
            TopGun.Compared(1.04, new Estimate(1.0, 0.8, 1.2), 20));
        Assert.EndsWith(" Clearly tighter than the trend.", TopGun.Compared(1.04, new Estimate(0.7, 0.6, 0.9), 20));
        Assert.EndsWith(" " + TopGun.WorseThanTrend, TopGun.Compared(1.04, new Estimate(1.6, 1.2, 2.1), 20));
        Assert.DoesNotContain("Clearly", TopGun.Compared(1.04, new Estimate(1.6, double.NaN, double.NaN), 20));

        // Compare loads: under the load's trend, each group of it with the shots and the distance, and none without.
        var rifle = new Rifle("Heavy", 0.1, AngularUnit.Mrad) { WeightAsFiredLb = 11 };
        var load = new Load("Test load", null) { BulletWeightGrains = 140, MuzzleVelocityFps = 2710 };
        var book = RecordBook.Empty.With(rifle).With(load);
        GroupLab.Core.Imaging.PointD[] five = [new(0, 0), new(0.5, 0), new(0, 0.5), new(-0.5, 0), new(0, -0.5)];
        var lines = TopGun.Lines([("Heavy", "Test load", "Monday", five), ("Heavy", "Test load", "Tuesday", five[..4])], book, 3600);
        Assert.Equal(2, lines.Count);
        Assert.StartsWith("Monday on the same basis: about ", lines[1]);
        Assert.Single(TopGun.Lines([("Heavy", "Test load", "Monday", five)], book, null));
    }
}
