using System.Globalization;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Gltd.Validation;
using GroupLab.Core.Statistics;

namespace GroupLab.Cli.Library;

/// <summary>
/// What a person asks the parametric editor for, NOTES-FROM-PLANNING.md entry 99 section 3: a page, a grid of rows and columns, the spacing
/// between bulls, the ring size, how many sighters, and whether there is a load block. That set produces every multi-bull sheet in the
/// built-in library and a great many that are not in it.
/// </summary>
/// <param name="RingDmm">The outer ring's diameter, one of the documented disc stacks in <see cref="ParametricSheet.RingSizes"/>.</param>
public sealed record SheetSpec(string Name, string Page, int Columns, int Rows, double PitchInches, int RingDmm, int Sighters, bool LoadBlock);

/// <summary>How a check stands: fine, a warning that leaves the decision with the person, or a refusal because the sheet cannot work.</summary>
public enum CheckLevel
{
    Fine,
    Warning,
    Refusal,
}

/// <summary>One check on a designed sheet, as a sentence that gives the number and says what it means.</summary>
public sealed record SheetCheck(CheckLevel Level, string Sentence);

/// <summary>A designed sheet: the definition where one could be made, how many markers it carries, and every check on it.</summary>
public sealed record SheetDesign(SheetSpec Spec, TargetDefinition? Definition, int Markers, IReadOnlyList<SheetCheck> Checks)
{
    /// <summary>Whether it can be printed: nothing refused it.</summary>
    public bool Printable => Definition is not null && Checks.All(c => c.Level != CheckLevel.Refusal);
}

/// <summary>
/// The parametric target editor's engine, NOTES-FROM-PLANNING.md entries 99 and 100. A sheet is placed by the same rule the built-in library
/// was laid out with (tools/layout/layout.py's <c>solve</c>: the grid centred across the page, clear of the corner codes, a sighter row 1.2
/// pitches below it, the load block at the foot), finished by the same code that finishes a built-in sheet, and then checked.
/// <para>
/// <b>A custom target can be a bad target, and a form that lets a person build one that cannot be analysed is worse than no form</b> (entry
/// 99 section 4). A layout that does not fit its page, that the validator refuses, or that carries too few markers to register is refused. A
/// spacing that is tight for the stated dispersion is not refused: the editor says what proportion of shots it puts nearest the wrong bull and
/// leaves the decision with the person, in the voice of the flyer test and the print-at-actual-size sentence (entry 100 section 3).
/// </para>
/// </summary>
public static class ParametricSheet
{
    private const int Safe = 120, Qr = 260, Clearance = 30, LoadBlockHeight = 310;

    /// <summary>
    /// The fewest markers a sheet is allowed to carry: the fewest any built-in sheet carries, the 9 of GL-LR300-T's tile, which passes
    /// conformance test 43. Below it registration has not been shown to hold on any sheet this project prints.
    /// </summary>
    public const int FewestMarkers = 9;

    /// <summary>
    /// Below this many markers a sheet is thin: a photograph that loses a quarter of them, as the oblique mounted frames did, is near the
    /// floor. A warning, with the count, not a refusal.
    /// </summary>
    public const int ThinMarkers = 16;

    /// <summary>The bull spacing entry 56 section 7 sets as the rule: at least 6 sigma at the shooting distance, where 1 shot in 185 is misassigned.</summary>
    public const double SpacingInSigmas = 6;

    public static IReadOnlyList<string> Pages { get; } = ["letter", "a4", "tabloid", "a3"];

    /// <summary>The outer ring sizes with a documented disc stack, in dmm.</summary>
    public static IReadOnlyList<int> RingSizes { get; } = [.. LibraryBuilder.Stacks.Keys.Order()];

    private static readonly Dictionary<string, (PageSize Size, int Width, int Height)> PageSizes = new(StringComparer.Ordinal)
    {
        ["letter"] = (PageSize.Letter, 2159, 2794),
        ["a4"] = (PageSize.A4, 2100, 2970),
        ["tabloid"] = (PageSize.Tabloid, 2794, 4318),
        ["a3"] = (PageSize.A3, 2970, 4200),
    };

