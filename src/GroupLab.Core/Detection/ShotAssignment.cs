using GroupLab.Core.Imaging;

namespace GroupLab.Core.Detection;

/// <summary>How a set of shots was assigned to bulls.</summary>
public enum AssignmentMethod
{
    /// <summary>The globally optimal one-to-one matching, minimising total distance.</summary>
    OneToOne,

    /// <summary>Each shot to its nearest bull, because a one-to-one matching would be forced.</summary>
    NearestBull,
}

/// <summary>
/// One shot's assignment, page dmm: the bull it was given, or none, its distance there, its nearest bull and distance,
/// the margin between its nearest and second-nearest bulls, and whether that margin is small enough that a registration
/// error could flip it, or it was given a bull other than its nearest.
/// </summary>
public sealed record AssignedShot(int Shot, int? Bull, double Distance, int NearestBull, double NearestDistance, double Margin, bool Ambiguous);

/// <summary>How an assignment method reads to a person, NOTES-FROM-PLANNING.md entry 109 section 2: never the enum's name.</summary>
public static class AssignmentMethods
{
    public static string Words(this AssignmentMethod method) => method switch
    {
        AssignmentMethod.OneToOne => "one-to-one matching",
        _ => "nearest bull",
    };
}

/// <summary>An assignment of shots to bulls, the method used and why.</summary>
public sealed record ShotAssignmentResult(AssignmentMethod Method, string Reason, IReadOnlyList<AssignedShot> Shots);

/// <summary>
/// Stage S9 of docs/DETECTION-PIPELINE.md and docs/PHASE1-BRIEF.md section 4.5: shots to bulls. Nearest-bull is not the
/// rule. Where the definition declares one shot per bull, the defensible assignment is the globally optimal one-to-one
/// matching minimising total distance, which the survey measured getting right the one case on <c>338lmao.jpg</c> where
/// nearest-bull gives a shot to a bull that already has a much closer one (docs/SCAN-MEASUREMENTS.md section 7.2).
/// <list type="number">
/// <item>Shots no more than bulls: one-to-one matching, against a subset of the bulls when there are fewer, leaving bulls
/// empty.</item>
/// <item>More shots than bulls: no matching is forced, because on <c>n568-gm210m.jpg</c> forcing one manufactured a false
/// cross-cell result at 0.854 in. Each shot goes to its nearest bull within the gate, and every shot is flagged.</item>
/// </list>
/// A shot whose nearest and second-nearest bulls are within <see cref="AmbiguousMarginInches"/> of each other is flagged
/// either way, as is a shot the matching gave to a bull other than its nearest: the pipeline says so rather than deciding
/// silently. The manual interface of DESIGN.md section 13 is where a person settles them.
/// <para>
/// <b>Sighter and scoring bulls are two pools</b>, NOTES-FROM-PLANNING.md entry 73 section 1. A shot fired at a sighter can never belong
/// to a scoring bull, nor the reverse, yet one matching over both was free to give a sighter's hole to a scoring bull a row away when
/// the sighters had more holes than bulls, and the statistics then pooled it into the group. Given the bulls' scoring flags, each shot
/// joins the pool of its nearest bull and each pool is matched on its own, with the counts rule above applied per pool. The margin is
/// still measured against every bull, so a hole near the boundary between the two rows is flagged whichever pool it joined.
/// </para>
/// </summary>
public static class ShotAssignment
{
    /// <summary>The margin, inches, under which a 0.05 in registration error could flip a shot between bulls (section 7.2).</summary>
    public const double AmbiguousMarginInches = 0.15;

    private const double DmmPerInch = 254;

    /// <param name="shots">Shot centres, page dmm.</param>
    /// <param name="bulls">Bull centres, page dmm.</param>
    /// <param name="gateInches">Beyond this distance a shot is left unassigned by nearest-bull.</param>
    /// <param name="scoring">
    /// Whether each bull is a scoring bull, in the order of <paramref name="bulls"/>. When both kinds are present the two are matched as
    /// separate pools; without it every bull is one pool, as before.
    /// </param>
    /// <param name="capacity">
    /// How many shots each bull is expected to hold, in the order of <paramref name="bulls"/>, one where it is not given: a sheet shot with
    /// two a bull on named bulls, NOTES-FROM-PLANNING.md entry 113 section 4, is matched with each such bull taking two.
    /// </param>
    /// <param name="nearestOnly">Every shot to its nearest bull, never matched, because the person said the sheet is to be read that way.</param>
    /// <returns>
    /// Every shot's assignment, with bull indices into <paramref name="bulls"/>. The method is nearest-bull when either pool had more shots
    /// than bulls, and the reason says what happened in each pool.
    /// </returns>
    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 196: a sheet with one scoring bull, every zeroing grid among them, is shot as a group at that bull. So
    /// the bull takes every shot, with no limit, and none is flagged for there being more shots than bulls: that is the sheet working, not
    /// a doubt. Null where there is more than one scoring bull, so every other sheet keeps one place a bull unless the marking says more.
    /// </summary>
    public static IReadOnlyList<int>? OneBullTakesAll(IReadOnlyList<bool> scoring, int shots)
    {
        ArgumentNullException.ThrowIfNull(scoring);
        return scoring.Count(s => s) == 1 ? [.. scoring.Select(s => s ? Math.Max(1, shots) : 1)] : null;
    }

