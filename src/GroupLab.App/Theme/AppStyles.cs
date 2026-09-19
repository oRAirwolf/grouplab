using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace GroupLab.App.Theme;

/// <summary>
/// The application's look, NOTES-FROM-PLANNING.md entry 42 sections 3 and 4, added after Avalonia's Fluent theme: the palette of
/// <see cref="Tokens"/> for the resolved theme, the type scale, and the control styles. It is rebuilt whenever the resolved theme changes,
/// dark, light or following the system, so no control carries a colour of its own. A control that needs a role takes one of the classes
/// named here.
/// </summary>
public static class AppStyles
{
    /// <summary>
    /// A section heading, NOTES-FROM-PLANNING.md entry 109 section 1 principle 2: the heading style, semibold in the text colour, in sentence
    /// case. The headings are the structure, so they read at full strength, where entry 42 set them as dim spaced capitals.
    /// </summary>
    public const string Section = "section";

    /// <summary>A label, entry 109 section 1 principle 3: the left side of every figure row, at the label size in dim.</summary>
    public const string Label = "label";

    /// <summary>Words that act, such as the analysis banner's "Review them": amber, semibold, with no button drawn round them.</summary>
    public const string Link = "link";

    /// <summary>A one pixel vertical rule between groups of tools.</summary>
    public const string Divider = "divider";

    /// <summary>A screen title, the largest of the five styles.</summary>
    public const string Title = "title";

    /// <summary>
    /// A "why" disclosure, entry 109 section 1 principle 1: the sentences that explain a figure or a judgement, one click away on the item they
    /// explain, and remembered. Compact and quiet, so a closed one is a single short line.
    /// </summary>
    public const string Why = "why";

    /// <summary>A row of a plain table, entry 109 section 3: no border of its own; every other row carries <see cref="Shaded"/>.</summary>
    public const string TableRow = "table-row";

    public const string Shaded = "shaded";

    /// <summary>A tool drawn as an icon alone, entry 109 section 2, with a tooltip naming it and its key.</summary>
    public const string IconButton = "icon-button";

    /// <summary>The view controls floating over the canvas's corner, entry 109 section 2.</summary>
    public const string ViewCluster = "view-cluster";

    /// <summary>Secondary text, notes and legends: 11.5 point in dim.</summary>
    public const string Secondary = "secondary";

    public const string Dim = "dim";

    /// <summary>Quiet text that should read without shouting, such as a shot's provenance in the shot list (NOTES-FROM-PLANNING.md entry 46 section 3).</summary>
    public const string Faint = "faint";

    public const string Alert = "alert";

    /// <summary>A status pill, such as the scale's: mono, 11 point, a thin border; good in teal, a hand-drawn reference in amber.</summary>
    public const string Pill = "pill";

    public const string PillText = "pill-text";

    /// <summary>Something that worked: teal. On a status line it marks success, so that red keeps meaning something (NOTES-FROM-PLANNING.md entry 70 section 6).</summary>
    public const string Good = "good";

    public const string Warn = "warn";

    /// <summary>The top bar, the right column and the status line, each a panel surface with a one pixel separator in line.</summary>
    public const string Bar = "bar";

    public const string Side = "side";

    public const string StatusBar = "status-bar";

    /// <summary>
    /// The concept's chrome, NOTES-FROM-PLANNING.md entry 93 section 2. The rail is the narrow icon strip down the left, the breadcrumb
    /// header carries the document's identity and its counts, the primary action is the one amber button in the top right, and a keycap is
    /// the little bordered letter beside a tool that says which key does it.
    /// </summary>
    public const string Rail = "rail";

    public const string RailButton = "rail-button";

    public const string Breadcrumb = "breadcrumb";

    public const string Primary = "primary";

    public const string Keycap = "keycap";

    public const string KeycapText = "keycap-text";

    /// <summary>The review card: amber-tinted, because it is the thing that needs a person (entry 97 section 1).</summary>
    /// <summary>A heading with a hairline above it, entry 105 section 2.</summary>
    public const string Ruled = "ruled";

    public const string ReviewCard = "review-card";

    /// <summary>
    /// A judgement card in the analysis state, NOTES-FROM-PLANNING.md entry 103 section 2: a bold verdict and its evidence, divided from what is
    /// above it by a hairline and not boxed (entry 109 section 1). Neutral, because amber means something needs a person and a judgement is read,
    /// not acted on.
    /// </summary>
    public const string JudgementCard = "judgement-card";

