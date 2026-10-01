using GroupLab.Core.Records;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab chronograph-files &lt;folder&gt;</c>, NOTES-FROM-PLANNING.md entry 334: every chronograph file under a folder read as the
/// import reads it, counted by reader and by year (the first folder under the one given), with each file that does not read and each string
/// whose own figures differ from its shots named by its file. No string's name, note or location is printed.
/// </summary>
public static class ChronographFilesVerb
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        if (args.Length != 1 || !Directory.Exists(args[0]))
        {
            error.WriteLine("usage: grouplab chronograph-files <folder>");
            return 2;
        }

        string root = args[0];
        var counts = new SortedDictionary<(string Year, ChronographFormat Format), (int Files, int Strings, int Shots)>();
        var unread = new List<string>();
        var differ = new List<string>();
        var empty = new List<string>();
        int files = 0, timed = 0, timedShots = 0;
        var runs = new SortedDictionary<int, int>();
        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            if (!ChronographFiles.Extensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
            {
                continue;
            }

            files++;
            string relative = Path.GetRelativePath(root, file);
            string year = relative.Split(Path.DirectorySeparatorChar)[0];
            try
            {
                using var stream = File.OpenRead(file);
                var strings = ChronographFiles.ReadFile(stream, file, out int passedOver);
                if (strings.Count == 0)
                {
                    unread.Add($"{relative}: no sheet is a chronograph string ({passedOver} sheets)");
                    continue;
                }

                foreach (var format in strings.Select(s => s.Format).Distinct())
                {
                    var those = strings.Where(s => s.Format == format).ToList();
                    var c = counts.GetValueOrDefault((year, format));
                    counts[(year, format)] = (c.Files + 1, c.Strings + those.Count, c.Shots + those.Sum(s => s.VelocitiesFps.Count));
                }

                // Entry 342: how the strings that time their shots split into runs, which is what the pairing proposal reads.
                foreach (var each in strings.Where(s => s.Shots.Count > 1 && s.Shots.All(x => x.Time is not null)))
                {
                    timed++;
                    timedShots += each.Shots.Count;
                    int n = ChronographReconciliation.Runs(each.Shots).Count;
                    runs[n] = runs.GetValueOrDefault(n) + 1;
                }

                differ.AddRange(strings.Where(s => s.Disagrees is not null).Select(s => $"{relative}: {s.Disagrees}"));
                if (strings.All(s => s.VelocitiesFps.Count == 0))
                {
                    empty.Add(relative);
                }
            }
            catch (Exception e) when (e is IOException or FormatException or InvalidOperationException or ExcelDataReader.Exceptions.ExcelReaderException or NotSupportedException)
            {
                unread.Add($"{relative}: {e.GetType().Name}");
            }
        }

        output.WriteLine($"{files} files under the folder.");
        output.WriteLine("| Year | Reader | Files | Strings | Shots |");
        output.WriteLine("|---|---|---|---|---|");
        foreach (var ((year, format), (f, s, n)) in counts)
        {
            output.WriteLine($"| {year} | {format} | {f} | {s} | {n} |");
        }

        output.WriteLine($"Not read: {unread.Count}");
        foreach (string line in unread)
        {
            output.WriteLine("  " + line);
        }

        output.WriteLine($"Read, with no shot in them: {empty.Count}");
        foreach (string line in empty)
        {
            output.WriteLine("  " + line);
        }

        output.WriteLine($"Timed strings: {timed}, {timedShots} shots; runs separated by pauses: "
            + (runs.Count == 0 ? "none" : string.Join(", ", runs.Select(r => $"{r.Value} with {r.Key}"))));
        output.WriteLine($"Own figures differing from the shots: {differ.Count}");
        foreach (string line in differ)
        {
            output.WriteLine("  " + line);
        }

        return unread.Count == 0 ? 0 : 1;
    }
}