    /// <summary>
    /// The proportion of shots nearest a bull other than their own on a square lattice, entry 56 section 7: a shot stays nearest its own bull
    /// while both of its axis errors stay within half the spacing, so the rate is 1 - (1 - 2 Phi(-s / 2 sigma))^2, which entry 56 checked
    /// against 400,000 simulated shots.
    /// </summary>
    public static double Misassigned(double spacingOverSigma)
    {
        double outside = 2 * Distributions.NormalCdf(-spacingOverSigma / 2);
        return 1 - Math.Pow(1 - outside, 2);
    }

    /// <summary>
    /// The spacing check, entry 99 section 4 item 1 and entry 100 section 3: the chosen spacing against a stated five-shot group at a distance,
    /// as a number and what it means. A warning below <see cref="SpacingInSigmas"/>, a plain line at or above it, never a refusal.
    /// </summary>
    public static SheetCheck Spacing(double pitchInches, double groupMoa, double distanceYards)
    {
        double groupInches = groupMoa * 1.047 * distanceYards / 100;
        double sigma = RangeStatistics.Sigma(RangeStatistic.ExtremeSpread, groupInches, 5).Value;
        double ratio = pitchInches / sigma, rate = Misassigned(ratio);
        string oneIn = rate >= 0.5 ? "about half" : string.Create(CultureInfo.InvariantCulture, $"about one shot in {Math.Max(2, Math.Round(1 / rate)):0}");
        string sentence = string.Create(CultureInfo.InvariantCulture,
            $"A rifle shooting {groupMoa:0.##} MOA five-shot groups at {distanceYards:0} yd has a sigma of about {sigma:0.00} in, so {pitchInches:0.00} in between bulls is {ratio:0.0} sigma: {oneIn} ({100 * rate:0.##} percent) would land nearer a neighbouring bull than its own. Six sigma is where that falls to one in 185.");
        return new SheetCheck(ratio < SpacingInSigmas ? CheckLevel.Warning : CheckLevel.Fine, sentence);
    }

    /// <summary>Places, finishes and checks a sheet. It never throws for a bad layout; a bad layout comes back refused, with the reason.</summary>
    public static SheetDesign Design(SheetSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (!PageSizes.TryGetValue(spec.Page, out var page))
        {
            return Refused(spec, $"There is no {spec.Page} page here: choose letter, A4, tabloid or A3.");
        }

        if (spec.Columns < 1 || spec.Rows < 1 || spec.Columns * spec.Rows < 1)
        {
            return Refused(spec, "A sheet needs at least one row and one column of bulls.");
        }

        if (!LibraryBuilder.Stacks.ContainsKey(spec.RingDmm))
        {
            return Refused(spec, string.Create(CultureInfo.InvariantCulture, $"There is no ring of {spec.RingDmm / 254.0:0.00} in here: choose one of the listed sizes."));
        }

        // Pitch in whole even dmm, as layout.py requires, so the derived cell-boundary lattice lands on integer dmm.
        int pitch = 2 * (int)Math.Round(spec.PitchInches * 254 / 2);
        int ring = spec.RingDmm;
        if (pitch < ring + 20)
        {
            return Refused(spec, string.Create(CultureInfo.InvariantCulture,
                $"Bulls {spec.PitchInches:0.00} in apart with a {ring / 254.0:0.00} in ring overlap: the spacing has to be at least the ring plus 2 mm, {(ring + 20) / 254.0:0.00} in."));
        }

        // The library's own two fallbacks, found the hard way in Phase 0a and repeated here rather than rediscovered. A sighter row at the
        // conventional 1.2 pitches can push the marker line below it off the page, leaving the sighters outside the lattice (test 26f), which is
        // why GL-CF25-LTR declares a gap of 454 dmm rather than 456: so the gap is closed a step at a time until the sheet validates, and the
        // gap is declared when it departs from the convention. And a load block can leave no room for codes at the foot, which is why
        // GL-CF25-LTR-D carries its two codes at the top only: so with a load block, two codes are tried when four do not fit.
        int conventional = (int)(pitch * 1.2);
        var gaps = spec.Sighters > 0
            ? Enumerable.Range(0, 1 + ((conventional - Math.Max(pitch, ring + 20)) / 2)).Select(k => conventional - (2 * k)).ToList()
            : [conventional];
        string? shortOfRoom = null;
        (TargetDefinition Finished, List<Diagnostic> Errors)? firstInvalid = null;
        foreach (int codes in spec.LoadBlock ? new[] { 4, 2 } : [4])
        {
            foreach (int gap in gaps)
            {
                var attempt = Attempt(spec, page, pitch, ring, codes, gap, gap == conventional ? null : gap);
                if (attempt.Short is { } shortBy)
                {
                    shortOfRoom ??= string.Create(CultureInfo.InvariantCulture,
                        $"{spec.Rows} rows {spec.PitchInches:0.00} in apart{(spec.Sighters > 0 ? " with a sighter row" : "")}{(spec.LoadBlock ? " and a load block" : "")} need {shortBy / 254:0.00} in more height than a {spec.Page} page has. Fewer rows, a closer spacing or a larger page would fit.");
                    continue;
                }

                if (attempt.Failure is { } failure)
                {
                    return Refused(spec, failure);
                }

                if (attempt.Errors.Count == 0)
                {
                    return Checked(spec, attempt.Finished!);
                }

                firstInvalid ??= (attempt.Finished!, attempt.Errors);
            }
        }

        if (firstInvalid is { } invalid)
        {
            return new SheetDesign(spec, invalid.Finished, invalid.Finished.Fiducials?.Markers?.Count ?? 0,
                [.. invalid.Errors.Select(e => new SheetCheck(CheckLevel.Refusal, $"The layout fails the format's own check: {e.Message}"))]);
        }

        return Refused(spec, shortOfRoom ?? "The layout does not fit its page.");
    }

