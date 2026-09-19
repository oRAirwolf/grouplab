namespace GroupLab.Core.Ballistics;

/// <summary>
/// The atmosphere a ballistic coefficient is stated against, NOTES-FROM-PLANNING.md entry 110 section 2e. Many published G1 coefficients are
/// referenced to Army Standard Metro rather than ICAO, and reading one against the other misstates drag by a percent or two.
/// </summary>
public enum ReferenceAtmosphere
{
    /// <summary>ICAO: 59 °F, 29.9213 inHg, dry, 0.0764742 lb/ft³. The default.</summary>
    Icao,

    /// <summary>Army Standard Metro: 59 °F, 750 mmHg (29.5275 inHg) and 78 percent relative humidity.</summary>
    ArmyStandardMetro,
}

/// <summary>
/// The atmosphere of reference/ballistics-js/ballistics.js, ported as it stands (entry 110 section 2a): station pressure from altitude by the
/// ICAO troposphere, the density ratio with its virtual-temperature humidity correction, and the speed of sound with the Owen-Cramer moist-air
/// correction. The saturation vapour pressure is Herman Wobus's polynomial, shared by both so the two cannot drift apart.
/// </summary>
public static class Atmosphere
{
    public const double StandardTemperatureF = 59.0;
    public const double StandardPressureInHg = 29.9213;
    public const double StandardDensity = 0.0764742;
    public const double LapseRateFPerFoot = 0.00356616;
    public const double MetroPressureInHg = 29.5275;
    public const double MetroHumidityPct = 78;

    /// <summary>Saturation vapour pressure of water in inHg, by Wobus's polynomial.</summary>
    public static double SaturationVaporInHg(double temperatureF)
    {
        double tC = (temperatureF - 32) * 5 / 9;
        const double eso = 6.1078;
        const double c0 = 0.99999683, c1 = -0.90826951e-2, c2 = 0.78736169e-4, c3 = -0.61117958e-6, c4 = 0.43884187e-8;
        const double c5 = -0.29883885e-10, c6 = 0.21874425e-12, c7 = -0.17892321e-14, c8 = 0.11112018e-16, c9 = -0.30994571e-19;
        double p = c0 + (tC * (c1 + (tC * (c2 + (tC * (c3 + (tC * (c4 + (tC * (c5 + (tC * (c6 + (tC * (c7 + (tC * (c8 + (tC * c9)))))))))))))))));
        double hectopascals = eso / Math.Pow(p, 8);
        return hectopascals * 0.02953;
    }

    /// <summary>
    /// The speed of sound in ft/s: 49.0223 √(T °R) for dry air, and with humidity the Owen-Cramer factor √((1 + 0.378 xw)(1 − 0.061 xw)), xw
    /// the mole fraction of water vapour. With no pressure or no humidity it is the dry value, as in the JavaScript.
    /// </summary>
    public static double SpeedOfSound(double temperatureF, double? pressureInHg, double? humidityPct)
    {
        double dry = 49.0223 * Math.Sqrt(temperatureF + 459.67);
        if (pressureInHg is not { } pressure || humidityPct is not { } humidity || humidity <= 0)
        {
            return dry;
        }

        double xw = humidity / 100 * SaturationVaporInHg(temperatureF) / pressure;
        return dry * Math.Sqrt((1 + (0.378 * xw)) * (1 - (0.061 * xw)));
    }

    /// <summary>Air density over ICAO standard density: (P/P0)(T0/T)(1 − 0.3783 Pv/P).</summary>
    public static double DensityRatio(double temperatureF, double pressureInHg, double humidityPct)
    {
        double pv = humidityPct / 100 * SaturationVaporInHg(temperatureF);
        return pressureInHg / StandardPressureInHg * ((StandardTemperatureF + 459.67) / (temperatureF + 459.67)) * (1 - (0.3783 * pv / pressureInHg));
    }

    /// <summary>
    /// Air density over the density of the atmosphere the coefficient is stated against, entry 110 section 2e. The drag constant carries ICAO
    /// density, so a coefficient stated against Army Standard Metro takes its ratio against Metro density, the density of Metro's own
    /// temperature, pressure and humidity by the same formula. At the same air that is about 1.8 percent more drag than the same number read as
    /// ICAO (docs/QUESTIONS-FOR-PLANNING.md question 24 section 3 records this reading of section 2e).
    /// </summary>
    public static double DensityRatio(double temperatureF, double pressureInHg, double humidityPct, ReferenceAtmosphere reference) =>
        reference == ReferenceAtmosphere.Icao
            ? DensityRatio(temperatureF, pressureInHg, humidityPct)
            : DensityRatio(temperatureF, pressureInHg, humidityPct) / DensityRatio(StandardTemperatureF, MetroPressureInHg, MetroHumidityPct);

    /// <summary>
    /// Station pressure in inHg at an altitude in the ICAO troposphere, P0 (1 − L h / T0)^5.2561. The JavaScript's version computed four further
    /// variables it never used; they are left out (entry 110 section 2a).
    /// </summary>
    public static double PressureFromAltitude(double altitudeFt) =>
        StandardPressureInHg * Math.Pow(1 - (LapseRateFPerFoot * altitudeFt / (StandardTemperatureF + 459.67)), 5.2561);
}
