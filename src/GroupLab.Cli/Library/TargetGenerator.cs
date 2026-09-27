using System.Globalization;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Cli.Library;

/// <summary>What a person tells the target generator: the distance, the scope's lowest magnification (1 for a red dot, with the dot's
/// size), how many shots, and the page.</summary>
/// <param name="Diamond">Entry 243 section 4 item 4: the C bull's diamond instead of the disc, sized by the same rule.</param>
public sealed record GeneratorRequest(double DistanceYards, double LowestMagnification, double? RedDotMoa, int Shots, string Page, bool Diamond = false);

/// <summary>
/// The generator's answer: the bull it chose and why, the sheet, how many sheets, and the words that explain it. <see cref="Design"/> is the
/// parametric editor's own design, so every check the editor makes on a sheet it makes here.
/// </summary>
public sealed record GeneratedTargets(
    GeneratorRequest Request, int CenterDmm, int OuterDmm, int DotDmm, double PitchInches, int Columns, int Rows, int Sheets,
    SheetDesign? Design, IReadOnlyList<string> Explanation)
{
    /// <summary>One shot per bull: the bulls the set holds, which can be a few more than the shots asked for.</summary>
    public int Bulls => Columns * Rows * Sheets;
}

/// <summary>
/// The target generator, NOTES-FROM-PLANNING.md entry 226 section 4: a target sized for the optic at the distance, and as many sheets as
/// the shot count needs.
/// <para>
/// <b>The bull</b> is the aim point test's result (entry 226 section 3, "Can you see the bull?"): a black disc with a white center, the aim
/// being the white center, which is sized by the visibility rule to subtend <see cref="ArcminutesSeen"/> arcminutes through the lowest
/// magnification the sheet is for. For a red dot at 1x the white center is instead one and a half times the dot's own size, so the dot sits
/// inside it with paper showing round it. The black disc is three times the center, as the 2 inch bull that was the only design anyone
/// could center at 4x, and a small dot marks the exact middle. <b>Nothing small is the aim</b>: a crosshair that covers the dot still sits
/// in a white center that it cannot cover, which is what failed on the thin cross and the open gap.
/// </para>
/// <para>
/// <b>The sheet</b> puts bulls 1.35 diameters apart in the largest grid from 2 by 2 to 5 by 6 the page holds, with markers at the pitch or
/// at half of it, whichever registers, and the number of sheets is what the shots need. A set of more than one sheet is printed as a tiled assembly of that many
/// sheets, so each carries its place in the set in its codes, and the whole set's bulls are the shots to expect.
/// </para>
/// </summary>
public static class TargetGenerator
{
    /// <summary>The visibility rule: 3 to 4 arcminutes; the middle of it.</summary>
    public const double ArcminutesSeen = 3.5;

    /// <summary>The white center a red dot sits in, as a multiple of the dot.</summary>
    public const double DotMargin = 1.5;

    public const int MostColumns = 5, MostRows = 6;

    /// <summary>The C bull's proportions, entry 243 section 4: the diamond 1.25 in to the center's 0.36, and the dot 0.10 in to it, as E's.</summary>
    public const double DiamondToCenter = 1.25 / 0.36, DotToCenter = 0.10 / 0.36;

    private static double InchesPerMoa(double yards) => 1.0472 * yards / 100.0;

