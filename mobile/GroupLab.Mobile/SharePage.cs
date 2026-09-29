using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using GroupLab.Core.Reporting;
using Button = Avalonia.Controls.Button;

namespace GroupLab.Mobile;

/// <summary>
/// Share A, NOTES-FROM-PLANNING.md entry 280 section 2 (board ShareA; entry 278 features d and g): the picture with a results box on it,
/// dragged anywhere and resized by a pinch or its corner handle, its lines chosen by tapping it; chips for the mean radius circle (about the
/// group's center), a label, the box's style and the crop; then "Save to gallery" and "Share". The picture is drawn by the same control as the
/// desktop's window (<see cref="SharePicture"/>) from the same lines and circle (<see cref="ShareCard"/>), and saved as a PNG in the cache's
/// shared folder, the one folder the share sheet may read.
/// </summary>
internal sealed class SharePage : UserControl
{
    private readonly SharePicture picture;
    private readonly IReadOnlyList<ShareLine> lines;
    private readonly HashSet<string> chosen;
    private readonly StackPanel choices = new() { Spacing = 4, IsVisible = false };
    private readonly WrapPanel chips = new();
    private readonly TextBox label = new() { PlaceholderText = "A label across the top", IsVisible = false };
    private readonly TextBlock said = Screens.Line("");
    private readonly string name;
    private readonly MarkingState state;
    private readonly PixelSize frame;

    public SharePage(MarkingState state, string title, string date, UnitSettings units, Action back)
    {
        this.state = state;
        name = string.Concat($"{title} {date}".Split(Path.GetInvalidFileNameChars())).Trim();
        lines = ShareCard.Lines(state, title, date, units);
        chosen = [.. lines.Where(l => l.Shown).Select(l => l.Key)];
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title("Share a picture"));
        if (state.ImagePath is not { } path || !File.Exists(path))
        {
            column.Children.Add(Screens.Line("This result has no picture on the phone to share."));
            column.Children.Add(Screens.Choice("Back to the result", back));
            picture = null!;
            Content = Screens.Page(column);
            return;
        }

        var bitmap = new Bitmap(path);
        frame = bitmap.PixelSize;
        picture = new SharePicture(bitmap, bitmap.PixelSize.Width, bitmap.PixelSize.Height, state.ViewQuarterTurns)
        {
            Circles = ShareCard.Circles(state),
            Label = state.SheetLabel,
        };
        label.Text = state.SheetLabel ?? "";
        label.TextChanged += (_, _) =>
        {
            picture.Label = label.IsVisible ? label.Text : null;
            picture.InvalidateVisual();
        };
        picture.BoxTapped += () =>
        {
            choices.IsVisible = !choices.IsVisible;
        };
        foreach (var line in lines)
        {
            var box = new CheckBox { Content = new TextBlock { Text = line.Text, TextWrapping = Avalonia.Media.TextWrapping.Wrap }, IsChecked = chosen.Contains(line.Key), MinHeight = Screens.Touch };
            string key = line.Key;
            box.IsCheckedChanged += (_, _) =>
            {
                if (box.IsChecked == true)
                {
                    chosen.Add(key);
                }
                else
                {
                    chosen.Remove(key);
                }

                ShowLines();
            };
            choices.Children.Add(box);
        }

        choices.Children.Add(Screens.Choice("Done", () => choices.IsVisible = false));
        ShowLines();
        column.Children.Add(new Border { Child = picture, Classes = { PhoneStyles.Card } });
        column.Children.Add(Screens.Dim("Drag the box anywhere; pinch it or drag its corner to resize it; tap it to choose its lines."));
        column.Children.Add(choices);
        column.Children.Add(chips);
        column.Children.Add(label);
        Chips();
        column.Children.Add(Screens.Primary("Save to gallery", () => Save(gallery: true)));
        column.Children.Add(Screens.Choice("Share", () => Save(gallery: false)));
        column.Children.Add(said);
        column.Children.Add(Screens.Choice("Back to the result", back));
        Content = Screens.Page(column);
    }

    /// <summary>The picture's box shows the lines chosen, in their order.</summary>
    private void ShowLines()
    {
        picture.Lines = [.. lines.Where(l => chosen.Contains(l.Key)).Select(l => l.Text)];
        picture.InvalidateVisual();
    }

    /// <summary>The chips under the picture, each saying what it is set to: the circle, the label, the style and the crop.</summary>
    private void Chips()
    {
        chips.Children.Clear();
        chips.Children.Add(Chip(picture.ShowCircle ? "Mean radius circle: on" : "Mean radius circle: off", picture.ShowCircle, () => picture.ShowCircle = !picture.ShowCircle));
        chips.Children.Add(Chip(label.IsVisible ? "Label: on" : "Label: off", label.IsVisible, () =>
        {
            label.IsVisible = !label.IsVisible;
            picture.Label = label.IsVisible ? label.Text : null;
        }));
        chips.Children.Add(Chip("Box: " + picture.Style switch { ShareBoxStyle.Light => "light", ShareBoxStyle.Clear => "words only", _ => "dark" }, false,
            () => picture.Style = (ShareBoxStyle)(((int)picture.Style + 1) % 3)));
        chips.Children.Add(Chip(picture.Crop is null ? "Crop: whole picture" : "Crop: around the group", picture.Crop is not null, () =>
        {
            picture.Crop = picture.Crop is null ? ShareCard.GroupArea(state, frame.Width, frame.Height) : null;
            picture.InvalidateMeasure();
        }));
    }

    private Button Chip(string words, bool on, Action change)
    {
        var chip = new Button { Content = words, MinHeight = Screens.Touch, Margin = new Thickness(0, 0, 6, 6) };
        if (on)
        {
            chip.Classes.Add(GroupLab.App.Theme.AppStyles.Chosen);
        }

        chip.Click += (_, _) =>
        {
            change();
            picture.InvalidateVisual();
            Chips();
        };
        return chip;
    }

    /// <summary>The picture as a PNG in the shared folder, then into the gallery or out through the share sheet.</summary>
    private void Save(bool gallery)
    {
        string shared = Path.Combine(Phone.Platform.CacheFolder, "shared");
        try
        {
            // Only the newest shared file is kept, as for a session file.
            if (Directory.Exists(shared))
            {
                Directory.Delete(shared, recursive: true);
            }

            Directory.CreateDirectory(shared);
            string file = Path.Combine(shared, (name.Length > 0 ? name : "GroupLab") + ".png");
            byte[] png = picture.Png();
            File.WriteAllBytes(file, png);
            DiagnosticLog.Info("share.picture", ("gallery", gallery), ("bytes", png.Length), ("lines", picture.Lines.Count));
            said.Text = gallery
                ? Phone.Platform.SaveToGallery(file, "image/png") ?? "Saved to the gallery, in Pictures, GroupLab."
                : Phone.Platform.ShareFile(file, "image/png", "Share the picture") ?? "";
        }
        catch (IOException ex)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "share.picture", ex);
            said.Text = "The picture could not be made: " + ex.Message;
        }
    }
}