    /// <summary>A status word in the review list or on a chip, NOW or NEXT in amber, DONE in teal.</summary>
    public const string StatusWord = "status-word";

    /// <summary>A provenance chip: automatic in teal, corrected and manual neutral, as the concept's selected-detection panel draws them.</summary>
    public const string Chip = "chip";

    private sealed class GroupLabStyles : Styles
    {
    }

    public static void Apply(Application app)
    {
        ArgumentNullException.ThrowIfNull(app);
        Restyle(app);
        app.ActualThemeVariantChanged += (_, _) => Restyle(app);
    }

    private static void Restyle(Application app)
    {
        var palette = Tokens.For(app.ActualThemeVariant);
        foreach (var previous in app.Styles.OfType<GroupLabStyles>().ToList())
        {
            app.Styles.Remove(previous);
        }

        app.Styles.Add(Build(palette));
        foreach (var (key, value) in FluentResources(palette))
        {
            app.Resources[key] = value;
        }
    }

    /// <summary>
    /// The Fluent theme's own resources for the states its templates draw, pointer over, pressed, checked and focused, set to the palette so
    /// that hovering a button cannot bring back a Fluent grey. Amber is the one accent (entry 42 section 2).
    /// </summary>
    private static IEnumerable<(string Key, object Value)> FluentResources(Palette p)
    {
        yield return ("SystemAccentColor", p.Amber);
        yield return ("ControlCornerRadius", Tokens.ButtonRadius);
        foreach (string state in new[] { "", "PointerOver", "Pressed" })
        {
            yield return ($"ButtonBackground{state}", Brush(state == "" ? p.Panel2 : p.Line));
            yield return ($"ButtonForeground{state}", Brush(p.Text));
            yield return ($"ButtonBorderBrush{state}", Brush(state == "" ? p.Line2 : p.Faint));
            yield return ($"ToggleButtonBackground{state}", Brush(state == "" ? p.Panel2 : p.Line));
            yield return ($"ToggleButtonForeground{state}", Brush(p.Text));
            yield return ($"ToggleButtonBorderBrush{state}", Brush(state == "" ? p.Line2 : p.Faint));
            yield return ($"ToggleButtonBackgroundChecked{state}", Brush(p.AmberTint));
            yield return ($"ToggleButtonForegroundChecked{state}", Brush(p.Amber));
            yield return ($"ToggleButtonBorderBrushChecked{state}", Brush(p.AmberTintBorder));
            yield return ($"ComboBoxBackground{state}", Brush(p.FieldBg));
            yield return ($"ComboBoxForeground{state}", Brush(p.Text));
            yield return ($"ComboBoxBorderBrush{state}", Brush(state == "" ? p.FieldBorder : p.FocusRing));
        }

        foreach (string state in new[] { "", "PointerOver", "Focused" })
        {
            yield return ($"TextControlBackground{state}", Brush(p.FieldBg));
            yield return ($"TextControlForeground{state}", Brush(p.Text));
            yield return ($"TextControlBorderBrush{state}", Brush(state == "Focused" ? p.FocusRing : p.FieldBorder));
        }
    }

    /// <summary>Form text: a warning is amber, a refusal red, the meanings entry 93 fixed (entry 100 section 1).</summary>
    public const string FormWarning = "form-warning";

    public const string FormError = "form-error";

