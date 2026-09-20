using System.Globalization;
using System.Text.Json;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Ballistics;

/// <summary>
/// Runs where the corrected JavaScript's rows have been written by Node, which CI does before the tests. Anywhere without Node, the test is
/// skipped with that reason; on CI it must be there, so a missing file is a failure rather than a quiet skip. xunit v2 decides a skip at
/// discovery, so the check is made here.
/// </summary>
public sealed class CorrectedJavaScriptFactAttribute : FactAttribute
{
    public static string Fixture => Repo.PathTo("tests", "GroupLab.Core.Tests", "Fixtures", "ballistics-js-corrected.json");

    public CorrectedJavaScriptFactAttribute()
    {
        if (!File.Exists(Fixture) && Environment.GetEnvironmentVariable("CI") is null)
        {
            Skip = "reference/ballistics-js/cases.corrected.js has not been run here: it needs Node, which this machine does not have. CI runs it before the tests and this test then compares its rows.";
        }
    }
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 115 section 6: <c>reference/ballistics-js/ballistics.corrected.js</c> is ballistics.js with the five faults
/// put right and nothing else changed. This holds it to GroupLab's own solver, which is itself validated against py-ballisticcalc, under the
/// tolerances docs/BALLISTICS-VALIDATION.md section 2 committed before any comparison was run. The file is for Alan to upload or not; nothing
/// here reaches any website.
/// </summary>
public class CorrectedJavaScriptTests(ITestOutputHelper output)
{
    [CorrectedJavaScriptFact]
    public void TheCorrectedJavaScriptAgreesWithGroupLabsSolver()
    {
        Assert.True(File.Exists(CorrectedJavaScriptFactAttribute.Fixture),
            $"CI ran without {Path.GetFileName(CorrectedJavaScriptFactAttribute.Fixture)}: the workflow step that runs cases.corrected.js under Node did not write it.");
        var corrected = JsonDocument.Parse(File.ReadAllText(CorrectedJavaScriptFactAttribute.Fixture)).RootElement;
        var cases = JsonDocument.Parse(File.ReadAllText(Repo.PathTo("reference", "ballistics-cases.json"))).RootElement;
        var failures = new List<string>();
        int compared = 0;
        double worstDrop = 0, worstWind = 0, worstTime = 0;
        foreach (var c in cases.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            var rows = corrected.GetProperty("cases").EnumerateArray().Single(r => r.GetProperty("name").GetString() == name).GetProperty("rows").EnumerateArray().ToList();
            var port = BallisticSolver.Solve(BallisticsTranscriptionTests.Input(c), cases.GetProperty("maxRange").GetDouble(), cases.GetProperty("rangeStep").GetDouble()).Points;
            foreach (var r in rows)
            {
                double range = r.GetProperty("range").GetDouble();
                if (range < 99.5)
                {
                    continue;
                }

                var p = port.Single(x => Math.Abs(x.RangeYards - range) < 1e-6);
                double inches = range * 36;

                // The JavaScript prints drop in inches below the line of sight and its own MOA; both sides are turned into MOA the same way,
                // from inches at the range, so no difference of angular convention can enter.
                double jsDrop = -r.GetProperty("drop").GetDouble() / inches * BallisticSolver.MoaPerRadian;
                double jsWind = r.GetProperty("windDrift").GetDouble() / inches * BallisticSolver.MoaPerRadian;
                double jsTime = r.GetProperty("tof").GetDouble();
                double drop = -p.DropInches / inches * BallisticSolver.MoaPerRadian, wind = p.WindInches / inches * BallisticSolver.MoaPerRadian;

                // The JavaScript rounds its rows for display, so the allowance carries that rounding as well as the tolerance.
                double rounding = 0.005 / inches * BallisticSolver.MoaPerRadian;
                double dropAllowed = BallisticsIndependentTests.DropAbsoluteMoa + (BallisticsIndependentTests.DropShare * Math.Abs(jsDrop)) + rounding;
                double windAllowed = BallisticsIndependentTests.WindAbsoluteMoa + (BallisticsIndependentTests.WindShare * Math.Abs(jsWind)) + rounding;
                double timeShare = Math.Abs(p.TimeOfFlight - jsTime) / Math.Max(jsTime, 1e-9);
                worstDrop = Math.Max(worstDrop, Math.Abs(drop - jsDrop) / dropAllowed);
                worstWind = Math.Max(worstWind, Math.Abs(wind - jsWind) / windAllowed);
                worstTime = Math.Max(worstTime, timeShare / BallisticsIndependentTests.TimeShare);
                if (Math.Abs(drop - jsDrop) > dropAllowed)
                {
                    failures.Add(string.Create(CultureInfo.InvariantCulture, $"{name} at {range} yd: drop {jsDrop:0.000} MOA in the corrected JavaScript against the port's {drop:0.000}, allowed {dropAllowed:0.000}"));
                }

                if (Math.Abs(wind - jsWind) > windAllowed)
                {
                    failures.Add(string.Create(CultureInfo.InvariantCulture, $"{name} at {range} yd: wind {jsWind:0.000} MOA against {wind:0.000}, allowed {windAllowed:0.000}"));
                }

                if (timeShare > BallisticsIndependentTests.TimeShare)
                {
                    failures.Add(string.Create(CultureInfo.InvariantCulture, $"{name} at {range} yd: time of flight {jsTime:0.0000} s against {p.TimeOfFlight:0.0000}"));
                }

                compared++;
            }
        }

        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{compared} rows compared; largest share of the allowance used: drop {worstDrop:0.00}, wind {worstWind:0.00}, time of flight {worstTime:0.00}"));
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
        Assert.Equal(60, compared);
    }
}
