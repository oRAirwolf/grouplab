using Avalonia.Controls;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile;

/// <summary>
/// The caliber box on Capture and in the first caliber's question, NOTES-FROM-PLANNING.md entry 312 section 5. A choice from the list leaves
/// a short name in the box, "6.5 Creedmoor, 0.264 in", with the whole line beneath it; a tap on the box selects all of its text, so typing
/// replaces it at once; and a clear button inside it empties it. The same on Android and iOS.
/// <para>
/// Entry 314: what it offers follows the setting "Caliber box shows", calibers, cartridges or both, as on the desktop, and its list is made
/// afresh from what is typed, so a rare cartridge is found by its name and "65 creed" finds 6.5 Creedmoor.
/// </para>
/// </summary>
internal sealed class CaliberBox : StackPanel
{
    private readonly TextBlock under = Screens.Quiet("");

    private readonly AppSettingsStore settings;

    public CaliberBox(AppSettingsStore? settings = null)
    {
        this.settings = settings ?? Phone.Settings;
        Box = new AutoCompleteBox
        {
            ItemsSource = CaliberChoices.Suggest("", this.settings.LoadCaliberList()),
            FilterMode = AutoCompleteFilterMode.None,
            PlaceholderText = "Caliber, e.g. 6.5 Creedmoor or .308",
            MinHeight = Screens.Touch,
            ItemSelector = (_, item) => CaliberChoices.Short(item as string),
        };
        // The list is made for what was typed, in the order the lookup ranks it; the box's own populating runs only for typing, so a choice
        // writing its short name into the box does not remake the list under it.
        Box.Populating += (_, e) =>
        {
            e.Cancel = true;
            Box.ItemsSource = CaliberChoices.Suggest(e.Parameter, this.settings.LoadCaliberList());
            Box.PopulateComplete();
        };
        CaliberBoxes.SelectAllOnFocus(Box);
        CaliberBoxes.Clear(Box, Screens.Touch);
        Box.TextChanged += (_, _) => Explain();
        Explain();
        Spacing = 2;
        Children.Add(Box);
        Children.Add(under);
    }

    /// <summary>The box itself, for the tests.</summary>
    internal AutoCompleteBox Box { get; }

    /// <summary>What the box holds.</summary>
    public string? Text
    {
        get => Box.Text;
        set => Box.Text = value;
    }

    /// <summary>The whole line for a short name, under the box; nothing under it for typed text.</summary>
    internal string? Under => under.IsVisible ? under.Text : null;

    private void Explain()
    {
        string? line = CaliberChoices.Explain(Box.Text);
        under.Text = line ?? "";
        under.IsVisible = line is not null;
    }
}
