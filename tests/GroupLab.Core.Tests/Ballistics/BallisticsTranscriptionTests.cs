using System.Text.Json;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Ballistics;

/// <summary>
/// docs/BALLISTICS-VALIDATION.md section 1, NOTES-FROM-PLANNING.md entry 110 section 2f check 1: the port against ballistics.js itself, run
/// unchanged under Node on a GitHub runner by reference/ballistics-js/cases.js. It proves the transcription, not the physics. The port runs in
/// its JavaScript-compatible mode, so the comparison is made at the JavaScript's own sampled positions, and each quantity is allowed one unit
/// of the rounding the JavaScript applied to it. The helper functions are compared unrounded.
/// </summary>
public class BallisticsTranscriptionTests
{
    private static JsonElement Fixture() => JsonDocument.Parse(File.ReadAllText(Repo.PathTo("tests", "GroupLab.Core.Tests", "Fixtures", "ballistics-js.json"))).RootElement;

    private static JsonElement Cases() => JsonDocument.Parse(File.ReadAllText(Repo.PathTo("reference", "ballistics-cases.json"))).RootElement;

    /// <summary>A case from reference/ballistics-cases.json as the port's input.</summary>
    internal static BallisticInput Input(JsonElement c) => new(
        c.GetProperty("bc").GetDouble(),
        c.GetProperty("dragModel").GetString() == "G1" ? DragModel.G1 : DragModel.G7,
        c.GetProperty("muzzleVelocity").GetDouble(),
        c.GetProperty("bulletWeight").GetDouble(),
        SightHeightInches: c.GetProperty("sightHeight").GetDouble(),
        ZeroRangeYards: c.GetProperty("zeroRange").GetDouble(),
        TemperatureF: c.GetProperty("tempF").GetDouble(),
        PressureInHg: c.GetProperty("pressureInHg").GetDouble(),
        HumidityPct: c.GetProperty("humidityPct").GetDouble(),
        CrosswindMph: c.GetProperty("windSpeedMph").GetDouble());

    private static void Near(List<string> failures, string what, double expected, double actual, double allowed)
    {
        if (!(Math.Abs(expected - actual) <= allowed + 1e-9))
        {
            failures.Add($"{what}: ballistics.js {expected}, port {actual}");
        }
    }

