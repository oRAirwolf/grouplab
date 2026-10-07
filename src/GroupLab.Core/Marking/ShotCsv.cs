using System.Globalization;
using System.Text;
using GroupLab.Core.Imaging;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Marking;

/// <summary>A unit a column of imported coordinates can be in.</summary>
public enum CoordinateUnit
{
    Inch,
    Millimetre,
    Centimetre,
    Moa,
    Mil,
}

/// <summary>A CSV file as columns and rows of text, before anybody has said what the columns mean.</summary>
public sealed record CsvTable(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows);

/// <summary>
/// Shot coordinates as CSV, NOTES-FROM-PLANNING.md entry 169 section 8: out, one row per shot for spreadsheets and other tools, which read
/// CSV and not GroupLab's JSON; and in, from any program's export, through a mapping step that asks which column is across, which is up and
/// down, and what unit they are in. No program's format is named or assumed: the mapping covers them all.
/// </summary>
public static class ShotCsv
{
    /// <summary>
    /// Every shot in the group as a row: its label, its bull, and its offset from its own bull's aim point, right and up positive, in inches,
    /// and in MOA and mil at the distance shot when there is one. The header names the units and the distance, so the file explains itself
    /// without GroupLab. Entry 374 section 4: each row also names its sheet, its session (the picture it was marked on) and the distance, so
    /// rows from many sheets can be put together and still say where each came from.
    /// </summary>
    public static string Write(MarkingState state, string? sheet = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        var shots = state.Shots.Where(s => s.IsShot && !GroupAnalysis.OnSighter(state, s)).ToList();
        var offsets = GroupAnalysis.CompositeOffsets(state, shots);
        // Entry 376 section A6: each shot named by its bull, in bull order, lowest first.
        var named = ShotLabels.For(state);
        var labels = named.ToDictionary(l => l.ShotId, l => l.Name);
        var place = named.Select((l, k) => (l.ShotId, k)).ToDictionary(x => x.ShotId, x => x.k);
        var order = state.Bulls.Count == 0 ? [.. Enumerable.Range(0, shots.Count)] : Enumerable.Range(0, shots.Count).OrderBy(i => place.GetValueOrDefault(shots[i].Id, int.MaxValue)).ToList();
        double? distance = state.ShotDistanceInches;
        string at = distance is { } d ? string.Create(CultureInfo.InvariantCulture, $" at {d / 36:0.#} yd") : "";
        var text = new StringBuilder();
        text.Append(string.Join(",", new[]
        {
            "shot", "bull", "x right (in)", "y up (in)", $"x right (MOA{at})", $"y up (MOA{at})", $"x right (mil{at})", $"y up (mil{at})", "sheet", "session", "distance (yd)", "excluded",
        }.Select(Quote))).Append('\n');
        string sheetName = Quote(sheet ?? state.SheetLabel ?? "");
        string sessionName = Quote(MarkingFile.ImageName(state.ImagePath) is { } image ? Path.GetFileNameWithoutExtension(image) : "");
        string yards = distance is { } far ? (far / 36).ToString("0.#", CultureInfo.InvariantCulture) : "";
        foreach (int i in offsets.Count == shots.Count ? order : [])
        {
            var shot = shots[i];
            double x = offsets[i].X, y = -offsets[i].Y;
            string Angle(double inches, AngularUnit unit) => UnitSettings.AngleIn(inches, distance, unit) is { } a ? Signed(a, 3) : "";
            string bull = shot.Bull is { } b && state.Bulls.FirstOrDefault(x => x.Index == b) is { } aim ? aim.Label : "";
            text.Append(string.Join(",", new[]
            {
                Quote(labels.GetValueOrDefault(shot.Id) ?? "Shot " + (i + 1).ToString(CultureInfo.InvariantCulture)), Quote(bull),
                Signed(x, 4), Signed(y, 4),
                Angle(x, AngularUnit.Moa), Angle(y, AngularUnit.Moa), Angle(x, AngularUnit.Mrad), Angle(y, AngularUnit.Mrad),
                sheetName, sessionName, yards,
                shot.Exclusion is null ? "no" : "yes",
            })).Append('\n');
        }

        return text.ToString();
    }

    /// <summary>
    /// Entry 374 section 4: several CSV files read as one table, so shots from many sheets of any kind make one group (Unholy: "combine sheets
    /// without the set-of-three feature"). They must have the same columns, as GroupLab's own exports do; files that differ are refused by
    /// name rather than put together wrongly.
    /// </summary>
    public static (CsvTable? Pooled, string? Refusal) Pool(IReadOnlyList<(string Name, CsvTable Table)> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        if (files.Count == 0)
        {
            return (null, "No files were chosen.");
        }

        static string Columns(CsvTable t) => string.Join("\n", t.Headers.Select(h => h.Trim().ToLowerInvariant()));
        string first = Columns(files[0].Table);
        if (files.Skip(1).FirstOrDefault(f => Columns(f.Table) != first) is { Name: { } odd })
        {
            return (null, $"{odd} has different columns from {files[0].Name}, so the two cannot be put together. Import files with the same columns, such as GroupLab's own exports.");
        }

        return (new CsvTable(files[0].Table.Headers, [.. files.SelectMany(f => f.Table.Rows)]), null);
    }

