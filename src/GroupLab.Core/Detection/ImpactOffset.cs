using System.Globalization;

namespace GroupLab.Core.Detection;

/// <summary>A point in page dmm, used here for a hole, a bull or a translation between them.</summary>
public readonly record struct Offset(double X, double Y)
{
    public static Offset Zero => new(0, 0);

    public double Length => Math.Sqrt((X * X) + (Y * Y));

    public Offset Plus(Offset other) => new(X + other.X, Y + other.Y);

    public Offset Minus(Offset other) => new(X - other.X, Y - other.Y);

    /// <summary>The offset in inches, which is what a person is told and what a scope is dialled in.</summary>
    public string Describe(double dotsPerInch = 1) =>
        string.Create(CultureInfo.InvariantCulture, $"{X / 254.0:0.00} in right, {-Y / 254.0:0.00} in up");
}

/// <summary>One whole-sheet reading (entry 376 section A2): the bulls it gives the holes to, in order, the common offset, and its total distance.</summary>
public sealed record SheetReading(IReadOnlyList<int> Bulls, Offset Shift, double Cost);

/// <summary>
/// Entry 376 section A2: the reading taken, the readings that fit as well (the taken one first), and whether they are close enough that the
/// person must be asked which bulls were fired at.
/// </summary>
public sealed record WholeSheetReading(SheetReading Taken, IReadOnlyList<SheetReading> Choices, bool Ask);

/// <summary>
/// One subgroup's point of impact: the translation that best explains where its holes fell, and how sure that is.
/// </summary>
/// <param name="Name">What the subgroup is, for the screen: a load's name, or a row.</param>
/// <param name="Bulls">The bulls this subgroup was aimed at, by index into the definition's bull list.</param>
/// <param name="Shift">The translation from where the shots were aimed to where they landed.</param>
/// <param name="Certain">Whether the shift settled on one answer that clearly beat the alternatives.</param>
/// <param name="Why">What the screen says about it, in words.</param>
public sealed record ImpactOffset(string Name, IReadOnlyList<int> Bulls, Offset Shift, bool Certain, string Why)
{
    /// <summary>
    /// Whether the group landed far enough from the aim to be worth saying so. A tenth of an inch: below that it is the spread of the group
    /// talking rather than the rifle, and telling somebody to dial it would be telling them to chase noise.
    /// </summary>
    public bool Moved => Shift.Length > 25;
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 130 section 3 and entry 120 section 2: where a group of shots actually landed, found before
/// any hole is assigned to a bull.
/// <para>
/// <b>Why this exists, and it is the worst defect this project has found.</b> On scan 5 of the second range day, every shot
/// was measured against a bull it was not aimed at, because each hole was nearer a neighbouring bull than its own. Nothing
/// looked wrong: the group came out tight, centred, and the zero correction said there was nothing to dial. A shooter would
/// have believed it. A group that is quietly wrong is worse than one that is obviously wrong, and worse than no group at
/// all, because it is the only kind that changes what somebody does at the range.
/// </para>
/// <para>
/// The fix is to stop assuming the shots landed where they were aimed. A rifle that is not zeroed puts its whole group
/// somewhere else on the sheet, and that somewhere else is the point of impact, which is the thing the shooter came to
/// measure. So: find one translation per subgroup that best explains the holes, assign in that shifted frame, and report
/// the translation rather than hiding it inside the assignment.
/// </para>
/// </summary>
public static class ImpactOffsets
{
    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 229 section 4: a sheet with at most one shot to each scoring bull, whose shots all landed off their
    /// bulls by the same amount, the rifle's zero on the day. The reading <see cref="ReadWholeSheet"/> takes, where it moves the shots more
    /// than a tenth of an inch; null where it does not apply or moves nothing, so a sheet shot at its own bulls is assigned as before.
    /// </summary>
    public static ImpactOffset? WholeSheet(IReadOnlyList<Offset> holes, IReadOnlyList<Offset> bulls, IReadOnlyList<int> scoring)
    {
        if (ReadWholeSheet(holes, bulls, scoring) is not { } read)
        {
            return null;
        }

        var found = new ImpactOffset("the sheet", read.Taken.Bulls, read.Taken.Shift, true, Words(read.Taken.Shift, holes.Count));
        return found.Moved ? found : null;
    }

