using System.Globalization;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.StoreTargets;

/// <summary>
/// A store-bought target found in a picture: which product, and the fit from inches on it to the picture's pixels, from which its bulls and
/// its scale follow (entry 340 section 1).
/// </summary>
public sealed record StoreTargetMatch(StoreTarget Target, Homography ToImage, double Layout, int Inliers)
{
    /// <summary>
    /// Entry 341 section 2, in Alan's words as planning gave them: shown wherever a store-bought target's scale is in play, never for
    /// GroupLab's own sheets, and it blocks nothing.
    /// </summary>
    public const string Warning =
        "Scale from this target's printed size. Printed targets can vary a little from sheet to sheet; check it against a ruler or a GroupLab sheet if the numbers matter.";

    /// <summary>The words on the way to checking it: the scale tools, one tap away.</summary>
    public const string CheckScale = "Check the scale";

    public static StoreTargetMatch Of(StoreTargetCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return new StoreTargetMatch(candidate.Target, candidate.ToImage ?? throw new ArgumentException("a candidate with no fit", nameof(candidate)), candidate.Layout, candidate.Inliers);
    }

    /// <summary>The product's aim points in the picture's pixels, where they are in the picture.</summary>
    public IReadOnlyList<PointD> Bulls(int width, int height) =>
        [.. Target.Fingerprint.Bulls.Select(ToImage.Apply).Where(p => p.X >= 0 && p.Y >= 0 && p.X < width && p.Y < height)];

    /// <summary>
    /// The scale: the printed area's four corners through the fit, as a rectangle of the printed size, so it removes perspective as a
    /// rectangle the person drew does, and says where it came from.
    /// </summary>
    public RectangleReference Scale()
    {
        var layout = Target.Fingerprint.Layout;
        PointD[] corners =
        [
            ToImage.Apply(new PointD(layout.X0, layout.Y0)),
            ToImage.Apply(new PointD(layout.X0 + layout.Width, layout.Y0)),
            ToImage.Apply(new PointD(layout.X0 + layout.Width, layout.Y0 + layout.Height)),
            ToImage.Apply(new PointD(layout.X0, layout.Y0 + layout.Height)),
        ];
        return new RectangleReference(corners, layout.Width, layout.Height) { PrintedTarget = Target.Id };
    }

    /// <summary>What the screen says once the target is recognized.</summary>
    public string Said(int bulls) => string.Create(CultureInfo.CurrentCulture,
        $"Recognized the {Target.Title}. Its {(bulls == 1 ? "bull is" : $"{bulls} bulls are")} placed and its printed size sets the scale.");

    /// <summary>Whether a marking's scale came from a store-bought target's printed size, so the warning goes beside it.</summary>
    public static bool InPlay(MarkingState state) => state?.Scale is RectangleReference { PrintedTarget: not null };
}

/// <summary>
/// What recognition decided about one picture: a product named, the family members to ask about, or neither; with every product's numbers
/// for the log.
/// </summary>
public sealed record StoreTargetRecognition(StoreTargetMatch? Named, IReadOnlyList<StoreTargetMatch> Family, IReadOnlyList<StoreTargetCandidate> Candidates)
{
    /// <summary>Whether the person is asked which size it is: entry 340 section 2's question.</summary>
    public bool AsksWhichSize => Named is null && Family.Count > 1;

    /// <summary>Whether recognition found anything at all.</summary>
    public bool Found => Named is not null || AsksWhichSize;

    /// <summary>One line for the log, naming what was decided and nothing of the picture.</summary>
    public string Describe() => Named is { } named
        ? string.Create(CultureInfo.InvariantCulture, $"recognized {named.Target.Id}, layout {named.Layout:0.000}, {named.Inliers} features")
        : AsksWhichSize ? "one of " + string.Join(" or ", Family.Select(f => f.Target.Id)) + ", asking which" : "not a store-bought target GroupLab knows";
}
