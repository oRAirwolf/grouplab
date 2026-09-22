using System.Globalization;
using GroupLab.Core.Marking;

namespace GroupLab.App;

/// <summary>A quantity the ballistics page asks for whose unit is not a length at the target, so <see cref="UnitSettings"/> has no word for it.</summary>
internal enum BallisticMeasure
{
    /// <summary>A small length: sight height, twist, a bullet's length and diameter. Inches, or millimetres.</summary>
    SmallLength,

    /// <summary>A speed: muzzle velocity and its spread. Feet a second, or metres a second.</summary>
    Speed,

    /// <summary>Air temperature. Fahrenheit, or Celsius.</summary>
    Temperature,

    /// <summary>Station pressure. Inches of mercury, or hectopascals.</summary>
    Pressure,

    /// <summary>Height above sea level. Feet, or metres.</summary>
    Altitude,

    /// <summary>A wind speed. Miles an hour, or kilometres an hour.</summary>
    WindSpeed,
}

/// <summary>
/// The units the ballistics page's own fields are in, NOTES-FROM-PLANNING.md entry 131 section 8's imperial and metric toggle.
/// <para>
/// <b>Why these are not <see cref="UnitSettings"/>.</b> That record says how a length at the target, a distance to it and an angle are
/// shown, which is what the rest of GroupLab measures. A trajectory also needs a muzzle velocity, an air temperature, a station pressure, an
/// altitude and a wind speed, and none of those is any of those three. They follow the same toggle so a page is never half in one system.
/// </para>
/// <para>
/// <b>Everything GroupLab stores stays imperial.</b> The records hold inches and feet a second whatever a person types, and the solver works
/// in them, so switching the toggle changes what is on the screen and never what is on the record. A value typed in one system and read in
/// the other is the fault this exists to prevent, so the boxes are rewritten when the toggle moves.
/// </para>
/// </summary>
internal static class BallisticMeasures
{
    /// <summary>Whether this is the metric side of the toggle. The distance unit decides, so the page cannot be half metric.</summary>
    public static bool IsMetric(UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(units);
        return units.Distance == DistanceUnit.Metre;
    }

    /// <summary>What the field is measured in, for its label.</summary>
    public static string Symbol(BallisticMeasure measure, UnitSettings units) => (measure, IsMetric(units)) switch
    {
        (BallisticMeasure.SmallLength, false) => "in",
        (BallisticMeasure.SmallLength, true) => "mm",
        (BallisticMeasure.Speed, false) => "ft/s",
        (BallisticMeasure.Speed, true) => "m/s",
        (BallisticMeasure.Temperature, false) => "°F",
        (BallisticMeasure.Temperature, true) => "°C",
        (BallisticMeasure.Pressure, false) => "inHg",
        (BallisticMeasure.Pressure, true) => "hPa",
        (BallisticMeasure.Altitude, false) => "ft",
        (BallisticMeasure.Altitude, true) => "m",
        (BallisticMeasure.WindSpeed, false) => "mph",
        _ => "km/h",
    };

    /// <summary>What the person typed, turned into the imperial value everything here is stored and solved in.</summary>
    public static double ToImperial(double shown, BallisticMeasure measure, UnitSettings units) => (measure, IsMetric(units)) switch
    {
        (_, false) => shown,
        (BallisticMeasure.SmallLength, _) => shown / 25.4,
        (BallisticMeasure.Speed, _) => shown / 0.3048,
        (BallisticMeasure.Temperature, _) => (shown * 9 / 5) + 32,
        (BallisticMeasure.Pressure, _) => shown / 33.863886666667,
        (BallisticMeasure.Altitude, _) => shown / 0.3048,
        _ => shown / 1.609344,
    };

    /// <summary>An imperial value as the person's own units show it.</summary>
    public static double FromImperial(double imperial, BallisticMeasure measure, UnitSettings units) => (measure, IsMetric(units)) switch
    {
        (_, false) => imperial,
        (BallisticMeasure.SmallLength, _) => imperial * 25.4,
        (BallisticMeasure.Speed, _) => imperial * 0.3048,
        (BallisticMeasure.Temperature, _) => (imperial - 32) * 5 / 9,
        (BallisticMeasure.Pressure, _) => imperial * 33.863886666667,
        (BallisticMeasure.Altitude, _) => imperial * 0.3048,
        _ => imperial * 1.609344,
    };

    /// <summary>
    /// An imperial value written for a box, in the person's units, or an empty box where there is nothing. The decimals are the fewest that
    /// keep the value, so a sight height of 1.5 in reads 38.1 mm rather than 38 mm, and a velocity reads whole.
    /// </summary>
    public static string Text(double? imperial, BallisticMeasure measure, UnitSettings units) => imperial is { } value
        ? FromImperial(value, measure, units).ToString(measure is BallisticMeasure.Speed or BallisticMeasure.Altitude ? "0.###" : "0.####", CultureInfo.InvariantCulture)
        : "";
}
