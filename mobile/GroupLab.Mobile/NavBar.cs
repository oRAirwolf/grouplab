using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 413, Alan chose B: the bar along the bottom shows each place's icon over its name, and when the text size
/// the phone asks for would make any one name too wide for its fifth of the bar, it shows the icons alone, larger, for all five at once.
/// The names are measured at the size followed against the bar's own width, so a wide phone or the Fold's inner screen keeps them longer.
/// Each tab keeps its name for TalkBack and as a tip on a long press either way, and stays a full fifth wide and a thumb high.
/// </summary>
internal sealed class NavBar : UniformGrid
{
    /// <summary>An icon over its name, and an icon alone (entry 413 section 1.2).</summary>
    internal const double NamedIcon = 20, LoneIcon = 26;

    private readonly List<(Button Tab, PathIcon Icon, TextBlock Name)> places = [];

    public NavBar() => Rows = 1;

    /// <summary>True while the names are left off and the icons drawn larger.</summary>
    internal bool IconsOnly { get; private set; }

    internal void Add(Button tab, PathIcon icon, TextBlock name)
    {
        Avalonia.Automation.AutomationProperties.SetName(tab, name.Text);
        ToolTip.SetTip(tab, name.Text);
        tab.AddHandler(InputElement.HoldingEvent, (_, e) =>
        {
            if (e.HoldingState == HoldingState.Started)
            {
                ToolTip.SetIsOpen(tab, true);
            }
        });

        // A name's size changes with the system's text size whether it is shown or not; a name not shown is never measured, so the bar
        // asks again itself.
        name.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBlock.FontSizeProperty || e.Property == TextBlock.FontFamilyProperty)
            {
                InvalidateMeasure();
            }
        };
        places.Add((tab, icon, name));
        Children.Add(tab);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (double.IsFinite(availableSize.Width) && places.Count > 0)
        {
            Set(places.Any(p => Wider(p.Tab, p.Name, availableSize.Width / places.Count)));
        }

        return base.MeasureOverride(availableSize);
    }

    // Measured semibold, as the current place's name is drawn, so moving between places never changes which way the bar is drawn.
    private static bool Wider(Button tab, TextBlock name, double fifth)
    {
        using var layout = new TextLayout(name.Text ?? "", new Typeface(name.FontFamily, name.FontStyle, FontWeight.SemiBold), name.FontSize, null);
        return layout.WidthIncludingTrailingWhitespace > fifth - tab.Padding.Left - tab.Padding.Right;
    }

    private void Set(bool iconsOnly)
    {
        if (iconsOnly == IconsOnly && places.TrueForAll(p => p.Name.IsVisible != iconsOnly))
        {
            return;
        }

        IconsOnly = iconsOnly;
        foreach (var (tab, icon, name) in places)
        {
            name.IsVisible = !iconsOnly;
            icon.Width = icon.Height = iconsOnly ? LoneIcon : NamedIcon;
            tab.Classes.Set(PhoneStyles.NavIconsOnly, iconsOnly);
        }
    }
}
