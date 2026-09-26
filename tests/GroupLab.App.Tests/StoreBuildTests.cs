using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using GroupLab.Core.Updates;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 224 section 3.1: the Microsoft Store's copy of GroupLab is kept up to date by the Store, so its own updater
/// is off. A check, even one asked for by hand, looks at nothing on the network, and Settings says who updates it where the update controls
/// would be.
/// </summary>
public class StoreBuildTests
{
    private static RecordedOutsideWorld Outside => TestDefaults.Outside;

    [AvaloniaFact]
    public async Task TheStoresCopyNeverUpdatesItselfAndSaysWhoDoes()
    {
        string root = Path.Combine(Path.GetTempPath(), $"grouplab-store-{Guid.NewGuid():N}");
        AppInfo.FromStoreOverride = true;
        Outside.Forget();
        try
        {
            var store = new AppSettingsStore(Path.Combine(root, "settings.json"));
            store.SaveUnits(UnitSettings.Imperial);
            var window = new MainWindow(store) { Width = 1400, Height = 900 };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            await window.CheckForUpdatesAsync(byHand: true);
            Assert.DoesNotContain(Outside.Asked, a => a.What == "get");

            window.ShowSettings();
            Dispatcher.UIThread.RunJobs();
            var said = window.SettingsBody.GetVisualDescendants().OfType<TextBlock>().Where(b => b.IsEffectivelyVisible).Select(b => b.Text ?? "").ToList();
            Assert.Contains(MainWindow.StoreUpdateWords, said);
            Assert.DoesNotContain(window.SettingsBody.GetVisualDescendants().OfType<Button>(), b => b.IsEffectivelyVisible && Equals(b.Content, "Check now"));
            window.Close();
        }
        finally
        {
            AppInfo.FromStoreOverride = null;
            Outside.Forget();
            GroupLab.Tests.Support.Temp.Delete(root);
        }
    }
}
