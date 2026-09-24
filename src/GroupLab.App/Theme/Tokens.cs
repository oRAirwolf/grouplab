using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace GroupLab.App.Theme;

/// <summary>One theme's colours by role, NOTES-FROM-PLANNING.md entry 42 section 2.</summary>
public sealed record Palette(
    Color Bg,
    Color Panel,
    Color Panel2,
    Color Sunk,
    Color Line,
    Color Line2,
    Color Text,
    Color Dim,
    Color Faint,
    Color Amber,
    Color Teal,
    Color Alert,
    Color OnAmber,
    Color AmberTint,
    Color AmberTintBorder,
    Color TealTint,
    Color TealTintBorder,
    Color FieldBg,
    Color FieldBorder,
    Color FocusRing,
    Color Disabled,
    Color WarningText,
    Color ErrorText,
    Color MarkRing,
    Color MarkAmber,
    Color MarkWord)
{
    /// <summary>
    /// The mark's brand roles, question 20 answered by NOTES-FROM-PLANNING.md entry 106 section 2: the rings, the holes with LAB, and GROUP,
    /// at the values of the committed mark files, which <c>ThemeTests</c> holds them to. They exist for the mark and nothing else, and are held
    /// to 3:1 for a graphic at header size, not to the body-text ratio; light <see cref="Amber"/>, tuned for text, differs from the mark's
    /// amber on purpose.
    /// </summary>
    public IReadOnlyList<(string Role, Color Colour)> MarkColours => [("mark ring", MarkRing), ("mark amber", MarkAmber), ("mark word", MarkWord)];

    /// <summary>The colours text is set in.</summary>
    public IReadOnlyList<(string Role, Color Colour)> TextColours => [("text", Text), ("dim", Dim), ("faint", Faint), ("amber", Amber), ("teal", Teal), ("alert", Alert), ("warning", WarningText), ("error", ErrorText)];

    /// <summary>
    /// The form roles, NOTES-FROM-PLANNING.md entry 100 section 1: the concept shows no form, so the parametric editor needed roles the token
    /// set had never had to produce. Each is here in every palette rather than chosen on the one screen that first needed it. Warning text
    /// is amber and error text red, the meanings entry 93 fixed: a warning is a decision for the person, a refusal is something wrong.
    /// </summary>
    public IReadOnlyList<(string Role, Color Colour)> FormEdges => [("field border", FieldBorder), ("focus ring", FocusRing)];

    /// <summary>The surfaces text is set on: the window, the panels, and raised surfaces such as buttons. The sunk image area carries marks, not text.</summary>
    public IReadOnlyList<(string Role, Color Colour)> TextSurfaces => [("bg", Bg), ("panel", Panel), ("panel2", Panel2), ("field", FieldBg)];
}

