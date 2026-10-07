using System.Globalization;
using System.Text.Json.Nodes;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Measurement;

namespace GroupLab.Core.Marking;

/// <summary>How a printer's scale was measured, NOTES-FROM-PLANNING.md entries 271 to 273.</summary>
public enum PrinterMethod
{
    /// <summary>A scan of one of its sheets, whose stated resolution is an absolute ruler.</summary>
    Scan,

    /// <summary>Lengths on one of its sheets or the check page, measured with a ruler or tape and typed in.</summary>
    Ruler,

    /// <summary>A bank, gift or ID card laid on the check page's outline and photographed.</summary>
    Card,

    /// <summary>The check page's crosshairs, measured with a digital caliper and typed in.</summary>
    Caliper,
}

/// <summary>
/// NOTES-FROM-PLANNING.md entries 271 to 273: the scale a printer prints GroupLab sheets at, measured once and applied to photographs from
/// then on, across the sheet and down it separately, because a printer's paper feed and its print head do not have to agree.
/// <para>
/// A photograph has no absolute ruler in it, so a sheet printed small cannot be told from a full-size sheet a little farther away
/// (<c>docs/WHAT-CAN-BE-MEASURED.md</c>). But print scale belongs to a printer and its settings, and it is stable, so a scan of one sheet, a
/// card on the check page, or a caliper or ruler reading gives the figures every later photograph of that printer's sheets is multiplied
/// by. The result then says whose figures it used, so a person who changed printers can see it is the wrong one.
/// </para>
/// </summary>
/// <param name="Name">What the person calls the printer, "My printer" unless they said otherwise.</param>
/// <param name="Across">How large the printer prints across the sheet: 0.992 is 99.2 percent of the intended size.</param>
/// <param name="Down">The same, down the sheet.</param>
/// <param name="Uncertainty">Half the width of the range each figure is believed to lie in, as a fraction: 0.003 is 0.3 percent.</param>
public sealed record PrinterProfile(string Name, double Across, double Down, PrinterMethod Method, DateOnly MeasuredOn, double Uncertainty)
{
    public const string DefaultName = "My printer";

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 291 section 5.2: how long a check is trusted before GroupLab offers another, about six months. Nothing
    /// measured sets it: a printer's scale is stable from week to week, but a new cartridge, a firmware update or a service can move it, and a
    /// check a person is reminded of twice a year costs them a minute.
    /// </summary>
    public const int StaleAfterDays = 182;

    /// <summary>
    /// Entry 291 section 5.2: the day the person said this printer was calibrated, serviced or set differently after it was checked, or null
    /// where they have not. A check measures the sheets printed before it; one printed after such a change may print at another size, and no
    /// photograph can tell when its sheet was printed, so the correction stays and the result says so, with the check beside it.
    /// </summary>
    public DateOnly? ChangedOn { get; init; }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 358 section 3: the paper a thermal printer was checked on, "4x6 labels", or null for an office printer's
    /// check, which holds for any paper. A thermal printer's feed scale depends on its paper, so its check belongs to the printer and the paper
    /// together, and the name it is saved under says both.
    /// </summary>
    public string? Paper { get; init; }

    /// <summary>
    /// Entry 386 section 3 (entry 372: "one label size per printer, remembered"): the label size loaded in a label printer such as the M220,
    /// millimetres, kept with its check, so every scale label is laid out for it and the size is asked for only when the person changes it.
    /// Null for any other printer.
    /// </summary>
    public (int Width, int Height)? LabelSize { get; init; }

    /// <summary>The words a label printer's check is kept under for a label size: "70 x 80 mm labels".</summary>
    public static string LabelPaper(int width, int height) => string.Create(CultureInfo.InvariantCulture, $"{width} x {height} mm labels");

    /// <summary>The paper a check on a page of this size belongs to: a label size's words, or null for every other page.</summary>
    public static string? PaperOf(PageSize size) => size switch
    {
        PageSize.Label4x6 => "4x6 labels",
        PageSize.A6 => "A6 labels",
        PageSize.Label100x150 => "100 x 150 mm labels",
        _ => null,
    };

    /// <summary>This profile as the check of a printer on a label paper, its name saying both; unchanged for any other page.</summary>
    public PrinterProfile OnPaper(PageSize size) => PaperOf(size) is { } paper && Paper is null
        ? this with { Name = $"{Name}, {paper}", Paper = paper }
        : this;

    /// <summary>
    /// The profile a photograph of a sheet on <paramref name="size"/> is corrected with (entry 358 section 3): on a label, the check made on that
    /// label paper where there is one, else the chosen printer's where it is not a label check; on any other page, the chosen printer's unless it
    /// is a label check, then the last office check saved.
    /// </summary>
    public static PrinterProfile? For(PageSize size, IReadOnlyList<PrinterProfile> saved, PrinterProfile? chosen)
    {
        ArgumentNullException.ThrowIfNull(saved);
        if (PaperOf(size) is { } paper)
        {
            return saved.LastOrDefault(p => p.Paper == paper) ?? (chosen?.Paper is null ? chosen : null);
        }

        return chosen is { Paper: null } ? chosen : saved.LastOrDefault(p => p.Paper is null);
    }

