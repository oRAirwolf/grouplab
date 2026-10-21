using System.Globalization;

namespace GroupLab.Core.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 465 4B: the scale's pill at the top of the analysis's side panel, and the one line under it saying how the
/// scale was found, built only from what each scale records: the printed markers and how many were found, a printer check or the scan's
/// own resolution behind them, a length or a rectangle measured, a store-bought target's printed size, scale markers in the photo, a
/// length at each bull, or shot coordinates imported. A set's tab says it of every sheet it pools, so the line never says more than the
/// sheets did.
/// </summary>
public static class ScaleSaid
{
    public const string Checked = "✓ Scale checked";

    public const string NoScale = "⚠ No scale";

    public const string ByHand = "⚠ Scale set by hand";

    public const string PrintedSize = "⚠ Scale from the printed size";

    public const string FromMarkers = "⚠ Scale from scale markers";

    public const string FromImport = "⚠ Scale from the imported numbers";

    public const string MoreThanOneKind = "⚠ Scales of more than one kind";

    /// <summary>Whether the pill is the checked one, teal: every sheet's scale came from its own printed markers.</summary>
    public static bool AllChecked(IReadOnlyList<ScaleReference?> scales) =>
        scales is { Count: > 0 } && scales.All(s => s is SheetReference);

    /// <summary>The pill's words for one scale.</summary>
    public static string Pill(ScaleReference? scale) => scale switch
    {
        null => NoScale,
        SheetReference => Checked,
        RectangleReference { PrintedTarget: not null } => PrintedSize,
        MarkerReference => FromMarkers,
        LengthReference length when ShotCsv.IsImported(length) => FromImport,
        _ => ByHand,
    };

    /// <summary>The pill's words for the sheets the figures come from: the scales that are set, all of one kind or said to be not.</summary>
    public static string Pill(IReadOnlyList<ScaleReference?> scales)
    {
        ArgumentNullException.ThrowIfNull(scales);
        var set = scales.Where(s => s is not null).ToList();
        return set.Count == 0 ? NoScale : set.Select(Pill).Distinct().ToList() is [var one] ? one : MoreThanOneKind;
    }

    /// <summary>
    /// The line under the pill: how the scale was found, for one sheet or for each sheet of a pool. <paramref name="noun"/> is what a pool
    /// holds, "sheet" or "picture".
    /// </summary>
    public static string How(IReadOnlyList<ScaleReference?> scales, UnitSettings units, string noun = "sheet")
    {
        ArgumentNullException.ThrowIfNull(scales);
        ArgumentNullException.ThrowIfNull(units);
        if (scales.Count == 0)
        {
            return One(null, units);
        }

        if (scales.Count == 1)
        {
            return One(scales[0], units);
        }

        if (scales.All(s => s is SheetReference))
        {
            var sheets = scales.Cast<SheetReference>().ToList();
            string on = sheets.Count == 2 ? $"both {noun}s" : string.Create(CultureInfo.InvariantCulture, $"all {sheets.Count} {noun}s");
            var corrections = sheets.GroupBy(Correction).Select(g => (Kind: g.Key, Count: g.Count())).OrderByDescending(g => g.Count).ToList();
            if (corrections is [var only])
            {
                return $"From the printed markers on {on}{Plural(only.Kind, noun)}.";
            }

            return $"From the printed markers on {on}: " + string.Join(", ", corrections.Select(c => Some(c.Kind, c.Count))) + ".";
        }

        var kinds = scales.GroupBy(Basis).Select(g => (Basis: g.Key, Count: g.Count())).OrderByDescending(g => g.Count).ToList();
        return "From " + string.Join("; ", kinds.Select(k => k.Basis is null
            ? string.Create(CultureInfo.InvariantCulture, $"no scale yet on {k.Count} {Noun(noun, k.Count)}")
            : string.Create(CultureInfo.InvariantCulture, $"{k.Basis} on {k.Count} {Noun(noun, k.Count)}"))) + ".";
    }