/// <summary>
/// Every colour, size and face the application uses, NOTES-FROM-PLANNING.md entry 42 sections 2 to 5: the concept screens' values in code,
/// two palettes resolved by theme variant. No colour literal appears anywhere else in the application, which is what makes the light theme
/// possible, and <c>ThemeTests</c> fails if one does.
/// <para>
/// Entry 42 section 2 asks that every text colour reach 4.5:1 against the surfaces it sits on, adjusting a value rather than a role where
/// one falls short. The concept screens' values fall short for dark <c>faint</c> #697079 and <c>alert</c> #e0604a, and for light
/// <c>faint</c> #868c94, <c>amber</c> #b46f16, <c>teal</c> #3f8873 and <c>alert</c> #bf4531. Each is moved along its own hue, toward
/// white in the dark theme and toward black in the light, by the smallest step that reaches 4.5:1 on bg, panel and panel2. The light
/// primary button's text is white, because #17120a on the adjusted amber is 3.4:1. The light selection and good-state tints are amber and
/// teal nine tenths of the way to white, which entry 42 leaves unstated.
/// </para>
/// </summary>
public static class Tokens
{
    public static Palette Dark { get; } = new(
        Bg: Hex(0x131417),
        Panel: Hex(0x1a1c20),
        Panel2: Hex(0x212429),
        Sunk: Hex(0x0f1013),
        Line: Hex(0x2c3037),
        Line2: Hex(0x3a3f47),
        Text: Hex(0xe6e8ea),
        Dim: Hex(0x9aa1a9),
        Faint: Hex(0x858b92),
        Amber: Hex(0xe0912f),
        Teal: Hex(0x6fbfa8),
        Alert: Hex(0xe1634d),
        OnAmber: Hex(0x17120a),
        AmberTint: Hex(0x221c12),
        AmberTintBorder: Hex(0x3a2d18),
        TealTint: Hex(0x141f1c),
        TealTintBorder: Hex(0x2c463f),
        FieldBg: Hex(0x15171b),
        FieldBorder: Hex(0x6f757e),
        FocusRing: Hex(0xe0912f),
        Disabled: Hex(0x7c828b),
        WarningText: Hex(0xe0912f),
        ErrorText: Hex(0xe1634d),
        MarkRing: Hex(0x6b727b),
        MarkAmber: Hex(0xe0912f),
        MarkWord: Hex(0x8a9199));

    public static Palette Light { get; } = new(
        Bg: Hex(0xf4f3f0),
        Panel: Hex(0xffffff),
        Panel2: Hex(0xeceae4),
        Sunk: Hex(0xe4e2dd),
        Line: Hex(0xd3d0c9),
        Line2: Hex(0xbdb9b0),
        Text: Hex(0x1a1c20),
        Dim: Hex(0x5a6068),
        Faint: Hex(0x666a70),
        Amber: Hex(0x965d12),
        Teal: Hex(0x367462),
        Alert: Hex(0xb8422f),
        OnAmber: Hex(0xffffff),
        AmberTint: Hex(0xf4efe7),
        AmberTintBorder: Hex(0xd0b694),
        TealTint: Hex(0xebf1ef),
        TealTintBorder: Hex(0xa5c0b8),
        FieldBg: Hex(0xffffff),
        FieldBorder: Hex(0x80858c),
        FocusRing: Hex(0x965d12),
        Disabled: Hex(0x868b92),
        WarningText: Hex(0x965d12),
        ErrorText: Hex(0xb8422f),
        MarkRing: Hex(0x8f8b83),
        MarkAmber: Hex(0xa9660f),
        MarkWord: Hex(0x6f6b64));

    /// <summary>
    /// High contrast, NOTES-FROM-PLANNING.md entry 93 section 3. It is not a fourth set of hand-picked values: it is <see cref="Dark"/> put
    /// through <see cref="Sharpen"/>, which keeps every hue and lifts every text colour until it reaches <see cref="HighContrastRatio"/>
    /// against the surfaces it sits on, over a black window and near-black panels, with the separators taken to a visible grey.
    /// <para>
    /// The entry's guard is the reason it is derived rather than drawn: <b>if a token set cannot produce the other themes, it is not a token
    /// set, it is a dark theme with names on it.</b> Deriving this one proves the roles carry meaning rather than values, and it is why the
    /// concept's approval could be taken as a design language rather than as one screenshot.
    /// </para>
    /// </summary>
    public static Palette HighContrast { get; } = Sharpen(Dark);

    /// <summary>What every text colour must reach against its surfaces in the high contrast theme: WCAG's AAA ratio for body text.</summary>
    public const double HighContrastRatio = 7.0;

    /// <summary>
    /// What a control's edge must reach against what it sits on: WCAG's 3:1 for user-interface components, and 4.5:1 in high contrast, which
    /// has no AAA figure for edges and takes the next step up. A disabled control's text is held to the same floor, because WCAG exempts it
    /// from contrast and a control nobody can see is not disabled, it is missing.
    /// </summary>
    public const double EdgeRatio = 3.0, HighContrastEdgeRatio = 4.5;