    /// <summary>The button under a printer in Settings that says it was changed, entry 291 section 5.2.</summary>
    public const string ChangedWords = "Printer calibrated or serviced";

    /// <summary>
    /// A scan's uncertainty: a flatbed's stated resolution is good to about a tenth of a percent overall, but its own two axes can differ
    /// by about two tenths, which is the size of what a scan says about across against down.
    /// </summary>
    public const double ScanUncertainty = 0.002;

    /// <summary>A card's: the card itself varies by about 0.15 percent new and 0.3 worn (entry 272), and its edges are read to about 0.1.</summary>
    public const double CardUncertainty = 0.003;

    /// <summary>How finely a person reads a ruler: a sixteenth of an inch at each end, so a thirty-second either way over the span.</summary>
    public const double RulerReadingInches = 1.0 / 32;

    /// <summary>How well a digital caliper is set on a crosshair's center by eye: about a tenth of a millimeter at each end.</summary>
    public const double CaliperReadingInches = 0.2 / 25.4;

    /// <summary>The one figure for the area, which is what a single percentage means: the geometric mean of across and down.</summary>
    public double Scale => Math.Sqrt(Across * Down);

    /// <summary>The profile a scan's measured scale makes, or null where the scan measured none GroupLab believes.</summary>
    public static PrinterProfile? FromScan(string? name, ScaleReport? report, DateOnly measuredOn) =>
        SheetReference.Correction(report) is not null && report?.ScaleX is { } x && report.ScaleY is { } y && Believable(x) && Believable(y)
            ? new PrinterProfile(Named(name), x, y, PrinterMethod.Scan, measuredOn, ScanUncertainty)
            : null;

    /// <summary>
    /// The profile one ruler measurement on a sheet makes (entry 271): the distance the person measured over the distance it was drawn, the
    /// same both ways. Null where either is not a positive number, or where the scale comes out beyond anything a printer does, which is far
    /// more likely a typing slip or the wrong two bulls than a real print.
    /// </summary>
    public static PrinterProfile? FromRuler(string? name, double measuredInches, double drawnInches, DateOnly measuredOn) =>
        Ratio(measuredInches, drawnInches) is { } k
            ? new PrinterProfile(Named(name), k, k, PrinterMethod.Ruler, measuredOn, RulerReadingInches / drawnInches)
            : null;

    /// <summary>
    /// The profile two typed lengths make (entry 273): the check page's crosshairs with a caliper, or its two long lines with a ruler, each
    /// over its drawn length. Null where either is a slip.
    /// </summary>
    public static PrinterProfile? FromLengths(string? name, PrinterMethod method, double acrossInches, double acrossDrawnInches, double downInches, double downDrawnInches, DateOnly measuredOn)
    {
        if (method is not (PrinterMethod.Ruler or PrinterMethod.Caliper) || Ratio(acrossInches, acrossDrawnInches) is not { } x || Ratio(downInches, downDrawnInches) is not { } y)
        {
            return null;
        }

        double reading = method == PrinterMethod.Caliper ? CaliperReadingInches : RulerReadingInches;
        return new PrinterProfile(Named(name), x, y, method, measuredOn, reading / Math.Min(acrossDrawnInches, downDrawnInches));
    }

    private static double? Ratio(double measured, double drawn) =>
        double.IsFinite(measured) && double.IsFinite(drawn) && measured > 0 && drawn > 0 && Believable(measured / drawn) ? measured / drawn : null;

    private static bool Believable(double k) => k >= SheetReference.LowestBelievable && k <= SheetReference.HighestBelievable;

    private static string Named(string? name) => string.IsNullOrWhiteSpace(name) ? DefaultName : name.Trim();

    /// <summary>Whether two measurements of a printer agree within what each says it knows, both ways.</summary>
    public bool AgreesWith(PrinterProfile other)
    {
        ArgumentNullException.ThrowIfNull(other);
        double allowed = Uncertainty + other.Uncertainty;
        return Math.Abs(Across - other.Across) <= allowed && Math.Abs(Down - other.Down) <= allowed;
    }

    /// <summary>How it was measured: "with a card", "from a scan".</summary>
    public string MethodWords => Method switch
    {
        PrinterMethod.Scan => "from a scan",
        PrinterMethod.Card => "with a card",
        PrinterMethod.Caliper => "with a caliper",
        _ => "with a ruler",
    };

    /// <summary>How it was measured and when: "with a card on 28 September".</summary>
    public string How => $"{MethodWords} on {Day(MeasuredOn)}";