    public static ShotAssignmentResult Assign(IReadOnlyList<PointD> shots, IReadOnlyList<PointD> bulls, double gateInches = double.PositiveInfinity, IReadOnlyList<bool>? scoring = null,
        IReadOnlyList<int>? capacity = null, bool nearestOnly = false)
    {
        ArgumentNullException.ThrowIfNull(shots);
        ArgumentNullException.ThrowIfNull(bulls);
        if (scoring is null || scoring.Distinct().Count() < 2)
        {
            return AssignPool(shots, bulls, gateInches, capacity, nearestOnly);
        }

        if (scoring.Count != bulls.Count)
        {
            throw new ArgumentException($"{scoring.Count} scoring flags were given for {bulls.Count} bulls.", nameof(scoring));
        }

        // A hole fired at a sighter is nearest a sighter, so each shot's pool is its nearest bull's.
        var ranked = shots.Select(s => Enumerable.Range(0, bulls.Count).Select(b => (Bull: b, Distance: Distance(s, bulls[b]))).OrderBy(x => x.Distance).ToList()).ToList();
        var assigned = new AssignedShot[shots.Count];
        var method = AssignmentMethod.OneToOne;
        var reasons = new List<string>();
        foreach (bool pool in (bool[])[true, false])
        {
            int[] poolBulls = [.. Enumerable.Range(0, bulls.Count).Where(b => scoring[b] == pool)];
            int[] poolShots = [.. Enumerable.Range(0, shots.Count).Where(s => scoring[ranked[s][0].Bull] == pool)];
            if (poolShots.Length == 0)
            {
                continue;
            }

            var part = AssignPool([.. poolShots.Select(s => shots[s])], [.. poolBulls.Select(b => bulls[b])], gateInches, capacity is null ? null : [.. poolBulls.Select(b => capacity[b])], nearestOnly);
            if (part.Method == AssignmentMethod.NearestBull)
            {
                method = AssignmentMethod.NearestBull;
            }

            reasons.Add(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{poolShots.Length} {(pool ? "scoring" : "sighter")} shots for {poolBulls.Length} {(pool ? "scoring" : "sighter")} bulls, {part.Reason}"));
            for (int k = 0; k < poolShots.Length; k++)
            {
                var shot = part.Shots[k];
                int s = poolShots[k];
                double margin = ranked[s].Count > 1 ? ranked[s][1].Distance - ranked[s][0].Distance : double.PositiveInfinity;
                assigned[s] = shot with
                {
                    Shot = s,
                    Bull = shot.Bull is { } b ? poolBulls[b] : null,
                    NearestBull = shot.NearestBull >= 0 ? poolBulls[shot.NearestBull] : -1,
                    Margin = margin,
                    Ambiguous = shot.Ambiguous || margin < AmbiguousMarginInches * DmmPerInch,
                };
            }
        }

        return new ShotAssignmentResult(method, string.Join("; ", reasons), assigned);
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    /// <summary>One pool of bulls, every shot competing for it: the matching of the class summary.</summary>
    private static ShotAssignmentResult AssignPool(IReadOnlyList<PointD> shots, IReadOnlyList<PointD> bulls, double gateInches, IReadOnlyList<int>? capacity = null, bool nearestOnly = false)
    {
        if (bulls.Count == 0)
        {
            return new ShotAssignmentResult(AssignmentMethod.NearestBull, "no bulls", [.. shots.Select((_, i) => new AssignedShot(i, null, double.NaN, -1, double.NaN, double.NaN, true))]);
        }

        var cost = new double[shots.Count, bulls.Count];
        for (int s = 0; s < shots.Count; s++)
        {
            for (int b = 0; b < bulls.Count; b++)
            {
                cost[s, b] = Math.Sqrt(Math.Pow(shots[s].X - bulls[b].X, 2) + Math.Pow(shots[s].Y - bulls[b].Y, 2));
            }
        }

        (int Nearest, double Distance, double Margin) Nearest(int s)
        {
            var order = Enumerable.Range(0, bulls.Count).OrderBy(b => cost[s, b]).ToList();
            return (order[0], cost[s, order[0]], order.Count > 1 ? cost[s, order[1]] - cost[s, order[0]] : double.PositiveInfinity);
        }

        double margin = AmbiguousMarginInches * DmmPerInch;
        double gate = gateInches * DmmPerInch;
        if (nearestOnly)
        {
            // The person's reading of the sheet: nothing is matched, and a shot is flagged only where its bulls are near to equal.
            return new ShotAssignmentResult(AssignmentMethod.NearestBull, "each shot to its nearest bull, as the marking asks",
                [.. Enumerable.Range(0, shots.Count).Select(s =>
                {
                    var (nearest, nearestDistance, gap) = Nearest(s);
                    return new AssignedShot(s, nearestDistance <= gate ? nearest : null, nearestDistance, nearest, nearestDistance, gap, gap < margin);
                })]);
        }

        // Each bull is as many places in the matching as the shots it is expected to hold, one unless the marking says more.
        int[] slots = [.. Enumerable.Range(0, bulls.Count).SelectMany(b => Enumerable.Repeat(b, Math.Max(1, capacity?[b] ?? 1)))];
        if (shots.Count <= slots.Length)
        {
            var slotCost = new double[shots.Count, slots.Length];
            for (int s = 0; s < shots.Count; s++)
            {
                for (int j = 0; j < slots.Length; j++)
                {
                    slotCost[s, j] = cost[s, slots[j]];
                }
            }

            var matched = Hungarian(slotCost, shots.Count, slots.Length);
            string reason = slots.Length > bulls.Count
                ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"matched with the bulls the marking names taking more than one, {slots.Length} places for {shots.Count} shots")
                : shots.Count == bulls.Count ? "as many shots as bulls" : "fewer shots than bulls, matched against a subset";
            return new ShotAssignmentResult(AssignmentMethod.OneToOne, reason,
                [.. Enumerable.Range(0, shots.Count).Select(s =>
                {
                    var (nearest, nearestDistance, gap) = Nearest(s);
                    int bull = slots[matched[s]];
                    return new AssignedShot(s, bull, cost[s, bull], nearest, nearestDistance, gap, gap < margin || bull != nearest);
                })]);
        }

        return new ShotAssignmentResult(AssignmentMethod.NearestBull, "more shots than bulls, so no one-to-one matching is forced",
            [.. Enumerable.Range(0, shots.Count).Select(s =>
            {
                var (nearest, nearestDistance, gap) = Nearest(s);
                return new AssignedShot(s, nearestDistance <= gate ? nearest : null, nearestDistance, nearest, nearestDistance, gap, true);
            })]);
    }

    /// <summary>
    /// The minimum-cost assignment of <paramref name="rows"/> rows to distinct columns of a rows by columns cost matrix,
    /// rows no more than columns, by the Hungarian method with potentials, O(rows squared times columns).
    /// </summary>
    private static int[] Hungarian(double[,] cost, int rows, int columns)
    {
        var u = new double[rows + 1];
        var v = new double[columns + 1];
        var p = new int[columns + 1];
        var way = new int[columns + 1];
        for (int i = 1; i <= rows; i++)
        {
            p[0] = i;
            int j0 = 0;
            var minimum = Enumerable.Repeat(double.PositiveInfinity, columns + 1).ToArray();
            var used = new bool[columns + 1];
            do
            {
                used[j0] = true;
                int i0 = p[j0], j1 = 0;
                double delta = double.PositiveInfinity;
                for (int j = 1; j <= columns; j++)
                {
                    if (used[j])
                    {
                        continue;
                    }

                    double current = cost[i0 - 1, j - 1] - u[i0] - v[j];
                    if (current < minimum[j])
                    {
                        minimum[j] = current;
                        way[j] = j0;
                    }

                    if (minimum[j] < delta)
                    {
                        delta = minimum[j];
                        j1 = j;
                    }
                }

                for (int j = 0; j <= columns; j++)
                {
                    if (used[j])
                    {
                        u[p[j]] += delta;
                        v[j] -= delta;
                    }
                    else
                    {
                        minimum[j] -= delta;
                    }
                }

                j0 = j1;
            }
            while (p[j0] != 0);

            do
            {
                int j1 = way[j0];
                p[j0] = p[j1];
                j0 = j1;
            }
            while (j0 != 0);
        }

        var assignment = new int[rows];
        for (int j = 1; j <= columns; j++)
        {
            if (p[j] != 0)
            {
                assignment[p[j] - 1] = j - 1;
            }
        }

        return assignment;
    }
}
