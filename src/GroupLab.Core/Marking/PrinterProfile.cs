using System.Globalization;
using System.Text.Json.Nodes;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Measurement;

namespace GroupLab.Core.Marking;

/// <summary>How a printer's scale was measured.</summary>
public enum PrinterMethod
{
    /// <summary>A scan of one of its sheets, whose stated resolution is an absolute ruler.</summary>
    Scan,

    /// <summary>One distance on one of its sheets, measured with a ruler and typed in.</summary>
    Ruler,
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 271: the scale a printer prints GroupLab sheets at, measured once and applied to photographs from then on.
/// <para>
/// A photograph has no absolute ruler in it, so a sheet printed small cannot be told from a full-size sheet a little farther away
/// (<c>docs/WHAT-CAN-BE-MEASURED.md</c>). But print scale belongs to a printer and its settings, and it is stable, so a scan of one sheet, or
/// one ruler measurement, gives the figure every later photograph of that printer's sheets is multiplied by. The result then says whose
/// figure it used and how it was measured, so a person who changed printers can see it is the wrong one.
/// </para>
/// </summary>
/// <param name="Name">What the person calls the printer, "My printer" unless they said otherwise.</param>
/// <param name="Scale">How large the printer prints: 0.992 is 99.2 percent of the intended size.</param>
/// <param name="Uncertainty">Half the width of the range the scale is believed to lie in, as a fraction: 0.001 is a tenth of a percent.</param>
public sealed record PrinterProfile(string Name, double Scale, PrinterMethod Method, DateOnly MeasuredOn, double Uncertainty)
{
    public const string DefaultName = "My printer";

    /// <summary>
    /// The least uncertainty a scan is given, a tenth of a percent: a flatbed's stated resolution is good to about that, and its own x and y
    /// are known to differ by more than a printer's do.
    /// </summary>
    public const double ScanFloor = 0.001;

    /// <summary>How finely a person reads a ruler: a sixteenth of an inch at each end, so a thirty-second either way over the span.</summary>
    public const double RulerReadingInches = 1.0 / 32;

    /// <summary>The profile a scan's measured scale makes, or null where the scan measured none GroupLab believes.</summary>
    public static PrinterProfile? FromScan(string? name, ScaleReport? report, DateOnly measuredOn)
    {
        if (SheetReference.Correction(report) is not { } scale || report is null)
        {
            return null;
        }

        // The scan's x and y scales differ a little; half that difference, and never less than the floor, is how well the one figure is known.
        double spread = Math.Abs(report.PixelsPerDmmX - report.PixelsPerDmmY) / (2 * report.PixelsPerDmmArea);
        return new PrinterProfile(Named(name), scale, PrinterMethod.Scan, measuredOn, Math.Max(ScanFloor, spread));
    }

    /// <summary>
    /// The profile one ruler measurement makes: the distance the person measured on the sheet over the distance it was drawn. Null where
    /// either is not a positive number, or where the scale comes out beyond anything a printer does, which is far more likely a typing slip
    /// or the wrong two bulls than a real print.
    /// </summary>
    public static PrinterProfile? FromRuler(string? name, double measuredInches, double drawnInches, DateOnly measuredOn)
    {
        if (!(measuredInches > 0) || !(drawnInches > 0) || double.IsInfinity(measuredInches) || double.IsInfinity(drawnInches))
        {
            return null;
        }

        double scale = measuredInches / drawnInches;
        return scale < SheetReference.LowestBelievable || scale > SheetReference.HighestBelievable
            ? null
            : new PrinterProfile(Named(name), scale, PrinterMethod.Ruler, measuredOn, RulerReadingInches / drawnInches);
    }

    private static string Named(string? name) => string.IsNullOrWhiteSpace(name) ? DefaultName : name.Trim();

    /// <summary>Whether two measurements of a printer agree within what each says it knows.</summary>
    public bool AgreesWith(PrinterProfile other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Math.Abs(Scale - other.Scale) <= Uncertainty + other.Uncertainty;
    }

    /// <summary>How it was measured, for the one line a result carries: "from a scan on 28 September".</summary>
    public string How => string.Create(CultureInfo.InvariantCulture,
        $"{(Method == PrinterMethod.Scan ? "from a scan" : "with a ruler")} on {MeasuredOn.ToString("d MMMM", CultureInfo.InvariantCulture)}");

    /// <summary>The one line a photograph's result carries once this profile has corrected it, entry 271 section 2, word for word.</summary>
    public string Line => string.Create(CultureInfo.InvariantCulture, $"Corrected for {Name}'s {Scale * 100:0.0} percent, measured {How}.");

    /// <summary>The offer a scan's result makes, entry 271 section 2.</summary>
    public static string Offer(double scale) => string.Create(CultureInfo.InvariantCulture,
        $"Your printer printed this sheet at {scale * 100:0.0} percent. Use this for photos of sheets from the same printer?");

