using System.Globalization;

namespace GroupLab.Core.Records;

/// <summary>One shot beside one reading, either of which may be absent: a reading that belongs to no shot, or a shot with no reading.</summary>
public sealed record ChronographPair(int? ShotId, int? Reading);

/// <summary>The velocities' own figures, over the readings kept: how many, their mean, and their standard deviation.</summary>
public sealed record VelocitySpread(int Readings, double MeanFps, double SdFps);

/// <summary>
/// Reconciling a chronograph's readings with the shots, NOTES-FROM-PLANNING.md entry 115 section 3 and DESIGN.md section 15: <b>the shot
/// sequence and the chronograph sequence are separate ordered lists that get reconciled, never assumed to align.</b> Chronographs drop shots,
/// record a neighbour's shot from the next bench, and log the fouling round fired into the berm, so an in-order pairing is a proposal a person
/// accepts, not a fact. A reading marked as belonging to no shot, or a shot marked as having none, is left out and the rest pair in order.
/// Nothing here assumes every shot has a velocity.
/// </summary>
public static class Chronograph
{
    /// <summary>
    /// The shots and readings side by side: those not marked as spare pair in order, and whichever list is longer keeps its tail with nothing
    /// beside it. The pairs are in shot order, with the readings that belong to no shot after them.
    /// </summary>
    public static IReadOnlyList<ChronographPair> Pair(
        IReadOnlyList<int> shots,
        IReadOnlyList<double> readings,
        IReadOnlySet<int>? shotsWithNoReading = null,
        IReadOnlySet<int>? readingsOfNoShot = null)
    {
        ArgumentNullException.ThrowIfNull(shots);
        ArgumentNullException.ThrowIfNull(readings);
        var kept = Enumerable.Range(0, readings.Count).Where(i => readingsOfNoShot?.Contains(i) != true).ToList();
        var pairs = new List<ChronographPair>();
        int next = 0;
        foreach (int shot in shots)
        {
            if (shotsWithNoReading?.Contains(shot) == true || next >= kept.Count)
            {
                pairs.Add(new ChronographPair(shot, null));
                continue;
            }

            pairs.Add(new ChronographPair(shot, kept[next++]));
        }

        pairs.AddRange(kept.Skip(next).Select(i => new ChronographPair(null, i)));
        pairs.AddRange((readingsOfNoShot ?? new HashSet<int>()).Order().Select(i => new ChronographPair(null, i)));
        return pairs;
    }

    /// <summary>What the two lists say about each other, in the words the screen shows before anybody accepts a mapping.</summary>
    public static string Describe(IReadOnlyList<ChronographPair> pairs, int readings)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        int matched = pairs.Count(p => p is { ShotId: not null, Reading: not null });
        int withoutReading = pairs.Count(p => p is { ShotId: not null, Reading: null });
        int withoutShot = pairs.Count(p => p is { ShotId: null, Reading: not null });
        var inv = CultureInfo.InvariantCulture;
        string counts = string.Create(inv, $"{matched} of {readings} readings sit beside a shot");
        if (withoutReading == 0 && withoutShot == 0)
        {
            return counts + ". The counts agree; check the order before accepting it, because a chronograph can miss a shot and record a neighbor's.";
        }

        var parts = new List<string>();
        if (withoutReading > 0)
        {
            parts.Add(string.Create(inv, $"{withoutReading} shot{(withoutReading == 1 ? " has" : "s have")} no reading"));
        }

        if (withoutShot > 0)
        {
            parts.Add(string.Create(inv, $"{withoutShot} reading{(withoutShot == 1 ? " belongs" : "s belong")} to no shot"));
        }

        return counts + ", and " + string.Join(" and ", parts) + ". Mark the odd ones and the rest pair in order.";
    }

    /// <summary>The readings' mean and standard deviation, on n minus 1, over the readings given; null where there are fewer than two.</summary>
    public static VelocitySpread? Spread(IReadOnlyList<double> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);
        if (readings.Count < 2)
        {
            return null;
        }

        double mean = readings.Average();
        double variance = readings.Sum(v => (v - mean) * (v - mean)) / (readings.Count - 1);
        return new VelocitySpread(readings.Count, mean, Math.Sqrt(variance));
    }

    /// <summary>
    /// How well a handful of shots pins down the rifle's velocity SD, NOTES-FROM-PLANNING.md entry 141 section 5.2.5.
    /// <para>
    /// <b>This is the figure the whole chronograph industry reports without it.</b> An SD of 10 ft/s from ten shots is not a rifle that
    /// holds 10 ft/s: the same rifle measured again could read 7 or 19, and nothing about the number 10 says so. The interval is
    /// chi-squared on n minus 1 degrees of freedom, which is exact when the velocities are normal, and it is wide at every sample size a
    /// person actually shoots. That width is the point.
    /// </para>
    /// </summary>
    /// <param name="readings">How many velocities the SD was computed from.</param>
    /// <param name="sdFps">The sample standard deviation.</param>
    /// <param name="confidence">The interval's coverage, 0.95 by default.</param>
    /// <returns>The interval, or null where there are fewer than two readings and the SD means nothing.</returns>
    public static (double LowerFps, double UpperFps)? SdInterval(int readings, double sdFps, double confidence = 0.95)
    {
        if (readings < 2 || !(sdFps >= 0) || !(confidence > 0) || !(confidence < 1))
        {
            return null;
        }

        double df = readings - 1;
        double tail = (1 - confidence) / 2;

        // The larger chi-squared quantile makes the smaller SD: the interval is built from the variance and turned back at the end.
        double high = Statistics.Distributions.ChiSquareQuantile(1 - tail, df);
        double low = Statistics.Distributions.ChiSquareQuantile(tail, df);
        return low <= 0 || high <= 0
            ? null
            : (sdFps * Math.Sqrt(df / high), sdFps * Math.Sqrt(df / low));
    }

    /// <summary>The largest reading less the smallest, the extreme spread, or null where there are fewer than two readings.</summary>
    public static double? ExtremeSpreadFps(IReadOnlyList<double> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);
        return readings.Count < 2 ? null : readings.Max() - readings.Min();
    }

    /// <summary>
    /// A list of velocities as a person pastes it: separated by commas, spaces or new lines, in any mixture. The reason names the first thing
    /// that is not a velocity, rather than dropping it silently.
    /// </summary>
    public static (IReadOnlyList<double> Velocities, string? Refusal) Read(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var velocities = new List<double>();
        foreach (string word in text.Split([',', ';', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (!double.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out double velocity))
            {
                return ([], $"\"{word}\" is not a velocity. Paste the readings as numbers, separated by commas, spaces or new lines.");
            }

            if (velocity is <= 0 or > 10000)
            {
                return ([], string.Create(CultureInfo.InvariantCulture, $"{velocity:0.#} is not a muzzle velocity in ft/s. Check the list, and the chronograph's units."));
            }

            velocities.Add(velocity);
        }

        return velocities.Count == 0 ? ([], "There are no readings in that.") : (velocities, null);
    }
}
