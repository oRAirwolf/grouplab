using System.Diagnostics;

namespace GroupLab.Cli.Bench;

/// <summary>
/// Runs each case enough times to be stable and reports the median and the spread, NOTES-FROM-PLANNING.md entry 117 section 4. One run is
/// thrown away first, because the first run of anything in .NET pays for its own compilation, and a record that included it would measure the
/// runtime rather than GroupLab.
/// </summary>
public static class BenchRunner
{
    /// <summary>How many timed runs a case gets when nobody says otherwise. Odd, so the median is a run and not an average of two.</summary>
    public const int DefaultRuns = 5;

    /// <summary>
    /// Measures one case, and every part it files. A case that throws is recorded as not measured, with what it threw: entry 117 section 3b,
    /// "where a case cannot be measured honestly, say so in the record rather than leaving a blank or inventing a number".
    /// </summary>
    public static IReadOnlyList<BenchMeasurement> Measure(BenchCase benchCase, int runs)
    {
        ArgumentNullException.ThrowIfNull(benchCase);
        var sink = new BenchSink();
        string? note;
        try
        {
            note = benchCase.Run(sink);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return [new BenchMeasurement(benchCase.Area, benchCase.Name, benchCase.What, 0, 0, 0, 0, null, Reason(ex))];
        }

        sink.Clear();
        var times = new List<double>(runs);
        for (int i = 0; i < runs; i++)
        {
            var clock = Stopwatch.StartNew();
            try
            {
                note = benchCase.Run(sink) ?? note;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return [new BenchMeasurement(benchCase.Area, benchCase.Name, benchCase.What, i, 0, 0, 0, note, Reason(ex))];
            }

            times.Add(clock.Elapsed.TotalMilliseconds);
        }

        var measured = new List<BenchMeasurement> { Summarise(benchCase.Area, benchCase.Name, benchCase.What, times, note, false) };
        foreach (var (name, part) in sink.Parts)
        {
            measured.Add(Summarise(benchCase.Area, benchCase.Name + ": " + name, "part of " + benchCase.Name, part, null, true));
        }

        return measured;
    }

    /// <summary>Every case, in the order they were given, each reported as it finishes so a long run says where it is.</summary>
    public static IReadOnlyList<BenchMeasurement> Run(IEnumerable<BenchCase> cases, int runs, Action<BenchMeasurement>? filed = null)
    {
        ArgumentNullException.ThrowIfNull(cases);
        var measured = new List<BenchMeasurement>();
        foreach (var benchCase in cases)
        {
            foreach (var measurement in Measure(benchCase, runs))
            {
                measured.Add(measurement);
                filed?.Invoke(measurement);
            }
        }

        return measured;
    }

    private static BenchMeasurement Summarise(string area, string name, string what, List<double> times, string? note, bool part)
    {
        if (times.Count == 0)
        {
            return new BenchMeasurement(area, name, what, 0, 0, 0, 0, note, "nothing was timed", part);
        }

        var sorted = new List<double>(times);
        sorted.Sort();
        return new BenchMeasurement(area, name, what, sorted.Count, Median(sorted), sorted[0], sorted[^1], note, null, part);
    }

    private static string Reason(Exception ex) => ex.GetType().Name + ": " + ex.Message.ReplaceLineEndings(" ");

    private static double Median(IReadOnlyList<double> sorted) =>
        sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2;
}
