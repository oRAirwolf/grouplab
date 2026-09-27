using System.Globalization;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Marking;

/// <summary>A known length drawn on the photograph: its two ends, in image pixels, and how long it is.</summary>
public sealed record DrawnLength(PointD A, PointD B, double Inches)
{
    public double Pixels => Math.Sqrt(((B.X - A.X) * (B.X - A.X)) + ((B.Y - A.Y) * (B.Y - A.Y)));
}

/// <summary>
/// The scale at one bull, NOTES-FROM-PLANNING.md entry 228 section 2.2: a length across and, best, one up and down, drawn near it. Two
/// lengths make a linear map that carries the photograph's angle at that bull, stretching one direction more than the other; one length
/// is a single number and cannot.
/// </summary>
public sealed record BullScale(int Bull, DrawnLength Across, DrawnLength? UpDown)
{
    /// <summary>Inches a pixel across, and up and down (the same where there is one length).</summary>
    public double AcrossInchesPerPixel => Across.Inches / Across.Pixels;

    public double UpDownInchesPerPixel => UpDown is { } u ? u.Inches / u.Pixels : AcrossInchesPerPixel;

    public double MeanInchesPerPixel => (AcrossInchesPerPixel + UpDownInchesPerPixel) / 2;

    /// <summary>The across length's direction, turned to point along the image's nearer axis the positive way: right, or down.</summary>
    public PointD Direction()
    {
        var v = new PointD(Across.B.X - Across.A.X, Across.B.Y - Across.A.Y);
        bool flip = Math.Abs(v.X) >= Math.Abs(v.Y) ? v.X < 0 : v.Y < 0;
        return flip ? new PointD(-v.X, -v.Y) : v;
    }

    /// <summary>
    /// The map from an image displacement near this bull to inches on the target: two lengths give the linear map that takes the across
    /// length to (its inches, 0) and the up and down one to (0, its inches); one gives the same scale both ways on the image's own axes. Each
    /// length is first turned to agree with <paramref name="reference"/>, across along it and up and down a quarter turn from it, so every
    /// bull shares one frame whichever end of each length was tapped first and however the photograph is turned. Null when the two lengths
    /// are drawn parallel, which no map can be read from.
    /// </summary>
    public (double A, double B, double C, double D)? Map(PointD? reference = null)
    {
        if (UpDown is not { } upDown)
        {
            double k = AcrossInchesPerPixel;
            return (k, 0, 0, k);
        }

        var r1 = reference ?? Direction();
        var r2 = new PointD(-r1.Y, r1.X);
        var v1 = new PointD(Across.B.X - Across.A.X, Across.B.Y - Across.A.Y);
        var v2 = new PointD(upDown.B.X - upDown.A.X, upDown.B.Y - upDown.A.Y);
        double l1 = (v1.X * r1.X) + (v1.Y * r1.Y) >= 0 ? Across.Inches : -Across.Inches;
        double l2 = (v2.X * r2.X) + (v2.Y * r2.Y) >= 0 ? upDown.Inches : -upDown.Inches;
        double det = (v1.X * v2.Y) - (v2.X * v1.Y);
        if (Math.Abs(det) < 1e-5 * Across.Pixels * upDown.Pixels)
        {
            return null;
        }

        // M [v1 v2] = [(l1, 0) (0, l2)], so M = diag(l1, l2) [v1 v2]^-1.
        double ia = v2.Y / det, ib = -v2.X / det, ic = -v1.Y / det, id = v1.X / det;
        return (l1 * ia, l1 * ib, l2 * ic, l2 * id);
    }

    /// <summary>A displacement in the image near this bull, in inches on the target, in the frame <paramref name="reference"/> sets.</summary>
    public PointD Apply(PointD delta, PointD? reference = null)
    {
        var (a, b, c, d) = Map(reference) ?? (AcrossInchesPerPixel, 0, 0, AcrossInchesPerPixel);
        return new PointD((a * delta.X) + (b * delta.Y), (c * delta.X) + (d * delta.Y));
    }
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 228 section 2.2: a scale at each bull, for a photograph of a target GroupLab did not print where four known
/// corners are not available. Each shot is measured from its own bull with its own bull's scale, so an angled photograph, which shrinks the
/// far side of the sheet, no longer makes the far bulls' groups look small. A bull with no scale of its own takes the nearest one's.
/// <para>
/// It cannot remove the angle between bulls: a distance from one bull to another uses the scales' average, which is why the figures that
/// span bulls say so, and why the four-corner rectangle is still the better choice wherever it can be used.
/// </para>
/// </summary>
public sealed record PerBullReference : ScaleReference
{
    /// <summary>How far two scales may differ, as a share, before GroupLab says the photograph was taken at an angle.</summary>
    public const double Disagreement = 0.03;

    public PerBullReference(IReadOnlyList<BullScale> scales, IReadOnlyDictionary<int, PointD> bulls)
    {
        ArgumentNullException.ThrowIfNull(scales);
        ArgumentNullException.ThrowIfNull(bulls);
        if (scales.Count == 0)
        {
            throw new ArgumentException("a scale at each bull needs at least one bull's scale.", nameof(scales));
        }

        Scales = [.. scales];
        Bulls = new Dictionary<int, PointD>(bulls);
    }

    public IReadOnlyList<BullScale> Scales { get; }

    /// <summary>Where each bull's aim point is in the image, which each scale is anchored at.</summary>
    public IReadOnlyDictionary<int, PointD> Bulls { get; }

    private double MeanInchesPerPixel => Scales.Average(s => s.MeanInchesPerPixel);