    /// <summary>
    /// Entry 376 section A2: which bulls a sheet of one shot per bull was fired at, matched as one problem rather than hole by hole.
    /// <para>
    /// Every reading is a whole-sheet matching, one hole to a bull, around one common offset (Hungarian assignment, re-centred on the median
    /// and repeated). Two kinds are tried. <b>In shooting order</b>: bulls 1 to k, for k from the number of holes to a few more for holes not
    /// found, the fewest that fit as well as more do; people shoot a sheet from bull 1. <b>Anywhere</b>: every scoring bull a candidate.
    /// </para>
    /// <para>
    /// Holes that fit the bulls as aimed, with no offset, as well as any reading are read that way, so an ordinary sheet is untouched. Otherwise
    /// the shooting order is taken when it fits as well as any reading and its offset is less than the spacing between bulls; a rifle
    /// further off than that would have been hitting the next bull. Failing that, a single reading that fits clearly better than every other
    /// is taken. Otherwise the holes fit more than one answer and <see cref="WholeSheetReading.Ask"/> is set, so the person is asked which
    /// bulls were fired at; meanwhile the reading with the smallest offset is used.
    /// </para>
    /// <para>
    /// Alan's tablet photo of 5 October (bulls 1 to 15, an inch low and half an inch right) is why: the same holes fit bulls 6 to 20 exactly
    /// as well, and the rule this replaces let one hole in ten fall outside the order, so once the fifteenth hole was added by hand both
    /// readings passed and the sheet fell back to nearest bull.
    /// </para>
    /// Null where it does not apply: fewer than three holes, fewer than two scoring bulls, or more holes than scoring bulls.
    /// </summary>
    public static WholeSheetReading? ReadWholeSheet(IReadOnlyList<Offset> holes, IReadOnlyList<Offset> bulls, IReadOnlyList<int> scoring)
    {
        ArgumentNullException.ThrowIfNull(holes);
        ArgumentNullException.ThrowIfNull(bulls);
        ArgumentNullException.ThrowIfNull(scoring);
        var order = scoring.Where(i => i >= 0 && i < bulls.Count).Distinct().Order().ToList();
        int n = holes.Count;
        if (order.Count < 2 || n < 3 || n > order.Count)
        {
            return null;
        }

        var inOrder = new List<SheetReading>();
        for (int k = n; k <= Math.Min(order.Count, n + 1 + (n / 5)); k++)
        {
            inOrder.Add(Readings(holes, bulls, [.. order.Take(k)])[0]);
        }

        double fewest = inOrder.Min(r => r.Cost);
        var ordered = inOrder.First(r => Close(r.Cost, fewest, n));

        var anywhere = Readings(holes, bulls, order);
        double best = Math.Min(anywhere[0].Cost, ordered.Cost);

        // Holes that fit the bulls as aimed, with no offset at all, as well as any reading does are a sheet shot where it was aimed: read as
        // such, exactly as before any of this. Scan 4 of 20 September, its holes partly merged, fitted a shooting order 0.8 in off almost as
        // well, and nothing on that sheet was off its bull.
        var (asAimed, plain) = Match(holes, [.. order.Select(i => bulls[i])], Offset.Zero);
        if (Close(plain, best, n))
        {
            var aimed = new SheetReading([.. asAimed.Select(m => order[m]).Order()], Offset.Zero, plain);
            return new WholeSheetReading(aimed, [aimed], false);
        }

        bool orderFits = Close(ordered.Cost, best, n);
        var choices = new List<SheetReading>();
        foreach (var reading in (orderFits ? [ordered] : Enumerable.Empty<SheetReading>()).Concat(anywhere.Where(r => Close(r.Cost, best, n))))
        {
            if (!choices.Any(c => c.Shift.Minus(reading.Shift).Length <= SameOffset || c.Bulls.SequenceEqual(reading.Bulls)))
            {
                choices.Add(reading);
            }
        }

        if (orderFits && ordered.Shift.Length < Spacing(bulls, order))
        {
            return new WholeSheetReading(ordered, choices, false);
        }

        if (choices.Count == 1)
        {
            return new WholeSheetReading(choices[0], choices, false);
        }

        var guess = choices.MinBy(r => r.Shift.Length)!;
        return new WholeSheetReading(guess, [guess, .. choices.Where(c => c != guess).Take(2)], true);
    }

