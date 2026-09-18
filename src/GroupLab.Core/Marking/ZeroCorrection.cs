using GroupLab.Core.Imaging;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Marking;

/// <summary>
/// One axis of a zero correction, in inches at the target. <see cref="Dial"/> is the direction the turret moves the point of impact, which
/// is the opposite of the direction the group sits: a group low is corrected by dialling up. <see cref="Distinguishable"/> says whether the
/// offset is larger than the sampling error of the centre at this many shots, and where it is not, <see cref="ShotsToSettle"/> is how many
/// shots would settle it if the rifle keeps shooting as it has.
/// </summary>
public sealed record ZeroAxis(double OffsetInches, double HalfWidthInches, bool Distinguishable, int? ShotsToSettle, string Dial, string Sits, Clicks? Clicks = null);

/// <summary>
/// The scope correction and its uncertainty, NOTES-FROM-PLANNING.md entries 91 and 53 section 3. It answers a different question from the
/// group statistics: "what do I dial now", read standing at a bench, against "how well does this rifle shoot", read afterwards sitting down.
/// <para>
/// <b>The rule is entry 53 section 3's: adjust an axis only where its interval excludes zero.</b> The offset is an estimate from n shots and
/// its standard error on an axis is sigma / sqrt(n), so the smallest offset distinguishable from zero at 95 percent is
/// t(0.975, df) / sqrt(n) times sigma: 1.603 sigma at three shots, 1.031 at five, 0.664 at ten, 0.402 at twenty-five. On the sheet that
/// prompted entry 91 the measured offset was 0.63 of what ten shots can resolve, so dialling it would have moved a zero to chase sampling
/// noise, which is the most common error in practical zeroing and exactly the confident wrong answer DESIGN.md section 2 exists to prevent.
/// </para>
/// <para>
/// <b>Degrees of freedom follow the group's shape,</b> entry 53 section 3: a circular group estimates sigma from all 2n coordinates, so
/// df = 2n - 2; where circularity is rejected each axis is estimated on its own with df = n - 1 and the wider interval is accepted, because
/// a group that strings vertically genuinely knows less about its vertical centre.
/// </para>
/// <para>
/// <b>What this is not.</b> It is not a ballistic correction: it puts the impact on the aim at the distance shot, and moving a zero between
/// distances is the solver's job, which is Phase 5. It is not a click count either, because clicks need the scope's click value, which is a
/// rifle record (entry 91 section 4). Where the marking names a rifle and the shot distance is set, each axis worth dialling also carries its
/// <see cref="Clicks"/>, whole clicks and what rounding leaves, which is entry 97 section 2's "twelve clicks right".
/// </para>
/// </summary>
public sealed record ZeroCorrection(
    int Shots,
    double SigmaInches,
    int DegreesOfFreedom,
    bool Circular,
    ZeroAxis Windage,
    ZeroAxis Elevation,
    double CentreRegionInches,
    double DetectableInches)
{
    /// <summary>True where at least one axis is worth dialling.</summary>
    public bool Worth => Windage.Distinguishable || Elevation.Distinguishable;
}

/// <summary>
/// The zero correction, its uncertainty, and the shot count that would settle an offset too small to call, from a marking's own shots
/// (NOTES-FROM-PLANNING.md entries 91 and 92). Nothing here rounds to clicks or crosses a distance: see <see cref="ZeroCorrection"/>.
/// </summary>
public static class Zeroing
{
    /// <summary>The confidence level every figure here carries.</summary>
    public const double Level = 0.95;

    /// <summary>The most shots the "how many would settle it" answer will offer before saying it cannot be settled by shooting more.</summary>
    public const int MostShotsOffered = 200;

    /// <summary>
    /// The smallest offset distinguishable from zero at <see cref="Level"/>, as a multiple of sigma, entry 53 section 3's table:
    /// t(0.975, df) / sqrt(n), with df = 2n - 2 for a circular group and n - 1 where circularity is rejected.
    /// </summary>
    public static double DetectableMultiple(int shots, bool circular)
    {
        double df = circular ? (2.0 * shots) - 2 : shots - 1.0;
        return df < 1 ? double.PositiveInfinity : Distributions.StudentTQuantile(1 - ((1 - Level) / 2), df) / Math.Sqrt(shots);
    }

