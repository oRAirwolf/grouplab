using System.Globalization;
using System.Text.Json;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Ballistics;

/// <summary>
/// The Phase 5 gate, "a ballistic solver validated against an independent implementation", docs/BALLISTICS-VALIDATION.md section 2 and
/// NOTES-FROM-PLANNING.md entry 110 section 2f check 2. The reference is py-ballisticcalc 2.3.1, run on a GitHub runner by
/// reference/py-ballisticcalc/generate.py; its tables are committed and the library is not. The tolerances below are the ones that file stated,
/// committed before the tables existed, and they are not to be widened to make a case pass.
/// </summary>
public class BallisticsIndependentTests(ITestOutputHelper output)
{
    /// <summary>Drop: 0.10 MOA plus 1 percent of the reference drop.</summary>
    public const double DropAbsoluteMoa = 0.10, DropShare = 0.01;

    /// <summary>Wind deflection: 0.05 MOA plus 2 percent of the reference deflection.</summary>
    public const double WindAbsoluteMoa = 0.05, WindShare = 0.02;

    /// <summary>Time of flight: 0.5 percent.</summary>
    public const double TimeShare = 0.005;

    /// <summary>
    /// The G1 cases fail the gate, and are held here by name rather than by any widening of the tolerance. The G1 table in ballistics.js,
    /// which entry 110 section 2a says to port as it stands, is not the standard G1 function above Mach 0.85: py-ballisticcalc's copy of the
    /// standard table gives 0.4805 at Mach 1.0 and 0.6625 at Mach 1.4, where the JavaScript's gives 0.5210 and 0.5295. Which table to carry is
    /// question 25 of docs/QUESTIONS-FOR-PLANNING.md. The test fails if either case starts to pass, so this list cannot outlive its fix.
    /// </summary>
    public static readonly IReadOnlySet<string> KnownFailing = new HashSet<string> { "308-168-g1-dry", "6mm-105-g1-cold-humid" };

    [Fact]
    public void EveryCaseAgreesWithPyBallisticcalcWithinTheStatedTolerance()
    {
        var cases = JsonDocument.Parse(File.ReadAllText(Repo.PathTo("reference", "ballistics-cases.json"))).RootElement;
        var reference = JsonDocument.Parse(File.ReadAllText(Repo.PathTo("reference", "py-ballisticcalc", "tables.json"))).RootElement;
        output.WriteLine(reference.GetProperty("source").GetString());
        var failures = new List<string>();
        var failingCases = new HashSet<string>();
        int compared = 0;
        double worstDrop = 0, worstWind = 0, worstTime = 0;
        foreach (var c in cases.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            var rows = reference.GetProperty("cases").EnumerateArray().Single(r => r.GetProperty("name").GetString() == name).GetProperty("rows").EnumerateArray().ToList();
            var port = BallisticSolver.Solve(BallisticsTranscriptionTests.Input(c), cases.GetProperty("maxRange").GetDouble(), cases.GetProperty("rangeStep").GetDouble()).Points;
            output.WriteLine($"{name}: range yd, drop MOA port / reference / allowed, wind MOA port / reference / allowed, time of flight percent");
            foreach (var r in rows)
            {
                double range = r.GetProperty("rangeYd").GetDouble();
                if (range < 99.5)
                {
                    continue;
                }

                var p = port.Single(x => Math.Abs(x.RangeYards - range) < 1e-6);
                double inches = range * 36;
                // Both sides turned into MOA the same way, from inches at the range, so no difference of angular convention can enter.
                double refDrop = -r.GetProperty("heightIn").GetDouble() / inches * BallisticSolver.MoaPerRadian;
                double refWind = r.GetProperty("windageIn").GetDouble() / inches * BallisticSolver.MoaPerRadian;
                double refTime = r.GetProperty("tof").GetDouble();
                double drop = -p.DropInches / inches * BallisticSolver.MoaPerRadian, wind = p.WindInches / inches * BallisticSolver.MoaPerRadian;
                double dropAllowed = DropAbsoluteMoa + (DropShare * Math.Abs(refDrop)), windAllowed = WindAbsoluteMoa + (WindShare * Math.Abs(refWind));
                double timeShare = Math.Abs(p.TimeOfFlight - refTime) / refTime;
                if (!KnownFailing.Contains(name))
                {
                    worstDrop = Math.Max(worstDrop, Math.Abs(drop - refDrop) / dropAllowed);
                    worstWind = Math.Max(worstWind, Math.Abs(wind - refWind) / windAllowed);
                    worstTime = Math.Max(worstTime, timeShare / TimeShare);
                }
                output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"  {range,5:0}  {drop,7:0.000} {refDrop,7:0.000} {dropAllowed,5:0.00}   {wind,6:0.000} {refWind,6:0.000} {windAllowed,5:0.00}   {100 * timeShare,5:0.000}"));
                bool fails = Math.Abs(drop - refDrop) > dropAllowed || Math.Abs(wind - refWind) > windAllowed || timeShare > TimeShare;
                if (fails)
                {
                    failingCases.Add(name);
                }

                if (KnownFailing.Contains(name))
                {
                    compared++;
                    continue;
                }

                if (Math.Abs(drop - refDrop) > dropAllowed)
                {
                    failures.Add(string.Create(CultureInfo.InvariantCulture, $"{name} at {range} yd: drop {drop:0.000} MOA against {refDrop:0.000}, allowed {dropAllowed:0.000}"));
                }

                if (Math.Abs(wind - refWind) > windAllowed)
                {
                    failures.Add(string.Create(CultureInfo.InvariantCulture, $"{name} at {range} yd: wind {wind:0.000} MOA against {refWind:0.000}, allowed {windAllowed:0.000}"));
                }

                if (timeShare > TimeShare)
                {
                    failures.Add(string.Create(CultureInfo.InvariantCulture, $"{name} at {range} yd: time of flight {p.TimeOfFlight:0.0000} s against {refTime:0.0000}"));
                }

                compared++;
            }
        }

        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"largest share of the allowance used, the cases held to it: drop {worstDrop:0.00}, wind {worstWind:0.00}, time of flight {worstTime:0.00}"));
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
        Assert.True(failingCases.SetEquals(KnownFailing), $"failing: {string.Join(", ", failingCases)}; held as known failures: {string.Join(", ", KnownFailing)}");
        Assert.Equal(60, compared);
    }
}
