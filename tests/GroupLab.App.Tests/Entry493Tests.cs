using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 493 on the computer: Check your printer's fifth way, Numbers from another GroupLab, taken as a person would:
/// a number outside any printer's scale refused in plain words, the two numbers kept as a check that says it was typed in, and Printers and
/// paper showing both numbers plainly for the next device to copy.
/// </summary>
public class Entry493Tests
{
    private static void Press(Window owner, string words)
    {
        owner.GetVisualDescendants().OfType<Button>().First(b => b.Content is string s && s == words)
            .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void NumbersFromAnotherGroupLabAreTakenAsShownAndSaySo()
    {
        string path = Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json");
        var store = new AppSettingsStore(path);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        try
        {
            window.OpenPrinterCheck("Office laser");
            Dispatcher.UIThread.RunJobs();
            var wizard = window.OwnedWindows.Single();
            var choice = wizard.GetVisualDescendants().OfType<RadioButton>()
                .Single(r => r.Content is StackPanel p && p.Children.OfType<TextBlock>().Any(t => t.Text == PrinterProfile.TypedTitle));
            Assert.Contains(((StackPanel)choice.Content!).Children.OfType<TextBlock>(), t => t.Text == PrinterProfile.TypedDetail);
            choice.IsChecked = true;
            Press(wizard, "Next");
            Assert.Contains(wizard.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == PrinterProfile.TypedHelp);

            var boxes = wizard.GetVisualDescendants().OfType<TextBox>().ToList();
            Assert.Equal(2, boxes.Count);
            boxes[0].Text = "80";
            boxes[1].Text = "99.41";
            Press(wizard, "Use these");
            Assert.Contains(wizard.GetVisualDescendants().OfType<TextBlock>(), t => t.IsVisible && (t.Text ?? "").Contains("80% is outside it", StringComparison.Ordinal));
            Assert.Empty(store.LoadPrinters());

            boxes[0].Text = "99.23%";
            Press(wizard, "Use these");
            Assert.Contains(wizard.GetVisualDescendants().OfType<TextBlock>(), t => (t.Text ?? "").Contains("typed in from another device's check on", StringComparison.Ordinal));
            Press(wizard, "Save and finish");

            var kept = Assert.Single(store.LoadPrinters());
            Assert.Equal(PrinterMethod.Typed, kept.Method);
            Assert.Equal(0.9923, kept.Across, 10);
            Assert.Equal(0.9941, kept.Down, 10);

            window.ShowSettings(SettingsCategory.Printers);
            Dispatcher.UIThread.RunJobs();
            var said = window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
            Assert.Contains("To type into another GroupLab: 99.23% across, 99.41% down", said);
            Assert.Contains(said, t => t.StartsWith("Office laser: 99.2 by 99.4%, typed in from another device's check on ", StringComparison.Ordinal));
        }
        finally
        {
            window.Close();
            File.Delete(path);
        }
    }
}
