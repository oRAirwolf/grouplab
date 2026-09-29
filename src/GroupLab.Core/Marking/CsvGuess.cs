using System.Globalization;

namespace GroupLab.Core.Marking;

/// <summary>One line of the import card: a value GroupLab guessed, or null when it could not, and the words that say why.</summary>
public sealed record GuessLine<T>(T? Value, string Words)
    where T : struct
{
    /// <summary>Whether GroupLab could guess this line; when it could not, the line says so and asks.</summary>
    public bool Guessed => Value is not null;
}

/// <summary>
/// What GroupLab makes of a CSV file of shots before anybody has said anything, NOTES-FROM-PLANNING.md entry 278 section 2 (CSV B): which
/// column is across, which is up and down, the unit, which way is up, and whether the numbers are measured from the point of aim or from
/// the group's own center. Every line is a guess to be checked, with its reason in plain words, and a line it cannot guess says so.
/// </summary>
public sealed record CsvGuess(
    GuessLine<int> Across,
    GuessLine<int> UpDown,
    GuessLine<CoordinateUnit> Unit,
    GuessLine<bool> UpIsPositive,
    GuessLine<bool> FromGroupCentre)
{
    /// <summary>Whether every line has a value, so the file can be imported as it stands.</summary>
    public bool Complete => Across.Guessed && UpDown.Guessed && Unit.Guessed && UpIsPositive.Guessed && FromGroupCentre.Guessed;

    /// <summary>The lines in the order the card shows them.</summary>
    public IReadOnlyList<string> Lines => [Across.Words, UpDown.Words, Unit.Words, UpIsPositive.Words, FromGroupCentre.Words];

    /// <summary>A plain name for a unit, as the card and the import dialog say it.</summary>
    public static string Name(CoordinateUnit unit) => unit switch
    {
        CoordinateUnit.Millimetre => "millimeters",
        CoordinateUnit.Centimetre => "centimeters",
        CoordinateUnit.Moa => "MOA",
        CoordinateUnit.Mil => "mil",
        _ => "inches",
    };

    private static readonly (string[] Words, CoordinateUnit Unit)[] UnitWords =
    [
        (["in", "inch", "inches", "\""], CoordinateUnit.Inch),
        (["mm", "millimeter", "millimeters", "millimetre", "millimetres"], CoordinateUnit.Millimetre), // British on purpose: what a file may say
        (["cm", "centimeter", "centimeters", "centimetre", "centimetres"], CoordinateUnit.Centimetre), // British on purpose: what a file may say
        (["moa"], CoordinateUnit.Moa),
        (["mil", "mils", "mrad", "milliradian", "milliradians"], CoordinateUnit.Mil),
    ];

    private static readonly string[] DownWords = ["down", "drop", "below"];
    private static readonly string[] UpWords = ["up", "elevation", "height", "above", "rise"];
    private static readonly string[] CentreWords = ["center", "centre", "mpi", "poi", "mean", "centroid"]; // British on purpose: what a file may say
    private static readonly string[] AimWords = ["aim", "poa", "aimpoint"];

    /// <summary>
    /// The largest number a column may hold before inches, centimeters, MOA and mil all stop being likely: a shot more than twenty of any of
    /// them from where it was measured is off any target these files come from, while twenty millimeters is under an inch.
    /// </summary>
    public const double MillimetreSized = 20;

    /// <summary>
    /// How close to zero the numbers must average, as a share of the group's widest spread, to look measured from the group's own center.
    /// Measured that way they average to zero but for rounding; measured from the aim point they average to wherever the group landed.
    /// </summary>
    public const double CentredShare = 0.01;

    /// <summary>Every line's guess for a table, with the file's name as one more place a unit or an origin may be written.</summary>
    public static CsvGuess For(CsvTable table, string? fileName = null)
    {
        ArgumentNullException.ThrowIfNull(table);
        var across = Column(table, across: true, other: null);
        var upDown = Column(table, across: false, other: across.Value);
        if (across.Value is { } a && upDown.Value is { } u && a == u)
        {
            upDown = new GuessLine<int>(null, "Up and down: no other column of numbers. Choose it.");
        }

        var both = new[] { across.Value, upDown.Value }.OfType<int>().ToList();
        var name = Tokens(Path.GetFileNameWithoutExtension(fileName ?? ""));
        var numbers = both.Count == 2 ? Pairs(table, both[0], both[1]) : [];
        return new CsvGuess(across, upDown, UnitOf(table, both, name, numbers), Direction(table, upDown.Value), Origin(table, name, numbers));
    }

    private static GuessLine<int> Column(CsvTable table, bool across, int? other)
    {
        string axis = across ? "Across" : "Up and down";
        if (ShotCsv.Guess(table, across) is { } named)
        {
            return new GuessLine<int>(named, $"{axis}: the column called \"{Heading(table, named)}\".");
        }

        // No heading names the axis: the columns of numbers, left to right, leaving out one that only counts the shots 1, 2, 3.
        var numeric = Enumerable.Range(0, table.Headers.Count).Where(i => i != other && IsMeasurements(table, i)).ToList();
        bool enough = across ? numeric.Count >= 2 : other is not null && numeric.Count >= 1;
        if (enough)
        {
            int c = numeric[0];
            return new GuessLine<int>(c, $"{axis}: the {(across ? "first" : "next")} column of numbers, \"{Heading(table, c)}\"; no heading says which way it runs. Check it.");
        }

        return new GuessLine<int>(null, $"{axis}: no heading says which column it is. Choose it.");
    }

    private static GuessLine<CoordinateUnit> UnitOf(CsvTable table, List<int> columns, IReadOnlyList<string> fileName, IReadOnlyList<(double X, double Y)> numbers)
    {
        foreach (int c in columns)
        {
            if (Named(Tokens(table.Headers[c])) is { } unit)
            {
                return new GuessLine<CoordinateUnit>(unit, $"Unit: {Name(unit)}, from the heading \"{Heading(table, c)}\".");
            }
        }

        if (Named(fileName) is { } fromName)
        {
            return new GuessLine<CoordinateUnit>(fromName, $"Unit: {Name(fromName)}, from the file's name.");
        }

        double largest = numbers.Count == 0 ? 0 : numbers.Max(p => Math.Max(Math.Abs(p.X), Math.Abs(p.Y)));
        if (largest > MillimetreSized)
        {
            return new GuessLine<CoordinateUnit>(CoordinateUnit.Millimetre, string.Create(CultureInfo.InvariantCulture,
                $"Unit: millimeters, because nothing names it and a number as large as {largest:0.#} would be far off the target in any other unit. Check it."));
        }

        return new GuessLine<CoordinateUnit>(null, "Unit: nothing in the file says it, and the numbers could be inches, centimeters, mil or MOA. Choose it.");
    }

    private static GuessLine<bool> Direction(CsvTable table, int? upDown)
    {
        var words = upDown is { } u ? Tokens(table.Headers[u]) : [];
        if (words.Any(DownWords.Contains))
        {
            return new GuessLine<bool>(false, "Up and down: the heading says down, so a larger number is lower on the target.");
        }

        if (words.Any(UpWords.Contains))
        {
            return new GuessLine<bool>(true, "Up and down: the heading says up, so a larger number is higher on the target.");
        }

        return new GuessLine<bool>(true, "Up and down: nothing says which way, so a larger number is taken as higher, the usual way for a target. Change it if the group comes out upside down.");
    }

    private static GuessLine<bool> Origin(CsvTable table, IReadOnlyList<string> fileName, IReadOnlyList<(double X, double Y)> numbers)
    {
        var words = table.Headers.SelectMany(Tokens).Concat(fileName).ToList();
        if (words.Any(CentreWords.Contains))
        {
            return new GuessLine<bool>(true, "Measured from: the group's own center, as the file says.");
        }

        if (words.Any(AimWords.Contains))
        {
            return new GuessLine<bool>(false, "Measured from: the point of aim, as the file says.");
        }

        if (numbers.Count < 3)
        {
            return new GuessLine<bool>(null, "Measured from: too few shots to tell whether from the point of aim or the group's center. Choose it.");
        }

        double spread = Math.Max(numbers.Max(p => p.X) - numbers.Min(p => p.X), numbers.Max(p => p.Y) - numbers.Min(p => p.Y));
        double meanX = numbers.Average(p => p.X), meanY = numbers.Average(p => p.Y);
        return spread > 0 && Math.Abs(meanX) <= CentredShare * spread && Math.Abs(meanY) <= CentredShare * spread
            ? new GuessLine<bool>(true, "Measured from: the group's own center, because the numbers average out at zero, as they do measured that way. Check it.")
            : new GuessLine<bool>(false, "Measured from: the point of aim, because the numbers do not average out at zero. Check it.");
    }

    private static CoordinateUnit? Named(IReadOnlyList<string> words) =>
        UnitWords.Where(u => words.Any(u.Words.Contains)).Select(u => (CoordinateUnit?)u.Unit).FirstOrDefault();

    private static string Heading(CsvTable table, int column) =>
        string.IsNullOrWhiteSpace(table.Headers[column]) ? $"column {column + 1}" : table.Headers[column];

    private static List<string> Tokens(string text) =>
        [.. text.ToLowerInvariant().Replace("\"", " \" ", StringComparison.Ordinal)
            .Split([' ', '_', '(', ')', '[', ']', '-', '.', '/', ',', ':'], StringSplitOptions.RemoveEmptyEntries)];

    /// <summary>Whether a column holds measurements: most rows a number, and not simply the shots counted 1, 2, 3.</summary>
    private static bool IsMeasurements(CsvTable table, int column)
    {
        var values = table.Rows.Select(r => Number(r, column)).ToList();
        var present = values.OfType<double>().ToList();
        if (present.Count == 0 || present.Count * 2 < table.Rows.Count)
        {
            return false;
        }

        bool counting = present.Select((v, i) => v == i + 1).All(x => x) || present.Select((v, i) => v == i).All(x => x);
        return !counting;
    }

    private static List<(double X, double Y)> Pairs(CsvTable table, int x, int y) =>
        [.. table.Rows.Select(r => (X: Number(r, x), Y: Number(r, y))).Where(p => p.X is not null && p.Y is not null).Select(p => (p.X!.Value, p.Y!.Value))];

    private static double? Number(IReadOnlyList<string> row, int column) =>
        column < row.Count && double.TryParse(row[column].Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && double.IsFinite(v) ? v : null;
}
