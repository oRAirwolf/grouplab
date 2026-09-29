using System.Globalization;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Marking;

/// <summary>A length at the target, as the screen shows it.</summary>
public enum LinearUnit
{
    Inch,
    Centimetre,
    Millimetre,
}

/// <summary>The shot distance, as the screen shows it.</summary>
public enum DistanceUnit
{
    Yard,
    Metre,
}

/// <summary>
/// The application's unit setting, NOTES-FROM-PLANNING.md entry 25 section 1: three axes that vary independently, linear, angular and
/// distance. It changes display only and never storage. Every length is stored canonically in inches at the target plane, and the shot
/// distance in inches too (docs/STATISTICS.md section 13), and converted here at the edge, so a marking file means the same thing
/// whoever opens it.
/// <para>
/// The angular choices are MOA, mil and SMOA. "Mil" is the milliradian, the unit a mil turret is marked in, and not the 6400 NATO mil,
/// which is 1.8 percent different: entry 25 warns that a turret dialled in the wrong angular unit sends the next group elsewhere.
/// Angular figures follow section 12.5's half-angle form and are absent, with the reason, until a shot distance is set.
/// </para>
/// </summary>
public sealed record UnitSettings(LinearUnit Linear, AngularUnit Angular, DistanceUnit Distance)
{
    public static UnitSettings Imperial { get; } = new(LinearUnit.Inch, AngularUnit.Moa, DistanceUnit.Yard);

    public static UnitSettings Metric { get; } = new(LinearUnit.Centimetre, AngularUnit.Mrad, DistanceUnit.Metre);

    /// <summary>
    /// The angular units offered, in the order the screen lists them: mil first, then MOA, entry 294 section 1 ("Scope unit: mil / MOA"),
    /// and SMOA for the scopes marked in inches per hundred yards.
    /// </summary>
    public static IReadOnlyList<AngularUnit> AngularChoices { get; } = [AngularUnit.Mrad, AngularUnit.Moa, AngularUnit.Smoa];

    /// <summary>
    /// The angle unit before anybody has said which their scope is, entry 294 section 1: the first run asks, on both platforms, and nothing
    /// is guessed from the region any more. The region guess started almost every American in MOA, which is what made a mil shooter feel
    /// like an afterthought; mil is the unit most competition scopes are marked in, and it lasts only until the question is answered.
    /// </summary>
    public const AngularUnit Unanswered = AngularUnit.Mrad;

    /// <summary>
    /// The units a session aims in, entry 294 section 1: where the session names a rifle, its scope's unit decides every aiming figure (the
    /// zero correction, the clicks, the dope, the hit chance, group sizes as angles), whatever Settings says; where it names none, Settings
    /// decides. Only the angle changes; lengths and distances stay as chosen. A figure a person tapped to another unit keeps that unit.
    /// </summary>
    public UnitSettings Aiming(Rifle? rifle) =>
        rifle is not null && ScopeUnit(rifle.ClickUnit) is { } scope && scope != Angular ? this with { Angular = scope } : this;

    /// <summary>Entry 294 section 1: the first run's question, word for word, on both platforms.</summary>
    public const string ScopeQuestion = "Is your scope in mil or MOA?";

    /// <summary>The first run's third answer, for somebody with rifles of each.</summary>
    public const string ScopeBoth = "Both, I have rifles of each";

    /// <summary>What the answer changes, said under the question.</summary>
    public const string ScopeQuestionSays = "GroupLab then works in it everywhere you aim: the zero correction, the clicks, the dope and the hit chance. Each rifle keeps its own scope unit, and the other unit is always a tap away.";

    /// <summary>The top of Units in Settings, entry 294 section 1.</summary>
    public const string ScopeUnitLabel = "Scope unit: mil / MOA";

    /// <summary>The one line under it saying what it changes.</summary>
    public const string ScopeUnitSays = "Everything you aim with follows it: the zero correction, clicks, Shots Needed to Zero, the dope, the hit chance and group sizes as angles. A session that names a rifle follows that rifle's scope instead.";

    /// <summary>A scope's unit as one of <see cref="AngularChoices"/>, or null for one no scope is marked in.</summary>
    public static AngularUnit? ScopeUnit(AngularUnit unit) => AngularChoices.Contains(unit) ? unit : null;

    /// <summary>
    /// The units the analysis page reads in, entry 131 section 3.2: the page's own choice where a person has made one, and the application's
    /// settings where they have not.
    /// <para>
    /// This is a view and never a change to what is stored. A marking holds inches because that is what it was measured in; somebody
    /// switching the page to centimetres is asking to read it differently. Keeping those apart is what lets one session be read either way
    /// by two people without either of them altering it.
    /// </para>
    /// </summary>
    public static UnitSettings ForAnalysis(string? chosen, UnitSettings settings) =>
        chosen?.Trim().ToLowerInvariant() switch
        {
            // Entry 294: the page's toggle is inches or centimeters; the angle stays the scope's, which Settings or the rifle says.
            "imperial" => Imperial with { Angular = settings.Angular },
            "metric" => Metric with { Angular = settings.Angular },
            _ => settings,
        };

    /// <summary>What the toggle switches to from here, so pressing it twice returns to where it started.</summary>
    public static string Other(string? chosen, UnitSettings settings)
    {
        var showing = ForAnalysis(chosen, settings);
        return showing.Linear == LinearUnit.Inch ? "metric" : "imperial";
    }