    /// <summary>Whether a reading costing <paramref name="cost"/> fits as well as the best one does, by the rule a rival is judged by in Solve.</summary>
    private static bool Close(double cost, double best, int holes) => cost * ClearlyBetter <= best || cost - best <= holes * MeaningfulGap;

    /// <summary>The smallest distance between two of these bulls, in page dmm.</summary>
    private static double Spacing(IReadOnlyList<Offset> bulls, IReadOnlyList<int> among)
    {
        double least = double.PositiveInfinity;
        for (int a = 0; a < among.Count; a++)
        {
            for (int b = a + 1; b < among.Count; b++)
            {
                least = Math.Min(least, bulls[among[a]].Minus(bulls[among[b]]).Length);
            }
        }

        return least;
    }

    /// <summary>
    /// Every distinct whole-sheet reading of these holes against these bulls, best first: each a one-to-one matching around one offset.
    /// The seeds are two holes paired with every bull, since the true offset carries each hole onto some bull, and two in case one is a flyer.
    /// </summary>
    private static List<SheetReading> Readings(IReadOnlyList<Offset> holes, IReadOnlyList<Offset> bulls, IReadOnlyList<int> among)
    {
        var targets = among.Select(i => bulls[i]).ToList();
        var seeds = new List<Offset> { Offset.Zero };
        foreach (int h in new[] { 0, holes.Count / 2 }.Distinct())
        {
            seeds.AddRange(targets.Select(t => holes[h].Minus(t)));
        }

        var found = new List<SheetReading>();
        foreach (var seed in seeds)
        {
            var shift = seed;
            for (int round = 0; round < Rounds; round++)
            {
                var (pairs, _) = Match(holes, targets, shift);
                var next = new Offset(
                    Middle([.. holes.Select((h, i) => h.X - targets[pairs[i]].X)]),
                    Middle([.. holes.Select((h, i) => h.Y - targets[pairs[i]].Y)]));
                bool settled = next.Minus(shift).Length < 0.01;
                shift = next;
                if (settled)
                {
                    break;
                }
            }

            var (matched, cost) = Match(holes, targets, shift);
            if (!found.Any(f => f.Shift.Minus(shift).Length <= SameOffset))
            {
                found.Add(new SheetReading([.. matched.Select(m => among[m]).Order()], shift, cost));
            }
        }

        found.Sort((a, b) => a.Cost.CompareTo(b.Cost));
        return found;
    }

    /// <summary>The one-to-one matching of holes, moved back by <paramref name="shift"/>, to bulls, and its total distance.</summary>
    private static (int[] Matched, double Cost) Match(IReadOnlyList<Offset> holes, IReadOnlyList<Offset> targets, Offset shift)
    {
        var cost = new double[holes.Count, targets.Count];
        for (int h = 0; h < holes.Count; h++)
        {
            for (int t = 0; t < targets.Count; t++)
            {
                cost[h, t] = holes[h].Minus(shift).Minus(targets[t]).Length;
            }
        }

        var matched = ShotAssignment.Hungarian(cost, holes.Count, targets.Count);
        return (matched, matched.Select((t, h) => cost[h, t]).Sum());
    }

    /// <summary>Entry 376 section A2: where a reading puts the group, for a choice: "about 1.02 in low and 0.44 in right".</summary>
    public static string Landed(Offset shift) => shift.Length <= 25
        ? "landing where aimed"
        : string.Create(CultureInfo.InvariantCulture,
            $"about {Math.Abs(shift.Y) / 254:0.00} in {(shift.Y < 0 ? "high" : "low")} and {Math.Abs(shift.X) / 254:0.00} in {(shift.X < 0 ? "left" : "right")}");

