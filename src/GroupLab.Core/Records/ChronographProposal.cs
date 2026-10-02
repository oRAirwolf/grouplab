using System.Globalization;

namespace GroupLab.Core.Records;

/// <summary>
/// The marks a file's own evidence proposes before anybody pairs a string with the shots: the readings that look as if they belong to no shot
/// of this group, the shots that look as if they have no reading, and a plain sentence for each saying why. A person accepts, changes or
/// clears them; nothing here is applied without that.
/// </summary>
public sealed record ChronographProposal(IReadOnlySet<int> ShotsWithNoReading, IReadOnlySet<int> ReadingsOfNoShot, IReadOnlyList<string> Reasons)
{
    /// <summary>
    /// Entry 351: what each reading of no shot was proposed as, by the evidence that put it there: another group, from the pauses; left out,
    /// where the chronograph left it out of its own figures; or this group's clean bore shot. A reading not here was not proposed apart.
    /// </summary>
    public IReadOnlyDictionary<int, ReadingGoesWith> Kinds { get; init; } = new Dictionary<int, ReadingGoesWith>();

    /// <summary>The pairing with these marks, as <see cref="Chronograph.Pair"/> makes it.</summary>
    public IReadOnlyList<ChronographPair> Pairs(IReadOnlyList<int> shots, IReadOnlyList<double> readings) =>
        Chronograph.Pair(shots, readings, ShotsWithNoReading, ReadingsOfNoShot);
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 342, worker B item 2, and DESIGN.md sections 15 and 17: when a session has marked shots and an imported string
/// that numbers its shots, propose which readings and shots stand apart, from what the file itself says. Three kinds of evidence, in this
/// order. <b>Time:</b> a chronograph that times each shot (the Garmin Xero does) shows the groups of a string as runs of readings separated by
/// pauses; where exactly one stretch of runs has as many readings as the group has shots, the readings outside it are proposed as belonging to
/// no shot of this group. <b>The chronograph's own marks:</b> a reading the Xero left out of its own figures ("--"), then one marked clean
/// bore, the fouling shot often fired off the paper. <b>Its numbering:</b> where there are more shots than readings, a number the chronograph
/// skips is a shot deleted on it, and the shot fired in that place is proposed as having no reading. Where nothing in the file says which,
/// nothing is proposed and the sentence says so, because guessing is how the numbers go quietly wrong.
/// </summary>
public static class ChronographReconciliation
{
    /// <summary>
    /// The shortest pause that separates two runs of readings, whatever the string's own rhythm: five minutes, because Alan's 2026-04-25 string
    /// has a pause of 3 min 18 s inside its last group, and 7.5 minutes or more between groups.
    /// </summary>
    public static readonly TimeSpan ShortestPause = TimeSpan.FromMinutes(5);

    /// <summary>A pause separates runs only when it is also this many times the string's median interval between shots.</summary>
    public const double PauseOverMedian = 3;

