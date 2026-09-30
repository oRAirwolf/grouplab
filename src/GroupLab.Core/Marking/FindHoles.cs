using System.Globalization;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Marking;

/// <summary>Where Find holes proposed a shot: how the hole looked, and why the finder was unsure of it, or null where it was not.</summary>
public sealed record HoleProposal(HoleLook Look, string? Doubt);

/// <summary>
/// Find holes, NOTES-FROM-PLANNING.md entry 318 section 2: on a target GroupLab did not print, once the person has set the scale (and the
/// bulls, if they marked them), <see cref="AnyTargetHoleFinder"/> proposes the holes. Each proposal becomes a normal mark to confirm, move or
/// remove, and one the finder is unsure of goes to the review queue. It is experimental and says so wherever it is offered: on the desktop
/// always, on the phone in GroupLab Dev only. docs/DETECTION-LEARNING-STUDY.md section 7 has what it finds and what it misses.
/// </summary>
public static class FindHoles
{
    /// <summary>The button's words, the same on every screen that offers it.</summary>
    public const string Label = "Find holes (Experimental)";

    /// <summary>What the button does, beside it.</summary>
    public const string Explanation =
        "Experimental: GroupLab proposes the holes on a target it did not print, using the scale you set. Check every one: move any that is off its hole and remove any that is not a hole.";

    /// <summary>Whether Find holes can be offered: a picture whose scale the person set by hand, as on a target GroupLab did not print.</summary>
    public static bool Offered(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ImagePath is not null && state.Scale is not null and not SheetReference;
    }

    /// <summary>
    /// The picture's pixels an inch on the target at <paramref name="at"/>, from the scale: the mean of the two directions, so a rectangle's
    /// perspective is taken where the holes are.
    /// </summary>
    public static double PixelsPerInch(ScaleReference scale, PointD at)
    {
        ArgumentNullException.ThrowIfNull(scale);
        var a = scale.ToTarget(at);
        var dx = scale.ToTarget(new PointD(at.X + 1, at.Y));
        var dy = scale.ToTarget(new PointD(at.X, at.Y + 1));
        double inchesPerPixel = (Math.Sqrt(Math.Pow(dx.X - a.X, 2) + Math.Pow(dx.Y - a.Y, 2)) + Math.Sqrt(Math.Pow(dy.X - a.X, 2) + Math.Pow(dy.Y - a.Y, 2))) / 2;
        return inchesPerPixel > 1e-12 ? 1 / inchesPerPixel : throw new InvalidOperationException("the scale gives no size to a pixel");
    }

    /// <summary>
    /// The holes proposed on <paramref name="value"/>, the picture's brightest channel, with the marking's scale taken at its bulls (the middle
    /// of the picture where there are none) and its bullet, where one is named.
    /// </summary>
    public static AnyTargetFinding Run(GrayImage value, MarkingState state, IImagingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(backend);
        if (!Offered(state))
        {
            throw new InvalidOperationException("Find holes needs a picture and a scale set by hand.");
        }

        var at = state.Bulls.Count > 0
            ? new PointD(state.Bulls.Average(b => b.Image.X), state.Bulls.Average(b => b.Image.Y))
            : new PointD(value.Width / 2.0, value.Height / 2.0);
        return AnyTargetHoleFinder.Find(value, PixelsPerInch(state.Scale!, at), backend, state.Calibre?.DiameterInches);
    }

    /// <summary>What the screen says once the holes are placed.</summary>
    public static string Said(int placed, int doubted) => (placed, doubted) switch
    {
        (0, _) => "Find holes proposed nothing new. Add any holes by hand.",
        (_, 0) => string.Create(CultureInfo.CurrentCulture, $"Find holes proposed {placed} {(placed == 1 ? "hole" : "holes")}. Check each one: move any that is off its hole and remove any that is not a hole."),
        _ => string.Create(CultureInfo.CurrentCulture,
            $"Find holes proposed {placed} {(placed == 1 ? "hole" : "holes")}, {doubted} of them to check in the review. Check each one: move any that is off its hole and remove any that is not a hole."),
    };
}