    /// <summary>What a person is told when every shot was given to the bull it was fired at rather than its nearest.</summary>
    public static string WholeSheetWords(Offset shift, int moved) => string.Create(CultureInfo.InvariantCulture,
        $"All shots are about {Math.Abs(shift.Y) / 254:0.00} in {(shift.Y < 0 ? "high" : "low")} and {Math.Abs(shift.X) / 254:0.00} in {(shift.X < 0 ? "left" : "right")} of the bulls they were fired at, so {moved} {(moved == 1 ? "is" : "are")} given to the bull below or beside {(moved == 1 ? "it" : "them")} rather than the nearest one. The rifle's zero moved them; zero first, or a sheet of one shot per bull cannot tell whose shot is whose.");

    /// <summary>How near two offsets have to be to count as the same answer, in page dmm. A millimetre.</summary>
    private const double SameOffset = 10;

    /// <summary>
    /// How much better the best offset has to be than a genuinely different one before it is called certain. A ratio, so it
    /// does not depend on the number of shots or the size of the sheet.
    /// </summary>
    private const double ClearlyBetter = 0.8;

    /// <summary>How many rounds of "assign, then re-centre" to run. It settles in two or three; ten is a cheap ceiling.</summary>
    private const int Rounds = 10;

    /// <summary>
    /// How much better, per hole and in page dmm, the best reading has to be than a different one. A tenth of a millimetre a hole: small
    /// enough that a genuinely better answer clears it easily, and large enough that two answers which both fit exactly do not.
    /// </summary>
    private const double MeaningfulGap = 1.0;

    /// <summary>
    /// The translation that best explains where these holes fell, given the bulls they were aimed at.
    /// <para>
    /// It works the way a person would: guess that some hole belongs to some bull, shift everything by that much, see which
    /// bull each hole is nearest to now, re-centre on what that implies, and repeat until it stops moving. Every
    /// hole-to-bull pair is tried as a starting guess, so a group that has landed a whole bull away is found as easily as
    /// one that has landed slightly low, and the answer does not depend on where the search happened to begin.
    /// </para>
    /// </summary>
    public static ImpactOffset Solve(string name, IReadOnlyList<Offset> holes, IReadOnlyList<Offset> bulls, IReadOnlyList<int> bullIndexes) =>
        Solve(name, holes, bulls, bullIndexes, out _);

    private static ImpactOffset Solve(string name, IReadOnlyList<Offset> holes, IReadOnlyList<Offset> bulls, IReadOnlyList<int> bullIndexes, out List<(Offset Shift, double Cost)> settled)
    {
        settled = [];
        ArgumentNullException.ThrowIfNull(holes);
        ArgumentNullException.ThrowIfNull(bulls);
        ArgumentNullException.ThrowIfNull(bullIndexes);

        if (holes.Count == 0 || bulls.Count == 0)
        {
            return new ImpactOffset(name, bullIndexes, Offset.Zero, true, "No shots to place.");
        }

        // Entry 130 section 3.1: where the shooter has said which bulls they aimed at, only those are candidates. It is not a refinement.
        // A sheet of twenty five bulls where ten were shot has a translation for almost any answer if every bull is allowed to catch a
        // hole, and the honest reading is the one that puts the shots on the bulls the shooter says they were aiming at.
        var targets = bullIndexes.Count > 0
            ? bullIndexes.Where(i => i >= 0 && i < bulls.Count).Select(i => bulls[i]).ToList()
            : [.. bulls];

        if (targets.Count == 0)
        {
            targets = [.. bulls];
        }

        foreach (var seed in Seeds(holes, targets))
        {
            var shift = Settle(seed, holes, targets);
            settled.Add((shift, Cost(shift, holes, targets)));
        }

        settled.Sort((a, b) => a.Cost.CompareTo(b.Cost));
        var best = settled[0];

        // A different answer is one that is not the same translation. Several seeds landing on the same place is agreement,
        // not disagreement, and must not count against certainty.
        var rival = settled.FirstOrDefault(s => s.Shift.Minus(best.Shift).Length > SameOffset);

        // Two readings that both explain the holes perfectly are the case this has to catch, and a ratio alone cannot: nothing is 80 percent
        // of nothing, so two zero-cost answers would compare as one clearly beating the other. A gap that has to be real in dmm as well as
        // in proportion says what is true, which is that the sheet could have been shot either way and nobody can tell from the holes.
        bool certain = rival == default
            || (best.Cost < rival.Cost * ClearlyBetter && rival.Cost - best.Cost > holes.Count * MeaningfulGap);

        string why = certain
            ? Words(best.Shift, holes.Count)
            : "The shots could be read as landing in more than one place, so where they were aimed is not certain. Check the "
              + "assignment before trusting the figures.";

        return new ImpactOffset(name, bullIndexes, best.Shift, certain, why);
    }