    /// <summary>
    /// The proposal for these shots, in firing order, against these readings, in the order the chronograph recorded them; reading i is the
    /// string's velocity i, counted from 0 as <see cref="Chronograph.Pair"/> counts them.
    /// </summary>
    public static ChronographProposal Propose(IReadOnlyList<int> shots, IReadOnlyList<ChronographShot> readings)
    {
        ArgumentNullException.ThrowIfNull(shots);
        ArgumentNullException.ThrowIfNull(readings);
        var noShot = new SortedSet<int>();
        var noReading = new HashSet<int>();
        var kinds = new Dictionary<int, ReadingGoesWith>();
        var reasons = new List<string>();
        var inv = CultureInfo.InvariantCulture;
        int excess = readings.Count - shots.Count;
        bool saidWhich = false;
        if (shots.Count == 0 || readings.Count == 0)
        {
            return new ChronographProposal(noReading, noShot, reasons) { Kinds = kinds };
        }

        if (excess > 0)
        {
            var runs = Runs(readings);
            if (runs.Count > 1)
            {
                var fits = new List<(int First, int Last)>();
                for (int a = 0; a < runs.Count; a++)
                {
                    for (int b = a; b < runs.Count; b++)
                    {
                        if (runs[b].Last - runs[a].First + 1 == shots.Count)
                        {
                            fits.Add((runs[a].First, runs[b].Last));
                        }
                    }
                }

                if (fits.Count == 1)
                {
                    var (first, last) = fits[0];
                    for (int i = 0; i < readings.Count; i++)
                    {
                        if (i < first || i > last)
                        {
                            noShot.Add(i);
                            kinds[i] = ReadingGoesWith.NotThisGroup;
                        }
                    }

                    reasons.Add(string.Create(inv,
                        $"The chronograph timed {runs.Count} runs of shots with pauses between them; only shots {readings[first].Number} to {readings[last].Number} ({Clock(readings[first])} to {Clock(readings[last])}) are as many as this group's {shots.Count}, so the other {noShot.Count} are proposed as belonging to no shot of it."));
                    excess = 0;
                }
                else if (fits.Count > 1)
                {
                    saidWhich = true;
                    reasons.Add(string.Create(inv,
                        $"The chronograph timed {runs.Count} runs of shots with pauses between them, and {fits.Count} stretches of them are as many as this group's {shots.Count}; mark the readings that were not this group."));
                }
            }
        }

        if (excess > 0)
        {
            excess -= Mark(readings, noShot, kinds, ReadingGoesWith.LeftOut, excess, r => r.LeftOutByChronograph, reasons,
                n => $"Shot {n} was left out of the chronograph's own figures, so it is proposed as belonging to no shot of this group.");
        }

        if (excess > 0)
        {
            excess -= Mark(readings, noShot, kinds, ReadingGoesWith.CleanBore, excess, r => r.CleanBore, reasons,
                n => $"Shot {n} is marked clean bore on the chronograph, often a fouling shot fired off the paper, so it is proposed as belonging to no shot of this group.");
        }

        if (excess > 0 && !saidWhich)
        {
            reasons.Add(string.Create(inv,
                $"There {(excess == 1 ? "is 1 more reading" : $"are {excess} more readings")} than shots and nothing in the file says which; mark {(excess == 1 ? "it" : "them")}."));
        }

        if (excess < 0)
        {
            int missing = -excess;
            int first = readings[0].Number;
            var numbers = readings.Select(r => r.Number).ToHashSet();
            for (int n = first + 1; n < readings[^1].Number && missing > 0; n++)
            {
                int at = n - first;
                if (!numbers.Contains(n) && at < shots.Count)
                {
                    noReading.Add(shots[at]);
                    missing--;
                    reasons.Add(string.Create(inv,
                        $"The chronograph's numbering skips shot {n}, deleted on the chronograph, so the shot fired in that place is proposed as having no reading."));
                }
            }

            if (missing > 0)
            {
                reasons.Add(string.Create(inv,
                    $"The chronograph has {(missing == 1 ? "1 reading" : $"{missing} readings")} fewer than the shots and nothing in the file says which it missed; the last {(missing == 1 ? "shot goes" : "shots go")} without until you mark the right {(missing == 1 ? "one" : "ones")}."));
            }
        }

        return new ChronographProposal(noReading, noShot, reasons) { Kinds = kinds };
    }

    /// <summary>
    /// The runs of readings the chronograph timed, split where a pause is longer than <see cref="ShortestPause"/> and <see cref="PauseOverMedian"/>
    /// times the median interval; one run when any reading has no time or the times go backwards.
    /// </summary>
    public static IReadOnlyList<(int First, int Last)> Runs(IReadOnlyList<ChronographShot> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);
        if (readings.Count < 2 || readings.Any(r => r.Time is null))
        {
            return readings.Count == 0 ? [] : [(0, readings.Count - 1)];
        }

        var gaps = new List<TimeSpan>();
        for (int i = 1; i < readings.Count; i++)
        {
            var gap = readings[i].Time!.Value - readings[i - 1].Time!.Value;
            if (gap < TimeSpan.Zero)
            {
                return [(0, readings.Count - 1)];
            }

            gaps.Add(gap);
        }

        var sorted = gaps.Order().ToList();
        var median = sorted[sorted.Count / 2];
        var pause = TimeSpan.FromTicks(Math.Max(ShortestPause.Ticks, (long)(median.Ticks * PauseOverMedian)));
        var runs = new List<(int First, int Last)>();
        int start = 0;
        for (int i = 1; i < readings.Count; i++)
        {
            if (gaps[i - 1] > pause)
            {
                runs.Add((start, i - 1));
                start = i;
            }
        }

        runs.Add((start, readings.Count - 1));
        return runs;
    }

    private static int Mark(IReadOnlyList<ChronographShot> readings, SortedSet<int> noShot, Dictionary<int, ReadingGoesWith> kinds, ReadingGoesWith kind, int most,
        Func<ChronographShot, bool> which, List<string> reasons, Func<int, string> why)
    {
        int marked = 0;
        for (int i = 0; i < readings.Count && marked < most; i++)
        {
            if (which(readings[i]) && noShot.Add(i))
            {
                kinds[i] = kind;
                marked++;
                reasons.Add(why(readings[i].Number));
            }
        }

        return marked;
    }

    private static string Clock(ChronographShot reading) => reading.Time is { } t ? t.ToString(@"hh\:mm", CultureInfo.InvariantCulture) : "";
}