    /// <summary>The sentence the screen shows where an angular figure would be without a shot distance.</summary>
    public const string AngularNeedsDistance = "angular figures need the shot distance";

    /// <summary>
    /// The first-run default from the system's region: inches and yards in the United States, Liberia and Myanmar, the three countries that
    /// have not adopted the metric system officially, and centimetres and metres everywhere else. It is only a starting point; the choice is
    /// remembered once made. Entry 294 section 1: the angle is never guessed from the region; it is <see cref="Unanswered"/> until the first
    /// run's "Is your scope in mil or MOA?" is answered.
    /// </summary>
    public static UnitSettings ForRegion(string? twoLetterRegion) =>
        (twoLetterRegion?.ToUpperInvariant() is "US" or "LR" or "MM" ? Imperial : Metric) with { Angular = Unanswered };

    public static double FromInches(double inches, LinearUnit unit) => unit switch
    {
        LinearUnit.Centimetre => inches * 2.54,
        LinearUnit.Millimetre => inches * 25.4,
        _ => inches,
    };

    public static double ToInches(double value, LinearUnit unit) => unit switch
    {
        LinearUnit.Centimetre => value / 2.54,
        LinearUnit.Millimetre => value / 25.4,
        _ => value,
    };

    public static double DistanceFromInches(double inches, DistanceUnit unit) => unit == DistanceUnit.Metre ? inches * 0.0254 : inches / 36;

    public static double DistanceToInches(double value, DistanceUnit unit) => unit == DistanceUnit.Metre ? value / 0.0254 : value * 36;

    public static string Symbol(LinearUnit unit) => unit switch
    {
        LinearUnit.Centimetre => "cm",
        LinearUnit.Millimetre => "mm",
        _ => "in",
    };

    public static string Symbol(DistanceUnit unit) => unit == DistanceUnit.Metre ? "m" : "yd";

    public static string Symbol(AngularUnit unit) => unit switch
    {
        AngularUnit.Moa => "MOA",
        AngularUnit.Mrad => "mil",
        AngularUnit.Smoa => "SMOA",
        AngularUnit.Mil => "NATO mil",
        AngularUnit.Degree => "deg",
        _ => "rad",
    };

    /// <summary>The number of decimals a length is shown to: a thousandth of an inch, and the nearest equivalent in the metric units.</summary>
    public int LinearDecimals => Linear switch
    {
        LinearUnit.Centimetre => 2,
        LinearUnit.Millimetre => 1,
        _ => 3,
    };

    /// <summary>A length in the chosen unit, without its symbol.</summary>
    public string Number(double inches) => FromInches(inches, Linear).ToString("F" + LinearDecimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    /// <summary>A length in the chosen unit, with its symbol.</summary>
    public string Length(double inches) => Number(inches) + " " + Symbol(Linear);

    /// <summary>A shot distance in the chosen unit, with its symbol.</summary>
    public string DistanceText(double inches) => DistanceFromInches(inches, Distance).ToString("0.#", CultureInfo.InvariantCulture) + " " + Symbol(Distance);

    /// <summary>
    /// A speed in the person's units: feet a second where distances are in yards, metres a second where they are in metres. A muzzle velocity
    /// is read beside a distance far more often than beside a group size, so it follows the distance unit rather than the linear one.
    /// </summary>
    public string Speed(double feetPerSecond) =>
        (Distance == DistanceUnit.Metre ? feetPerSecond * 0.3048 : feetPerSecond).ToString("0", CultureInfo.InvariantCulture)
        + (Distance == DistanceUnit.Metre ? " m/s" : " ft/s");

    /// <summary>
    /// A difference between two speeds: a velocity SD or an extreme spread, in the same unit as <see cref="Speed"/> but to one decimal.
    /// A muzzle velocity of 2710.4 ft/s and one of 2710 are the same shot, so <see cref="Speed"/> rounds; an SD of 10.4 ft/s rounded to 10
    /// throws away a tenth of the thing being reported.
    /// </summary>
    public string SpeedDifference(double feetPerSecond) =>
        (Distance == DistanceUnit.Metre ? feetPerSecond * 0.3048 : feetPerSecond).ToString("0.0", CultureInfo.InvariantCulture)
        + (Distance == DistanceUnit.Metre ? " m/s" : " ft/s");

    /// <summary>A length at the target as an angle at the shot distance, or null without one.</summary>
    public double? Angle(double inches, double? distanceInches) =>
        distanceInches is { } d && d > 0 ? Statistics.Angular.ToAngle(inches, d, 1, Angular) : null;

    /// <summary>A length at the target in one named angular unit, or null without a shot distance: the zero block shows MOA and mil both.</summary>
    public static double? AngleIn(double inches, double? distanceInches, AngularUnit unit) =>
        distanceInches is { } d && d > 0 ? Statistics.Angular.ToAngle(inches, d, 1, unit) : null;

    /// <summary>A length at the target as an angle with its symbol, or null without a shot distance.</summary>
    public string? AngleText(double inches, double? distanceInches) =>
        Angle(inches, distanceInches) is { } angle ? angle.ToString("0.00", CultureInfo.InvariantCulture) + " " + Symbol(Angular) : null;
}