    /// <summary>The theme variant the high contrast palette answers to. It inherits from dark, so any control Avalonia styles itself stays dark rather than turning light.</summary>
    public static ThemeVariant HighContrastVariant { get; } = new("GroupLabHighContrast", ThemeVariant.Dark);

    /// <summary>The palette for a resolved theme variant. Follow-system resolves to one of the three before it reaches here.</summary>
    /// <summary>
    /// The composite plot's inks, NOTES-FROM-PLANNING.md entry 169 section 3: paper, one ink for every mark, a ring grey that still reads in
    /// daylight, and one accent for the group centre, the extreme spread and a picked shot. The first outside user found the plot's pastel
    /// rings and faint outlines hard to read, and the lesson he took from another program was its contrast. The dark version is as stark as
    /// the light one, white on black, not a dimmed copy. ThemeTests holds each ink to its ratio on the paper.
    /// </summary>
    public static PlotInks Plot(ThemeVariant? variant) => variant == ThemeVariant.Light
        ? new PlotInks(Hex(0xffffff), Hex(0x000000), Hex(0x4d4d4d), Hex(0xc8102e))
        : new PlotInks(Hex(0x0a0a0a), Hex(0xffffff), Hex(0xb3b3b3), Hex(0xff5a5f));

    public static Palette For(ThemeVariant? variant) =>
        variant == HighContrastVariant ? HighContrast : variant == ThemeVariant.Light ? Light : Dark;

    /// <summary>
    /// One palette pushed to <see cref="HighContrastRatio"/>: the window goes black and the panels nearly so, the separators go to a grey
    /// that can be seen, and every text and accent colour is lifted along its own hue until it clears the ratio on every surface it is set
    /// on. Nothing here invents a colour; it moves the ones the concept chose.
    /// </summary>
    private static Palette Sharpen(Palette p)
    {
        var bg = Hex(0x000000);
        var panel = Hex(0x090a0c);
        var panel2 = Hex(0x141619);
        Color Lift(Color colour) => Toward(colour, Hex(0xffffff), [bg, panel, panel2], HighContrastRatio);
        return p with
        {
            Bg = bg,
            Panel = panel,
            Panel2 = panel2,
            Sunk = bg,
            Line = Hex(0x6d747c),
            Line2 = Hex(0x99a1aa),
            Text = Hex(0xffffff),
            Dim = Lift(p.Dim),
            Faint = Lift(p.Faint),
            Amber = Lift(p.Amber),
            Teal = Lift(p.Teal),
            Alert = Lift(p.Alert),
            OnAmber = Hex(0x000000),
            AmberTint = bg,
            AmberTintBorder = Lift(p.Amber),
            TealTint = bg,
            TealTintBorder = Lift(p.Teal),
            FieldBg = bg,
            FieldBorder = Toward(p.FieldBorder, Hex(0xffffff), [bg, panel, panel2], HighContrastEdgeRatio),
            FocusRing = Lift(p.FocusRing),
            Disabled = Toward(p.Disabled, Hex(0xffffff), [bg, panel, panel2], HighContrastEdgeRatio),
            WarningText = Lift(p.WarningText),
            ErrorText = Lift(p.ErrorText),
        };
    }

    /// <summary>
    /// A colour moved along the straight line toward <paramref name="target"/> by the smallest step that reaches <paramref name="ratio"/>
    /// against every one of <paramref name="surfaces"/>, in a hundredth-of-the-way steps. It is the same method entry 42 used by hand for
    /// the colours the concept's own values left short of 4.5:1, written down so the next palette does not need a person with a calculator.
    /// </summary>
    private static Color Toward(Color colour, Color target, IReadOnlyList<Color> surfaces, double ratio)
    {
        for (int step = 0; step <= 100; step++)
        {
            var moved = Mix(colour, target, step / 100.0);
            if (surfaces.All(s => ContrastRatio(moved, s) >= ratio))
            {
                return moved;
            }
        }

        return target;
    }