    [Fact]
    public void EveryFlatFireRowMatchesTheJavaScriptToItsOwnRounding()
    {
        var cases = Cases();
        var js = Fixture().GetProperty("cases");
        var failures = new List<string>();
        int rows = 0;
        foreach (var c in cases.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            var expected = js.EnumerateArray().Single(e => e.GetProperty("name").GetString() == name).GetProperty("rows").EnumerateArray().ToList();
            var port = BallisticSolver.Solve(Input(c), cases.GetProperty("maxRange").GetDouble(), cases.GetProperty("rangeStep").GetDouble(), javaScriptCompatible: true).Points;
            Assert.True(expected.Count == port.Count, $"{name}: ballistics.js has {expected.Count} rows, the port {port.Count}");
            for (int i = 0; i < port.Count; i++)
            {
                var e = expected[i];
                var p = port[i];
                string at = $"{name} at {p.RangeYards} yd";
                Near(failures, at + " range", e.GetProperty("range").GetDouble(), p.RangeYards, 0);
                Near(failures, at + " velocity", e.GetProperty("velocity").GetDouble(), p.VelocityFps, 0.1);
                Near(failures, at + " energy", e.GetProperty("energy").GetDouble(), p.EnergyFtLb, 1);
                Near(failures, at + " drop", e.GetProperty("drop").GetDouble(), p.DropInches, 0.01);
                Near(failures, at + " drop MOA", e.GetProperty("dropMOA").GetDouble(), p.DropMoa, 0.01);
                Near(failures, at + " drop mil", e.GetProperty("dropMil").GetDouble(), p.DropMil, 0.01);
                Near(failures, at + " time of flight", e.GetProperty("tof").GetDouble(), p.TimeOfFlight, 0.001);
                Near(failures, at + " wind drift", e.GetProperty("windDrift").GetDouble(), p.WindInches, 0.01);
                Near(failures, at + " wind MOA", e.GetProperty("windMOA").GetDouble(), p.WindMoa, 0.01);
                Near(failures, at + " wind mil", e.GetProperty("windMil").GetDouble(), p.WindMil, 0.01);
                Near(failures, at + " Mach", e.GetProperty("mach").GetDouble(), p.Mach, 0.001);
                rows++;
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
        Assert.Equal(66, rows);
    }

    private static void Same(List<string> failures, string what, double expected, double actual)
    {
        if (!(Math.Abs(expected - actual) <= 1e-9 * Math.Max(1, Math.Abs(expected))))
        {
            failures.Add($"{what}: ballistics.js {expected:R}, port {actual:R}");
        }
    }

    /// <summary>
    /// The JavaScript's own drag tables, which the compatible mode flies, as it interpolates them, at every hundredth of Mach from 0 to 5.2. The
    /// solver itself flies the standard tables, an intentional difference (NOTES-FROM-PLANNING.md entry 111 section 1).
    /// </summary>
    [Fact]
    public void TheDragTablesInterpolateAsTheJavaScriptDoes()
    {
        var failures = new List<string>();
        foreach (var (name, model) in new[] { ("G1", DragModel.G1), ("G7", DragModel.G7) })
        {
            foreach (var pair in Fixture().GetProperty("tables").GetProperty(name).EnumerateArray())
            {
                double mach = pair[0].GetDouble();
                Same(failures, $"{name} at Mach {mach}", pair[1].GetDouble(), DragTables.Cd(mach, model, javaScript: true));
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>The atmosphere at 175 combinations of temperature, pressure and humidity, and station pressure at seven altitudes.</summary>
    [Fact]
    public void TheAtmosphereMatchesTheJavaScript()
    {
        var failures = new List<string>();
        foreach (var a in Fixture().GetProperty("atmospheres").EnumerateArray())
        {
            double t = a.GetProperty("tempF").GetDouble(), p = a.GetProperty("pressureInHg").GetDouble(), h = a.GetProperty("humidityPct").GetDouble();
            Same(failures, $"density ratio at {t} F {p} inHg {h}%", a.GetProperty("densityRatio").GetDouble(), Atmosphere.DensityRatio(t, p, h));
            Same(failures, $"speed of sound at {t} F {p} inHg {h}%", a.GetProperty("speedOfSound").GetDouble(), Atmosphere.SpeedOfSound(t, p, h));
        }

        foreach (var a in Fixture().GetProperty("altitudes").EnumerateArray())
        {
            double altitude = a.GetProperty("altitudeFt").GetDouble();
            Same(failures, $"pressure at {altitude} ft", a.GetProperty("pressureInHg").GetDouble(), Atmosphere.PressureFromAltitude(altitude));
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>Miller's stability at standard pressure, where the port's pressure term is 1, Litz's spin drift both ways, and the Coriolis horizontal term.</summary>
    [Fact]
    public void TheSpinAndCoriolisTermsMatchTheJavaScript()
    {
        var failures = new List<string>();
        var f = Fixture();
        foreach (var s in f.GetProperty("stability").EnumerateArray())
        {
            Same(failures, "Miller stability", s.GetProperty("sg").GetDouble(), Stability.MillerStability(s.GetProperty("twist").GetDouble(), s.GetProperty("diameter").GetDouble(),
                s.GetProperty("length").GetDouble(), s.GetProperty("weight").GetDouble(), s.GetProperty("velocity").GetDouble(), s.GetProperty("tempF").GetDouble()));
        }

        foreach (var s in f.GetProperty("spin").EnumerateArray())
        {
            double sg = s.GetProperty("sg").GetDouble(), tof = s.GetProperty("tof").GetDouble();
            Same(failures, $"spin drift right, SG {sg}, {tof} s", s.GetProperty("right").GetDouble(), Stability.SpinDriftInches(sg, tof, 1));
            Same(failures, $"spin drift left, SG {sg}, {tof} s", s.GetProperty("left").GetDouble(), Stability.SpinDriftInches(sg, tof, -1));
        }

        foreach (var c in f.GetProperty("coriolis").EnumerateArray())
        {
            Same(failures, "Coriolis horizontal", c.GetProperty("horizontal").GetDouble(),
                Stability.CoriolisHorizontalInches(c.GetProperty("latitude").GetDouble(), c.GetProperty("rangeFt").GetDouble(), c.GetProperty("tof").GetDouble()));
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }
}