    /// <summary>One placement: the grid, the sighter row at the given gap, the given number of codes. Short of room, or finished and validated.</summary>
    private static (double? Short, string? Failure, TargetDefinition? Finished, List<Diagnostic> Errors) Attempt(
        SheetSpec spec, (PageSize Size, int Width, int Height) page, int pitch, int ring, int codeCount, int sighterGap, int? declaredGap)
    {
        var (size, width, height) = page;
        int dataBlock = spec.LoadBlock ? LoadBlockHeight : 0;
        var xs = Enumerable.Range(0, spec.Columns).Select(i => (int)Math.Round((width - ((spec.Columns - 1) * pitch)) / 2.0) + (i * pitch)).ToList();
        var sighterXs = spec.Sighters > 0
            ? Enumerable.Range(0, spec.Sighters).Select(i => (int)Math.Round((width - ((spec.Sighters - 1) * pitch)) / 2.0) + (i * pitch)).ToList()
            : [];

        // layout.py's solve: a column clashes with a corner code when it comes within the clearance of the code's band.
        bool Clash(double half, IEnumerable<int> columns) => columns.Any(x =>
            !(x + half + Clearance <= Safe || x - half - Clearance >= Safe + Qr) || !(x + half + Clearance <= width - Safe - Qr || x - half - Clearance >= width - Safe));
        double r2 = ring / 2.0;
        double top = Clash(r2, xs) ? Safe + Qr + Clearance + r2 : Safe + Clearance + r2;
        int bandAtFoot = codeCount == 4 ? Qr + (dataBlock > 0 ? Clearance : 0) : 0;
        double bottomSighter = bandAtFoot > 0 && Clash(r2, sighterXs) ? height - Safe - dataBlock - bandAtFoot - Clearance - r2 : height - Safe - dataBlock - Clearance - r2;
        double bottomBull = bandAtFoot > 0 && Clash(r2, xs) ? height - Safe - dataBlock - bandAtFoot - Clearance - r2 : height - Safe - dataBlock - Clearance - r2;
        double bottom = spec.Sighters > 0 ? Math.Min(bottomBull, bottomSighter - sighterGap) : bottomBull;
        double free = bottom - top - ((spec.Rows - 1) * pitch);
        if (free < 0)
        {
            return (-free, null, null, []);
        }

        int y0 = (int)Math.Round(top + (free / 2));
        var ys = Enumerable.Range(0, spec.Rows).Select(j => y0 + (j * pitch)).ToList();
        var bulls = new List<Bull>();
        foreach (int y in ys)
        {
            foreach (int x in xs)
            {
                bulls.Add(new Bull(x, y, "std", (bulls.Count + 1).ToString(CultureInfo.InvariantCulture), true, null));
            }
        }

        foreach (var (x, i) in sighterXs.Select((x, i) => (x, i)))
        {
            bulls.Add(new Bull(x, ys[^1] + sighterGap, "std", "S" + (i + 1).ToString(CultureInfo.InvariantCulture), false, null));
        }

        var definition = new TargetDefinition(
            1, 0, null, string.IsNullOrWhiteSpace(spec.Name) ? "Custom sheet" : spec.Name.Trim(),
            string.Create(CultureInfo.InvariantCulture, $"{spec.Columns * spec.Rows} scoring bulls on a {pitch / 10.0:0.0} mm grid, designed in the parametric editor."),
            "GroupLab parametric editor", "CC0-1.0", DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "dmm",
            new Page(size, width, height, Orientation.Portrait),
            LibraryBuilder.Inks,
            [new RingSet("std", LibraryBuilder.Discs(ring))],
            bulls,
            declaredGap is { } g ? new Cells(CellsMode.Grid, false, g, null, null, null, null) : null,
            new Fiducials("grid-boundary-1", FiducialFamily.AprilTag36h11, 40, 10, "fid", null),
            LibraryBuilder.Codes(width, height, dataBlock, codeCount),
            LibraryBuilder.Print,
            dataBlock > 0
                ? new DataBlock(Corners1.SafeMargin, height - Corners1.SafeMargin - dataBlock, width - (2 * Corners1.SafeMargin), dataBlock, DataBlockLayout.Fields3x3, FieldSet.Standard9, 280, "black", "text", 2, null)
                : null,
            null,
            null,
            null,
            []);

        TargetDefinition finished;
        try
        {
            finished = LibraryBuilder.Finish(definition.Name, "custom", definition).Definition;
        }
        catch (InvalidOperationException ex)
        {
            return (null, "The sheet cannot be encoded into its printed codes: " + ex.Message, null, []);
        }

        return (null, null, finished, [.. GltdValidator.Validate(finished).Where(d => d.Severity == Severity.Error)]);
    }

