using System.Globalization;
using System.Text.Json.Nodes;

namespace GroupLab.Cli.Bench;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 388 section 4, Phase 9's baseline gate: the times a person waits, measured once and committed in
/// <c>docs/performance-baseline.json</c>, and a later measurement on the same machine held to them. A figure may be up to
/// <see cref="Slower"/> slower, and never fails by less than <see cref="FloorMs"/>, because a desktop's own load moves a short figure by more
/// than that between runs (entry 342 saw 25 percent from a second worker alone). The gate is run by hand on the machine the baseline names,
/// with <c>grouplab bench --gate docs/performance-baseline.json</c>; CI reports and does not gate, as docs/PERFORMANCE.md's method says.
/// </summary>
public static class BenchGate
{
    /// <summary>How much slower than its baseline a figure may be before the gate fails: a quarter.</summary>
    public const double Slower = 0.25;

    /// <summary>The least a figure must have slowed by, in milliseconds, before the gate fails.</summary>
    public const double FloorMs = 20;

    /// <summary>A case's key in the baseline: its area and name, as the record's table heads them.</summary>
    public static string Key(BenchMeasurement m) => m.Area + " / " + m.Name;

    /// <summary>
    /// The lines the gate prints, one per figure the baseline names, and whether every one passed. A figure the baseline names and this
    /// run did not measure fails too: a gate that quietly stops checking a case is no gate.
    /// </summary>
    public static (IReadOnlyList<string> Lines, bool Passed) Check(IReadOnlyList<BenchMeasurement> measured, JsonObject baseline)
    {
        ArgumentNullException.ThrowIfNull(measured);
        ArgumentNullException.ThrowIfNull(baseline);
        var lines = new List<string>();
        bool passed = true;
        var cases = measured.GroupBy(Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        foreach (var (key, value) in baseline["bench"]?.AsObject() ?? [])
        {
            double was = value!.GetValue<double>();
            if (!cases.TryGetValue(key, out var now) || now.Failure is not null)
            {
                lines.Add($"FAILED  {key}: in the baseline at {was:0} ms and not measured now");
                passed = false;
                continue;
            }

            bool slow = now.MedianMs > was * (1 + Slower) && now.MedianMs - was > FloorMs;
            passed &= !slow;
            lines.Add(string.Create(CultureInfo.InvariantCulture,
                $"{(slow ? "FAILED" : "ok"),-7} {key}: {now.MedianMs:0} ms against {was:0} ms ({(now.MedianMs - was) / was * 100:+0;-0;0} percent)"));
        }

        return (lines, passed);
    }

    /// <summary>
    /// The figures the baseline holds, in a person's terms (entry 388 section 4): opening a scan and a photograph, identification, the
    /// hole detection on its own, the analysis, and the whole path from a file to the figures, alone and ten at a time.
    /// </summary>
    public static IReadOnlyList<string> Gated { get; } =
    [
        "images / load a 600 dpi Letter scan",
        "images / load a phone photograph",
        "measurement / identify the sheet from its codes",
        "measurement / the generated sheet, stage by stage",
        "measurement / a 600 dpi scan, stage by stage",
        "measurement / the generated sheet, stage by stage: S5-S8.holes",
        "measurement / a 600 dpi scan, stage by stage: S5-S8.holes",
        "measurement / find holes on a target GroupLab did not print",
        "statistics / the whole analysis of a marking",
        "end to end / one sheet from file to figures",
        "end to end / a batch of ten sheets",
    ];

    /// <summary>The baseline's own bench figures from a run, for writing the file the first time and whenever planning moves it.</summary>
    public static JsonObject Figures(IEnumerable<BenchMeasurement> measured, IEnumerable<string> keys)
    {
        var wanted = keys.ToHashSet(StringComparer.Ordinal);
        var figures = new JsonObject();
        foreach (var m in measured.Where(m => m.Failure is null && wanted.Contains(Key(m))).DistinctBy(Key))
        {
            figures[Key(m)] = Math.Round(m.MedianMs, 1);
        }

        return figures;
    }
}