    /// <summary>
    /// Reads CSV text into columns and rows: comma, semicolon or tab separated, whichever the header line uses most, with double quotes
    /// around a field that holds the separator. The first line is the header. Blank lines are skipped.
    /// </summary>
    public static CsvTable Read(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n').Where(l => l.Trim().Length > 0).ToList();
        if (lines.Count == 0)
        {
            throw new FormatException("The file is empty.");
        }

        char separator = new[] { ',', ';', '\t' }.OrderByDescending(c => lines[0].Count(x => x == c)).First();
        var headers = Split(lines[0].TrimStart((char)0xFEFF), separator);
        var rows = lines.Skip(1).Select(l => (IReadOnlyList<string>)Split(l, separator)).ToList();
        return new CsvTable(headers, rows);
    }

    /// <summary>
    /// The column that most likely holds one axis, by its header: "x" or "across" or "windage" for across, "y" or "up" or "elevation" for up
    /// and down. A guess to start the mapping step from, never a decision.
    /// </summary>
    public static int? Guess(CsvTable table, bool across)
    {
        ArgumentNullException.ThrowIfNull(table);
        string[] words = across ? ["x", "across", "windage", "horizontal", "h"] : ["y", "up", "elevation", "vertical", "v"];
        for (int i = 0; i < table.Headers.Count; i++)
        {
            var tokens = table.Headers[i].ToLowerInvariant().Split([' ', '_', '(', ')', '-', '.', '/'], StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Any(t => words.Contains(t)))
            {
                return i;
            }
        }

        return null;
    }

    /// <summary>
    /// The shots of a mapped table, as offsets from the aim point in inches on the screen's axes, right and down positive. Rows whose two
    /// columns are not both numbers are skipped and counted, so a total line or a note at the bottom of an export does not stop the import.
    /// An angle needs the distance it was measured at.
    /// </summary>
    public static (IReadOnlyList<PointD> Offsets, int Skipped) Shots(CsvTable table, int xColumn, int yColumn, CoordinateUnit unit, bool yUpIsPositive, double? distanceInches)
    {
        ArgumentNullException.ThrowIfNull(table);
        if (unit is CoordinateUnit.Moa or CoordinateUnit.Mil && distanceInches is not > 0)
        {
            throw new ArgumentException("Coordinates in mil or MOA need the distance they were shot at.", nameof(distanceInches));
        }

        double Inches(double value) => unit switch
        {
            CoordinateUnit.Millimetre => value / 25.4,
            CoordinateUnit.Centimetre => value / 2.54,
            CoordinateUnit.Moa => Angular.FromAngle(value, distanceInches!.Value, 1, AngularUnit.Moa),
            CoordinateUnit.Mil => Angular.FromAngle(value, distanceInches!.Value, 1, AngularUnit.Mrad),
            _ => value,
        };

        var offsets = new List<PointD>();
        int skipped = 0;
        foreach (var row in table.Rows)
        {
            if (Number(row, xColumn) is { } x && Number(row, yColumn) is { } y)
            {
                offsets.Add(new PointD(Inches(x), yUpIsPositive ? -Inches(y) : Inches(y)));
            }
            else
            {
                skipped++;
            }
        }

        return (offsets, skipped);
    }

    /// <summary>The image scale an imported marking is built on: a thousand pixels to the inch, around an aim point well inside the image.</summary>
    public const double PixelsPerInch = 1000;

    public static readonly PointD Aim = new(20000, 20000);

    /// <summary>
    /// A marking made from imported offsets: no image, a scale of <see cref="PixelsPerInch"/>, the point of aim at <see cref="Aim"/> and each
    /// shot placed by hand at its offset, so every figure GroupLab computes works on it as on a marked sheet. Numbers measured from the
    /// group's own center (entry 278 section 2) say nothing about where the aim was, so then no point of aim is set and the result shows the
    /// group's size and shape without an offset from aim.
    /// </summary>
    public static MarkingState Marking(IReadOnlyList<PointD> offsets, double? distanceInches, bool fromGroupCentre = false)
    {
        ArgumentNullException.ThrowIfNull(offsets);
        var session = new MarkingSession();
        session.Load(MarkingState.Empty with { ShotDistanceInches = distanceInches });
        session.SetScale(new LengthReference(Aim, new PointD(Aim.X + PixelsPerInch, Aim.Y), 1));
        if (!fromGroupCentre)
        {
            session.SetPointOfAim(Aim);
        }

        foreach (var o in offsets)
        {
            session.AddShot(new PointD(Aim.X + (o.X * PixelsPerInch), Aim.Y + (o.Y * PixelsPerInch)));
        }

        return session.State;
    }

    private static double? Number(IReadOnlyList<string> row, int column) =>
        column < row.Count && double.TryParse(row[column].Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && double.IsFinite(v) ? v : null;

    private static string Signed(double value, int decimals) => value.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    private static string Quote(string field) => field.IndexOfAny([',', '"', '\n', ';']) >= 0 ? "\"" + field.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"" : field;

    private static List<string> Split(string line, char separator)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    quoted = false;
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                quoted = true;
            }
            else if (c == separator)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString().Trim());
        return fields;
    }
}
