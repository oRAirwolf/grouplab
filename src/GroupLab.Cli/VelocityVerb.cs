using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Records;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab velocity</c>, NOTES-FROM-PLANNING.md entry 322 section 3: velocity's predicted share of a group's vertical against the vertical
/// measured, from a session file. The file uses the export's own names where they exist:
/// <code>
/// {
///   "distanceYards": 100, "ballisticCoefficient": 0.243, "dragModel": "G7", "bulletWeightGrains": 175,
///   "sightHeightInches": 1.5, "zeroRangeYards": 100, "temperatureF": 59, "altitudeFt": 0,
///   "shots": [ { "id": 1, "x": 0.12, "y": -0.30 }, ... ],
///   "velocitiesFps": [ 2701, 2695, ... ]          or   "chronograph": "2701, 2695 ...",
///   "shotVelocities": [ { "shotId": 1, "ordinal": 1 }, ... ]
/// }
/// </code>
/// Shot positions are inches at the target, y up. <c>shotVelocities</c> is the pairing a person accepted, ordinals counting readings from 1;
/// without it there is no regression, because the readings are never assumed to line up with the shots (DESIGN.md section 15). The options
/// supply or override the file's values.
/// </summary>
public static class VelocityVerb
{
    public const string Usage = """
        grouplab velocity <session.json> [--distance <yd>] [--bc <bc>] [--model G1|G7] [--weight <grains>] [--sight <in>] [--zero <yd>]
                          [--confidence <0 to 1, 0.9 by default>]
        """;

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        if (args.Length == 0 || args[0].StartsWith("--", StringComparison.Ordinal))
        {
            error.WriteLine(Usage);
            return 2;
        }

