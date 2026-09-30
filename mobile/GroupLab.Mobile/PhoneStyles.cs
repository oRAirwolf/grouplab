using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using GroupLab.App.Theme;
using RadioButton = Avalonia.Controls.RadioButton;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 246, answering request 49: look B, "cards for the thumb". The desktop's own styles come first
/// (<see cref="AppStyles"/>, so the palette, the faces and every Fluent state are the desktop's), and these rules sit on top of them for a
/// thumb: related things on rounded panels, big tappable choices, figures as tiles, pill buttons and a bottom bar with the current place
/// in an amber pill. Every color and every size is a token from <see cref="Tokens"/>; nothing here is a literal of its own but the radii
/// and the touch height, which are the phone's.
/// </summary>
internal static class PhoneStyles
{
    public const string Card = "card";
    public const string Title = "title-large";
    public const string Heading = "heading";
    public const string Dim = "dim";
    public const string Choice = "choice";
    public const string Tile = "tile";
    public const string TileHeadline = "tile-headline";
    public const string TileLabel = "tile-label";
    public const string TileValue = "tile-value";
    public const string Nav = "nav";
    public const string NavItem = "nav-item";
    public const string NavPill = "nav-pill";
    public const string NavLabel = "nav-label";
    public const string On = "on";
    public const string Row = "row";
    public const string Primary = AppStyles.Primary;

    /// <summary>A panel's corners and a pill's, in device independent pixels.</summary>
    public const double CardRadius = 10, PillRadius = 22;

    private sealed class Rules : Styles
    {
    }

    /// <summary>Applies the desktop's styles and then these, and again whenever the theme changes, in that order, so these stay on top.</summary>
    public static void Apply(Application app)
    {
        ArgumentNullException.ThrowIfNull(app);
        AppStyles.Apply(app);
        Restyle(app);
        app.ActualThemeVariantChanged += (_, _) => Restyle(app);
    }

    private static void Restyle(Application app)
    {
        foreach (var previous in app.Styles.OfType<Rules>().ToList())
        {
            app.Styles.Remove(previous);
        }

        app.Styles.Add(Build(Tokens.For(app.ActualThemeVariant)));
    }

    private static SolidColorBrush Brush(Color c) => new(c);

    private static Style Rule(Func<Selector?, Selector> selector, params (AvaloniaProperty Property, object Value)[] setters)
    {
        var style = new Style(selector);
        foreach (var (property, value) in setters)
        {
            style.Setters.Add(new Setter(property, value));
        }

        return style;
    }

