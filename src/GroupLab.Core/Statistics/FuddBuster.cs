using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Statistics;

/// <summary>Three shots drawn from the group: which ones, by their place in it, and their extreme spread in inches.</summary>
public sealed record ThreeShots(IReadOnlyList<int> Shots, double ExtremeSpreadInches);

/// <summary>
/// What averaging small groups would have said: the group split into groups of <see cref="GroupSize"/> shots, the extreme spreads of those
/// averaged, over <see cref="Splits"/> different splits; the mean of those averages, and the lowest and highest a single split gave.
/// </summary>
public sealed record SplitAverages(int GroupSize, int Splits, double MeanInches, double LowestInches, double HighestInches);

/// <summary>
/// One step of the zero chase: shots <see cref="First"/> to <see cref="Last"/>, counted from 1 in the order fired, their center, how far that
/// center is from the center of every shot, and the correction a shooter setting the zero to it would have dialled, in clicks where the
/// scope's click value and the distance are known.
/// </summary>
public sealed record ChaseStep(int First, int Last, PointD CentreInches, double FromTrueCentreInches, Clicks? Across, Clicks? Up);

/// <summary>The three lessons of <see cref="FuddBuster"/>, from one result's own shots.</summary>
public sealed record SmallSampleLesson(
    int Shots,
    ulong Seed,
    ThreeShots Tightest,
    ThreeShots Widest,
    double AllShotsExtremeSpreadInches,
    double MeanRadiusInches,
    SplitAverages Threes,
    SplitAverages Fives,
    PointD TrueCentreInches,
    IReadOnlyList<ChaseStep> Chase);

/// <summary>
/// "Fudd buster mode", NOTES-FROM-PLANNING.md entry 279 section 3: Unholy's idea and his name for it, kept by Alan (entry 280 section 3).
/// After an analysis of <see cref="LeastShots"/> or more, the shooter's own shots show why small samples mislead, in three ways:
/// <list type="number">
/// <item>Random three-shot groups drawn from his own shots, the tightest and the widest, which came from the same gun, the same ammunition
/// and the same range trip.</item>
/// <item>What averaging three-shot or five-shot groups would have given, against the figures from every shot, and how much that average
/// moves with how the shots happen to be split. Averaging small groups' extreme spreads is biased low, since a small group's extreme
/// spread is smaller than a large one's from the same rifle, and it is still noisy; that is what the numbers show, not that it is invalid.</item>
/// <item>The zero chase: the shots in the order fired, five at a time, and the zero set to each five-shot center in turn; how far each such
/// correction is from the rifle's true center, the center of every shot, and how many clicks each would have been.</item>
/// </list>
/// Pure functions of the shots. The random draws use a seed made from the shots themselves, so the same result always shows the same
/// examples. The window that shows them waits for planning's concept and Alan's choice (entry 281 section 2 chose board FuddA).
/// </summary>
public static class FuddBuster
{
    /// <summary>The fewest shots the lessons are worked out for.</summary>
    public const int LeastShots = 20;

    /// <summary>How many random three-shot groups are drawn to find the tightest and the widest.</summary>
    public const int ThreeShotDraws = 2000;

    /// <summary>How many different splits of the shots into small groups are averaged.</summary>
    public const int SplitCount = 200;

    /// <summary>The zero chase's step: five shots at a time.</summary>
    public const int ChaseShots = 5;

