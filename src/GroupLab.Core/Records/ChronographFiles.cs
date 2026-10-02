using System.Globalization;
using System.Text;
using ExcelDataReader;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Records;

/// <summary>Which reader read a chronograph file.</summary>
public enum ChronographFormat
{
    /// <summary>Any CSV with a column of velocities, DESIGN.md section 17's "most users arrive from a spreadsheet".</summary>
    Generic,

    /// <summary>LabRadar's series report, semicolon separated, with its units in a line of their own.</summary>
    LabRadar,

    /// <summary>Garmin Xero's export from ShotView: a CSV, or an Excel workbook with one sheet per string (entry 334's format B).</summary>
    GarminXero,

    /// <summary>
    /// The BulletSeeker chronograph's Excel export, shots across the columns with the radar's raw samples below them (entry 334's format A,
    /// named in entry 339). Not a Garmin format: Alan had a BulletSeeker for a short time before his Xero C1 and C2. Experimental.
    /// </summary>
    BulletSeeker,
}

/// <summary>One shot as the chronograph numbered it, with what the person noted about it where the file carries that.</summary>
/// <param name="LeftOutByChronograph">The Xero shows "--" for its difference from the average: it left the shot out of its own figures.</param>
/// <param name="Time">The time of day the chronograph recorded the shot, where it times them (the Xero does); entry 342's proposal reads pauses from it.</param>
public sealed record ChronographShot(int Number, double Fps, bool CleanBore = false, bool ColdBore = false, string? Note = null, bool LeftOutByChronograph = false,
    TimeSpan? Time = null);

/// <summary>
/// The air and the projectile a file states, offered to the person and used only when accepted (entry 329's conditions); never a location,
/// which no reader here reads.
/// </summary>
public sealed record ChronographConditions(double? ProjectileGrains, double? TemperatureF, double? PressureInHg, double? HumidityPct);

/// <summary>
/// What a chronograph file gave for one string: the reader, the velocities in ft/s in the order recorded, a sentence saying what was read
/// and how, whether the reader is Experimental, and for a generic file the columns there were and the one taken, so a person can choose
/// another. A file that numbers its shots and states its own figures adds the shots, the string's name, the conditions it states, and
/// <see cref="Disagrees"/> where the shots do not give its own average, SD or spread.
/// </summary>
public sealed record ChronographImport(ChronographFormat Format, IReadOnlyList<double> VelocitiesFps, string Said, bool Experimental, IReadOnlyList<string> Columns, int? Column)
{
    public string? Name { get; init; }

    public IReadOnlyList<ChronographShot> Shots { get; init; } = [];

    /// <summary>Whether the file gives its speeds in m/s; the readings are held in ft/s either way, and shown in the file's own unit (entry 351).</summary>
    public bool InMetres { get; init; }

    public ChronographConditions? Conditions { get; init; }

    /// <summary>Where the file's own average, SD or spread differs from the shots' by more than its rounding, said; null where they agree.</summary>
    public string? Disagrees { get; init; }
}

/// <summary>
/// Chronograph files, NOTES-FROM-PLANNING.md entries 331 section 2 and 334, and DESIGN.md section 17: one import interface whose every reader
/// ends in the same list of numbers the hand-entry box makes (<see cref="Chronograph.Read"/>), which then goes through the same reconciliation
/// with the shots. The Garmin Xero reader was proven on Alan's own exports from May 2024 on; LabRadar's is built from its published layout
/// with no real file, and BulletSeeker's from Alan's own files, so those two are Experimental and say so.
/// <para>
/// <b>Location.</b> Older exports carry where the person shot: a place name, a latitude and a longitude. No reader here reads those rows, or
/// any row it does not name, so a location never enters GroupLab, as GPS in a photograph never does.
/// </para>
/// </summary>
public static class ChronographFiles
{
    /// <summary>The words the Experimental readers add to what they say.</summary>
    public const string ExperimentalWords = "Experimental: read from the published layout, not yet checked against a real file.";

    /// <summary>The words the BulletSeeker reader adds: it is read from Alan's own files and stays Experimental.</summary>
    public const string BulletSeekerWords = "Experimental: a BulletSeeker export.";