    public JsonObject ToJson() => new()
    {
        ["name"] = Name,
        ["scale"] = Scale,
        ["method"] = Method == PrinterMethod.Scan ? "scan" : "ruler",
        ["measuredOn"] = MeasuredOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ["uncertainty"] = Uncertainty,
    };

    /// <summary>A profile read back, or null where anything in it is missing or beyond belief.</summary>
    public static PrinterProfile? FromJson(JsonNode? node)
    {
        try
        {
            if (node is not JsonObject o || (string?)o["name"] is not { } name || (double?)o["scale"] is not { } scale
                || (double?)o["uncertainty"] is not { } uncertainty
                || !DateOnly.TryParseExact((string?)o["measuredOn"], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var on)
                || scale < SheetReference.LowestBelievable || scale > SheetReference.HighestBelievable || !(uncertainty >= 0))
            {
                return null;
            }

            var method = (string?)o["method"] switch { "scan" => PrinterMethod.Scan, "ruler" => PrinterMethod.Ruler, _ => (PrinterMethod?)null };
            return method is { } m ? new PrinterProfile(Named(name), scale, m, on, uncertainty) : null;
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            return null;
        }
    }
}

/// <summary>
/// The distance a person measures with a ruler on a GroupLab sheet, entry 271 section 2: from bull 1 to the last bull on its row, centre to
/// centre, which on the 5 by 5 sheets is bull 1 to bull 5. The longest distance along one row is the one a ruler reads best, because the
/// same sixteenth at each end is a smaller part of it.
/// </summary>
/// <param name="From">The first bull's name as the sheet prints it.</param>
/// <param name="To">The second bull's name.</param>
/// <param name="DrawnInches">How far apart their centres are drawn.</param>
public sealed record RulerSpan(string From, string To, double DrawnInches)
{
    /// <summary>What the ruler dialog asks, in the person's words.</summary>
    public string Ask => string.Create(CultureInfo.InvariantCulture,
        $"Measure from the center of bull {From} to the center of bull {To}. They are drawn {DrawnInches:0.000} in apart.");

    /// <summary>
    /// A ruler reading as a person types it, in inches: "5.75", "5 3/4", "5.75 in", "5.75\"", "146 mm" or "14.6 cm". Null for anything else,
    /// so a slip is asked again rather than guessed at.
    /// </summary>
    public static double? ReadInches(string? typed)
    {
        string text = (typed ?? "").Trim().ToLowerInvariant();
        double per = 1;
        foreach (var (suffix, inches) in new[] { ("mm", 1 / 25.4), ("cm", 1 / 2.54), ("inches", 1.0), ("inch", 1.0), ("in", 1.0), ("\"", 1.0) })
        {
            if (text.EndsWith(suffix, StringComparison.Ordinal))
            {
                text = text[..^suffix.Length].Trim();
                per = inches;
                break;
            }
        }

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        double? value = parts.Length switch
        {
            1 => Number(parts[0]),
            2 when per == 1 && Number(parts[0]) is { } whole && Fraction(parts[1]) is { } part => whole + part,
            _ => null,
        };
        return value is > 0 ? value * per : null;

        static double? Number(string s) => Fraction(s) ?? (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && double.IsFinite(v) ? v : null);

        static double? Fraction(string s) => s.Split('/') is [var a, var b]
            && int.TryParse(a, NumberStyles.None, CultureInfo.InvariantCulture, out int n) && int.TryParse(b, NumberStyles.None, CultureInfo.InvariantCulture, out int d) && d > 0
            ? (double)n / d
            : null;
    }

    /// <summary>The span on this sheet, or null on a sheet with fewer than two scoring bulls.</summary>
    public static RulerSpan? Of(TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var bulls = definition.Bulls.Select((b, i) => (Bull: b, Name: b.Label ?? (i + 1).ToString(CultureInfo.InvariantCulture))).Where(b => b.Bull.Scoring).ToList();
        if (bulls.Count < 2)
        {
            return null;
        }

        var first = bulls[0];
        // The row is the bulls level with the first, within a millimetre; where it has none, the bull farthest from it.
        var row = bulls.Skip(1).Where(b => Math.Abs(b.Bull.Y - first.Bull.Y) <= 10).ToList();
        var to = (row.Count > 0 ? row : bulls.Skip(1).ToList())
            .MaxBy(b => Math.Pow(b.Bull.X - first.Bull.X, 2) + Math.Pow(b.Bull.Y - first.Bull.Y, 2));
        double dmm = Math.Sqrt(Math.Pow(to.Bull.X - first.Bull.X, 2) + Math.Pow(to.Bull.Y - first.Bull.Y, 2));
        return new RulerSpan(first.Name, to.Name, dmm / 254);
    }
}
