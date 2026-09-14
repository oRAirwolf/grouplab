namespace GroupLab.Core.Statistics;

/// <summary>The angular units of docs/STATISTICS.md section 12.5.</summary>
public enum AngularUnit
{
    Degree,
    Radian,

    /// <summary>True minute of angle, 1.047 in at 100 yd, DESIGN.md section 14's default.</summary>
    Moa,

    /// <summary>Shooter's minute of angle, inches per hundred yards, 1 in at 100 yd exactly.</summary>
    Smoa,

    /// <summary>Milliradian.</summary>
    Mrad,

    /// <summary>NATO mil, 6400 to the circle.</summary>
    Mil,
}

/// <summary>
/// Linear size at the target to angle and back, docs/STATISTICS.md section 12.5: the half-angle form shotGroups uses,
/// angle = k atan(x / 2d), not the small-angle approximation, because the two differ at short distances and matching removes
/// a whole class of validation mismatch. Section 13: an angular value needs one known distance, and a group with none, or with
/// several, has no angular value at all.
/// </summary>
public static class Angular
{
    /// <summary>The constant k of section 12.5's table.</summary>
    public static double Constant(AngularUnit unit) => unit switch
    {
        AngularUnit.Degree => 360 / Math.PI,
        AngularUnit.Radian => 2,
        AngularUnit.Moa => 21600 / Math.PI,
        AngularUnit.Smoa => 1 / Math.Atan(1.0 / 7200),
        AngularUnit.Mrad => 2000,
        AngularUnit.Mil => 6400 / Math.PI,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null),
    };

    /// <param name="linear">A size at the target, in the shots' unit.</param>
    /// <param name="distance">The distance to the target, in its own unit.</param>
    /// <param name="shotUnitsPerDistanceUnit">How many shot units one distance unit holds: 36 for yards and inches, 100 for metres and centimetres.</param>
    public static double ToAngle(double linear, double distance, double shotUnitsPerDistanceUnit, AngularUnit unit) =>
        Constant(unit) * Math.Atan(linear / (2 * distance * shotUnitsPerDistanceUnit));

    /// <summary>The size at the target that subtends <paramref name="angle"/>, the inverse of <see cref="ToAngle"/>.</summary>
    public static double FromAngle(double angle, double distance, double shotUnitsPerDistanceUnit, AngularUnit unit) =>
        2 * distance * shotUnitsPerDistanceUnit * Math.Tan(angle / Constant(unit));
}
