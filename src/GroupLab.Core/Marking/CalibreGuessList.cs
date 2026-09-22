namespace GroupLab.Core.Marking;

/// <summary>What kind of firearm a rifle record is for, which decides the list a calibre is guessed from.</summary>
public enum FirearmType
{
    Rifle,
    Pistol,
}

/// <summary>
/// The diameters GroupLab may guess at, Alan's own two lists.
/// <para>
/// <b>This is not the list a person may choose from.</b> <see cref="Calibre.Common"/> is that, and anything at all can still be typed by
/// hand. This one exists because a guess should land on something somebody actually shoots: reading 0.2371 in off a sheet and offering
/// "0.237 in" is arithmetic pretending to be knowledge, where offering .243 with .224 beside it is a question a shooter can answer.
/// </para>
/// </summary>
public static class CalibreGuessList
{
    /// <summary>The rifle diameters, smallest first. 0.222 is the .22 rimfire, which is why it carries its own name.</summary>
    public static IReadOnlyList<double> Rifle { get; } =
        [0.172, 0.204, 0.222, 0.224, 0.243, 0.257, 0.264, 0.277, 0.284, 0.308, 0.338, 0.375, 0.416, 0.458, 0.510];

    /// <summary>The pistol diameters, smallest first.</summary>
    public static IReadOnlyList<double> Pistol { get; } = [0.312, 0.355, 0.400, 0.410, 0.430, 0.451, 0.500];

    public static IReadOnlyList<double> For(FirearmType firearm) => firearm == FirearmType.Pistol ? Pistol : Rifle;

    /// <summary>
    /// What one of these is called: the diameter in both units as everything else shows it, and for 0.222 the name a shooter uses, because
    /// "0.222 in" is not what anybody calls a box of rimfire.
    /// </summary>
    public static string Name(double inches) =>
        Math.Abs(inches - 0.222) < 1e-9 ? "22LR, " + Calibre.Shown(inches) : Calibre.Shown(inches);

    public static Calibre Of(double inches) => new(Name(inches), inches);

    /// <summary>The listed diameter nearest a reading, which is what a guess snaps to.</summary>
    public static Calibre Nearest(double inches, FirearmType firearm) =>
        Of(For(firearm).MinBy(d => Math.Abs(d - inches)));

    /// <summary>
    /// Every diameter on either list, for the neighbours a guess offers beside its preselection.
    /// <para>
    /// <b>The preselection respects the firearm type and the neighbours do not, on purpose.</b> Most of the pairs that cannot be told apart
    /// straddle the two lists: .308 against .312, .451 against .458, .500 against .510. Hiding the other half because a record says "rifle"
    /// would state a calibre as measured on evidence that does not separate it, which is the one thing this must never do, and the firearm
    /// type is a field somebody may simply not have set.
    /// </para>
    /// </summary>
    public static IReadOnlyList<double> Either { get; } = [.. Rifle.Concat(Pistol).Distinct().Order()];
}
