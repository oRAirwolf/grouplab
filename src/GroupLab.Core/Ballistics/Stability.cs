namespace GroupLab.Core.Ballistics;

/// <summary>
/// The spin and Earth-rotation terms of reference/ballistics-js/ballistics.js that entry 110 section 2a ports: Miller's stability factor,
/// Litz's spin drift and the Coriolis horizontal term, and the Coriolis vertical term with its sign corrected (entry 111 section 2). Aerodynamic
/// jump is not here: the file's formula is not a published one (entry 110 section 2c).
/// </summary>
public static class Stability
{
    /// <summary>The Earth's rotation, rad/s.</summary>
    public const double EarthRotation = 7.2921e-5;

    /// <summary>
    /// Miller's gyroscopic stability factor, Don Miller's twist rule as published in Precision Shooting from 2005 and in wide use since:
    /// SG = 30 m / (t² d³ l (1 + l²)), m in grains, d in inches, t and l in calibers, corrected for velocity by (V / 2800)^(1/3) and for the
    /// atmosphere by ((T + 460) / 519) (29.92 / P), the standard form of Miller's atmospheric correction. The JavaScript applied the temperature factor only; Miller's correction scales with
    /// pressure as well, and entry 110 section 2e asks for it. At 29.92 inHg the two agree exactly.
    /// </summary>
    public static double MillerStability(double twistInches, double diameterInches, double lengthInches, double weightGrains, double velocityFps, double temperatureF, double pressureInHg = 29.92)
    {
        if (twistInches <= 0 || diameterInches <= 0 || lengthInches <= 0 || weightGrains <= 0)
        {
            return double.NaN;
        }

        double t = twistInches / diameterInches, l = lengthInches / diameterInches;
        double raw = 30 * weightGrains / (t * t * Math.Pow(diameterInches, 3) * l * (1 + (l * l)));
        return raw * Math.Pow(velocityFps / 2800, 1.0 / 3.0) * ((temperatureF + 460) / (59 + 460)) * (29.92 / pressureInHg);
    }

    /// <summary>
    /// Litz's spin drift fit, from Bryan Litz, Applied Ballistics for Long Range Shooting: 1.25 (SG + 1.2) TOF^1.83 inches, to the right for a
    /// right-hand twist (direction 1) and to the left for a left-hand one (−1). Zero where the stability factor is unknown.
    /// </summary>
    public static double SpinDriftInches(double stability, double timeOfFlight, int twistDirection)
    {
        if (!double.IsFinite(stability) || stability <= 0 || timeOfFlight <= 0)
        {
            return 0;
        }

        return twistDirection * 1.25 * (stability + 1.2) * Math.Pow(timeOfFlight, 1.83);
    }

    /// <summary>
    /// The Coriolis horizontal deflection in inches, Ω sin(latitude) × range × time of flight, positive to the right in the northern hemisphere
    /// whatever the direction of fire.
    /// </summary>
    public static double CoriolisHorizontalInches(double latitudeDegrees, double rangeFt, double timeOfFlight) =>
        EarthRotation * Math.Sin(latitudeDegrees * Math.PI / 180) * rangeFt * timeOfFlight * 12;

    /// <summary>
    /// The Coriolis vertical deflection, the Eötvös effect, in inches, positive up: half of 2ΩV cos(latitude) sin(azimuth) times t², V the
    /// average horizontal speed. In local east, north and up coordinates the Earth's rotation is Ω(0, cos φ, sin φ), and for a bullet moving
    /// east at speed V the Coriolis acceleration −2Ω × v has vertical component +2ΩV cos φ. So fire toward the east strikes high, toward the west
    /// low, and north or south not at all. ballistics.js had the sign reversed (docs/QUESTIONS-FOR-PLANNING.md question 24, entry 111 section 2).
    /// </summary>
    public static double CoriolisVerticalInches(double latitudeDegrees, double azimuthDegrees, double averageVelocityFps, double timeOfFlight) =>
        0.5 * 2 * EarthRotation * averageVelocityFps * Math.Cos(latitudeDegrees * Math.PI / 180) * Math.Sin(azimuthDegrees * Math.PI / 180) * timeOfFlight * timeOfFlight * 12;
}
