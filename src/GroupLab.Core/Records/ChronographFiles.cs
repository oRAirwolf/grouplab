using System.Globalization;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Records;

/// <summary>Which reader read a chronograph file.</summary>
public enum ChronographFormat
{
    /// <summary>Any CSV with a column of velocities, DESIGN.md section 17's "most users arrive from a spreadsheet".</summary>
    Generic,

    /// <summary>LabRadar's series report, semicolon separated, with its units in a line of their own.</summary>
    LabRadar,

    /// <summary>Garmin Xero's export from the ShotView app, a speed column whose header names the unit.</summary>
    GarminXero,
}

/// <summary>
/// What a chronograph file gave: the reader, the velocities in ft/s in the order recorded, a sentence saying what was read and how, whether
/// the reader is Experimental, and for a generic file the columns there were and the one taken, so a person can choose another.
/// </summary>
public sealed record ChronographImport(ChronographFormat Format, IReadOnlyList<double> VelocitiesFps, string Said, bool Experimental, IReadOnlyList<string> Columns, int? Column);

/// <summary>
/// Chronograph files, NOTES-FROM-PLANNING.md entry 331 section 2 and DESIGN.md section 17: one import interface whose every reader ends in
/// the same list of numbers the hand-entry box makes (<see cref="Chronograph.Read"/>), which then goes through the same reconciliation with
/// the shots. LabRadar's and Garmin Xero's readers are built from their published layouts with no real file to check them against, so they
/// are Experimental and say so until one passes. Nothing here reads a stranger's upload.
/// </summary>
public static class ChronographFiles
{
    /// <summary>The words the Experimental readers add to what they say.</summary>
    public const string ExperimentalWords = "Experimental: read from the published layout, not yet checked against a real file.";

    private const double FeetPerMetre = 1 / 0.3048;

    /// <summary>
    /// Reads a file's text. <paramref name="column"/> and <paramref name="metres"/> are a person's choices for a generic file, overriding the
    /// guess; LabRadar and Xero files say their own column and unit.
    /// </summary>
    public static ChronographImport Read(string text, int? column = null, bool? metres = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n')
            .Select(l => l.TrimStart((char)0xFEFF)).Where(l => l.Trim().Length > 0).ToList();
        if (lines.Count == 0)
        {
            throw new FormatException("The file is empty.");
        }

        return LabRadar(lines) ?? Xero(lines) ?? Generic(text, column, metres);
    }