    private static Color Mix(Color a, Color b, double t) =>
        Color.FromRgb((byte)Math.Round(a.R + ((b.R - a.R) * t)), (byte)Math.Round(a.G + ((b.G - a.G) * t)), (byte)Math.Round(a.B + ((b.B - a.B) * t)));

    /// <summary>WCAG relative luminance contrast, the same ratio <c>ThemeTests</c> checks, so the palettes are built against the test's own rule.</summary>
    public static double ContrastRatio(Color a, Color b)
    {
        static double Channel(byte v) => v / 255.0 <= 0.03928 ? v / 255.0 / 12.92 : Math.Pow(((v / 255.0) + 0.055) / 1.055, 2.4);
        static double Luminance(Color c) => (0.2126 * Channel(c.R)) + (0.7152 * Channel(c.G)) + (0.0722 * Channel(c.B));
        double la = Luminance(a), lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    // Typography, entry 42 section 3. The faces are embedded (Assets/Fonts), each with the fallback stack section 3 names.
    public static FontFamily Sans { get; } = new("avares://GroupLab.App/Assets/Fonts#IBM Plex Sans, $Default");

    public static FontFamily Condensed { get; } = new("avares://GroupLab.App/Assets/Fonts#IBM Plex Sans Condensed, avares://GroupLab.App/Assets/Fonts#IBM Plex Sans, $Default");

    public static FontFamily Mono { get; } = new("avares://GroupLab.App/Assets/Fonts#IBM Plex Mono, Cascadia Mono, Consolas, Menlo, monospace");

    // The type scale, NOTES-FROM-PLANNING.md entry 109 section 1 principle 2: five styles and no more. A screen title, a section heading, a
    // label, a value and a detail. Prose is a label or a detail, never the value style. The one size outside the five is mean radius's lead
    // figure, which entries 73 and 92 set and entry 109 section 3 keeps.
    public const double TitleSize = 16;
    public const double HeadingSize = 14;
    public const double LabelSize = 13;
    /// <summary>Entry 169 section 4 raised it from 18: the figures are the product, and the largest text beside the lead figure.</summary>
    public const double ValueSize = 22;
    public const double DetailSize = 11.5;
    public const double LeadValueSize = 29;

    /// <summary>Every size text may take on the marking, analysis and settings screens, which a test holds them to.</summary>
    public static IReadOnlySet<double> TypeScale { get; } = new HashSet<double> { TitleSize, HeadingSize, LabelSize, ValueSize, DetailSize, LeadValueSize };

    // The older names, each now one of the five, so every use of them lands on the scale.
    public const double SectionLabelSize = DetailSize;
    public const double SectionLabelSpacing = 0.9;
    public const double ListHeaderSpacing = 0.6;
    public const double TableRowSize = DetailSize;
    public const double SecondarySize = DetailSize;
    public const double ButtonSize = LabelSize;
    public const double BodySize = LabelSize;
    public const double WordmarkSize = 13;
    public const double WordmarkSpacing = 1.3;
    public const double FigureSize = ValueSize;
    public const double FigureSpacing = -0.21;
    public const double LeadFigureSize = LeadValueSize;

    // The spacing grid, NOTES-FROM-PLANNING.md entry 109 section 1 principle 4: a base unit of 4 pixels and multiples of it for every gap and
    // padding, where entry 42 section 4's scale had 6 and 14 between them. A control's margin is half the base, so two side by side are one
    // base apart.
    public const double Space4 = 4;
    public const double Space8 = 8;
    public const double Space12 = 12;
    public const double Space16 = 16;
    public const double Space20 = 20;
    public const double Space24 = 24;
    public const double ControlMargin = 2;

    /// <summary>No fill at all, for a disclosure's header or a table row, which take the surface they sit on.</summary>
    public static IBrush Clear { get; } = Brushes.Transparent;
    public const double RightColumnWidth = 372;
    public static Thickness SectionPadding { get; } = new(Space16, Space12);
    public static Thickness RowPadding { get; } = new(Space16, Space4);
    public static Thickness ButtonPadding { get; } = new(Space12, Space4);
    public static Thickness PillPadding { get; } = new(Space8, Space4);
    public static CornerRadius SurfaceRadius { get; } = new(3);
    public static CornerRadius ButtonRadius { get; } = new(4);

    // The marks drawn on the image, entry 42 section 5. They are drawn on a photograph, whose colours do not change with the theme, so they
    // take the design values in both themes, unadjusted: a mark is not text on a surface.
    public static Color MarkHalo { get; } = Color.FromArgb(140, 0x0b, 0x0c, 0x0e);
    public const double MarkHaloWidth = 3;
    public const double MarkCoreWidth = 1.6;
    public const double MarkSelectedCoreWidth = 2;
    public static Color MarkImpact { get; } = Hex(0xc8442f);
    public static Color MarkSelected { get; } = Hex(0xe0912f);
    public static Color MarkExcluded { get; } = Hex(0x9aa1a9);
    public static Color MarkTeal { get; } = Hex(0x6fbfa8);
    public static Color MarkFaint { get; } = Hex(0x697079);
    public static Color MarkAlert { get; } = Hex(0xe0604a);

    /// <summary>
    /// A shot a person placed or corrected, NOTES-FROM-PLANNING.md entry 97 section 1: neither teal, which is what the software found on its
    /// own, nor amber, which is what still needs a person. The concept's "corrected" and "manual" chips are neutral, and so is the mark.
    /// </summary>
    public static Color MarkPlaced { get; } = Hex(0xe6e8ea);

    /// <summary>
    /// The document's paper, entry 93 section 2 and entry 97 section 1: the document is light and the application is dark. The concept's
    /// sheet is this warm off-white inset in the dark canvas with a one pixel edge, in every theme, because a sheet of paper does not change
    /// colour when the application does.
    /// </summary>
    public static Color Paper { get; } = Hex(0xf2f1ec);

    /// <summary>
    /// A sheet definition's own ink, as the composite plot draws a bull (NOTES-FROM-PLANNING.md entry 103 section 1). It is data from the
    /// definition, #RRGGBB, not a colour of the interface, and paper where it does not parse.
    /// </summary>
    public static Color Ink(string srgb) => Color.TryParse(srgb, out var colour) ? colour : Paper;

    /// <summary>An ink laid over paper at a strength, as one solid colour, so a faded ring does not show the disc beneath it through.</summary>
    public static Color Faded(Color ink, Color paper, double strength) => Color.FromRgb(
        (byte)Math.Round((ink.R * strength) + (paper.R * (1 - strength))),
        (byte)Math.Round((ink.G * strength) + (paper.G * (1 - strength))),
        (byte)Math.Round((ink.B * strength) + (paper.B * (1 - strength))));

    public static Color PaperEdge { get; } = Hex(0xcfccc3);

    /// <summary>The empty sheet's hint, dark on the paper.</summary>
    public static Color PaperText { get; } = Hex(0x5a6068);

    /// <summary>A mark label's plate and text: near black at about 80 percent, and the dark theme's text colour, so a label reads on any photograph.</summary>
    public static Color MarkPlate { get; } = Color.FromArgb(200, 0x0b, 0x0c, 0x0e);

    /// <summary>The dimming behind a screen that asks one question over the whole window, entry 165's first run question.</summary>
    public static Color Scrim { get; } = Color.FromArgb(160, 0, 0, 0);

    public static Color MarkLabelText { get; } = Hex(0xe6e8ea);

    private static Color Hex(uint rgb) => Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
}

/// <summary>The composite plot's four inks, entry 169 section 3.</summary>
public sealed record PlotInks(Color Paper, Color Ink, Color Ring, Color Accent);