    private const double FeetPerMetre = 1 / 0.3048;

    /// <summary>The file types the import offers.</summary>
    public static IReadOnlyList<string> Extensions { get; } = [".csv", ".txt", ".xls", ".xlsx", ".xlsm"];

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 352 item 3, every file a person gives GroupLab is untrusted: the largest chronograph file read, 10 MB,
    /// about seventy times the largest of Alan's 387 exports (135 KB). A larger one is refused before any more of it is read.
    /// </summary>
    public const int MostBytes = 10 * 1024 * 1024;

    /// <summary>What a workbook may unpack to, all its parts together; more is a file made to fill memory, not an export.</summary>
    public const long MostUnpackedBytes = 64L * 1024 * 1024;

    /// <summary>The most parts a workbook may hold.</summary>
    public const int MostParts = 10_000;

    /// <summary>The most sheets, and cells over all of them, a workbook may give: far beyond any export, and a bound on time and memory.</summary>
    public const int MostSheets = 1_000, MostCells = 4_000_000;

    /// <summary>What every refusal of a file too large says.</summary>
    public const string TooLargeWords = "It is larger than any chronograph export (more than 10 MB), so GroupLab did not read it.";

    /// <summary>What a workbook that cannot be read says.</summary>
    public const string NotAWorkbookWords = "It is not a workbook GroupLab can read: it may be damaged, or another kind of file with a spreadsheet's name.";

