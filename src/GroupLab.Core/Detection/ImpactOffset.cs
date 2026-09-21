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
    /// <summary>Whether the group landed far enough from the aim to matter, which is what makes this worth showing at all.</summary>
    public bool Moved => Shift.Length > 0.5;
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
    /// The translation that best explains where these holes fell, given the bulls they were aimed at.
    /// <para>
    /// It works the way a person would: guess that some hole belongs to some bull, shift everything by that much, see which
    /// bull each hole is nearest to now, re-centre on what that implies, and repeat until it stops moving. Every
    /// hole-to-bull pair is tried as a starting guess, so a group that has landed a whole bull away is found as easily as
    /// one that has landed slightly low, and the answer does not depend on where the search happened to begin.
    /// </para>
    /// </summary>
    public static ImpactOffset Solve(string name, IReadOnlyList<Offset> holes, IReadOnlyList<Offset> bulls, IReadOnlyList<int> bullIndexes)
    {
        ArgumentNullException.ThrowIfNull(holes);
        ArgumentNullException.ThrowIfNull(bulls);
        ArgumentNullException.ThrowIfNull(bullIndexes);

        if (holes.Count == 0 || bulls.Count == 0)
        {
            return new ImpactOffset(name, bullIndexes, Offset.Zero, true, "No shots to place.");
        }

        var settled = new List<(Offset Shift, double Cost)>();
        foreach (var seed in Seeds(holes, bulls))
        {
            var shift = Settle(seed, holes, bulls);
            settled.Add((shift, Cost(shift, holes, bulls)));
        }

        settled.Sort((a, b) => a.Cost.CompareTo(b.Cost));
        var best = settled[0];

        // A different answer is one that is not the same translation. Several seeds landing on the same place is agreement,
        // not disagreement, and must not count against certainty.
        var rival = settled.FirstOrDefault(s => s.Shift.Minus(best.Shift).Length > SameOffset);
        bool certain = rival == default || best.Cost <= rival.Cost * ClearlyBetter;

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

    /// <summary>Assign, re-centre, repeat, until it stops moving.</summary>
    private static Offset Settle(Offset start, IReadOnlyList<Offset> holes, IReadOnlyList<Offset> bulls)
    {
        var shift = start;
        for (int round = 0; round < Rounds; round++)
        {
            double sumX = 0;
            double sumY = 0;
            foreach (var hole in holes)
            {
                var bull = Nearest(hole.Minus(shift), bulls);
                sumX += hole.X - bull.X;
                sumY += hole.Y - bull.Y;
            }

            var next = new Offset(sumX / holes.Count, sumY / holes.Count);
            if (next.Minus(shift).Length < 0.01)
            {
                return next;
            }

            shift = next;
        }

        return shift;
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
        if (shift.Length <= 0.5)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{shots} shots, landing where they were aimed.");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{shots} shots, landing {shift.Describe()} of where they were aimed.");
    }
}
