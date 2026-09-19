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
/// <para>
/// The two G1 cases failed on the first run, because ballistics.js's G1 table was not the standard function. With the standard table carried
/// (NOTES-FROM-PLANNING.md entry 111 section 1) they pass under the same tolerances, and every case is held to them.
/// </para>
/// </summary>
public class BallisticsIndependentTests(ITestOutputHelper output)
{
    /// <summary>Drop: 0.10 MOA plus 1 percent of the reference drop.</summary>
    public const double DropAbsoluteMoa = 0.10, DropShare = 0.01;

    /// <summary>Wind deflection: 0.05 MOA plus 2 percent of the reference deflection.</summary>
    public const double WindAbsoluteMoa = 0.05, WindShare = 0.02;

    /// <summary>Time of flight: 0.5 percent.</summary>
    public const double TimeShare = 0.005;

    [Fact]
    public void EveryCaseAgreesWithPyBallisticcalcWithinTheStatedTolerance()
    {
        var cases = JsonDocument.Parse(File.ReadAllText(Repo.PathTo("reference", "ballistics-cases.json"))).RootElement;
        var reference = JsonDocument.Parse(File.ReadAllText(Repo.PathTo("reference", "py-ballisticcalc", "tables.json"))).RootElement;
        output.WriteLine(reference.GetProperty("source").GetString());
        var failures = new List<string>();
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
                worstDrop = Math.Max(worstDrop, Math.Abs(drop - refDrop) / dropAllowed);
                worstWind = Math.Max(worstWind, Math.Abs(wind - refWind) / windAllowed);
                worstTime = Math.Max(worstTime, timeShare / TimeShare);
                output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"  {range,5:0}  {drop,7:0.000} {refDrop,7:0.000} {dropAllowed,5:0.00}   {wind,6:0.000} {refWind,6:0.000} {windAllowed,5:0.00}   {100 * timeShare,5:0.000}"));
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

        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"largest share of the allowance used: drop {worstDrop:0.00}, wind {worstWind:0.00}, time of flight {worstTime:0.00}"));
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
        Assert.Equal(60, compared);
    }
}
