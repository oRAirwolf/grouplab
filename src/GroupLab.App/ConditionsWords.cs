using System.Globalization;
using GroupLab.Core.Marking;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 416: the one line that says which conditions on the day are in use and where they came from, in the person's
/// own units, as the desktop's Conditions on the day and the phone's Conditions row both say it.
/// </summary>
internal static class ConditionsWords
{
    /// <summary>The words of the line, in the person's own units.</summary>
    public static string Summary(VelocityConditions? used, UnitSettings units)
    {
        if (used is null || (used.StandardAir && used.AngleDegrees == 0))
        {
            return "Standard day assumed: no temperature or altitude entered.";
        }

        var inv = CultureInfo.InvariantCulture;
        string Shown(double imperial, BallisticMeasure measure, string format) =>
            BallisticMeasures.FromImperial(imperial, measure, units).ToString(format, inv) + " " + BallisticMeasures.Symbol(measure, units).TrimStart('°');
        var air = used.Air;
        string where = air.PressureInHg is { } p ? Shown(p, BallisticMeasure.Pressure, BallisticMeasures.IsMetric(units) ? "0" : "0.00") : Shown(air.AltitudeFt, BallisticMeasure.Altitude, "0");
        string angle = used.AngleDegrees == 0 ? "shot level" : string.Create(inv, $"shot at {used.AngleDegrees:0.#} degrees");
        return $"{Shown(air.TemperatureF, BallisticMeasure.Temperature, "0.#")}, {where}, {air.HumidityPct.ToString("0", inv)}% humidity, {angle}, from {used.From}.";
    }

}