    /// <summary>
    /// Every hole-to-bull pair as a starting guess. It is n by m of them, which for a sheet of 25 bulls and 25 shots is 625
    /// starts of a handful of rounds each: nothing beside detecting the holes in the first place, and it removes the one
    /// failure that matters, which is settling into the wrong answer because the search began near it.
    /// </summary>
    private static IEnumerable<Offset> Seeds(IReadOnlyList<Offset> holes, IReadOnlyList<Offset> bulls)
    {
        yield return Offset.Zero;
        foreach (var hole in holes)
        {
            foreach (var bull in bulls)
            {
                yield return hole.Minus(bull);
            }
        }
    }

    /// <summary>
    /// Assign, re-centre, repeat, until it stops moving.
    /// <para>
    /// It re-centres on the <b>median</b> of what the holes imply, not the mean, and that is the whole of entry 130 section 3.4. Scan 6 had
    /// one shot far from everything else. A mean lets that one shot pull the point of impact, and a point of impact pulled by one wild shot
    /// moves every other shot's measurement with it, which turns one bad shot into a whole bad group. A median ignores it: one value out of
    /// eleven cannot move the middle one far, however far out it is.
    /// </para>
    /// </summary>
    private static Offset Settle(Offset start, IReadOnlyList<Offset> holes, IReadOnlyList<Offset> bulls)
    {
        var shift = start;
        var acrossBuffer = new double[holes.Count];
        var downBuffer = new double[holes.Count];

        for (int round = 0; round < Rounds; round++)
        {
            for (int i = 0; i < holes.Count; i++)
            {
                var bull = Nearest(holes[i].Minus(shift), bulls);
                acrossBuffer[i] = holes[i].X - bull.X;
                downBuffer[i] = holes[i].Y - bull.Y;
            }

            var next = new Offset(Middle(acrossBuffer), Middle(downBuffer));
            if (next.Minus(shift).Length < 0.01)
            {
                return next;
            }

            shift = next;
        }

        return shift;
    }

    /// <summary>The middle value, which is what makes one wild shot cost nothing.</summary>
    private static double Middle(double[] values)
    {
        var sorted = (double[])values.Clone();
        Array.Sort(sorted);
        int middle = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
    }

    /// <summary>Total distance from each hole to the bull it would be given, which is what a translation is judged by.</summary>
    private static double Cost(Offset shift, IReadOnlyList<Offset> holes, IReadOnlyList<Offset> bulls)
    {
        double total = 0;
        foreach (var hole in holes)
        {
            var aimed = hole.Minus(shift);
            total += aimed.Minus(Nearest(aimed, bulls)).Length;
        }

        return total;
    }

    private static Offset Nearest(Offset point, IReadOnlyList<Offset> bulls)
    {
        var best = bulls[0];
        double bestDistance = double.MaxValue;
        foreach (var bull in bulls)
        {
            double d = point.Minus(bull).Length;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = bull;
            }
        }

        return best;
    }

    private static string Words(Offset shift, int shots)
    {
        // The same tenth of an inch ImpactOffset.Moved uses, so the words and the flag can never disagree.
        if (shift.Length <= 25)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{shots} shots, landing where they were aimed.");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{shots} shots, landing {shift.Describe()} of where they were aimed.");
    }
}
