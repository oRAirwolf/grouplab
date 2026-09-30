using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 314 section 1 on Android and iOS: the setting "Caliber box shows", the desktop's own, and the caliber box
/// following it, with its list made afresh from what is typed.
/// </summary>
public class Entry314Tests
{
    [AvaloniaFact]
    public void TheCaliberBoxFollowsTheSettingAndFindsWhatIsTyped()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-entry314-{Guid.NewGuid():N}");
        var store = new AppSettingsStore(Path.Combine(folder, "settings.json"));
        try
        {
            if (Phone.Platform is null)
            {
                Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
            }

            // Settings: three choices, Both until one is chosen, and a choice is remembered.
            var view = new SettingsView(store);
            var window = new Window { Width = 412, Height = 915, Content = view };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(view.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == AppSettingsStore.CaliberListLabel);
            var choices = view.GetLogicalDescendants().OfType<RadioButton>().Where(r => r.GroupName == "caliberList").ToList();
            Assert.Equal(3, choices.Count);
            Assert.True(choices[2].IsChecked);
            choices[0].IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(CaliberList.Calibers, store.LoadCaliberList());

            // Calibers only: diameters, and no cartridge line.
            var caliber = new CaliberBox(store);
            window.Content = caliber;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(CaliberChoices.Suggest("", CaliberList.Calibers), caliber.Box.ItemsSource!.Cast<string>());

            // Both: typed with its punctuation left out, the caliber first and then the cartridge.
            store.SaveCaliberList(CaliberList.Both);
            caliber.Box.Focus();
            Dispatcher.UIThread.RunJobs();
            window.KeyTextInput("65 creed");
            Dispatcher.UIThread.RunJobs();
            var shown = caliber.Box.ItemsSource!.Cast<string>().ToList();
            Assert.Equal(CaliberChoices.Suggest("65 creed", CaliberList.Both), shown);
            Assert.StartsWith(".264 (6.5 mm)", shown[0], StringComparison.Ordinal);
            Assert.Contains("6.5 Creedmoor, 0.264 in (6.71 mm)", shown);
            window.Close();
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }
}
