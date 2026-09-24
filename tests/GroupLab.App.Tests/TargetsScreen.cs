using Avalonia.Controls;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 155: the print window became the print panel of the Targets screen, so the tests that covered it open that
/// screen and hold its panel, rather than a window of its own that nothing in the application opens any more.
/// </summary>
internal static class TargetsScreen
{
    public static PrintPanel Open(int width = 1200, int height = 800)
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        var main = new MainWindow(store) { Width = width, Height = height };
        main.Show();
        main.ShowLibrary();
        Dispatcher.UIThread.RunJobs();
        return main.TargetsPanel;
    }

    /// <summary>Closes the window the panel is on.</summary>
    public static void Close(this PrintPanel panel) => (TopLevel.GetTopLevel(panel) as Window)?.Close();
}