    private static GroupLabStyles Build(Palette p) =>
    [
        Rule(x => x.OfType<TextBlock>().Class(FormWarning), (TextBlock.ForegroundProperty, Brush(p.WarningText))),
        Rule(x => x.OfType<TextBlock>().Class(FormError), (TextBlock.ForegroundProperty, Brush(p.ErrorText))),
        Rule(x => x.OfType<TextBox>().Class(":disabled"), (TemplatedControl.ForegroundProperty, Brush(p.Disabled))),
        Rule(x => x.OfType<Window>(), (TemplatedControl.BackgroundProperty, Brush(p.Bg)), (TemplatedControl.ForegroundProperty, Brush(p.Text)), (TemplatedControl.FontFamilyProperty, Tokens.Sans), (TemplatedControl.FontSizeProperty, Tokens.BodySize)),
        Rule(x => x.OfType<TextBlock>().Class(Section), (TextBlock.FontSizeProperty, Tokens.HeadingSize), (TextBlock.FontWeightProperty, FontWeight.SemiBold), (TextBlock.ForegroundProperty, Brush(p.Text))),
        Rule(x => x.OfType<TextBlock>().Class(Label), (TextBlock.FontSizeProperty, Tokens.LabelSize), (TextBlock.ForegroundProperty, Brush(p.Dim))),
        Rule(x => x.OfType<Button>().Class(Link), (TemplatedControl.BackgroundProperty, Tokens.Clear), (TemplatedControl.BorderThicknessProperty, new Thickness(0)), (TemplatedControl.PaddingProperty, new Thickness(0)), (Layoutable.MarginProperty, new Thickness(0)), (TemplatedControl.ForegroundProperty, Brush(p.Amber)), (TemplatedControl.FontWeightProperty, FontWeight.SemiBold), (Layoutable.MinHeightProperty, 0.0)),
        Rule(x => x.OfType<Border>().Class(Divider), (Border.BackgroundProperty, Brush(p.Line2))),
        Rule(x => x.OfType<TextBlock>().Class(Title), (TextBlock.FontSizeProperty, Tokens.TitleSize), (TextBlock.FontWeightProperty, FontWeight.SemiBold), (TextBlock.ForegroundProperty, Brush(p.Text))),
        Rule(x => x.OfType<Expander>().Class(Why), (TemplatedControl.PaddingProperty, new Thickness(0, Tokens.Space4, 0, 0)), (TemplatedControl.BackgroundProperty, Tokens.Clear), (TemplatedControl.BorderThicknessProperty, new Thickness(0)), (TemplatedControl.FontSizeProperty, Tokens.DetailSize), (Layoutable.MinHeightProperty, 0.0)),
        Rule(x => x.OfType<Expander>().Class(Why).Template().OfType<ToggleButton>(), (TemplatedControl.PaddingProperty, new Thickness(0)), (Layoutable.MinHeightProperty, 0.0), (TemplatedControl.BackgroundProperty, Tokens.Clear), (TemplatedControl.BorderThicknessProperty, new Thickness(0)), (TemplatedControl.ForegroundProperty, Brush(p.Dim)), (TemplatedControl.FontSizeProperty, Tokens.DetailSize)),
        Rule(x => x.OfType<Expander>().Class(Why).Template().OfType<Border>().Name("ExpanderContent"), (Border.BorderThicknessProperty, new Thickness(0)), (Border.BackgroundProperty, Tokens.Clear), (Border.PaddingProperty, new Thickness(0, Tokens.Space4, 0, 0))),
        Rule(x => x.OfType<Button>().Class(TableRow), (TemplatedControl.BackgroundProperty, Tokens.Clear), (TemplatedControl.BorderThicknessProperty, new Thickness(0)), (TemplatedControl.CornerRadiusProperty, new CornerRadius(0)), (TemplatedControl.PaddingProperty, new Thickness(Tokens.Space4, 2)), (Layoutable.MarginProperty, new Thickness(0)), (TemplatedControl.FontSizeProperty, Tokens.DetailSize)),
        Rule(x => x.OfType<Button>().Class(TableRow).Class(Shaded), (TemplatedControl.BackgroundProperty, Brush(p.Panel2))),
        Rule(x => x.OfType<Button>().Class(TableRow).Class(Warn), (TemplatedControl.BackgroundProperty, Brush(p.AmberTint))),
        Rule(x => x.OfType<Border>().Class(ViewCluster), (Border.BackgroundProperty, new SolidColorBrush(p.Panel, 0.92)), (Border.BorderBrushProperty, Brush(p.Line2)), (Border.BorderThicknessProperty, new Thickness(1)), (Border.CornerRadiusProperty, Tokens.ButtonRadius), (Border.PaddingProperty, new Thickness(Tokens.Space4)), (Layoutable.MarginProperty, new Thickness(Tokens.Space12))),
        Rule(x => x.OfType<Border>().Class(Ruled), (Border.BorderBrushProperty, Brush(p.Line2))),
        Rule(x => x.OfType<TextBlock>().Class(Secondary), (TextBlock.FontSizeProperty, Tokens.SecondarySize), (TextBlock.ForegroundProperty, Brush(p.Dim))),
        Rule(x => x.OfType<TextBlock>().Class(Dim), (TextBlock.ForegroundProperty, Brush(p.Dim))),
        Rule(x => x.OfType<TextBlock>().Class(Faint), (TextBlock.FontSizeProperty, Tokens.SecondarySize), (TextBlock.ForegroundProperty, Brush(p.Faint))),
        Rule(x => x.OfType<TextBlock>().Class(Alert), (TextBlock.ForegroundProperty, Brush(p.Alert))),
        Rule(x => x.OfType<TextBlock>().Class(Good), (TextBlock.ForegroundProperty, Brush(p.Teal))),
        Rule(x => x.OfType<TextBlock>().Class(PillText), (TextBlock.FontFamilyProperty, Tokens.Mono), (TextBlock.FontSizeProperty, Tokens.DetailSize), (TextBlock.ForegroundProperty, Brush(p.Dim))),
        Rule(x => x.OfType<TextBlock>().Class(PillText).Class(Good), (TextBlock.ForegroundProperty, Brush(p.Teal))),
        Rule(x => x.OfType<TextBlock>().Class(PillText).Class(Warn), (TextBlock.ForegroundProperty, Brush(p.Amber))),
        Rule(x => x.OfType<Border>().Class(Pill), (Border.BorderBrushProperty, Brush(p.Line2)), (Border.BorderThicknessProperty, new Thickness(1)), (Border.CornerRadiusProperty, Tokens.SurfaceRadius), (Border.PaddingProperty, Tokens.PillPadding)),
        Rule(x => x.OfType<Border>().Class(Pill).Class(Good), (Border.BorderBrushProperty, Brush(p.TealTintBorder)), (Border.BackgroundProperty, Brush(p.TealTint))),
        Rule(x => x.OfType<Border>().Class(Pill).Class(Warn), (Border.BorderBrushProperty, Brush(p.AmberTintBorder)), (Border.BackgroundProperty, Brush(p.AmberTint))),
        Rule(x => x.OfType<Border>().Class(Bar), (Border.BackgroundProperty, Brush(p.Panel)), (Border.BorderBrushProperty, Brush(p.Line)), (Border.BorderThicknessProperty, new Thickness(0, 0, 0, 1))),
        Rule(x => x.OfType<Border>().Class(Side), (Border.BackgroundProperty, Brush(p.Panel)), (Border.BorderBrushProperty, Brush(p.Line)), (Border.BorderThicknessProperty, new Thickness(1, 0, 0, 0))),
        Rule(x => x.OfType<Border>().Class(StatusBar), (Border.BackgroundProperty, Brush(p.Panel)), (Border.BorderBrushProperty, Brush(p.Line)), (Border.BorderThicknessProperty, new Thickness(0, 1, 0, 0))),
        Rule(x => x.OfType<Button>(), ButtonSetters()),
        Rule(x => x.OfType<ToggleButton>(), ButtonSetters()),
        Rule(x => x.OfType<Button>().Class(IconButton), (TemplatedControl.PaddingProperty, new Thickness(Tokens.Space8))),
        Rule(x => x.OfType<ToggleButton>().Class(IconButton), (TemplatedControl.PaddingProperty, new Thickness(Tokens.Space8))),

        // Entry 93 section 2: the document is light and the application is dark, so the chrome is panel over the window's own colour, and
        // the two accents keep their meanings: teal for what the software found on its own, amber for what needs a person and for the one
        // primary action.
        Rule(x => x.OfType<Border>().Class(Rail), (Border.BackgroundProperty, Brush(p.Panel)), (Border.BorderBrushProperty, Brush(p.Line)), (Border.BorderThicknessProperty, new Thickness(0, 0, 1, 0))),
        Rule(x => x.OfType<Button>().Class(RailButton), (TemplatedControl.BackgroundProperty, Brush(p.Panel)), (TemplatedControl.BorderBrushProperty, Brush(p.Panel)), (TemplatedControl.ForegroundProperty, Brush(p.Dim)), (TemplatedControl.FontSizeProperty, Tokens.LabelSize), (TemplatedControl.PaddingProperty, new Thickness(Tokens.Space8, Tokens.Space8)), (Layoutable.MarginProperty, new Thickness(Tokens.Space4, Tokens.Space4, Tokens.Space4, 0))),
        Rule(x => x.OfType<Button>().Class(RailButton).Class(Warn), (TemplatedControl.ForegroundProperty, Brush(p.Amber)), (TemplatedControl.BackgroundProperty, Brush(p.AmberTint)), (TemplatedControl.BorderBrushProperty, Brush(p.AmberTintBorder))),
        Rule(x => x.OfType<Border>().Class(Breadcrumb), (Border.BackgroundProperty, Brush(p.Panel)), (Border.BorderBrushProperty, Brush(p.Line)), (Border.BorderThicknessProperty, new Thickness(0, 0, 0, 1)), (Border.PaddingProperty, new Thickness(Tokens.Space12, Tokens.Space8))),
        Rule(x => x.OfType<Button>().Class(Primary), (TemplatedControl.BackgroundProperty, Brush(p.Amber)), (TemplatedControl.ForegroundProperty, Brush(p.OnAmber)), (TemplatedControl.BorderBrushProperty, Brush(p.Amber)), (TemplatedControl.FontWeightProperty, FontWeight.SemiBold)),
        Rule(x => x.OfType<Border>().Class(Keycap), (Border.BackgroundProperty, Brush(p.Sunk)), (Border.BorderBrushProperty, Brush(p.Line2)), (Border.BorderThicknessProperty, new Thickness(1)), (Border.CornerRadiusProperty, Tokens.SurfaceRadius), (Border.PaddingProperty, new Thickness(Tokens.Space4, 0)), (Layoutable.MarginProperty, new Thickness(Tokens.Space8, 0, 0, 0))),
        Rule(x => x.OfType<TextBlock>().Class(KeycapText), (TextBlock.FontFamilyProperty, Tokens.Mono), (TextBlock.FontSizeProperty, Tokens.SectionLabelSize), (TextBlock.ForegroundProperty, Brush(p.Dim))),
        Rule(x => x.OfType<TextBlock>().Class(Warn), (TextBlock.ForegroundProperty, Brush(p.Amber))),
        Rule(x => x.OfType<Border>().Class(ReviewCard), (Border.BackgroundProperty, Brush(p.AmberTint)), (Border.BorderBrushProperty, Brush(p.AmberTintBorder)), (Border.BorderThicknessProperty, new Thickness(1)), (Border.CornerRadiusProperty, Tokens.ButtonRadius), (Border.PaddingProperty, new Thickness(Tokens.Space12))),
        Rule(x => x.OfType<Border>().Class(JudgementCard), (Border.BorderBrushProperty, Brush(p.Line2)), (Border.BorderThicknessProperty, new Thickness(0, 1, 0, 0)), (Border.PaddingProperty, new Thickness(0, Tokens.Space12, 0, 0))),
        Rule(x => x.OfType<TextBlock>().Class(StatusWord), (TextBlock.FontSizeProperty, Tokens.SectionLabelSize), (TextBlock.FontWeightProperty, FontWeight.SemiBold), (TextBlock.LetterSpacingProperty, Tokens.SectionLabelSpacing)),
        Rule(x => x.OfType<Border>().Class(Chip), (Border.BorderBrushProperty, Brush(p.Line2)), (Border.BorderThicknessProperty, new Thickness(1)), (Border.CornerRadiusProperty, new CornerRadius(9)), (Border.PaddingProperty, new Thickness(Tokens.Space8, 1)), (Layoutable.MarginProperty, new Thickness(0, 0, Tokens.Space8, 0))),
        Rule(x => x.OfType<Border>().Class(Chip).Class(Good), (Border.BorderBrushProperty, Brush(p.TealTintBorder)), (Border.BackgroundProperty, Brush(p.TealTint))),
    ];

    /// <summary>Buttons, entry 42 section 4: 12 point at weight 500, padding 6 by 12, radius 4, margin 2.</summary>
    private static (AvaloniaProperty, object)[] ButtonSetters() =>
    [
        (TemplatedControl.FontSizeProperty, Tokens.ButtonSize),
        (TemplatedControl.FontWeightProperty, FontWeight.Medium),
        (TemplatedControl.PaddingProperty, Tokens.ButtonPadding),
        (TemplatedControl.CornerRadiusProperty, Tokens.ButtonRadius),
        (Layoutable.MarginProperty, new Thickness(Tokens.ControlMargin)),
    ];

    private static IBrush Brush(Color colour) => new SolidColorBrush(colour);

    private static Style Rule(Func<Selector?, Selector> selector, params (AvaloniaProperty Property, object Value)[] setters)
    {
        var style = new Style(selector);
        foreach (var (property, value) in setters)
        {
            style.Setters.Add(new Setter(property, value));
        }

        return style;
    }
}