    /// <summary>
    /// How many shots would make an offset of this size distinguishable from zero, if the rifle keeps shooting as it has, or null where
    /// <see cref="MostShotsOffered"/> would not do it. It is the useful answer when the correction is not yet supportable: "shoot fifteen
    /// more before you touch the turret" beats a false correction.
    /// </summary>
    public static int? ShotsToSettle(double offsetInches, double sigmaInches, bool circular, int from = 2)
    {
        if (sigmaInches <= 0 || Math.Abs(offsetInches) <= 0)
        {
            return null;
        }

        for (int n = Math.Max(2, from); n <= MostShotsOffered; n++)
        {
            if (DetectableMultiple(n, circular) * sigmaInches <= Math.Abs(offsetInches))
            {
                return n;
            }
        }

        return null;
    }

    /// <summary>
    /// The correction for a marking, or null where there is nothing to say: no scale, no point of reference, or too few shots for a
    /// dispersion figure, since without sigma there is no way to tell an offset from noise. Excluded shots are left out, as they are from
    /// the reduced figures; sighters were never in.
    /// </summary>
    public static ZeroCorrection? For(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Scale is null)
        {
            return null;
        }

        var sighters = state.Bulls.Where(b => !b.Scoring).Select(b => b.Index).ToHashSet();
        var shots = state.Shots
            .Where(s => s.IsShot && s.Exclusion is null && !(s.Bull is { } b && sighters.Contains(b)))
            .ToList();
        if (shots.Count < GroupAnalysis.MinimumShotsForDispersion || !GroupAnalysis.HasOrigin(state, shots))
        {
            return null;
        }

        var offsets = GroupAnalysis.CompositeOffsets(state, shots);
        int n = offsets.Count;
        var centre = GroupStatistics.Centre(offsets);
        bool circular = ShapeTests.Circularity(offsets).PValue >= 1 - Level;
        var (xx, xy, yy) = GroupStatistics.Covariance(offsets);
        double sigma = GroupStatistics.Rayleigh(offsets).Sigma.Value;
        double across = circular ? sigma : Math.Sqrt(xx), down = circular ? sigma : Math.Sqrt(yy);
        int df = circular ? (2 * n) - 2 : n - 1;
        double t = Distributions.StudentTQuantile(1 - ((1 - Level) / 2), df);

        ZeroAxis Axis(double offset, double axisSigma, string sitsPositive, string dialPositive, string sitsNegative, string dialNegative)
        {
            double half = t * axisSigma / Math.Sqrt(n);
            bool worth = Math.Abs(offset) > half;
            string dial = offset >= 0 ? dialPositive : dialNegative;

            // Entry 97 section 2: in clicks, where the rifle and the distance are known, and only for an axis worth dialling.
            var clicks = worth && state.Rifle is { ClickValue: > 0 } rifle && state.ShotDistanceInches is { } distance and > 0
                ? Clicks.For(offset, distance, rifle, dial)
                : null;
            return new ZeroAxis(
                offset,
                half,
                worth,
                worth ? null : ShotsToSettle(offset, axisSigma, circular, n + 1),
                dial,
                offset >= 0 ? sitsPositive : sitsNegative,
                clicks);
        }

        // The screen's axes: x to the right, y down the image, so a centre with positive y sits low and is corrected by dialling up.
        return new ZeroCorrection(
            n,
            sigma,
            df,
            circular,
            Axis(centre.X, across, "right", "left", "left", "right"),
            Axis(centre.Y, down, "low", "up", "high", "down"),
            sigma * Math.Sqrt(Distributions.ChiSquareQuantile(Level, 2) / n),
            DetectableMultiple(n, circular) * sigma);
    }
}