        string json;
        try
        {
            json = File.ReadAllText(args[0]);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"velocity: the session file cannot be read: {ex.Message}");
            return 2;
        }

        return Print(json, args[1..], output, error);
    }

    /// <summary>The figures for a session given as text, with the options after the file name.</summary>
    public static int Print(string json, string[] options, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        var inv = CultureInfo.InvariantCulture;
        JsonObject root;
        double? distance, bc, weight, sight, zero, temp, altitude;
        double confidence = 0.90;
        DragModel? model;
        try
        {
            root = JsonNode.Parse(json) as JsonObject ?? throw new FormatException("the session file is not a JSON object");
            distance = Number(root, "distanceYards");
            bc = Number(root, "ballisticCoefficient");
            weight = Number(root, "bulletWeightGrains");
            sight = Number(root, "sightHeightInches");
            zero = Number(root, "zeroRangeYards");
            temp = Number(root, "temperatureF");
            altitude = Number(root, "altitudeFt");
            model = (string?)root["dragModel"] is { } m ? ParseModel(m) : null;
            for (int i = 0; i < options.Length; i++)
            {
                string option = options[i];
                string Next() => i + 1 < options.Length ? options[++i] : throw new FormatException($"{option} needs a value");
                double Value() => double.Parse(Next(), NumberStyles.Float, inv);
                switch (option)
                {
                    case "--distance": distance = Value(); break;
                    case "--bc": bc = Value(); break;
                    case "--model": model = ParseModel(Next()); break;
                    case "--weight": weight = Value(); break;
                    case "--sight": sight = Value(); break;
                    case "--zero": zero = Value(); break;
                    case "--confidence": confidence = Value(); break;
                    default: throw new FormatException($"unknown option {option}");
                }
            }

            if (distance is null || bc is null || model is null)
            {
                throw new FormatException("the distance, the BC and the drag model are required, in the file or as --distance, --bc and --model");
            }
        }
        catch (Exception ex) when (ex is FormatException or JsonException or InvalidOperationException)
        {
            error.WriteLine($"velocity: {ex.Message}");
            error.WriteLine(Usage);
            return 2;
        }

        var shots = new List<(int Id, double Y)>();
        foreach (var shot in root["shots"] as JsonArray ?? [])
        {
            if (shot?["y"] is null)
            {
                error.WriteLine("velocity: every shot needs a y, its height at the target in inches, up positive.");
                return 2;
            }

            shots.Add(((int?)shot["id"] ?? shots.Count + 1, (double)shot["y"]!));
        }

        IReadOnlyList<double> readings;
        if (root["velocitiesFps"] is JsonArray list)
        {
            readings = [.. list.Select(v => (double)v!)];
        }
        else
        {
            var (read, refusal) = Chronograph.Read((string?)root["chronograph"] ?? "");
            if (refusal is not null)
            {
                error.WriteLine($"velocity: {refusal}");
                return 2;
            }

            readings = read;
        }

        List<(double Fps, double VerticalInches)>? matched = null;
        if (root["shotVelocities"] is JsonArray mapping)
        {
            matched = [];
            foreach (var pair in mapping)
            {
                int shotId = (int)pair!["shotId"]!, ordinal = (int)pair["ordinal"]!;
                int at = shots.FindIndex(s => s.Id == shotId);
                if (at < 0 || ordinal < 1 || ordinal > readings.Count)
                {
                    error.WriteLine(string.Create(inv, $"velocity: the pairing names shot {shotId} and reading {ordinal}, and one of them is not in the file."));
                    return 2;
                }

                matched.Add((readings[ordinal - 1], shots[at].Y));
            }
        }

        // The point-mass drop does not depend on the bullet's weight, which the BC already carries; the solver asks for one only for energy.
        var input = new BallisticInput(bc.Value, model.Value, 1, weight ?? 150, sight ?? 1.5, zero ?? 100, temp ?? 59, null, altitude ?? 0);
        VelocityVerticalResult? result;
        string? why;
        try
        {
            (result, why) = VelocityVertical.Analyze(input, distance.Value, readings, [.. shots.Select(s => s.Y)], matched, confidence);
        }
        catch (ArgumentException ex)
        {
            why = ex.Message;
            result = null;
        }

        if (result is null)
        {
            error.WriteLine($"velocity: {why}");
            return 1;
        }

        string level = string.Create(inv, $"{confidence * 100:0.#}%");
        output.WriteLine(string.Create(inv, $"{model} BC {bc:0.000}, sight {input.SightHeightInches:0.00} in, zeroed at {input.ZeroRangeYards:0} yd, {input.TemperatureF:0.#} F at {input.AltitudeFt:0} ft, shot at {distance:0} yd"));
        output.WriteLine(string.Create(inv, $"Intervals at {level}."));
        output.WriteLine();
        output.WriteLine(string.Create(inv, $"Velocity            {result.Readings} readings, mean {result.MeanFps:0} fps, SD {result.VelocitySdFps.Value:0.0} fps ({result.VelocitySdFps.Lower:0.0} to {result.VelocitySdFps.Upper:0.0})"));
        output.WriteLine(string.Create(inv, $"Solver              {result.DropPerFpsInches * 10:+0.000;-0.000} in of height per 10 fps at {distance:0} yd"));
        output.WriteLine(string.Create(inv, $"Predicted vertical  SD {result.PredictedVerticalSdInches.Value:0.000} in from velocity alone ({result.PredictedVerticalSdInches.Lower:0.000} to {result.PredictedVerticalSdInches.Upper:0.000})"));
        output.WriteLine(string.Create(inv, $"Measured vertical   SD {result.MeasuredVerticalSdInches.Value:0.000} in over {result.Shots} shots ({result.MeasuredVerticalSdInches.Lower:0.000} to {result.MeasuredVerticalSdInches.Upper:0.000})"));
        output.WriteLine(string.Create(inv, $"Share               {result.Share.Value * 100:0}% of the vertical variance ({result.Share.Lower * 100:0} to {result.Share.Upper * 100:0}%)"));
        output.WriteLine();
        output.WriteLine(result.Sentence);
        if (result.Slope is { } slope)
        {
            output.WriteLine(slope.Sentence);
        }
        else
        {
            output.WriteLine(matched is null
                ? "No readings are paired with shots, so there is no shot by shot regression."
                : "Fewer than three paired shots, or velocities that do not vary, so there is no shot by shot regression.");
        }

        output.WriteLine("The predicted figures come from the solver and the readings, and only the measured vertical from the shots.");
        foreach (string line in BallisticSolver.NotModelled)
        {
            output.WriteLine(line);
        }

        return 0;
    }

    private static double? Number(JsonObject root, string name) => root[name] is { } node ? (double)node : null;

    private static DragModel ParseModel(string text) =>
        Enum.TryParse(text, ignoreCase: true, out DragModel model) && Enum.IsDefined(model) ? model : throw new FormatException("the drag model is G1 or G7");
}