    /// <summary>
    /// LabRadar's series report: semicolon separated, a line "Units velocity;fps" (or m/s), then a table whose header row starts "Shot ID" and
    /// has a "V0" column, the muzzle velocity, one row per shot until the table ends. A decimal comma is read as a point.
    /// </summary>
    private static ChronographImport? LabRadar(List<string> lines)
    {
        static string[] Cells(string line) => [.. line.Split(';').Select(c => c.Trim().Trim('"'))];
        int header = lines.FindIndex(l => Cells(l) is [var first, ..] && first.Equals("Shot ID", StringComparison.OrdinalIgnoreCase));
        if (header < 0 || !lines.Any(l => l.Contains("Device ID", StringComparison.OrdinalIgnoreCase) || l.Contains("Units velocity", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var names = Cells(lines[header]);
        int v0 = Array.FindIndex(names, n => n.Equals("V0", StringComparison.OrdinalIgnoreCase));
        if (v0 < 0)
        {
            throw new FormatException("This looks like a LabRadar report, but its table has no V0 column, the muzzle velocity.");
        }

        string unit = lines.Select(Cells).FirstOrDefault(c => c is [var n, _, ..] && n.Equals("Units velocity", StringComparison.OrdinalIgnoreCase))?[1] ?? "fps";
        bool inMetres = unit.Replace(" ", "", StringComparison.Ordinal).Equals("m/s", StringComparison.OrdinalIgnoreCase);
        var velocities = new List<double>();
        foreach (var cells in lines.Skip(header + 1).Select(Cells))
        {
            if (cells.Length <= v0 || !int.TryParse(cells[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                break;
            }

            if (Number(cells[v0]) is { } v)
            {
                velocities.Add(inMetres ? v * FeetPerMetre : v);
            }
        }

        return new ChronographImport(ChronographFormat.LabRadar, velocities,
            $"A LabRadar report: {velocities.Count} shots, the muzzle velocity V0, in {(inMetres ? "m/s" : "ft/s")}. {ExperimentalWords}", true, names, v0);
    }

    /// <summary>
    /// Garmin Xero's export from ShotView: a header row with a column "SPEED (FPS)" or "SPEED (M/S)" after a shot number column, one row per
    /// shot numbered from 1, then the session's averages, which are not shots and are left out.
    /// </summary>
    private static ChronographImport? Xero(List<string> lines)
    {
        for (int header = 0; header < lines.Count; header++)
        {
            var table = ShotCsv.Read(lines[header] + "\n");
            int speed = table.Headers.ToList().FindIndex(h => h.Trim().StartsWith("SPEED (", StringComparison.OrdinalIgnoreCase));
            if (speed < 0)
            {
                continue;
            }

            bool inMetres = table.Headers[speed].Contains("M/S", StringComparison.OrdinalIgnoreCase);
            var rows = ShotCsv.Read(string.Join("\n", lines.Skip(header))).Rows;
            var velocities = new List<double>();
            foreach (var row in rows)
            {
                if (row.Count <= speed || !int.TryParse(row[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    break;
                }

                if (Number(row[speed]) is { } v)
                {
                    velocities.Add(inMetres ? v * FeetPerMetre : v);
                }
            }

            return new ChronographImport(ChronographFormat.GarminXero, velocities,
                $"A Garmin Xero export: {velocities.Count} shots, in {(inMetres ? "m/s" : "ft/s")}. {ExperimentalWords}", true, table.Headers, speed);
        }

        return null;
    }

    /// <summary>
    /// Any CSV: the column a person chose, or the one whose header names a velocity, or the first that holds numbers a chronograph could read;
    /// in m/s where the person says so or the header does, in ft/s otherwise. A file with no header row is read from its first line.
    /// </summary>
    private static ChronographImport Generic(string text, int? column, bool? metres)
    {
        var table = ShotCsv.Read(text);
        bool noHeader = table.Headers.Count > 0 && table.Headers.All(h => Number(h) is not null);
        var rows = noHeader ? [table.Headers, .. table.Rows] : table.Rows;
        var names = noHeader ? [.. table.Headers.Select((_, i) => $"column {i + 1}")] : table.Headers.Select((h, i) => string.IsNullOrWhiteSpace(h) ? $"column {i + 1}" : h.Trim()).ToList();
        string[] words = ["velocity", "speed", "fps", "v0", "vel", "mps", "m/s", "ft/s"];
        int? named = Enumerable.Range(0, names.Count).Cast<int?>().FirstOrDefault(i => words.Any(w => names[i!.Value].Contains(w, StringComparison.OrdinalIgnoreCase)));
        int? numeric = Enumerable.Range(0, names.Count).Cast<int?>().FirstOrDefault(i =>
        {
            var values = rows.Where(r => r.Count > i).Select(r => Number(r[i!.Value])).OfType<double>().ToList();
            return values.Count >= 2 && values.All(v => v is >= 100 and <= 5000);
        });
        int? chosen = column is { } c && c >= 0 && c < names.Count ? c : named ?? numeric;
        if (chosen is not { } at)
        {
            return new ChronographImport(ChronographFormat.Generic, [], "No column of velocities was found; choose the column.", false, names, null);
        }

        string header = names[at];
        bool inMetres = metres ?? (header.Contains("m/s", StringComparison.OrdinalIgnoreCase) || header.Contains("mps", StringComparison.OrdinalIgnoreCase));
        var velocities = rows.Where(r => r.Count > at).Select(r => Number(r[at])).OfType<double>().Select(v => inMetres ? v * FeetPerMetre : v).ToList();
        int skipped = rows.Count - velocities.Count;
        string how = column is not null ? "the column you chose" : named is not null ? "named by its header" : "the first column of numbers a chronograph could read";
        string units = metres is not null ? "as you chose" : inMetres ? "as its header says" : "taken as ft/s; choose m/s if they are";
        return new ChronographImport(ChronographFormat.Generic, velocities,
            $"{velocities.Count} velocities from \"{header}\", {how}, in {(inMetres ? "m/s" : "ft/s")}, {units}{(skipped > 0 ? $"; {skipped} rows without a number were left out" : "")}.",
            false, names, at);
    }

    /// <summary>A number written with a decimal point or a decimal comma, or null.</summary>
    private static double? Number(string text)
    {
        string t = text.Trim().Trim('"');
        if (t.Length == 0)
        {
            return null;
        }

        if (!t.Contains('.', StringComparison.Ordinal) && t.Count(ch => ch == ',') == 1)
        {
            t = t.Replace(',', '.');
        }

        return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && double.IsFinite(v) ? v : null;
    }
}