    private static Rules Build(Palette p) =>
    [
        // The frame: the window's surface, text and face, as the desktop's window has them, at the body size a phone reads at.
        Rule(x => x.OfType<Shell>(), (TemplatedControl.BackgroundProperty, Brush(p.Bg)), (TemplatedControl.ForegroundProperty, Brush(p.Text)),
            (TemplatedControl.FontFamilyProperty, Tokens.Sans), (TemplatedControl.FontSizeProperty, Tokens.TitleSize)),
        Rule(x => x.OfType<TextBlock>().Class(Title), (TextBlock.FontSizeProperty, Tokens.LeadValueSize), (TextBlock.FontWeightProperty, FontWeight.SemiBold),
            (TextBlock.ForegroundProperty, Brush(p.Text))),
        Rule(x => x.OfType<TextBlock>().Class(Heading), (TextBlock.FontSizeProperty, Tokens.ValueSize), (TextBlock.FontWeightProperty, FontWeight.SemiBold),
            (TextBlock.ForegroundProperty, Brush(p.Text))),
        Rule(x => x.OfType<TextBlock>().Class(Dim), (TextBlock.FontSizeProperty, Tokens.HeadingSize), (TextBlock.ForegroundProperty, Brush(p.Dim))),

        // Related things on one panel.
        Rule(x => x.OfType<Border>().Class(Card), (Border.BackgroundProperty, Brush(p.Panel)), (Border.BorderBrushProperty, Brush(p.Line)),
            (Border.BorderThicknessProperty, new Thickness(1)), (Border.CornerRadiusProperty, new CornerRadius(CardRadius)), (Border.PaddingProperty, new Thickness(Tokens.Space16, Tokens.Space12))),

        // A button a thumb hits: a pill, the desktop's colors, the body size.
        Rule(x => x.OfType<Button>(), (TemplatedControl.CornerRadiusProperty, new CornerRadius(PillRadius)), (Layoutable.MinHeightProperty, Screens.Touch),
            (TemplatedControl.PaddingProperty, new Thickness(Tokens.Space16, Tokens.Space8)), (TemplatedControl.FontSizeProperty, Tokens.TitleSize),
            (ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Center), (ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center)),
        Rule(x => x.OfType<Button>().Class(Primary), (TemplatedControl.BackgroundProperty, Brush(p.Amber)), (TemplatedControl.ForegroundProperty, Brush(p.OnAmber)),
            (TemplatedControl.BorderBrushProperty, Brush(p.Amber)), (TemplatedControl.FontWeightProperty, FontWeight.SemiBold)),

        // A row of a list on a card: no button drawn, the whole width, a thumb high.
        Rule(x => x.OfType<Button>().Class(Row), (TemplatedControl.BackgroundProperty, Tokens.Clear), (TemplatedControl.BorderThicknessProperty, new Thickness(0)),
            (TemplatedControl.CornerRadiusProperty, new CornerRadius(0)), (TemplatedControl.PaddingProperty, new Thickness(0, Tokens.Space8)),
            (ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch)),

        // A choice among several: a card of its own, amber when it is the one chosen.
        Rule(x => x.OfType<RadioButton>().Class(Choice), (TemplatedControl.BackgroundProperty, Brush(p.Panel)), (TemplatedControl.BorderBrushProperty, Brush(p.Line2)),
            (TemplatedControl.BorderThicknessProperty, new Thickness(1)), (TemplatedControl.CornerRadiusProperty, new CornerRadius(CardRadius)),
            (TemplatedControl.PaddingProperty, new Thickness(Tokens.Space8, Tokens.Space12)), (Layoutable.MinHeightProperty, Screens.Touch),
            (Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Stretch)),
        Rule(x => x.OfType<RadioButton>().Class(Choice).Class(":checked"), (TemplatedControl.BackgroundProperty, Brush(p.AmberTint)),
            (TemplatedControl.BorderBrushProperty, Brush(p.Amber))),

        // The circle beside its words, inside the card: the theme's template puts it at the card's top-left corner, since the card's
        // padding reaches only the words. The root inset by the padding's width at the left, and the circle's row centered on the words.
        Rule(x => x.OfType<RadioButton>().Class(Choice).Template().OfType<Border>().Name("RootBorder"), (Decorator.PaddingProperty, new Thickness(Tokens.Space12, 0, 0, 0))),
        Rule(x => x.OfType<RadioButton>().Class(Choice).Template().OfType<Grid>().Child().OfType<Grid>(), (Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center)),

        // Fields: the desktop's field colors, rounded as the cards are, a thumb high.
        Rule(x => x.OfType<TextBox>(), (TemplatedControl.CornerRadiusProperty, new CornerRadius(CardRadius)), (Layoutable.MinHeightProperty, Screens.Touch),
            (TemplatedControl.FontSizeProperty, Tokens.TitleSize), (TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center)),

        // Figures as tiles, two to a row; the one figure that answers the question in amber, as the desktop's headline figure is.
        Rule(x => x.OfType<Border>().Class(Tile), (Border.BackgroundProperty, Brush(p.Panel)), (Border.BorderBrushProperty, Brush(p.Line)),
            (Border.BorderThicknessProperty, new Thickness(1)), (Border.CornerRadiusProperty, new CornerRadius(CardRadius)), (Border.PaddingProperty, new Thickness(Tokens.Space12, Tokens.Space8))),
        Rule(x => x.OfType<Border>().Class(Tile).Class(TileHeadline), (Border.BackgroundProperty, Brush(p.AmberTint)), (Border.BorderBrushProperty, Brush(p.AmberTintBorder))),
        Rule(x => x.OfType<TextBlock>().Class(TileLabel), (TextBlock.FontSizeProperty, Tokens.LabelSize), (TextBlock.ForegroundProperty, Brush(p.Dim))),
        Rule(x => x.OfType<TextBlock>().Class(TileValue), (TextBlock.FontFamilyProperty, Tokens.Mono), (TextBlock.FontSizeProperty, Tokens.ValueSize),
            (TextBlock.ForegroundProperty, Brush(p.Text))),
        Rule(x => x.OfType<Border>().Class(TileHeadline).Descendant().OfType<TextBlock>().Class(TileValue), (TextBlock.ForegroundProperty, Brush(p.Amber))),

        // The bottom bar: the panel's surface under a hairline, each place an icon over its name, the current one in an amber pill.
        Rule(x => x.OfType<Border>().Class(Nav), (Border.BackgroundProperty, Brush(p.Panel)), (Border.BorderBrushProperty, Brush(p.Line)),
            (Border.BorderThicknessProperty, new Thickness(0, 1, 0, 0)), (Border.PaddingProperty, new Thickness(0, Tokens.Space4, 0, Tokens.Space8))),
        Rule(x => x.OfType<Button>().Class(NavItem), (TemplatedControl.BackgroundProperty, Tokens.Clear), (TemplatedControl.BorderThicknessProperty, new Thickness(0)),
            (TemplatedControl.ForegroundProperty, Brush(p.Dim)), (TemplatedControl.PaddingProperty, new Thickness(0, Tokens.Space4)), (TemplatedControl.CornerRadiusProperty, new CornerRadius(0))),
        Rule(x => x.OfType<Button>().Class(NavItem).Class(On), (TemplatedControl.ForegroundProperty, Brush(p.Text))),
        Rule(x => x.OfType<Border>().Class(NavPill), (Border.CornerRadiusProperty, new CornerRadius(14)), (Layoutable.WidthProperty, 56.0), (Layoutable.HeightProperty, 28.0)),
        Rule(x => x.OfType<Button>().Class(NavItem).Class(On).Descendant().OfType<Border>().Class(NavPill), (Border.BackgroundProperty, Brush(p.AmberTint)),
            (Border.BorderBrushProperty, Brush(p.AmberTintBorder)), (Border.BorderThicknessProperty, new Thickness(1))),
        Rule(x => x.OfType<Button>().Class(NavItem).Class(On).Descendant().OfType<PathIcon>(), (TemplatedControl.ForegroundProperty, Brush(p.Amber))),
        Rule(x => x.OfType<TextBlock>().Class(NavLabel), (TextBlock.FontSizeProperty, Tokens.LabelSize)),
        Rule(x => x.OfType<Button>().Class(NavItem).Class(On).Descendant().OfType<TextBlock>().Class(NavLabel), (TextBlock.FontWeightProperty, FontWeight.SemiBold)),
    ];
}
