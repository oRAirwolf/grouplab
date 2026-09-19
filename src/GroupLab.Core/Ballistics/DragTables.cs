namespace GroupLab.Core.Ballistics;

/// <summary>The standard projectile a ballistic coefficient is stated against.</summary>
public enum DragModel
{
    /// <summary>The flat-base, two-caliber ogive projectile of the G1 function.</summary>
    G1,

    /// <summary>The boat-tail, secant-ogive long-range projectile of the G7 function.</summary>
    G7,
}

/// <summary>
/// The G1 and G7 drag functions as Mach and drag coefficient pairs, from the US Army Ballistic Research Laboratory, public domain, as
/// reference/ballistics-js/ballistics.js carries them (NOTES-FROM-PLANNING.md entry 110 section 2a). They were copied from that file by a
/// script, not retyped, and the transcription test compares every value. Between points the coefficient is interpolated linearly in Mach,
/// and outside the table it is the end value, as the JavaScript does.
/// </summary>
public static class DragTables
{
    public static IReadOnlyList<(double Mach, double Cd)> G1 { get; } =
    [
        (0.00, 0.2629), (0.05, 0.2558), (0.10, 0.2487), (0.15, 0.2413), (0.20, 0.2344), (0.25, 0.2278),
        (0.30, 0.2214), (0.35, 0.2155), (0.40, 0.2104), (0.45, 0.2061), (0.50, 0.2032), (0.55, 0.2020),
        (0.60, 0.2034), (0.70, 0.2165), (0.725, 0.2230), (0.75, 0.2313), (0.775, 0.2417), (0.80, 0.2546),
        (0.825, 0.2706), (0.85, 0.2912), (0.875, 0.3197), (0.90, 0.3605), (0.925, 0.4068), (0.95, 0.4519),
        (0.975, 0.4913), (1.00, 0.5210), (1.025, 0.5429), (1.05, 0.5599), (1.075, 0.5714), (1.10, 0.5786),
        (1.125, 0.5822), (1.15, 0.5832), (1.175, 0.5820), (1.20, 0.5790), (1.225, 0.5747), (1.25, 0.5694),
        (1.30, 0.5568), (1.35, 0.5432), (1.40, 0.5295), (1.45, 0.5163), (1.50, 0.5036), (1.55, 0.4916),
        (1.60, 0.4804), (1.65, 0.4699), (1.70, 0.4602), (1.75, 0.4512), (1.80, 0.4428), (1.85, 0.4350),
        (1.90, 0.4275), (1.95, 0.4205), (2.00, 0.4139), (2.05, 0.4077), (2.10, 0.4018), (2.15, 0.3962),
        (2.20, 0.3909), (2.25, 0.3858), (2.30, 0.3810), (2.35, 0.3763), (2.40, 0.3718), (2.45, 0.3675),
        (2.50, 0.3633), (2.60, 0.3553), (2.70, 0.3477), (2.80, 0.3405), (2.90, 0.3338), (3.00, 0.3274),
        (3.10, 0.3215), (3.20, 0.3159), (3.30, 0.3107), (3.40, 0.3058), (3.50, 0.3013), (3.60, 0.2970),
        (3.70, 0.2930), (3.80, 0.2893), (3.90, 0.2857), (4.00, 0.2823), (4.20, 0.2762), (4.40, 0.2707),
        (4.60, 0.2657), (4.80, 0.2612), (5.00, 0.2571),
    ];

    public static IReadOnlyList<(double Mach, double Cd)> G7 { get; } =
    [
        (0.00, 0.1198), (0.05, 0.1197), (0.10, 0.1196), (0.15, 0.1194), (0.20, 0.1193), (0.25, 0.1194),
        (0.30, 0.1194), (0.35, 0.1194), (0.40, 0.1193), (0.45, 0.1193), (0.50, 0.1194), (0.55, 0.1193),
        (0.60, 0.1194), (0.65, 0.1197), (0.70, 0.1202), (0.725, 0.1207), (0.75, 0.1215), (0.775, 0.1226),
        (0.80, 0.1242), (0.825, 0.1266), (0.85, 0.1306), (0.875, 0.1368), (0.90, 0.1464), (0.925, 0.1660),
        (0.95, 0.2054), (0.975, 0.2993), (1.00, 0.3803), (1.025, 0.4015), (1.05, 0.4043), (1.075, 0.4034),
        (1.10, 0.4014), (1.125, 0.3987), (1.15, 0.3955), (1.20, 0.3884), (1.25, 0.3810), (1.30, 0.3732),
        (1.35, 0.3657), (1.40, 0.3580), (1.50, 0.3440), (1.55, 0.3376), (1.60, 0.3315), (1.65, 0.3260),
        (1.70, 0.3209), (1.75, 0.3160), (1.80, 0.3117), (1.85, 0.3078), (1.90, 0.3042), (1.95, 0.3010),
        (2.00, 0.2980), (2.05, 0.2951), (2.10, 0.2922), (2.15, 0.2892), (2.20, 0.2864), (2.25, 0.2835),
        (2.30, 0.2807), (2.35, 0.2779), (2.40, 0.2752), (2.45, 0.2725), (2.50, 0.2697), (2.55, 0.2670),
        (2.60, 0.2643), (2.65, 0.2615), (2.70, 0.2588), (2.75, 0.2561), (2.80, 0.2533), (2.85, 0.2506),
        (2.90, 0.2479), (2.95, 0.2451), (3.00, 0.2424), (3.50, 0.2157), (4.00, 0.1920), (4.50, 0.1710),
        (5.00, 0.1523),
    ];

    public static IReadOnlyList<(double Mach, double Cd)> For(DragModel model) => model == DragModel.G1 ? G1 : G7;

    /// <summary>The drag coefficient of the standard projectile at a Mach number, linear between table points.</summary>
    public static double Cd(double mach, DragModel model)
    {
        var table = For(model);
        if (mach <= table[0].Mach)
        {
            return table[0].Cd;
        }

        if (mach >= table[^1].Mach)
        {
            return table[^1].Cd;
        }

        for (int i = 0; i < table.Count - 1; i++)
        {
            if (mach >= table[i].Mach && mach <= table[i + 1].Mach)
            {
                double fraction = (mach - table[i].Mach) / (table[i + 1].Mach - table[i].Mach);
                return table[i].Cd + (fraction * (table[i + 1].Cd - table[i].Cd));
            }
        }

        return table[^1].Cd;
    }
}