    /// <summary>The two percentages, "99.2 by 99.4%", or one where they round the same.</summary>
    public string Percentages => Math.Round(Across * 1000) == Math.Round(Down * 1000)
        ? string.Create(CultureInfo.InvariantCulture, $"{Across * 100:0.0}%")
        : string.Create(CultureInfo.InvariantCulture, $"{Across * 100:0.0} by {Down * 100:0.0}%");

    /// <summary>
    /// The one line a photograph's result carries once this profile has corrected it, entry 273 section 5, with the day of the check (entry 291
    /// section 5.2) so a result says which correction it used: "Corrected for My printer, 99.2 by 99.4%, checked 28 September".
    /// </summary>
    public string Line => $"Corrected for {Name}, {Percentages}, checked {Day(MeasuredOn)}";

    /// <summary>This profile, with the day the person said the printer was calibrated, serviced or set differently.</summary>
    public PrinterProfile Changed(DateOnly on) => this with { ChangedOn = on };

    /// <summary>
    /// Why this check may no longer hold for sheets printed now, entry 291 section 5.2, or null where it still does: the printer was calibrated,
    /// serviced or set differently since, or the check is more than <see cref="StaleAfterDays"/> days old.
    /// </summary>
    public string? Stale(DateOnly today)
    {
        if (ChangedOn is { } changed && changed >= MeasuredOn)
        {
            return $"{Name} was calibrated or serviced on {Day(changed)}, after its check on {Day(MeasuredOn)}. Sheets printed before then are still corrected rightly; for sheets printed since, check it again on a sheet printed now.";
        }

        return today.DayNumber - MeasuredOn.DayNumber > StaleAfterDays
            ? $"{Name} was last checked on {Day(MeasuredOn)}, more than six months ago. A printer can drift; check it again on a sheet printed now."
            : null;
    }

    private static string Day(DateOnly day) => day.ToString("d MMMM", CultureInfo.InvariantCulture);

    /// <summary>The wizard's result, entry 272: "My printer prints at 99.2% across and 99.4% down (plus or minus 0.3%)".</summary>
    public string Result => string.Create(CultureInfo.InvariantCulture,
        $"{Name} prints at {Across * 100:0.0}% across and {Down * 100:0.0}% down (plus or minus {Uncertainty * 100:0.0}%)");

    /// <summary>The wizard's heading: whether the printer prints small, large or true, to within what the measurement can tell.</summary>
    public string Headline => Scale < 1 - Math.Max(Uncertainty, 0.001) ? $"{Name} prints a little small"
        : Scale > 1 + Math.Max(Uncertainty, 0.001) ? $"{Name} prints a little large"
        : $"{Name} prints at its true size";

    /// <summary>The offer a scan's result makes, entry 271 section 2.</summary>
    public static string Offer(double scale) => string.Create(CultureInfo.InvariantCulture,
        $"Your printer printed this sheet at {scale * 100:0.0} percent. Use this for photos of sheets from the same printer?");

    public JsonObject ToJson()
    {
        var json = new JsonObject
        {
            ["name"] = Name,
            ["across"] = Across,
            ["down"] = Down,
            ["method"] = Method.ToString().ToLowerInvariant(),
            ["measuredOn"] = MeasuredOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["uncertainty"] = Uncertainty,
        };
        if (ChangedOn is { } changed)
        {
            json["changedOn"] = changed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (Paper is { } paper)
        {
            json["paper"] = paper;
        }

        if (LabelSize is { } label)
        {
            json["labelSize"] = new JsonArray(label.Width, label.Height);
        }

        return json;
    }

    /// <summary>A profile read back, or null where anything in it is missing or beyond belief. Entry 271's one "scale" reads as both.</summary>
    public static PrinterProfile? FromJson(JsonNode? node)
    {
        try
        {
            if (node is not JsonObject o || (string?)o["name"] is not { } name || (double?)o["uncertainty"] is not { } uncertainty
                || ((double?)o["across"] ?? (double?)o["scale"]) is not { } across || ((double?)o["down"] ?? (double?)o["scale"]) is not { } down
                || !DateOnly.TryParseExact((string?)o["measuredOn"], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var on)
                || !Believable(across) || !Believable(down) || !(uncertainty >= 0)
                || !Enum.TryParse<PrinterMethod>((string?)o["method"], ignoreCase: true, out var method) || !Enum.IsDefined(method))
            {
                return null;
            }

            // Entry 291 section 5.2: a profile saved before a printer could be marked changed reads as never changed.
            DateOnly? changed = DateOnly.TryParseExact((string?)o["changedOn"], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var c) ? c : null;
            (int, int)? label = o["labelSize"] is JsonArray l && l.Count == 2 && (int?)l[0] is > 0 and var w && (int?)l[1] is > 0 and var h ? (w, h) : null;
            return new PrinterProfile(Named(name), across, down, method, on, uncertainty) { ChangedOn = changed, Paper = (string?)o["paper"], LabelSize = label };
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
