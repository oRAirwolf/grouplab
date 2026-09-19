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
/// The G1 and G7 drag functions as Mach and drag coefficient pairs: the US Army Ballistic Research Laboratory's standard functions, public
/// domain, as McCoy tabulates them in Modern Exterior Ballistics appendix A. Between points the coefficient is interpolated linearly in Mach,
/// and outside the table it is the end value.
/// <para>
/// <b>The source rule, NOTES-FROM-PLANNING.md entry 111 section 1.</b> Neither McCoy nor the BRL report was to hand, so the values carried are
/// those on which two independent transcriptions agree. They are committed in reference/drag-tables:
/// <list type="bullet">
/// <item>py-ballisticcalc 2.3.1's drag_tables.py;</item>
/// <item>poncelet's src/drag_tables.cpp, taken from JBM Ballistics' mcg1.txt and mcg7.txt.</item>
/// </list>
/// They agree at every one of the 79 G1 and 84 G7 points to the four decimals both publish, and a test holds this table to both. These
/// tables were generated from that file by a script, not retyped.
/// </para>
/// <para>
/// <b>Not ballistics.js's G1 table, on purpose.</b> That table is not the standard G1 function above Mach 0.85: 0.5210 where the standard is
/// 0.4805 at Mach 1.0, and half the standard at Mach 5 (docs/QUESTIONS-FOR-PLANNING.md question 25). Its G7 table agrees with the standard
/// below Mach 3.5 and carries fewer points. Both are kept here only as <see cref="JavaScriptG1"/> and <see cref="JavaScriptG7"/>, for the
/// transcription check of the port against the file; the solver never flies them. Do not restore them as the tables.
/// </para>
/// </summary>
public static class DragTables
{
    public static IReadOnlyList<(double Mach, double Cd)> G1 { get; } =
    [
        (0.00, 0.2629), (0.05, 0.2558), (0.10, 0.2487), (0.15, 0.2413), (0.20, 0.2344), (0.25, 0.2278),
        (0.30, 0.2214), (0.35, 0.2155), (0.40, 0.2104), (0.45, 0.2061), (0.50, 0.2032), (0.55, 0.2020),
        (0.60, 0.2034), (0.70, 0.2165), (0.725, 0.2230), (0.75, 0.2313), (0.775, 0.2417), (0.80, 0.2546),
        (0.825, 0.2706), (0.85, 0.2901), (0.875, 0.3136), (0.90, 0.3415), (0.925, 0.3734), (0.95, 0.4084),
        (0.975, 0.4448), (1.0, 0.4805), (1.025, 0.5136), (1.05, 0.5427), (1.075, 0.5677), (1.10, 0.5883),
        (1.125, 0.6053), (1.15, 0.6191), (1.20, 0.6393), (1.25, 0.6518), (1.30, 0.6589), (1.35, 0.6621),
        (1.40, 0.6625), (1.45, 0.6607), (1.50, 0.6573), (1.55, 0.6528), (1.60, 0.6474), (1.65, 0.6413),
        (1.70, 0.6347), (1.75, 0.6280), (1.80, 0.6210), (1.85, 0.6141), (1.90, 0.6072), (1.95, 0.6003),
        (2.00, 0.5934), (2.05, 0.5867), (2.10, 0.5804), (2.15, 0.5743), (2.20, 0.5685), (2.25, 0.5630),
        (2.30, 0.5577), (2.35, 0.5527), (2.40, 0.5481), (2.45, 0.5438), (2.50, 0.5397), (2.60, 0.5325),
        (2.70, 0.5264), (2.80, 0.5211), (2.90, 0.5168), (3.00, 0.5133), (3.10, 0.5105), (3.20, 0.5084),
        (3.30, 0.5067), (3.40, 0.5054), (3.50, 0.5040), (3.60, 0.5030), (3.70, 0.5022), (3.80, 0.5016),
        (3.90, 0.5010), (4.00, 0.5006), (4.20, 0.4998), (4.40, 0.4995), (4.60, 0.4992), (4.80, 0.4990),
        (5.00, 0.4988),
    ];

    public static IReadOnlyList<(double Mach, double Cd)> G7 { get; } =
    [
        (0.00, 0.1198), (0.05, 0.1197), (0.10, 0.1196), (0.15, 0.1194), (0.20, 0.1193), (0.25, 0.1194),
        (0.30, 0.1194), (0.35, 0.1194), (0.40, 0.1193), (0.45, 0.1193), (0.50, 0.1194), (0.55, 0.1193),
        (0.60, 0.1194), (0.65, 0.1197), (0.70, 0.1202), (0.725, 0.1207), (0.75, 0.1215), (0.775, 0.1226),
        (0.80, 0.1242), (0.825, 0.1266), (0.85, 0.1306), (0.875, 0.1368), (0.90, 0.1464), (0.925, 0.1660),
        (0.95, 0.2054), (0.975, 0.2993), (1.0, 0.3803), (1.025, 0.4015), (1.05, 0.4043), (1.075, 0.4034),
        (1.10, 0.4014), (1.125, 0.3987), (1.15, 0.3955), (1.20, 0.3884), (1.25, 0.3810), (1.30, 0.3732),
        (1.35, 0.3657), (1.40, 0.3580), (1.50, 0.3440), (1.55, 0.3376), (1.60, 0.3315), (1.65, 0.3260),
        (1.70, 0.3209), (1.75, 0.3160), (1.80, 0.3117), (1.85, 0.3078), (1.90, 0.3042), (1.95, 0.3010),
        (2.00, 0.2980), (2.05, 0.2951), (2.10, 0.2922), (2.15, 0.2892), (2.20, 0.2864), (2.25, 0.2835),
        (2.30, 0.2807), (2.35, 0.2779), (2.40, 0.2752), (2.45, 0.2725), (2.50, 0.2697), (2.55, 0.2670),
        (2.60, 0.2643), (2.65, 0.2615), (2.70, 0.2588), (2.75, 0.2561), (2.80, 0.2533), (2.85, 0.2506),
        (2.90, 0.2479), (2.95, 0.2451), (3.00, 0.2424), (3.10, 0.2368), (3.20, 0.2313), (3.30, 0.2258),
        (3.40, 0.2205), (3.50, 0.2154), (3.60, 0.2106), (3.70, 0.2060), (3.80, 0.2017), (3.90, 0.1975),
        (4.00, 0.1935), (4.20, 0.1861), (4.40, 0.1793), (4.60, 0.1730), (4.80, 0.1672), (5.00, 0.1618),
    ];

    /// <summary>ballistics.js's own G1 table, for the transcription check alone: not the standard function above Mach 0.85.</summary>
    public static IReadOnlyList<(double Mach, double Cd)> JavaScriptG1 { get; } =
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

    /// <summary>ballistics.js's own G7 table, for the transcription check alone.</summary>
    public static IReadOnlyList<(double Mach, double Cd)> JavaScriptG7 { get; } =
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

    public static IReadOnlyList<(double Mach, double Cd)> For(DragModel model, bool javaScript = false) =>
        javaScript ? (model == DragModel.G1 ? JavaScriptG1 : JavaScriptG7) : (model == DragModel.G1 ? G1 : G7);

    /// <summary>The drag coefficient of the standard projectile at a Mach number, linear between table points.</summary>
    public static double Cd(double mach, DragModel model, bool javaScript = false)
    {
        var table = For(model, javaScript);
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
