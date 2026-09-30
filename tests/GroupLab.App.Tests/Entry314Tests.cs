using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 314 section 1 on the desktop: the setting "Caliber box shows", Calibers, Cartridges or Both, Both until one
/// is chosen, remembered, and followed by the caliber box at once.
/// </summary>
public class Entry314Tests
{
    [AvaloniaFact]
    public void TheCaliberBoxOffersWhatTheSettingSays()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            var store = window.SettingsStore;
            Assert.Equal(CaliberList.Both, store.LoadCaliberList());
            var box = window.CalibreBox;
            Assert.Equal(CaliberChoices.Suggest("", CaliberList.Both), box.ItemsSource!.Cast<string>());

            window.ShowSettings();
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == AppSettingsStore.CaliberListLabel);
            var choices = window.GetLogicalDescendants().OfType<RadioButton>().Where(r => r.GroupName == "caliberList").ToList();
            Assert.Equal(3, choices.Count);
            Assert.True(choices[2].IsChecked);

            // Calibers only: the list holds diameters and no cartridge line, and the choice is remembered.
            choices[0].IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(CaliberList.Calibers, store.LoadCaliberList());
            Assert.Equal(CaliberChoices.Suggest("", CaliberList.Calibers), box.ItemsSource!.Cast<string>());
            Assert.All(box.ItemsSource!.Cast<string>(), l => Assert.StartsWith(".", l, StringComparison.Ordinal));

            // Cartridges only: names, each with its diameter.
            window.SetCaliberList(CaliberList.Cartridges);
            Assert.Equal(CaliberList.Cartridges, store.LoadCaliberList());
            Assert.Contains("6.5 Creedmoor, 0.264 in (6.71 mm)", box.ItemsSource!.Cast<string>());
            Assert.DoesNotContain(box.ItemsSource!.Cast<string>(), l => l.StartsWith(".264 (", StringComparison.Ordinal));
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