    /// <summary>A file's bytes, read to <see cref="MostBytes"/> at most; a <see cref="FormatException"/> saying so where it is larger.</summary>
    public static byte[] ReadBounded(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var copy = new MemoryStream();
        var buffer = new byte[81920];
        int n;
        while ((n = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (copy.Length + n > MostBytes)
            {
                throw new FormatException(TooLargeWords);
            }

            copy.Write(buffer, 0, n);
        }

        return copy.ToArray();
    }

    /// <summary>The same, for a stream a file picker gives, read without holding the screen.</summary>
    public static async Task<byte[]> ReadBoundedAsync(Stream stream, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var copy = new MemoryStream();
        var buffer = new byte[81920];
        int n;
        while ((n = await stream.ReadAsync(buffer, token).ConfigureAwait(false)) > 0)
        {
            if (copy.Length + n > MostBytes)
            {
                throw new FormatException(TooLargeWords);
            }

            copy.Write(buffer, 0, n);
        }

        return copy.ToArray();
    }

    /// <summary>
    /// A text file's words: by its byte order mark where it has one; UTF-16 without one where every other byte is zero; UTF-8, or where its
    /// bytes are not UTF-8, Latin-1, which reads every byte. Anything else with zero bytes in it is not text, and is refused in words.
    /// </summary>
    public static string Text(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length > MostBytes)
        {
            throw new FormatException(TooLargeWords);
        }

        ReadOnlySpan<byte> b = bytes;
        if (b.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]))
        {
            return new UTF8Encoding(false, false).GetString(b[3..]);
        }

        if (b.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xFE]) || b.StartsWith((ReadOnlySpan<byte>)[0xFE, 0xFF]))
        {
            return (b[0] == 0xFF ? Encoding.Unicode : Encoding.BigEndianUnicode).GetString(b.Slice(2, (b.Length - 2) & ~1));
        }

        var head = b[..Math.Min(b.Length, 4096)];
        if (head.Contains((byte)0))
        {
            int evenZeros = 0, oddZeros = 0;
            for (int i = 0; i < head.Length; i++)
            {
                if (head[i] == 0 && i % 2 == 0)
                {
                    evenZeros++;
                }
                else if (head[i] == 0)
                {
                    oddZeros++;
                }
            }

            int half = head.Length / 2;
            if (oddZeros >= 0.4 * half && evenZeros < 0.1 * half)
            {
                return Encoding.Unicode.GetString(b[..(b.Length & ~1)]);
            }

            if (evenZeros >= 0.4 * half && oddZeros < 0.1 * half)
            {
                return Encoding.BigEndianUnicode.GetString(b[..(b.Length & ~1)]);
            }

            throw new FormatException("It is not a text file: it may be a workbook or a picture with a text file's name.");
        }

        try
        {
            return new UTF8Encoding(false, true).GetString(b);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(b);
        }
    }

    /// <summary>A text file read to <see cref="MostBytes"/> at most and decoded as <see cref="Text"/> does.</summary>
    public static string ReadText(Stream stream) => Text(ReadBounded(stream));

    /// <summary>
    /// Every string in a file, by its name: a text file through <see cref="Read"/>, an Excel workbook sheet by sheet, each sheet a string
    /// (a Xero workbook of the strings selected in ShotView, one sheet per string). A sheet that is neither format is passed over and counted in <paramref name="passedOver"/>.
    /// </summary>
    public static IReadOnlyList<ChronographImport> ReadFile(Stream stream, string fileName, out int passedOver)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(fileName);
        passedOver = 0;
        byte[] bytes = ReadBounded(stream);
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is ".csv" or ".txt")
        {
            return [Read(Text(bytes))];
        }

        // Entry 352 item 3: a workbook is a zip (xlsx, xlsm) or a compound file (xls). A zip is unpacked once, counting and keeping nothing,
        // before the workbook reader sees it, so one that unpacks to far more than it holds is refused before anything holds it.
        if (bytes.AsSpan().StartsWith("PK"u8))
        {
            Unpacks(bytes);
        }

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var strings = new List<ChronographImport>();
        int sheets = 0, cells = 0;
        try
        {
            using var reader = ExcelReaderFactory.CreateReader(new MemoryStream(bytes, writable: false));
            do
            {
                if (++sheets > MostSheets)
                {
                    throw new FormatException("It holds more sheets than any chronograph export, so GroupLab did not read it.");
                }

                var rows = new List<string[]>();
                while (reader.Read())
                {
                    cells += Math.Max(1, reader.FieldCount);
                    if (cells > MostCells)
                    {
                        throw new FormatException("It holds more cells than any chronograph export, so GroupLab did not read it.");
                    }

                    rows.Add([.. Enumerable.Range(0, reader.FieldCount).Select(i => Cell(reader.GetValue(i)))]);
                }

                if ((XeroTable(rows) ?? BulletSeekerTable(rows)) is { } read)
                {
                    strings.Add(read);
                }
                else
                {
                    passedOver++;
                }
            }
            while (reader.NextResult());
        }
        catch (FormatException)
        {
            throw;
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            // The workbook reader's own errors name its insides; a person is told what it means for them.
            throw new FormatException(NotAWorkbookWords, e);
        }

        return strings;
    }

    /// <summary>Every part of a zip unpacked and counted, nothing kept; a <see cref="FormatException"/> where it is damaged or unpacks to too much.</summary>
    private static void Unpacks(byte[] bytes)
    {
        try
        {
            using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(bytes, writable: false), System.IO.Compression.ZipArchiveMode.Read);
            if (zip.Entries.Count > MostParts)
            {
                throw new FormatException("It holds more parts than any workbook, so GroupLab did not read it.");
            }

            long total = 0;
            var buffer = new byte[81920];
            foreach (var entry in zip.Entries)
            {
                using var part = entry.Open();
                int n;
                while ((n = part.Read(buffer, 0, buffer.Length)) > 0)
                {
                    total += n;
                    if (total > MostUnpackedBytes)
                    {
                        throw new FormatException("It unpacks to far more than any chronograph export, as a file made to fill a computer's memory does, so GroupLab did not read it.");
                    }
                }
            }
        }
        catch (FormatException)
        {
            throw;
        }
        catch (Exception e) when (e is InvalidDataException or IOException or NotSupportedException or ArgumentException or InvalidOperationException)
        {
            throw new FormatException(NotAWorkbookWords, e);
        }
    }

    /// <summary>
    /// Reads a text file. <paramref name="column"/> and <paramref name="metres"/> are a person's choices for a generic file, overriding the
    /// guess; LabRadar and Xero files say their own column and unit.
    /// </summary>
    public static ChronographImport Read(string text, int? column = null, bool? metres = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length > MostBytes)
        {
            throw new FormatException(TooLargeWords);
        }

        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n')
            .Select(l => l.TrimStart((char)0xFEFF)).Where(l => l.Trim().Length > 0).ToList();
        if (lines.Count == 0)
        {
            throw new FormatException("The file is empty.");
        }

        return LabRadar(lines) ?? XeroTable([.. lines.Select(l => ShotCsv.Read(l + "\n").Headers.ToArray())]) ?? Generic(text, column, metres);
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
        var shots = new List<ChronographShot>();
        foreach (var cells in lines.Skip(header + 1).Select(Cells))
        {
            if (cells.Length <= v0 || !int.TryParse(cells[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
            {
                break;
            }

            if (Number(cells[v0]) is { } v)
            {
                shots.Add(new ChronographShot(number, inMetres ? v * FeetPerMetre : v));
            }
        }

        return new ChronographImport(ChronographFormat.LabRadar, [.. shots.Select(s => s.Fps)],
            $"A LabRadar report: {shots.Count} shots, the muzzle velocity V0, in {(inMetres ? "m/s" : "ft/s")}. {ExperimentalWords}", true, names, v0)
        { Shots = shots, InMetres = inMetres };
    }

    /// <summary>
    /// Garmin Xero's export, from a CSV's lines or one sheet of a workbook: the string's name in the row above a header whose first cell is
    /// "#" and which has a "Speed (FPS)" or "Speed (M/S)" column, then one row per shot by its number (a gap is a deleted shot, and the
    /// numbering is kept), then a footer of the string's own figures, which are checked against the shots and never read as shots.
    /// </summary>
    private static ChronographImport? XeroTable(IReadOnlyList<string[]> rows)
    {
        int header = -1, speed = -1;
        for (int r = 0; r < rows.Count && header < 0; r++)
        {
            int at = Array.FindIndex(rows[r], h => h.Trim().StartsWith("SPEED (", StringComparison.OrdinalIgnoreCase));
            if (at >= 0 && rows[r].Length > 0 && rows[r][0].Trim() == "#")
            {
                (header, speed) = (r, at);
            }
        }

        if (header < 0)
        {
            return null;
        }

        var names = rows[header];
        // "Speed (M/S)", or "Speed (MPS)" as the Xero's metric export writes it (entry 339).
        bool inMetres = names[speed].Contains("M/S", StringComparison.OrdinalIgnoreCase) || names[speed].Contains("MPS", StringComparison.OrdinalIgnoreCase);
        int Column(string name) => Array.FindIndex(names, h => h.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));
        int clean = Column("Clean Bore"), cold = Column("Cold Bore"), note = Column("Shot Notes"), time = Column("Time");
        int delta = Array.FindIndex(names, h => h.Trim().StartsWith("Δ", StringComparison.Ordinal));
        string At(string[] row, int i) => i >= 0 && i < row.Length ? row[i].Trim() : "";
        static bool Ticked(string v) => v.Length > 0 && !v.Equals("false", StringComparison.OrdinalIgnoreCase) && v != "0" && !v.Equals("no", StringComparison.OrdinalIgnoreCase);

        var shots = new List<ChronographShot>();
        int r2 = header + 1;
        for (; r2 < rows.Count; r2++)
        {
            var row = rows[r2];
            if (!int.TryParse(At(row, 0), NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
            {
                break;
            }

            if (Number(At(row, speed)) is { } v)
            {
                shots.Add(new ChronographShot(number, inMetres ? v * FeetPerMetre : v, Ticked(At(row, clean)), Ticked(At(row, cold)), At(row, note) is { Length: > 0 } n ? n : null,
                    At(row, delta) == "--", Clock(At(row, time))));
            }
        }

        // The footer, by its own labels only: nothing else below the shots is read.
        double? Footer(string label) => rows.Skip(r2).FirstOrDefault(r => At(r, 0).Equals(label, StringComparison.OrdinalIgnoreCase)) is { } f ? Number(At(f, 1)) : null;
        double? average = Footer("AVERAGE SPEED"), sd = Footer("STD DEV"), spread = Footer("SPREAD"), grains = Footer("Projectile Weight (GRAINS)");
        double scale = inMetres ? FeetPerMetre : 1;
        string? name = header > 0 && At(rows[header - 1], 0) is { Length: > 0 } title ? title : null;
        var velocities = shots.Select(s => s.Fps).ToList();
        int deleted = shots.Count > 0 ? shots[^1].Number - shots.Count : 0;
        int leftOut = shots.Count(s => s.LeftOutByChronograph);

        // The Xero's own figures are of the shots it kept; those it left out are still recorded shots, and are kept, and said.
        var counted = leftOut > 0 && leftOut < shots.Count ? shots.Where(s => !s.LeftOutByChronograph).Select(s => s.Fps).ToList() : velocities;
        return new ChronographImport(ChronographFormat.GarminXero, velocities,
            $"A Garmin Xero export: {shots.Count} shots{(deleted > 0 ? $", {deleted} deleted on the chronograph and left out" : "")}"
            + $"{(leftOut > 0 ? $", {leftOut} of them left out of the chronograph's own figures" : "")}, in {(inMetres ? "m/s" : "ft/s")}.",
            false, names, speed)
        {
            Name = name,
            Shots = shots,
            InMetres = inMetres,
            Conditions = grains is not null ? new ChronographConditions(grains, null, null, null) : null,
            Disagrees = Check(counted, average * scale, sd * scale, spread * scale, null, null, 0.15 * scale),
        };
    }

    /// <summary>
    /// The BulletSeeker export: shots across the columns, a row "Shot Number" (Shot 1, Shot 2, ...) and a row "Mean Speed [fps]" with the
    /// reading for each, above many rows of the radar's raw samples, which are not read. The string's name is the first row's; the
    /// "Statistics" block (Min, Max, Avg, Deviation), where there is one, checks the shots, and the weather rows are offered as conditions.
    /// Nothing else above the shots is read, the location block included.
    /// </summary>
    private static ChronographImport? BulletSeekerTable(IReadOnlyList<string[]> rows)
    {
        int numbers = rows.ToList().FindIndex(r => r.Length > 1 && r[0].Trim().Equals("Shot Number", StringComparison.OrdinalIgnoreCase));
        int speeds = rows.ToList().FindIndex(r => r.Length > 1 && r[0].Trim().StartsWith("Mean Speed", StringComparison.OrdinalIgnoreCase));
        if (numbers < 0 || speeds < 0)
        {
            // A string saved with no shot in it: its header and nothing under it.
            bool header = rows.Count > 1 && rows[0].Length > 0 && rows[0][0].Trim() is "String" or "Name" && rows[1].Length > 0 && rows[1][0].Trim() == "Created";
            return header ? new ChronographImport(ChronographFormat.BulletSeeker, [], $"A BulletSeeker export with no shots in it. {BulletSeekerWords}", true, [], null) : null;
        }

        bool inMetres = rows[speeds][0].Contains("m/s", StringComparison.OrdinalIgnoreCase);
        var shots = new List<ChronographShot>();
        for (int c = 1; c < rows[speeds].Length && c < rows[numbers].Length; c++)
        {
            string label = rows[numbers][c].Trim();
            if (label.Length == 0)
            {
                continue;
            }

            int number = int.TryParse(label.Replace("Shot", "", StringComparison.OrdinalIgnoreCase).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : shots.Count + 1;
            if (Number(rows[speeds][c]) is { } v)
            {
                shots.Add(new ChronographShot(number, inMetres ? v * FeetPerMetre : v));
            }
        }

        string Label(string[] r) => r.Length > 0 ? r[0].Trim() : "";
        double? Value(string label) => rows.FirstOrDefault(r => Label(r).Equals(label, StringComparison.OrdinalIgnoreCase)) is { Length: > 1 } f ? Number(f[1]) : null;
        double? humidity = Value("Humidity") is { } h ? (h < 1 ? h * 100 : h) : null;
        string? name = rows.Count > 0 && rows[0].Length > 1 && (Label(rows[0]) is "String" or "Name") && rows[0][1].Trim() is { Length: > 0 } title ? title : null;
        var velocities = shots.Select(s => s.Fps).ToList();
        double scale = inMetres ? FeetPerMetre : 1;

        // The statistics are of the radar's unrounded speeds, and the shots are given as whole numbers, so they agree within a foot a second.
        return new ChronographImport(ChronographFormat.BulletSeeker, velocities,
            $"A BulletSeeker export: {shots.Count} shots, in {(inMetres ? "m/s" : "ft/s")}. {BulletSeekerWords}", true, [.. rows[numbers]], null)
        {
            Name = name,
            InMetres = inMetres,
            Shots = shots,
            Conditions = new ChronographConditions(null, Value("Temperature"), Value("Pressure"), humidity),
            Disagrees = Check(velocities, Value("Avg") * scale, Value("Deviation") * scale, null, Value("Min") * scale, Value("Max") * scale, 1.0 * scale),
        };
    }

    /// <summary>The shots' own mean, SD and spread against the file's, within <paramref name="tolerance"/> ft/s; what differs, or null.</summary>
    private static string? Check(IReadOnlyList<double> v, double? average, double? sd, double? spread, double? min, double? max, double tolerance)
    {
        if (v.Count == 0)
        {
            return average is not null ? "The file states figures for a string with no shots." : null;
        }

        double mean = v.Average();
        double sample = v.Count > 1 ? Math.Sqrt(v.Sum(x => (x - mean) * (x - mean)) / (v.Count - 1)) : 0;
        double population = Math.Sqrt(v.Sum(x => (x - mean) * (x - mean)) / v.Count);
        var differs = new List<string>();
        if (average is { } a && Math.Abs(a - mean) > tolerance)
        {
            differs.Add(string.Create(CultureInfo.InvariantCulture, $"average {a:0.0} against {mean:0.0}"));
        }

        if (sd is { } s && Math.Abs(s - sample) > tolerance && Math.Abs(s - population) > tolerance)
        {
            differs.Add(string.Create(CultureInfo.InvariantCulture, $"SD {s:0.0} against {sample:0.0}"));
        }

        if (spread is { } es && Math.Abs(es - (v.Max() - v.Min())) > tolerance)
        {
            differs.Add(string.Create(CultureInfo.InvariantCulture, $"spread {es:0.0} against {v.Max() - v.Min():0.0}"));
        }

        if (min is { } lo && Math.Abs(lo - v.Min()) > tolerance)
        {
            differs.Add(string.Create(CultureInfo.InvariantCulture, $"slowest {lo:0.0} against {v.Min():0.0}"));
        }

        if (max is { } hi && Math.Abs(hi - v.Max()) > tolerance)
        {
            differs.Add(string.Create(CultureInfo.InvariantCulture, $"fastest {hi:0.0} against {v.Max():0.0}"));
        }

        return differs.Count == 0 ? null : "The file's own figures differ from its shots: " + string.Join(", ", differs) + ".";
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
            false, names, at)
        { InMetres = inMetres };
    }

    /// <summary>
    /// A shot's time of day as the Xero writes it: "14:20:04" in a CSV, and in a workbook a date and time, or a fraction of a day where the
    /// cell is a number; null for anything else.
    /// </summary>
    internal static TimeSpan? Clock(string text)
    {
        var inv = CultureInfo.InvariantCulture;
        if (text.Length == 0)
        {
            return null;
        }

        if (TimeSpan.TryParse(text, inv, out var t) && t >= TimeSpan.Zero && t < TimeSpan.FromDays(1))
        {
            return t;
        }

        if (DateTime.TryParse(text, inv, DateTimeStyles.None, out var at))
        {
            return at.TimeOfDay;
        }

        return double.TryParse(text, NumberStyles.Float, inv, out double day) && day is >= 0 and < 1 ? TimeSpan.FromDays(day) : null;
    }

    /// <summary>A workbook cell as text: a number in the invariant culture, anything else as it is.</summary>
    private static string Cell(object? value) => value switch
    {
        null => "",
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    /// <summary>A number written with a decimal point, a decimal comma, or thousands separators beside a point ("2,853.4"), or null.</summary>
    private static double? Number(string text)
    {
        string t = text.Trim().Trim('"').Replace(" ", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
        if (t.Length == 0)
        {
            return null;
        }

        if (!t.Contains('.', StringComparison.Ordinal) && t.Count(ch => ch == ',') == 1 && t.IndexOf(',', StringComparison.Ordinal) >= t.Length - 3)
        {
            t = t.Replace(',', '.');
        }

        return double.TryParse(t, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double v) && double.IsFinite(v) ? v : null;
    }
}