    /// <summary>How one sheet's scale was found.</summary>
    private static string One(ScaleReference? scale, UnitSettings units) => scale switch
    {
        null => "Nothing has set the scale yet: measure a known length on the picture.",
        SheetReference { MarkersFound: { } found, MarkersExpected: { } expected } sheet =>
            string.Create(CultureInfo.InvariantCulture, $"From {found} of {expected} printed markers on the sheet{Plural(Correction(sheet), "sheet", one: true)}."),
        SheetReference sheet => $"From the printed markers on the sheet{Plural(Correction(sheet), "sheet", one: true)}.",
        RectangleReference { Printed: { } printed } => $"From the printed size of the {printed.ShortName}, which GroupLab recognized; not checked against a ruler or a GroupLab sheet.",
        RectangleReference rectangle => $"From a {units.Number(rectangle.WidthInches)} by {units.Length(rectangle.HeightInches)} rectangle you measured on the picture.",
        LengthReference length when ShotCsv.IsImported(length) => "From the shot coordinates you imported, in the unit you gave them.",
        LengthReference length => $"From a {units.Length(length.Inches)} length you measured on the picture.",
        MarkerReference markers => FromMarkerSummary(markers.Summary),
        PerBullReference perBull => perBull.Scales.Count == 1
            ? "From a length you measured at 1 bull."
            : string.Create(CultureInfo.InvariantCulture, $"From lengths you measured at {perBull.Scales.Count} bulls."),
        _ => "From " + scale.Describe(units) + ".",
    };

    /// <summary>What was behind a sheet's printed markers: a printer check, the scan's own resolution, or neither.</summary>
    private enum Behind
    {
        PrinterCheck,
        ScanResolution,
        Neither,
    }

    private static Behind Correction(SheetReference sheet) =>
        sheet.ScaleFrom is not null ? Behind.PrinterCheck : sheet.PrintScale is not null ? Behind.ScanResolution : Behind.Neither;

    /// <summary>The words after "the printed markers on ..." when every sheet had the same behind it.</summary>
    private static string Plural(Behind behind, string noun, bool one = false) => behind switch
    {
        Behind.PrinterCheck => ", against your printer check",
        Behind.ScanResolution => one ? ", against the scan's resolution" : ", against each scan's resolution",
        _ => one ? $"; with no printer check, the figures are in the {noun}'s own inches" : $"; with no printer check, the figures are in the {noun}s' own inches",
    };

    /// <summary>Some of a pool's sheets, by what was behind their markers.</summary>
    private static string Some(Behind behind, int count) => behind switch
    {
        Behind.PrinterCheck => string.Create(CultureInfo.InvariantCulture, $"{count} against your printer check"),
        Behind.ScanResolution => string.Create(CultureInfo.InvariantCulture, $"{count} against {(count == 1 ? "its" : "their")} scan's resolution"),
        _ => string.Create(CultureInfo.InvariantCulture, $"{count} in {(count == 1 ? "its" : "their")} own inches, with no printer check"),
    };

    /// <summary>What a scale came from, a few words, for a pool of sheets whose scales differ; null for no scale.</summary>
    private static string? Basis(ScaleReference? scale) => scale switch
    {
        null => null,
        SheetReference => "the printed markers",
        RectangleReference { PrintedTarget: not null } => "a store-bought target's printed size",
        RectangleReference => "a rectangle you measured",
        LengthReference length when ShotCsv.IsImported(length) => "imported shot coordinates",
        LengthReference => "a length you measured",
        MarkerReference => "scale markers in the photo",
        PerBullReference => "lengths you measured at each bull",
        _ => "a scale of another kind",
    };

    private static string Noun(string noun, int count) => count == 1 ? noun : noun + "s";

    /// <summary>A scale markers reading's own first sentence, "Scale from ... in the photo: good to about 0.4 percent.", as "From ...".</summary>
    private static string FromMarkerSummary(string summary)
    {
        const string Lead = "Scale from ";
        if (string.IsNullOrWhiteSpace(summary) || !summary.StartsWith(Lead, StringComparison.Ordinal))
        {
            return "From the scale markers in the photo.";
        }

        int end = summary.IndexOf(". ", StringComparison.Ordinal);
        string first = end < 0 ? summary.TrimEnd() : summary[..(end + 1)];
        return "From " + first[Lead.Length..];
    }
}
