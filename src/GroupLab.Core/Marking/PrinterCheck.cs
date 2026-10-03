using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;

namespace GroupLab.Core.Marking;

/// <summary>What a picture of the check page measured: the profile, or why there is none, and what the paper's edge says.</summary>
public sealed record PrinterCheckResult(PrinterProfile? Profile, string? Why, PaperEdge? Paper, CardMeasure? Card);

/// <summary>
/// The printer check of NOTES-FROM-PLANNING.md entries 272 and 273, from a picture of the check page: a scan measures the printer by its
/// stated resolution, as any GroupLab sheet does; a photograph measures it by the card laid on the outline. Caliper and ruler readings are
/// typed and need no picture (<see cref="PrinterProfile.FromLengths"/>).
/// </summary>
public static class PrinterCheck
{
    /// <summary>A full frame's diagonal, 43.27 mm, which a 35 mm equivalent focal length is stated against.</summary>
    private const double FullFrameDiagonalMm = 43.27;

    public const string NoPage = "GroupLab could not read the check page's markers. Take the picture again with the whole page in view.";

    public const string NoCard = "GroupLab could not find all four sides of the card. Lay it inside the outline, flat, and take the picture again; a card with color works best on white paper.";

    /// <summary>Whether a definition is the printer check page, grid style 4, which is measured this way and not searched for holes.</summary>
    public static bool IsCheckPage(TargetDefinition? definition) => definition?.Grids?.Any(g => g.StyleOrDefault == GridStyle4.Style) == true;

    /// <summary>The printer measured from a picture of the check page.</summary>
    public static PrinterCheckResult Measure(GrayImage grey, ImageMetadata metadata, TargetDefinition page, IImagingBackend backend, string? name, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(grey);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(page);
        var measured = SheetMeasurer.Measure(grey, metadata, page, new MeasureOptions(), backend);
        if (measured.Registration is not { } registration)
        {
            return new PrinterCheckResult(null, NoPage, null, null);
        }

        if (PrinterProfile.FromScan(name, measured.Scale, today) is { } scan)
        {
            return new PrinterCheckResult(scan.OnPaper(page.Page.Size), null, null, null);
        }

        var paper = PaperEdgeCheck.Measure(grey, registration.Mapping);
        double perMm = (measured.Scale?.PixelsPerDmmArea ?? 0) * 10;
        double? focal = metadata.FocalLength35mm is > 0 and var f35
            ? f35 * Math.Sqrt(((double)grey.Width * grey.Width) + ((double)grey.Height * grey.Height)) / FullFrameDiagonalMm
            : null;
        if (CardCheck.Measure(grey, registration.Mapping, page, perMm, focal) is not { } card)
        {
            return new PrinterCheckResult(null, NoCard, paper, null);
        }

        // Entry 358 section 3: a check label measures that printer on that paper, across and along the feed.
        var profile = new PrinterProfile(string.IsNullOrWhiteSpace(name) ? PrinterProfile.DefaultName : name.Trim(), card.Across, card.Down,
            PrinterMethod.Card, today, PrinterProfile.CardUncertainty).OnPaper(page.Page.Size);
        return new PrinterCheckResult(profile, null, paper, card);
    }

    /// <summary>
    /// What a printer's scale means for a group (entry 273's result screen): "a 1.00 MOA group read 1.01", or that it reads true.
    /// </summary>
    public static string WhatItMeans(PrinterProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        double read = 1 / profile.Scale;
        return Math.Abs(read - 1) < 0.005
            ? "a group reads its true size"
            : string.Create(System.Globalization.CultureInfo.InvariantCulture, $"a 1.00 MOA group read {read:0.00}");
    }
}