    /// <summary>
    /// The lessons from shots given in the order fired, as offsets from the aim point in inches on the screen's axes (right and down
    /// positive), or null below <see cref="LeastShots"/>. The clicks need the rifle's click value and the distance shot.
    /// </summary>
    public static SmallSampleLesson? Of(IReadOnlyList<PointD> shots, Rifle? rifle = null, double? distanceInches = null)
    {
        ArgumentNullException.ThrowIfNull(shots);
        if (shots.Count < LeastShots)
        {
            return null;
        }

        ulong seed = SeedOf(shots);
        var random = new StatisticsRandom(seed);
        int n = shots.Count;

        ThreeShots? tightest = null, widest = null;
        for (int d = 0; d < ThreeShotDraws; d++)
        {
            int a = random.NextInt(n), b = random.NextInt(n - 1), c = random.NextInt(n - 2);
            b += b >= a ? 1 : 0;
            int lo = Math.Min(a, b), hi = Math.Max(a, b);
            c += c >= lo ? 1 : 0;
            c += c >= hi ? 1 : 0;
            int[] three = [a, b, c];
            Array.Sort(three);
            double spread = ExtremeSpread(three.Select(i => shots[i]).ToList());
            if (tightest is null || spread < tightest.ExtremeSpreadInches)
            {
                tightest = new ThreeShots(three, spread);
            }

            if (widest is null || spread > widest.ExtremeSpreadInches)
            {
                widest = new ThreeShots(three, spread);
            }
        }

        var centre = GroupStatistics.Centre(shots);
        var chase = new List<ChaseStep>();
        for (int first = 0; first + ChaseShots <= n; first += ChaseShots)
        {
            var five = shots.Skip(first).Take(ChaseShots).ToList();
            var at = GroupStatistics.Centre(five);
            double off = Math.Sqrt(Math.Pow(at.X - centre.X, 2) + Math.Pow(at.Y - centre.Y, 2));
            Clicks? Dial(double inches, string towardPositive, string towardNegative) =>
                rifle is not null && distanceInches is > 0 && inches != 0 ? Clicks.For(inches, distanceInches.Value, rifle, inches > 0 ? towardNegative : towardPositive) : null;
            // The correction moves the impact onto the aim: a center right is dialled left, a center low (down positive) is dialled up.
            chase.Add(new ChaseStep(first + 1, first + ChaseShots, at, off, Dial(at.X, "right", "left"), Dial(at.Y, "down", "up")));
        }

        return new SmallSampleLesson(
            n,
            seed,
            tightest!,
            widest!,
            ExtremeSpread(shots),
            GroupStatistics.Rayleigh(shots).MeanRadius.Value, // the mean radius GroupLab shows everywhere (entry 278 section 5)
            Averages(shots, 3, random),
            Averages(shots, 5, random),
            centre,
            chase);
    }

    /// <summary>The largest center-to-center distance between any two of the shots.</summary>
    public static double ExtremeSpread(IReadOnlyList<PointD> shots)
    {
        ArgumentNullException.ThrowIfNull(shots);
        double most = 0;
        for (int i = 0; i < shots.Count; i++)
        {
            for (int j = i + 1; j < shots.Count; j++)
            {
                most = Math.Max(most, Math.Sqrt(Math.Pow(shots[i].X - shots[j].X, 2) + Math.Pow(shots[i].Y - shots[j].Y, 2)));
            }
        }

        return most;
    }

    /// <summary>
    /// The seed for a result: the shots' positions to a thousandth of an inch, folded into one number, so the same shots give the same
    /// examples every time and different shots different ones.
    /// </summary>
    public static ulong SeedOf(IReadOnlyList<PointD> shots)
    {
        ArgumentNullException.ThrowIfNull(shots);
        ulong h = 1469598103934665603UL;
        foreach (var s in shots)
        {
            foreach (long v in new[] { (long)Math.Round(s.X * 1000), (long)Math.Round(s.Y * 1000) })
            {
                h ^= unchecked((ulong)v);
                h = unchecked(h * 1099511628211UL);
            }
        }

        return h;
    }