    /// <summary>A sheet that validated: its marker count checked, and the print instruction every sheet carries.</summary>
    private static SheetDesign Checked(SheetSpec spec, TargetDefinition finished)
    {
        int markers = finished.Fiducials?.Markers?.Count ?? 0;
        var checks = new List<SheetCheck>
        {
            markers < FewestMarkers
                ? new SheetCheck(CheckLevel.Refusal, string.Create(CultureInfo.InvariantCulture, $"The layout leaves room for {markers} markers. No sheet this project prints registers on fewer than {FewestMarkers}, so this one cannot be analysed. A wider spacing or fewer bulls leaves more room."))
                : markers < ThinMarkers
                    ? new SheetCheck(CheckLevel.Warning, string.Create(CultureInfo.InvariantCulture, $"The layout carries {markers} markers. A photograph that loses a quarter of them, as an oblique one can, keeps {markers - (markers / 4)}, near the {FewestMarkers} no sheet here registers below."))
                    : new SheetCheck(CheckLevel.Fine, string.Create(CultureInfo.InvariantCulture, $"The layout carries {markers} markers.")),
            new(CheckLevel.Fine, "Print at actual size, 100 percent. Never fit to page: a sheet printed at any other scale measures wrong."),
        };
        return new SheetDesign(spec, finished, markers, checks);
    }

    private static SheetDesign Refused(SheetSpec spec, string sentence) => new(spec, null, 0, [new SheetCheck(CheckLevel.Refusal, sentence)]);
}