    public static GeneratedTargets Generate(GeneratorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var words = new List<string>();
        if (request.DistanceYards <= 0 || request.LowestMagnification < 1 || request.Shots < 1)
        {
            return new GeneratedTargets(request, 0, 0, 0, 0, 0, 0, 0, null, ["The distance, the magnification and the shots all have to be more than nothing, and a scope's magnification at least 1x."]);
        }

        double inchesPerMoa = InchesPerMoa(request.DistanceYards);
        double centerMoa;
        if (request.LowestMagnification < 1.5 && request.RedDotMoa is { } dotMoa and > 0)
        {
            centerMoa = Math.Max(DotMargin * dotMoa, ArcminutesSeen);
            words.Add(Say($"A {dotMoa:0.#} MOA dot covers {dotMoa * inchesPerMoa:0.00} in at {request.DistanceYards:0} yd, so the white center is {DotMargin:0.#} times the dot, {centerMoa * inchesPerMoa:0.00} in, and the dot sits inside it with paper showing round it."));
        }
        else
        {
            centerMoa = ArcminutesSeen / request.LowestMagnification;
            words.Add(Say($"At {request.LowestMagnification:0.#}x a feature has to subtend about {ArcminutesSeen:0.#} arcminutes to be centered on, so at {request.DistanceYards:0} yd the white center is {centerMoa * inchesPerMoa:0.00} in across."));
        }

        int center = Even(centerMoa * inchesPerMoa * 254);
        int outer, dot;
        List<Disc> discs;
        if (request.Diamond)
        {
            // Entry 243 section 4 item 4: the C bull in the aim point card's proportions, the white center measured point to point.
            outer = Even(center * DiamondToCenter);
            dot = Math.Max(10, Even(center * DotToCenter));
            discs = [new(outer, "black", DiscShape.Square, 45), new(center, "paper", DiscShape.Square, 45), new(dot, "black")];
            words.Add(Say($"The bull is a black diamond standing on a point, {outer / 254.0:0.00} in point to point, with that white center as a diamond and a small dot in the middle: its points lie on the vertical and horizontal lines through the aim, so a crosshair lines up with the shape even where the dot cannot be made out."));
        }
        else
        {
            outer = Even(center * 3);
            dot = Math.Max(10, Even(center / 4.0));
            discs = [new(outer, "black"), new(center, "paper"), new(dot, "black")];
            words.Add(Say($"The bull is a black disc {outer / 254.0:0.00} in across with that white center and a small dot in the middle: the aim is the white center, which a crosshair cannot cover, as it covered the thin cross and the open gap in the aim point test."));
        }

        // Bulls 1.35 diameters apart, in a whole number of 4 dmm so either marker scheme can use the pitch.
        int pitchDmm = 4 * (int)Math.Ceiling(outer * 1.35 / 4);
        double pitchInches = pitchDmm / 254.0;

        // Every grid from 2 by 2 to 5 by 6 with either marker scheme; the most bulls a sheet wins, then the fewest rows.
        SheetDesign? best = null;
        int bestColumns = 0, bestRows = 0;
        bool bestHalf = false;
        for (int columns = MostColumns; columns >= 2; columns--)
        {
            for (int rows = MostRows; rows >= 2; rows--)
            {
                if (columns * rows <= bestColumns * bestRows)
                {
                    continue;
                }

                foreach (bool half in (bool[])[false, true])
                {
                    var design = ParametricSheet.Design(Spec(request, columns, rows, pitchInches, outer, discs, 1, half));
                    if (design.Printable)
                    {
                        (best, bestColumns, bestRows, bestHalf) = (design, columns, rows, half);
                        break;
                    }
                }
            }
        }

        if (best is null)
        {
            words.Add(Say($"Four {outer / 254.0:0.00} in bulls do not fit a {request.Page} page with their markers and codes, and a sheet needs at least two rows and two columns to register. A larger page, a closer distance or more magnification would; for a large target, print tiled pages."));
            return new GeneratedTargets(request, center, outer, dot, pitchInches, 0, 0, 0, null, words);
        }

        // Fewer rows where one sheet holds every shot with room to spare.
        int perSheet = bestColumns * bestRows;
        int sheets = (int)Math.Ceiling(request.Shots / (double)perSheet);
        if (sheets == 1 && Math.Max(2, (int)Math.Ceiling(request.Shots / (double)bestColumns)) is var fewer && fewer < bestRows
            && ParametricSheet.Design(Spec(request, bestColumns, fewer, pitchInches, outer, discs, 1, bestHalf)).Printable)
        {
            bestRows = fewer;
        }

        var final = ParametricSheet.Design(Spec(request, bestColumns, bestRows, pitchInches, outer, discs, sheets, bestHalf));
        int bulls = bestColumns * bestRows * sheets;
        words.Add(sheets == 1
            ? Say($"{bestColumns * bestRows} bulls on one {request.Page} sheet, {pitchInches:0.00} in apart: one shot per bull.")
            : Say($"{bestColumns * bestRows} bulls a sheet, {pitchInches:0.00} in apart, on {sheets} sheets: one shot per bull. Each sheet's codes say which of the {sheets} it is."));
        if (bulls > request.Shots)
        {
            words.Add(Say($"That is {bulls} bulls for {request.Shots} shots; leave {bulls - request.Shots} empty and enter the rounds fired, so GroupLab expects {request.Shots}."));
        }

        return new GeneratedTargets(request, center, outer, dot, pitchInches, bestColumns, bestRows, sheets, final, words);
    }

    private static SheetSpec Spec(GeneratorRequest r, int columns, int rows, double pitch, int outer, IReadOnlyList<Disc> discs, int sheets, bool half) =>
        new(Say($"GroupLab generated, {r.DistanceYards:0} yd, {(r.LowestMagnification < 1.5 && r.RedDotMoa is { } d ? $"{d:0.#} MOA dot" : $"{r.LowestMagnification:0.#}x")}{(r.Diamond ? ", diamond" : "")}"),
            r.Page, columns, rows, pitch, outer, 0, false, discs, sheets, half);

    private static int Even(double dmm) => 2 * (int)Math.Round(dmm / 2, MidpointRounding.AwayFromZero);

    private static string Say(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
}
