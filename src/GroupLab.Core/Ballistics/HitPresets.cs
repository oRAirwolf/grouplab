namespace GroupLab.Core.Ballistics;

/// <summary>
/// One confidence preset: a name, the situation it stands for in a sentence, and the uncertainty it gives every source a person cannot
/// measure. Only standard deviations; a preset never sets a bias, because a bias is something a person knows about their own equipment.
/// </summary>
public sealed record HitPreset(string Name, string Situation, Func<double, IReadOnlyDictionary<HitSource, HitUncertainty>> Errors);

/// <summary>
/// GroupLab's own confidence presets, NOTES-FROM-PLANNING.md entry 156 section 8 item 2. One choice sets every uncertainty a shooter cannot
/// measure at once; the muzzle velocity's spread, the precision and the zero come from what GroupLab measured and no preset touches them.
/// <para>
/// <b>Where each number comes from.</b> They are GroupLab's judgment of each situation, written down so they can be argued with, and
/// docs/STATISTICS.md section 12.6 gives the reasoning for every one. None is taken from another calculator. The range is a standard deviation
/// in yards, or a share of the distance where it is guessed, because a guess is wrong in proportion; the wind is full-value crosswind in
/// mph; the drag is a percentage of the drag the ballistic coefficient implies.
/// </para>
/// </summary>
public static class HitPresets
{
    public static IReadOnlyList<HitPreset> All { get; } =
    [
        new("Known distance, measured air",
            "A range with marked distances, the air measured at the line with a weather meter, and the wind read from flags and mirage by somebody who has shot there before.",
            _ => Errors(range: 0.5, wind: 1.5, drag: 1, temperature: 2, pressure: 0.03, humidity: 5, inclination: 0.5, azimuth: 5, latitude: 0.5)),
        new("Lasered distance, estimated wind",
            "A distance from a laser rangefinder, the air from a phone or a forecast, and the wind estimated from what can be seen downrange.",
            _ => Errors(range: 1.5, wind: 3, drag: 2, temperature: 5, pressure: 0.1, humidity: 15, inclination: 1, azimuth: 10, latitude: 1)),
        new("Estimated distance, estimated wind",
            "No rangefinder: the distance judged by eye or from a map, the air guessed from the season, and the wind estimated with nothing to read it from.",
            yards => Errors(range: 0.05 * yards, wind: 4, drag: 3, temperature: 10, pressure: 0.3, humidity: 25, inclination: 2, azimuth: 15, latitude: 2)),
    ];

    /// <summary>The preset's own name for a person who has edited one of its figures.</summary>
    public const string Custom = "Custom";

    private static Dictionary<HitSource, HitUncertainty> Errors(double range, double wind, double drag, double temperature, double pressure, double humidity, double inclination, double azimuth, double latitude) => new()
    {
        [HitSource.Range] = new(range),
        [HitSource.Wind] = new(wind),
        [HitSource.Drag] = new(drag),
        [HitSource.Temperature] = new(temperature),
        [HitSource.Pressure] = new(pressure),
        [HitSource.Humidity] = new(humidity),
        [HitSource.Inclination] = new(inclination),
        [HitSource.Azimuth] = new(azimuth),
        [HitSource.Latitude] = new(latitude),
    };
}
