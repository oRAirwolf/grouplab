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
    /// <summary>A section label: 10 point semibold, uppercase, spaced, in faint.</summary>
    public const string Section = "section";

    /// <summary>Secondary text, notes and legends: 11.5 point in dim.</summary>
    public const string Secondary = "secondary";

    public const string Dim = "dim";

    public const string Alert = "alert";

    /// <summary>A status pill, such as the scale's: mono, 11 point, a thin border; good in teal, a hand-drawn reference in amber.</summary>
    public const string Pill = "pill";

    public const string PillText = "pill-text";

    public const string Good = "good";

    public const string Warn = "warn";

    /// <summary>The top bar, the right column and the status line, each a panel surface with a one pixel separator in line.</summary>
    public const string Bar = "bar";

    public const string Side = "side";

    public const string StatusBar = "status-bar";

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
            yield return ($"ComboBoxBackground{state}", Brush(p.Panel2));
            yield return ($"ComboBoxForeground{state}", Brush(p.Text));
            yield return ($"ComboBoxBorderBrush{state}", Brush(state == "" ? p.Line2 : p.Faint));
        }

        foreach (string state in new[] { "", "PointerOver", "Focused" })
        {
            yield return ($"TextControlBackground{state}", Brush(state == "Focused" ? p.Panel : p.Panel2));
            yield return ($"TextControlForeground{state}", Brush(p.Text));
            yield return ($"TextControlBorderBrush{state}", Brush(state == "Focused" ? p.Amber : p.Line2));
        }
    }

    private static GroupLabStyles Build(Palette p) =>
    [
        Rule(x => x.OfType<Window>(), (TemplatedControl.BackgroundProperty, Brush(p.Bg)), (TemplatedControl.ForegroundProperty, Brush(p.Text)), (TemplatedControl.FontFamilyProperty, Tokens.Sans), (TemplatedControl.FontSizeProperty, Tokens.BodySize)),
        Rule(x => x.OfType<TextBlock>().Class(Section), (TextBlock.FontSizeProperty, Tokens.SectionLabelSize), (TextBlock.FontWeightProperty, FontWeight.SemiBold), (TextBlock.LetterSpacingProperty, Tokens.SectionLabelSpacing), (TextBlock.ForegroundProperty, Brush(p.Faint))),
        Rule(x => x.OfType<TextBlock>().Class(Secondary), (TextBlock.FontSizeProperty, Tokens.SecondarySize), (TextBlock.ForegroundProperty, Brush(p.Dim))),
        Rule(x => x.OfType<TextBlock>().Class(Dim), (TextBlock.ForegroundProperty, Brush(p.Dim))),
        Rule(x => x.OfType<TextBlock>().Class(Alert), (TextBlock.ForegroundProperty, Brush(p.Alert))),
        Rule(x => x.OfType<TextBlock>().Class(PillText), (TextBlock.FontFamilyProperty, Tokens.Mono), (TextBlock.FontSizeProperty, 11.0), (TextBlock.ForegroundProperty, Brush(p.Dim))),
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
