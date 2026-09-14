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
/// </summary>
public static class ShotAssignment
{
    /// <summary>The margin, inches, under which a 0.05 in registration error could flip a shot between bulls (section 7.2).</summary>
    public const double AmbiguousMarginInches = 0.15;

    private const double DmmPerInch = 254;

    /// <param name="shots">Shot centres, page dmm.</param>
    /// <param name="bulls">Bull centres, page dmm; normally the scoring bulls.</param>
    /// <param name="gateInches">Beyond this distance a shot is left unassigned by nearest-bull.</param>
    public static ShotAssignmentResult Assign(IReadOnlyList<PointD> shots, IReadOnlyList<PointD> bulls, double gateInches = double.PositiveInfinity)
    {
        ArgumentNullException.ThrowIfNull(shots);
        ArgumentNullException.ThrowIfNull(bulls);
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
        if (shots.Count <= bulls.Count)
        {
            var matched = Hungarian(cost, shots.Count, bulls.Count);
            return new ShotAssignmentResult(AssignmentMethod.OneToOne, shots.Count == bulls.Count ? "as many shots as bulls" : "fewer shots than bulls, matched against a subset",
                [.. Enumerable.Range(0, shots.Count).Select(s =>
                {
                    var (nearest, nearestDistance, gap) = Nearest(s);
                    int bull = matched[s];
                    return new AssignedShot(s, bull, cost[s, bull], nearest, nearestDistance, gap, gap < margin || bull != nearest);
                })]);
        }

        double gate = gateInches * DmmPerInch;
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