    private static SplitAverages Averages(IReadOnlyList<PointD> shots, int size, StatisticsRandom random)
    {
        int n = shots.Count, groups = n / size;
        var order = Enumerable.Range(0, n).ToArray();
        var averages = new double[SplitCount];
        for (int s = 0; s < SplitCount; s++)
        {
            // A fresh random order each split, then consecutive groups of the given size; the few shots left over sit out.
            for (int i = n - 1; i > 0; i--)
            {
                int j = random.NextInt(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            double total = 0;
            for (int g = 0; g < groups; g++)
            {
                total += ExtremeSpread([.. order.Skip(g * size).Take(size).Select(i => shots[i])]);
            }

            averages[s] = total / groups;
        }

        return new SplitAverages(size, SplitCount, averages.Average(), averages.Min(), averages.Max());
    }
}

/// <summary>
/// The words of "Fudd buster mode", NOTES-FROM-PLANNING.md entry 281 section 2, page A: one page, three sections, the same words on the phone
/// and in the desktop's window. Each sentence says exactly what the numbers are: averaging small groups' extreme spreads is biased low and
/// still noisy, which is what the numbers show; it is never called invalid.
/// </summary>
public static class FuddBusterWords
{
    public const string Title = "Fudd buster mode";

    public const string Credit = "Unholy's idea.";

    public const string SameRifle = "These shots came from the same gun, with the same ammunition, on the same range trip.";

    /// <summary>Section 1: the tightest and the widest three shots drawn from the group.</summary>
    public static IReadOnlyList<string> Threes(SmallSampleLesson lesson, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(lesson);
        ArgumentNullException.ThrowIfNull(units);
        return
        [
            $"The tightest three shots GroupLab drew from your {lesson.Shots}: {units.Length(lesson.Tightest.ExtremeSpreadInches)} across (shots {Shots(lesson.Tightest)}).",
            $"The widest three: {units.Length(lesson.Widest.ExtremeSpreadInches)} across (shots {Shots(lesson.Widest)}).",
            SameRifle,
        ];
    }

    /// <summary>Section 2: what averaging three-shot or five-shot groups would have said, against every shot.</summary>
    public static IReadOnlyList<string> Averages(SmallSampleLesson lesson, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(lesson);
        ArgumentNullException.ThrowIfNull(units);
        string Line(SplitAverages a) =>
            $"Averaged as {a.GroupSize}-shot groups: {units.Length(a.MeanInches)}, and from {units.Length(a.LowestInches)} to {units.Length(a.HighestInches)} depending only on how the shots happened to fall into groups ({a.Splits} ways tried).";
        return
        [
            $"All {lesson.Shots} shots: extreme spread {units.Length(lesson.AllShotsExtremeSpreadInches)}, mean radius {units.Length(lesson.MeanRadiusInches)}.",
            Line(lesson.Threes),
            Line(lesson.Fives),
            "Averaging small groups' extreme spreads comes out smaller than the whole group's, because a few shots rarely include the rifle's widest, and it still moves with the luck of the split. The mean radius of every shot is the steadier measure.",
        ];
    }

    /// <summary>Section 3: the zero chased five shots at a time.</summary>
    public static IReadOnlyList<string> Chase(SmallSampleLesson lesson, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(lesson);
        ArgumentNullException.ThrowIfNull(units);
        var lines = new List<string>
        {
            $"The rifle's true center is taken as the center of all {lesson.Shots} shots. Setting the zero to each group of five in turn would have moved it like this:",
        };
        foreach (var step in lesson.Chase)
        {
            string clicks = step.Across is null && step.Up is null ? ""
                : "; " + string.Join(", ", new[] { step.Across?.Describe(), step.Up?.Describe() }.Where(c => c is not null && !c.StartsWith("0 ", StringComparison.Ordinal)));
            lines.Add($"Shots {step.First} to {step.Last}: {units.Length(step.FromTrueCentreInches)} from the true center{clicks.TrimEnd(';', ' ')}.");
        }

        lines.Add("Every one of those corrections chased where five shots happened to land, not where the rifle shoots.");
        return lines;
    }

    private static string Shots(ThreeShots three) => string.Join(", ", three.Shots.Select(i => (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)));
}
