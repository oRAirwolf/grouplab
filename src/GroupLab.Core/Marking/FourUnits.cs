using System.Globalization;

namespace GroupLab.Core.Marking;

/// <summary>One correction said four ways, plus the scope's own clicks where the rifle records them.</summary>
/// <param name="Moa">Minutes of angle, the unit most scopes here are marked in.</param>
/// <param name="Mil">Milliradians.</param>
/// <param name="Inches">At the distance shot.</param>
/// <param name="Centimetres">At the distance shot.</param>
/// <param name="Clicks">What to turn, where the scope's unit and click value are known.</param>
public sealed record InFourUnits(double Moa, double Mil, double Inches, double Centimetres, string? Clicks)
{
    /// <summary>How the screen writes each one, so the four rows of the table agree on their wording.</summary>
    public string Say(string unit) => unit switch
    {
        "moa" => string.Create(CultureInfo.InvariantCulture, $"{Moa:0.0} MOA"),
        "mil" => string.Create(CultureInfo.InvariantCulture, $"{Mil:0.00} mil"),
        "in" => string.Create(CultureInfo.InvariantCulture, $"{Inches:0.00} in"),
        "cm" => string.Create(CultureInfo.InvariantCulture, $"{Centimetres:0.0} cm"),
        _ => "",
    };
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 3.1: the zero correction in MOA, mil, inches and centimetres at once.
/// <para>
/// <b>Showing all four is not indecision, it is the honest answer.</b> A scope adjusts in one of two angular units and a person measures a
/// target in one of two linear ones, and which pair somebody thinks in depends on the scope they own and where they grew up. Making them
/// convert in their head, at the range, from a number they are about to dial, is where mistakes come from. The table costs four lines and
/// removes the arithmetic entirely.
/// </para>
/// <para>
/// Where the rifle records what its scope adjusts in, that unit is the headline and the clicks are spelled out, because "Up 8 clicks" is
/// what a person actually does. Where it does not, MOA leads, being the commoner marking on scopes this project has seen.
/// </para>
/// </summary>
public static class FourUnits
{
    /// <summary>One minute of angle at one hundred yards, in inches. The true value, not the 1 inch approximation.</summary>
    public const double MoaInchesPerHundredYards = 1.0471975511965976;

    /// <summary>One milliradian at one hundred yards, in inches.</summary>
    public const double MilInchesPerHundredYards = 3.5999999999999996;

    /// <summary>
    /// A correction of so many inches at a distance, said four ways.
    /// </summary>
    /// <param name="inches">How far to move the group, in inches, at the distance it was shot.</param>
    /// <param name="distanceYards">The distance the shots were fired at.</param>
    /// <param name="clicks">What the scope's own clicks say, where the rifle records them.</param>
    public static InFourUnits Of(double inches, double distanceYards, string? clicks = null)
    {
        if (distanceYards <= 0)
        {
            // Without a distance an angle cannot be worked out at all, and a wrong angle is worse than none: it would be dialled.
            return new InFourUnits(double.NaN, double.NaN, inches, inches * 2.54, clicks);
        }

        double hundreds = distanceYards / 100.0;
        return new InFourUnits(
            inches / (MoaInchesPerHundredYards * hundreds),
            inches / (MilInchesPerHundredYards * hundreds),
            inches,
            inches * 2.54,
            clicks);
    }

    /// <summary>
    /// Which unit leads, from what the rifle's scope records. The headline is the one a person will actually turn the turret in.
    /// </summary>
    public static string Headline(string? scopeUnits) =>
        scopeUnits?.Trim().ToLowerInvariant() switch
        {
            "mil" or "mrad" or "milliradian" or "milliradians" => "mil",
            "moa" or "minute" or "minutes" => "moa",
            _ => "moa",
        };

    /// <summary>The other three, in the order the table shows them beneath the headline.</summary>
    public static IReadOnlyList<string> Beneath(string headline) =>
        headline == "mil" ? ["moa", "in", "cm"] : ["mil", "in", "cm"];

    /// <summary>
    /// What to turn, in the scope's own clicks, or null where the rifle does not record a click value. Never guessed: a scope that adjusts
    /// in quarter minutes and one that adjusts in tenth mils are both common, and assuming either would send somebody the wrong distance.
    /// </summary>
    public static string? Clicks(double inches, double distanceYards, string? scopeUnits, double? clickValue)
    {
        if (clickValue is not { } value || value <= 0 || distanceYards <= 0)
        {
            return null;
        }

        var four = Of(inches, distanceYards);
        string unit = Headline(scopeUnits);
        double angular = unit == "mil" ? four.Mil : four.Moa;
        double clicks = Math.Abs(angular) / value;

        if (clicks < 0.5)
        {
            return "less than one click";
        }

        int whole = (int)Math.Round(clicks, MidpointRounding.AwayFromZero);
        string each = string.Create(CultureInfo.InvariantCulture, $"{value:0.##} {(unit == "mil" ? "mil" : "MOA")}");
        return string.Create(CultureInfo.InvariantCulture, $"{whole} click{(whole == 1 ? "" : "s")} at {each}");
    }
}
