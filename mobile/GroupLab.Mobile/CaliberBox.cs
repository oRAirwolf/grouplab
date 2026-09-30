using Avalonia.Controls;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile;

/// <summary>
/// The caliber box on Capture and in the first caliber's question, NOTES-FROM-PLANNING.md entry 312 section 5. A choice from the list leaves
/// a short name in the box, "6.5 Creedmoor, 0.264 in", with the whole line beneath it; a tap on the box selects all of its text, so typing
/// replaces it at once; and a clear button inside it empties it. The same on Android and iOS.
/// </summary>
internal sealed class CaliberBox : StackPanel
{
    private readonly TextBlock under = Screens.Quiet("");

    public CaliberBox()
    {
        Box = new AutoCompleteBox
        {
            ItemsSource = CartridgeTable.Suggest(""),
            FilterMode = AutoCompleteFilterMode.Contains,
            PlaceholderText = "Caliber, e.g. 6.5 Creedmoor or .308",
            MinHeight = Screens.Touch,
            ItemSelector = (_, item) => CaliberChoices.Short(item as string),
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
