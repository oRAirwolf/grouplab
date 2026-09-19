using System.Globalization;
using GroupLab.Core.Ballistics;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab trajectory</c>, NOTES-FROM-PLANNING.md entry 110 section 2g: a trajectory table from stated inputs, so the solver can be checked
/// by hand against any calculator. Nothing is assumed that is not printed above the table, and what the solver does not model is printed below
/// it.
/// </summary>
public static class TrajectoryVerb
{
    public const string Usage = """
        grouplab trajectory --bc <bc> --model G1|G7 --mv <fps> --weight <grains> [--sight <in>] [--zero <yd>] [--max <yd>] [--step <yd>]
                            [--temp <F>] [--pressure <inHg> | --altitude <ft>] [--humidity <%>] [--wind <mph, + from the left>]
                            [--angle <degrees>] [--reference icao|asm] [--twist <in> --diameter <in> --length <in> [--left-twist]]
                            [--latitude <degrees>]
        """;

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        if (ReferenceEquals(output, Console.Out))
        {
            // The notes below the table name the Eötvös term, which a console left in its code page cannot print.
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }

        double? bc = null, mv = null, weight = null, pressure = null, twist = null, diameter = null, length = null, latitude = null;
        DragModel? model = null;
        double sight = 1.5, zero = 100, max = 1000, step = 100, temp = 59, altitude = 0, humidity = 50, wind = 0, angle = 0;
        var reference = ReferenceAtmosphere.Icao;
        int direction = 1;
        try
        {
            for (int i = 0; i < args.Length; i++)
            {
                string option = args[i];
                double Next() => i + 1 < args.Length
                    ? double.Parse(args[++i], NumberStyles.Float, CultureInfo.InvariantCulture)
                    : throw new FormatException($"{option} needs a value");
                switch (option)
                {
                    case "--bc": bc = Next(); break;
                    case "--model":
                        model = i + 1 < args.Length && Enum.TryParse(args[++i], ignoreCase: true, out DragModel m) ? m : throw new FormatException("--model is G1 or G7");
                        break;
                    case "--mv": mv = Next(); break;
                    case "--weight": weight = Next(); break;
                    case "--sight": sight = Next(); break;
                    case "--zero": zero = Next(); break;
                    case "--max": max = Next(); break;
                    case "--step": step = Next(); break;
                    case "--temp": temp = Next(); break;
                    case "--pressure": pressure = Next(); break;
                    case "--altitude": altitude = Next(); break;
                    case "--humidity": humidity = Next(); break;
                    case "--wind": wind = Next(); break;
                    case "--angle": angle = Next(); break;
                    case "--twist": twist = Next(); break;
                    case "--diameter": diameter = Next(); break;
                    case "--length": length = Next(); break;
                    case "--latitude": latitude = Next(); break;
                    case "--left-twist": direction = -1; break;
                    case "--reference":
                        reference = i + 1 < args.Length ? args[++i].ToLowerInvariant() switch
                        {
                            "icao" => ReferenceAtmosphere.Icao,
                            "asm" => ReferenceAtmosphere.ArmyStandardMetro,
                            _ => throw new FormatException("--reference is icao or asm"),
                        } : throw new FormatException("--reference needs a value");
                        break;
                    default: throw new FormatException($"unknown option {option}");
                }
            }

            if (bc is null || model is null || mv is null || weight is null)
            {
                throw new FormatException("--bc, --model, --mv and --weight are required");
            }
        }
        catch (FormatException ex)
        {
            error.WriteLine($"trajectory: {ex.Message}");
            error.WriteLine(Usage);
            return 2;
        }

        var input = new BallisticInput(bc.Value, model.Value, mv.Value, weight.Value, sight, zero, temp, pressure, altitude, humidity, wind, angle, reference,
            twist, direction, diameter, length, latitude);
        Trajectory trajectory;
        try
        {
            trajectory = BallisticSolver.Solve(input, max, step);
        }
        catch (ArgumentException ex)
        {
            error.WriteLine($"trajectory: {ex.Message}");
            return 2;
        }

        var inv = CultureInfo.InvariantCulture;
        output.WriteLine(string.Create(inv, $"{model} BC {bc:0.000} ({(reference == ReferenceAtmosphere.Icao ? "ICAO" : "Army Standard Metro")}), {mv:0} fps, {weight:0.#} gr, sight {sight:0.00} in, zeroed at {zero:0} yd"));
        output.WriteLine(string.Create(inv, $"{temp:0.#} F, {input.StationPressureInHg:0.00} inHg{(pressure is null ? $" from {altitude:0} ft" : "")}, {humidity:0}% humidity, crosswind {wind:0.#} mph from the {(wind >= 0 ? "left" : "right")}, angle {angle:0.#} degrees"));
        if (trajectory.Stability is { } sg)
        {
            output.WriteLine(string.Create(inv, $"Miller stability {sg:0.00}, {(direction > 0 ? "right" : "left")}-hand twist"));
        }

        bool spin = trajectory.Stability is not null, coriolis = latitude is not null;
        output.WriteLine();
        output.WriteLine("  Range    Vel   Energy    Drop   Drop  Drop   Wind   Wind   TOF    Mach" + (spin ? "   Spin" : "") + (coriolis ? "  Coriolis" : ""));
        output.WriteLine("     yd    fps    ft-lb      in    MOA   mil     in    MOA     s        " + (spin ? "     in" : "") + (coriolis ? "        in" : ""));
        foreach (var p in trajectory.Points)
        {
            output.WriteLine(string.Create(inv,
                $"{p.RangeYards,7:0} {p.VelocityFps,6:0} {p.EnergyFtLb,8:0} {p.DropInches,7:0.00} {p.DropMoa,6:0.00} {p.DropMil,5:0.00} {p.WindInches,6:0.00} {p.WindMoa,6:0.00} {p.TimeOfFlight,5:0.000} {p.Mach,7:0.000}")
                + (spin ? string.Create(inv, $" {p.SpinDriftInches ?? 0,6:0.00}") : "")
                + (coriolis ? string.Create(inv, $" {p.CoriolisInches ?? 0,9:0.00}") : ""));
        }

        output.WriteLine();
        output.WriteLine("Drop MOA is the elevation to dial, positive when the bullet is low. Wind is positive to the right.");
        foreach (string note in trajectory.NotModelled)
        {
            output.WriteLine(note);
        }

        return 0;
    }
}
