using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Updates;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 501: what the Mac App Store copy does differently because it runs in Apple's App Sandbox, held where it can be
/// held off a Mac. A picture opened there is kept in GroupLab's own originals folder, once for each picture, and the session points at the
/// copy; anywhere else the picture stays where it is. The open hook takes links and files instead of a started process. The store copy's
/// update line names the store it came from. What only a Mac can show, that it starts, reads a sheet and saves a session inside the
/// sandbox, is mac-store.yml's --self-check run.
/// </summary>
public sealed class Entry501Tests : IDisposable
{
    private readonly string folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-501-{Guid.NewGuid():N}")).FullName;

    public void Dispose()
    {
        AppInfo.SandboxedOverride = null;
        TheOutsideWorld.OpensThings = null;
        GroupLab.Tests.Support.Temp.Delete(folder);
    }

    [AvaloniaFact]
    public void InTheSandboxAPictureIsKeptOnceInGroupLabsOwnFolder()
    {
        var window = new MainWindow(new AppSettingsStore(Path.Combine(folder, "settings", "settings.json"))) { Width = 1400, Height = 900 };
        try
        {
            string picture = Path.Combine(folder, "chosen", "target.PNG");
            Directory.CreateDirectory(Path.GetDirectoryName(picture)!);
            File.WriteAllBytes(picture, [1, 2, 3, 4, 5]);
            string twin = Path.Combine(folder, "chosen", "same-picture.png");
            File.Copy(picture, twin);

            AppInfo.SandboxedOverride = false;
            Assert.Equal(picture, window.KeptHere(picture));

            AppInfo.SandboxedOverride = true;
            string kept = window.KeptHere(picture);
            Assert.StartsWith(Path.Combine(folder, "settings", "originals"), kept, StringComparison.Ordinal);
            Assert.EndsWith(".png", kept, StringComparison.Ordinal);
            Assert.Equal(File.ReadAllBytes(picture), File.ReadAllBytes(kept));
            Assert.Equal(kept, window.KeptHere(twin));
            Assert.Equal(kept, window.KeptHere(kept));
            Assert.Single(Directory.GetFiles(Path.Combine(folder, "settings", "originals")));
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void TheOpenHookTakesLinksAndFilesAndTheStoreLineNamesItsStore()
    {
        var opened = new List<string>();
        TheOutsideWorld.OpensThings = what =>
        {
            opened.Add(what);
            return true;
        };
        var world = new TheOutsideWorld();
        world.OpenAddress("https://grouplab.org/");
        world.OpenFile(Path.Combine(folder, "a.pdf"));
        Assert.Equal(["https://grouplab.org/", Path.Combine(folder, "a.pdf")], opened);

        Assert.Contains("Mac App Store", MainWindow.MacStoreUpdateWords, StringComparison.Ordinal);
        Assert.Contains("Microsoft Store", MainWindow.MicrosoftStoreUpdateWords, StringComparison.Ordinal);
        Assert.Equal(OperatingSystem.IsMacOS() ? MainWindow.MacStoreUpdateWords : MainWindow.MicrosoftStoreUpdateWords, MainWindow.StoreUpdateWords);
    }
}
