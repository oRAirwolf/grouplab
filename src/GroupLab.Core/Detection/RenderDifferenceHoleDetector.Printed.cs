using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;

namespace GroupLab.Core.Detection;

/// <summary>
/// What a GroupLab sheet printed, in page dmm, for the two refusals of NOTES-FROM-PLANNING.md entry 354 section 1: the box everything printed
/// lies in, words included, and each line of words with its own box.
/// </summary>
internal sealed record PrintedMatter(double Left, double Top, double Right, double Bottom, IReadOnlyList<(string Text, double Left, double Top, double Right, double Bottom)> Words);

public static partial class RenderDifferenceHoleDetector
{
    /// <summary>
    /// How far printing may have moved on the paper and still be that printing, inches: what a crinkled sheet leaves between its markers after
    /// registration. It is the lift below which a bent sheet is not taken (<see cref="BentSheetMapping.LiftedFromDmm"/>, 0.04 in), so
    /// anything the registration leaves unbent is within it. On the crinkled .22 sheet of entry 354 the markers the fit left out were 0.01
    /// to 0.035 in off it.
    /// </summary>
    internal const double MovedPrintInches = BentSheetMapping.LiftedFromDmm / 254;

    /// <summary>
    /// What a mark in a photograph's margin must be to be kept as a stray shot: its own area this share of the hull's or more, its box no
    /// longer than this, and a bullet's size, this share of the named calibre's hole by area or the smallest hole any bullet makes.
    /// </summary>
    internal const double CleanHoleSolidity = 0.85, CleanHoleAspect = 1.5, CleanHoleOfCalibre = 0.6;

    /// <summary>What a mark past everything the sheet printed is refused with.</summary>
    internal const string PastThePrint = "past everything the sheet printed, in its margin, where a curled or torn edge, its shadow and the board behind it show, and not a clean round hole";

    /// <summary>
    /// The words on a sheet the detector cannot subtract. The expected artwork leaves words out and relies on the opening to remove their
    /// strokes, which a sharp scan's do; a photograph's blur thickens a bull's number until it reads as a small dark mark.
    /// </summary>
    private static readonly SceneLayer[] WordLayers = [SceneLayer.Labels, SceneLayer.Identifier, SceneLayer.Name, SceneLayer.PrintNote];

    /// <summary>
    /// The sheet as it prints, the print instruction along its bottom edge included, so the box holds everything ink can be on: a mark
    /// beyond it is on the margin, and the words are where the sheet set them (<see cref="SheetGlyphs"/>, the PDF's own positions).
    /// </summary>
    internal static PrintedMatter Printed(TargetDefinition definition, int tile)
    {
        var pages = SceneBuilder.Build(definition, new RenderOptions(AllowInvalid: true, PrintNote: SceneBuilder.ActualSizeNote)).Pages;
        var scene = pages[Math.Clamp(tile, 0, pages.Count - 1)];
        double left = double.MaxValue, top = double.MaxValue, right = double.MinValue, bottom = double.MinValue;
        void Take(double l, double t, double r, double b)
        {
            left = Math.Min(left, l);
            top = Math.Min(top, t);
            right = Math.Max(right, r);
            bottom = Math.Max(bottom, b);
        }

        var words = new List<(string, double, double, double, double)>();
        foreach (var item in scene.Items)
        {
            switch (item)
            {
                case RectFill r:
                    Take(r.X, r.Y, r.X + r.Width, r.Y + r.Height);
                    break;
                case DiscBand d:
                    Take(d.CentreX - d.OuterRadius, d.CentreY - d.OuterRadius, d.CentreX + d.OuterRadius, d.CentreY + d.OuterRadius);
                    break;
                case TextRun t:
                    var points = SheetGlyphs.Contours(t).SelectMany(c => c).ToList();
                    if (points.Count == 0)
                    {
                        break;
                    }

                    double l = points.Min(p => p.X), tp = points.Min(p => p.Y), rt = points.Max(p => p.X), b = points.Max(p => p.Y);
                    Take(l, tp, rt, b);
                    if (WordLayers.Contains(t.Layer))
                    {
                        words.Add((t.Text, l / Scene.UnitsPerDmm, tp / Scene.UnitsPerDmm, rt / Scene.UnitsPerDmm, b / Scene.UnitsPerDmm));
                    }

                    break;
            }
        }

        return left > right
            ? new PrintedMatter(0, 0, definition.Page.Width, definition.Page.Height, words)
            : new PrintedMatter(left / Scene.UnitsPerDmm, top / Scene.UnitsPerDmm, right / Scene.UnitsPerDmm, bottom / Scene.UnitsPerDmm, words);
    }

    /// <summary>Whether a mark's centre, page dmm, lies past everything printed.</summary>
    internal static bool IsPastThePrint(PrintedMatter printed, PointD page) =>
        page.X < printed.Left || page.X > printed.Right || page.Y < printed.Top || page.Y > printed.Bottom;

    /// <summary>
    /// The printing a mark's centre, page dmm, lies within <paramref name="reachDmm"/> of: the nearest marker, code, data block or line of
    /// words, named as a person reads it, or null.
    /// </summary>
    private static string? Beside(IReadOnlyList<Zone> zones, PrintedMatter printed, PointD page, double reachDmm)
    {
        static double Gap(PointD p, double left, double top, double right, double bottom)
        {
            double dx = Math.Max(0, Math.Max(left - p.X, p.X - right)), dy = Math.Max(0, Math.Max(top - p.Y, p.Y - bottom));
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        var near = zones.Select(z => (Name: z.Name, Gap: Gap(page, z.Left, z.Top, z.Right, z.Bottom)))
            .Concat(printed.Words.Select(w => (Name: $"the printed words \"{w.Text}\"", Gap: Gap(page, w.Left, w.Top, w.Right, w.Bottom))))
            .Where(c => c.Gap <= reachDmm)
            .OrderBy(c => c.Gap)
            .FirstOrDefault();
        return near.Name;
    }

    /// <summary>
    /// The line of words a whole mark lies on, every point of its hull within that line's box widened by <see cref="MovedPrintInches"/>, or
    /// null. A hole that only touches the words is larger than they are and reaches past the box, so it is never taken for them.
    /// </summary>
    internal static string? WordsUnder(PrintedMatter printed, IReadOnlyList<PointD> hull, IPageMapping registration)
    {
        if (printed.Words.Count == 0 || hull.Count == 0)
        {
            return null;
        }

        var points = hull.Select(registration.ToPage).ToList();
        double slack = MovedPrintInches * 254;
        foreach (var (text, left, top, right, bottom) in printed.Words)
        {
            if (points.All(p => p.X >= left - slack && p.X <= right + slack && p.Y >= top - slack && p.Y <= bottom + slack))
            {
                return text;
            }
        }

        return null;
    }
}
