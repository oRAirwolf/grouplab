using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Measurement;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab compare-photos</c>, NOTES-FROM-PLANNING.md entry 113 section 4: a sheet's flat scan against photographs of the same sheet as it
/// hung. The scan is the truth for the photographs, the same holes measured flat: its marking once a person has corrected it, named with
/// <c>--truth</c>, or its own detection otherwise, and the command says which. Each photograph gets one row of <see cref="PhotoComparison"/>'s
/// table, read against the gates' thresholds; nothing here decides a gate. It reads only the files it is given and writes nothing.
/// </summary>
public static class PhotoVerb
{
    public const string Usage = "grouplab compare-photos <scan> <photograph>... [--truth <corrected scan marking>] [--library <directory>]... [--calibre <diameter>] [--model auto|homography|radial|surface]";

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        var images = new List<string>();
        var libraries = new List<string>();
        string? truthPath = null;
        Calibre? calibre = null;

        // NOTES-FROM-PLANNING.md entry 130 section 6b item 2: the bent-sheet model reported beside the ordinary one, on the same
        // photographs, so the two can be compared rather than argued about. It never changes what the application does: the default is
        // Auto, exactly as before, and the candidate runs only where it is asked for.
        var model = RegistrationModel.Auto;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--truth" when i + 1 < args.Length:
                    truthPath = args[++i];
                    break;
                case "--library" when i + 1 < args.Length:
                    libraries.Add(args[++i]);
                    break;
                case "--model" when i + 1 < args.Length:
                    if (!Enum.TryParse(args[++i], ignoreCase: true, out model))
                    {
                        error.WriteLine($"compare-photos: {args[i]} is not a registration model; use auto, homography, radial or surface");
                        return 2;
                    }

                    break;
                case "--calibre" when i + 1 < args.Length:
                    calibre = Calibre.Parse(args[++i], out string? problem);
                    if (problem is not null)
                    {
                        error.WriteLine($"compare-photos: {problem}");
                        return 2;
                    }

                    break;
                case var option when option.StartsWith("--", StringComparison.Ordinal):
                    error.WriteLine($"compare-photos: unknown option {option}");
                    error.WriteLine(Usage);
                    return 2;
                default:
                    images.Add(args[i]);
                    break;
            }
        }

        if (images.Count < 2)
        {
            error.WriteLine("compare-photos: give the scan and at least one photograph of the same sheet");
            error.WriteLine(Usage);
            return 2;
        }

        var scan = AnalyzeVerb.Analyze(images[0], null, out string? failure, libraries.Count > 0 ? libraries : null, calibre);
        if (failure is not null || scan is null || scan.Failure is not null || scan.Marking is not { } scanMarking || scan.Automatic.Scale is not { } scanScale)
        {
            error.WriteLine($"compare-photos: the scan could not be measured: {failure ?? scan?.Failure ?? "it did not register"}");
            return 1;
        }

        // The truth's holes on the page: the corrected marking's when there is one, the scan's own detection otherwise.
        MarkingState truth = scanMarking;
        var truthMapping = scanScale.Mapping;
        if (truthPath is not null)
        {
            try
            {
                truth = MarkingFile.Read(File.ReadAllText(truthPath)).State;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException)
            {
                error.WriteLine($"compare-photos: the truth marking could not be read: {ex.Message}");
                return 1;
            }

            if (truth.Scale is SheetReference own)
            {
                truthMapping = own.Mapping;
            }

            output.WriteLine($"Truth: the corrected scan marking, {Path.GetFileName(truthPath)}.");
        }
        else
        {
            output.WriteLine("Truth: the scan's own detection, not corrected by a person. Name a corrected marking with --truth when there is one.");
        }

        var truthHoles = truth.Shots.Where(s => s.IsShot).Select(s => truthMapping.ToPage(s.Image)).ToList();
        var truthBulls = Bulls(scan.Automatic.Measurement);
        var rows = new List<PhotoComparisonRow>();
        foreach (string photo in images.Skip(1))
        {
            string name = Path.GetFileName(photo);
            var result = AnalyzeVerb.Analyze(photo, null, out string? photoFailure, libraries.Count > 0 ? libraries : null, calibre,
                model == RegistrationModel.Auto ? null : new MeasureOptions(Model: model));
            if (photoFailure is not null || result is null || result.Failure is not null || result.Marking is not { } marking || result.Automatic.Scale is not { } scale)
            {
                output.WriteLine($"{name}: failed, {photoFailure ?? result?.Failure ?? "it did not register"}");
                continue;
            }

            if (result.Automatic.Definition?.Name != scan.Automatic.Definition?.Name)
            {
                output.WriteLine($"{name}: its codes name {result.Automatic.Definition?.Name}, not the scan's {scan.Automatic.Definition?.Name}; it is not the same sheet.");
                continue;
            }

            rows.Add(PhotoComparison.Compare(
                name,
                scale.Mapping.Model,
                truthBulls,
                truthHoles,
                Bulls(result.Automatic.Measurement),
                [.. marking.Shots.Where(s => s.IsShot).Select(s => scale.Mapping.ToPage(s.Image))]));
        }

        foreach (string line in PhotoComparison.Table(rows))
        {
            output.WriteLine(line);
        }

        return rows.Count == images.Count - 1 ? 0 : 1;
    }

    private static Dictionary<int, PointD> Bulls(SheetMeasurement measurement) =>
        measurement.Bulls.Where(b => b.Recovered is not null).ToDictionary(b => b.Index, b => b.Recovered!.Value);
}