    /// <summary>The scale a bull uses: its own, or the nearest bull's that has one.</summary>
    public BullScale For(int bull)
    {
        if (Scales.FirstOrDefault(s => s.Bull == bull) is { } own)
        {
            return own;
        }

        var at = Bulls.TryGetValue(bull, out var p) ? p : new PointD(0, 0);
        return Scales.MinBy(s => Bulls.TryGetValue(s.Bull, out var q) ? ((q.X - at.X) * (q.X - at.X)) + ((q.Y - at.Y) * (q.Y - at.Y)) : double.MaxValue)!;
    }

    /// <summary>A shot's offset from its bull, in inches, with that bull's scale.</summary>
    public PointD Offset(int bull, PointD origin, PointD at) => For(bull).Apply(new PointD(at.X - origin.X, at.Y - origin.Y), Reference);

    /// <summary>The one frame every bull is measured in: the first scale's across direction.</summary>
    public PointD Reference => Scales[0].Direction();

    /// <summary>
    /// A point on the target, for what is not an offset from a bull: the nearest bull's scale about it, from that bull's place at the average
    /// scale. Within one bull this is exact to that bull's scale; between bulls it is the average.
    /// </summary>
    public override PointD ToTarget(PointD image)
    {
        double k = MeanInchesPerPixel;
        if (Bulls.Count == 0)
        {
            return new PointD(image.X * k, image.Y * k);
        }

        var nearest = Bulls.MinBy(b => ((b.Value.X - image.X) * (b.Value.X - image.X)) + ((b.Value.Y - image.Y) * (b.Value.Y - image.Y)));
        var local = For(nearest.Key).Apply(new PointD(image.X - nearest.Value.X, image.Y - nearest.Value.Y), Reference);
        return new PointD((nearest.Value.X * k) + local.X, (nearest.Value.Y * k) + local.Y);
    }

    public override string Description => string.Create(CultureInfo.InvariantCulture,
        $"a scale drawn at each of {Scales.Count} bulls, {Scales.Count(s => s.UpDown is not null)} of them with a length both ways, each shot measured with its own bull's scale; it assumes the sheet flat about each bull");

    public override string Describe(UnitSettings units) => Description;

    /// <summary>Square on is assumed at any bull with one length only; with two lengths at every bull, the angle is carried.</summary>
    public override bool AssumesSquareOn => Scales.Any(s => s.UpDown is null);

    /// <summary>
    /// How far the scale can be trusted, as a share: the spread of the bulls' scales about their mean and, at each bull with two lengths,
    /// how far across and up and down disagree. A 2 percent scale error is a 2 percent error in every size (entry 228 section 2).
    /// </summary>
    public double RelativeUncertainty
    {
        get
        {
            double mean = MeanInchesPerPixel;
            double spread = Scales.Count > 1
                ? Math.Sqrt(Scales.Sum(s => Math.Pow(s.MeanInchesPerPixel - mean, 2)) / (Scales.Count - 1)) / mean
                : 0;
            var both = Scales.Where(s => s.UpDown is not null).ToList();
            double axes = both.Count > 0
                ? both.Average(s => Math.Abs(s.AcrossInchesPerPixel - s.UpDownInchesPerPixel) / (s.AcrossInchesPerPixel + s.UpDownInchesPerPixel))
                : 0;
            return Math.Sqrt((spread * spread) + (axes * axes));
        }
    }

    /// <summary>What a person should be told about these scales: an angle seen, a single length, a bull with none of its own.</summary>
    public IReadOnlyList<string> Checks(Func<int, string> label)
    {
        ArgumentNullException.ThrowIfNull(label);
        var said = new List<string>();
        foreach (var s in Scales.Where(s => s.UpDown is not null))
        {
            double ratio = Math.Max(s.AcrossInchesPerPixel, s.UpDownInchesPerPixel) / Math.Min(s.AcrossInchesPerPixel, s.UpDownInchesPerPixel) - 1;
            if (ratio > Disagreement)
            {
                said.Add(string.Create(CultureInfo.InvariantCulture,
                    $"At bull {label(s.Bull)} the scale across and the scale up and down differ by {100 * ratio:0.#} percent: the photograph was taken at an angle. Four corners of something rectangular, or a straighter photograph or a scan, would remove it."));
            }
        }

        if (Scales.Count > 1)
        {
            double most = Scales.Max(s => s.MeanInchesPerPixel), least = Scales.Min(s => s.MeanInchesPerPixel);
            if ((most / least) - 1 > Disagreement)
            {
                said.Add(string.Create(CultureInfo.InvariantCulture,
                    $"The scales at the bulls differ by up to {100 * ((most / least) - 1):0.#} percent, so the photograph was taken at an angle. Each shot is measured with its own bull's scale; four corners would do better."));
            }
        }

        foreach (var s in Scales.Where(s => s.UpDown is null))
        {
            said.Add($"Bull {label(s.Bull)} has one length only. An angled photograph shrinks one direction more than the other; draw a second length at right angles to the first.");
        }

        var without = Bulls.Keys.Where(b => Scales.All(s => s.Bull != b)).ToList();
        if (without.Count > 0)
        {
            said.Add($"{(without.Count == 1 ? "Bull " + label(without[0]) + " has" : "Bulls " + string.Join(", ", without.Select(label)) + " have")} no scale of its own and uses the nearest bull's.");
        }

        return said;
    }

    /// <summary>
    /// Entry 228 section 2.3: a single scale for the whole sheet is right only for a flatbed scan. A photograph says so in its camera fields,
    /// and then a person is told.
    /// </summary>
    public static string? SingleScaleOnAPhoto(ScaleReference? scale, ImageMetadata? read) =>
        scale is LengthReference && read is { CameraMake: not null } or { CameraModel: not null }
            ? "This looks like a photograph, and one length sets one scale for the whole sheet, which is right only for a scan. Draw a scale at each bull, or tap four corners of something rectangular."
            : null;
}
