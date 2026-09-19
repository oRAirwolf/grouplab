using System.Globalization;
using System.Text.RegularExpressions;
using GroupLab.Core.Marking;

namespace GroupLab.Cli;

/// <summary>
/// The two paths NOTES-FROM-PLANNING.md entry 111 section 4 asks to have ready before the range material of 20 September arrives, and which
/// did not exist: detection over a folder of new scans from one command, and the Phase 3 timing measurement read from the application's own
/// log. Neither processes anything by itself. The range material enters the corpus through the intake tool first, as every donated image does,
/// because its photographs carry location data; these commands are run on what intake passes.
/// </summary>
public static partial class FolderVerbs
{
    public const string AnalyzeFolderUsage = "grouplab analyze-folder <directory> [--library <directory>]... [--calibre <diameter>] [--markings <directory>]";

    public const string TimingUsage = "grouplab timing <grouplab-log-file>";

    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".tif", ".tiff"];

    /// <summary>
    /// <c>grouplab analyze-folder</c>: every image in a folder through the same analysis as <c>grouplab analyze</c>, one line each. The line
    /// gives the definition the sheet's codes named, the markers found of those it carries, the holes detected, the review items left open,
    /// the registration residual and the time taken, or why the image failed. With <c>--markings</c> each result is saved as a marking file that
    /// the marking screen opens, which after correction is that sheet's truth pass. Images are read in name order, and nothing else is written.
    /// </summary>
    public static int AnalyzeFolder(string directory, string[] rest, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(rest);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        var libraries = new List<string>();
        Calibre? calibre = null;
        string? markings = null;
        for (int i = 0; i < rest.Length; i++)
        {
            switch (rest[i])
            {
                case "--library" when i + 1 < rest.Length:
                    libraries.Add(rest[++i]);
                    break;
                case "--calibre" when i + 1 < rest.Length:
                    calibre = Calibre.Parse(rest[++i], out string? problem);
                    if (problem is not null)
                    {
                        error.WriteLine($"analyze-folder: {problem}");
                        return 2;
                    }

                    break;
                case "--markings" when i + 1 < rest.Length:
                    markings = rest[++i];
                    break;
                default:
                    error.WriteLine($"analyze-folder: unknown option {rest[i]}");
                    error.WriteLine(AnalyzeFolderUsage);
                    return 2;
            }
        }

        if (!Directory.Exists(directory))
        {
            error.WriteLine($"analyze-folder: there is no folder {directory}");
            return 2;
        }

        var images = Directory.EnumerateFiles(directory)
            .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Order(StringComparer.Ordinal)
            .ToList();
        if (images.Count == 0)
        {
            error.WriteLine($"analyze-folder: {directory} holds no images");
            return 1;
        }

        if (markings is not null)
        {
            Directory.CreateDirectory(markings);
        }

        var inv = CultureInfo.InvariantCulture;
        output.WriteLine("image                                   definition            markers  holes  open  residual in      ms");
        int analysed = 0, failed = 0;
        foreach (string image in images)
        {
            string name = Path.GetFileName(image);
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var result = AnalyzeVerb.Analyze(image, null, out string? failure, libraries.Count > 0 ? libraries : null, calibre);
            long ms = clock.ElapsedMilliseconds;
            if (failure is not null || result is null || result.Failure is not null || result.Marking is not { } marking)
            {
                failed++;
                output.WriteLine(string.Create(inv, $"{name,-39} failed: {failure ?? result?.Failure ?? "no marking"}"));
                continue;
            }

            analysed++;
            string markers = marking.Scale is SheetReference { MarkersFound: { } found, MarkersExpected: { } expected } ? $"{found}/{expected}" : "-";
            int open = ReviewQueue.Open(ReviewQueue.For(marking, false));
            string residual = result.Automatic.Measurement.Registration is { } registration ? (registration.RmsResidual / 254).ToString("0.0000", inv) : "-";
            output.WriteLine(string.Create(inv,
                $"{name,-39} {Short(result.Automatic.Definition?.Name),-21} {markers,7} {marking.Shots.Count(s => s.IsShot),6} {open,5} {residual,12} {ms,7}"));
            if (markings is not null)
            {
                File.WriteAllText(Path.Combine(markings, Path.GetFileNameWithoutExtension(name) + ".grouplab.json"), MarkingFile.Write(marking));
            }
        }

        output.WriteLine(string.Create(inv, $"{analysed} analysed, {failed} failed, of {images.Count} images"));
        return failed == 0 ? 0 : 1;
    }

    /// <summary>
    /// <c>grouplab timing</c>: the Phase 3 gate's measurement, DESIGN.md section 21, read from the log GroupLab writes. The gate is a person
    /// correcting a full 25-shot sheet's misassignments in under two minutes, so the clock is theirs, and the log already records each step:
    /// the image opened, the marks detected, every review choice and typed bull, and Accept and analyse. For each image opened this prints the
    /// seconds from the marks appearing to Accept, the seconds from opening, the choices made and the items left open. It judges nothing: the
    /// gate is met or not by a person reading the number against two minutes.
    /// </summary>
    public static int Timing(string logPath, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        if (!File.Exists(logPath))
        {
            error.WriteLine($"timing: there is no file {logPath}");
            return 2;
        }

        var sheets = Sheets(File.ReadLines(logPath));
        if (sheets.Count == 0)
        {
            output.WriteLine("No image was opened in this log.");
            return 0;
        }

        var inv = CultureInfo.InvariantCulture;
        output.WriteLine("image                                   detected to accept  opened to accept  choices  open at accept");
        foreach (var s in sheets)
        {
            string fromDetection = s.Accepted is { } a && s.Detected is { } d ? $"{(a - d).TotalSeconds:0.0} s" : "-";
            string fromOpening = s.Accepted is { } b ? $"{(b - s.Opened).TotalSeconds:0.0} s" : "not accepted";
            output.WriteLine(string.Create(inv, $"{s.File,-39} {fromDetection,18} {fromOpening,17} {s.Choices,8} {(s.OpenAtAccept?.ToString(inv) ?? "-"),15}"));
        }

        return 0;
    }

    /// <summary>One image's passage through the editor, as the log records it.</summary>
    public sealed record SheetTiming(string File, DateTime Opened, DateTime? Detected, DateTime? Accepted, int Choices, int? OpenAtAccept);

    /// <summary>Each image opened in a log, from its opening to the first Accept and analyse after it, with the review choices between.</summary>
    public static List<SheetTiming> Sheets(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var sheets = new List<SheetTiming>();
        SheetTiming? current = null;
        foreach (string line in lines)
        {
            var m = LogLine().Match(line);
            if (!m.Success)
            {
                continue;
            }

            var at = DateTime.Parse(m.Groups["time"].Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            string name = m.Groups["name"].Value, fields = m.Groups["fields"].Value;
            switch (name)
            {
                case "image.open":
                    if (current is not null)
                    {
                        sheets.Add(current);
                    }

                    current = new SheetTiming(Field(fields, "file") ?? "(unnamed)", at, null, null, 0, null);
                    break;
                case "detect.run" when current is { Detected: null, Accepted: null }:
                    current = current with { Detected = at };
                    break;
                case "review.choose" or "review.typed" when current is { Accepted: null }:
                    current = current with { Choices = current.Choices + 1 };
                    break;
                case "analysis.accept" when current is { Accepted: null }:
                    current = current with
                    {
                        Accepted = at,
                        OpenAtAccept = int.TryParse(Field(fields, "open"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int open) ? open : null,
                    };
                    break;
            }
        }

        if (current is not null)
        {
            sheets.Add(current);
        }

        return sheets;
    }

    private static string Short(string? name) => name is null ? "-" : name.Length <= 20 ? name : name[..20];

    private static string? Field(string fields, string key)
    {
        var m = Regex.Match(fields, $@"(?:^|\s){Regex.Escape(key)}=(?:""(?<v>[^""]*)""|(?<v>\S+))");
        return m.Success ? m.Groups["v"].Value : null;
    }

    /// <summary>A GroupLab log line: the UTC time, the level, the event's name and its fields.</summary>
    [GeneratedRegex(@"^(?<time>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z)\s+\S+\s+(?<name>\S+)(?<fields>.*)$")]
    private static partial Regex LogLine();
}
