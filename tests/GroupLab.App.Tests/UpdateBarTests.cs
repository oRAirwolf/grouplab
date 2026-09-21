using System.Text;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.Core.Updates;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 123 section 2.6: the update bar driven from a check to a started installer, with the recorder in the place of
/// the network and of the installer, so the whole sequence runs with nothing reached and nothing started.
/// <para>
/// The sequence the entry asks for is the last test here: save, say one line, close, start the installer silently, and reopen saying what it
/// updated from and to, on the screen the person was on.
/// </para>
/// </summary>
public class UpdateBarTests : IDisposable
{
    private static readonly (string PrivateKeyBase64, string PublicKeyBase64) Key = UpdateSignature.NewKeyPair();

    private const string Address = "https://example.invalid/grouplab-setup-win-x64.exe";

    private static readonly byte[] TheInstaller = Encoding.UTF8.GetBytes("this stands in for an installer");

    private readonly string _settings = Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json");

    private readonly BuildIdentity _wasBuild = MainWindow.ThisBuild;

    private readonly string _wasKey = MainWindow.TrustedKey;

    public UpdateBarTests()
    {
        // This working copy is a development build that trusts Alan's key, and neither can sign a manifest a test made. The window is told
        // what to be for the length of one test, and put back afterwards.
        TestDefaults.Outside.Forget();
        MainWindow.ThisBuild = BuildIdentity.Read("0.2.0-nightly.12+abc1234", "nightly");
        MainWindow.TrustedKey = Key.PublicKeyBase64;
        TestDefaults.Outside.Text[UpdateTrain.Nightly.ManifestAddress()!] = Signed().ToJson();
        TestDefaults.Outside.Download[Address] = TheInstaller;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        MainWindow.ThisBuild = _wasBuild;
        MainWindow.TrustedKey = _wasKey;
        TestDefaults.Outside.Forget();
        try
        {
            if (File.Exists(_settings))
            {
                File.Delete(_settings);
            }
        }
        catch (IOException)
        {
        }
    }

    private static SignedManifest Signed(string version = "0.2.0-nightly.13") =>
        UpdateSignature.Sign(
            new UpdateManifest(UpdateManifest.Current, version, "nightly", "def5678", "2026-09-21T08:00:00Z", "what changed",
                [
                    new UpdateAsset("windows", "installer", "grouplab-setup-win-x64.exe", TheInstaller.LongLength, UpdateSignature.Sha256(TheInstaller), Address),
                    new UpdateAsset("linux", "tarball", "grouplab-linux-x64.tar.gz", TheInstaller.LongLength, UpdateSignature.Sha256(TheInstaller), Address),
                    new UpdateAsset("macos", "zip", "grouplab-osx-x64.zip", TheInstaller.LongLength, UpdateSignature.Sha256(TheInstaller), Address),
                ]),
            Convert.FromBase64String(Key.PrivateKeyBase64));

    private MainWindow Open()
    {
        var window = new MainWindow(new AppSettingsStore(_settings)) { Width = 1400, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static void Click(MainWindow window, string label)
    {
        var button = window.GetLogicalDescendants().OfType<Button>().First(b => Equals(b.Content, label));
        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>What this platform's manifest entry is called, so the assertions read the same on all three.</summary>
    private static string ThePlatformsFile => UpdateAssets.Kind switch
    {
        "installer" => "grouplab-setup-win-x64.exe",
        "zip" => "grouplab-osx-x64.zip",
        _ => "grouplab-linux-x64.tar.gz",
    };

    private static bool Has(MainWindow window, string label) =>
        window.GetLogicalDescendants().OfType<Button>().Any(b => Equals(b.Content, label) && b.IsEffectivelyVisible);

    [AvaloniaFact]
    public async Task ACheckThatFindsANewerBuildOffersItAndInstallsNothing()
    {
        var window = Open();
        await window.CheckForUpdatesAsync(byHand: true);
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.ShowingUpdateBar);
        Assert.Equal(UpdateStage.Offered, window.UpdateNow.Stage);
        Assert.Equal("0.2.0-nightly.13", window.UpdateNow.Version!.Number);
        // Only Windows can replace GroupLab where it stands; elsewhere the bar points at the download instead of offering to install it.
        Assert.True(Has(window, UpdateAssets.CanInstallItself ? "Update now" : "Get the download"));
        Assert.True(Has(window, "Skip this version"));

        // Nothing was downloaded and nothing was started: an offer is an offer.
        Assert.DoesNotContain(TestDefaults.Outside.Asked, a => a.What is "download" or "installer");
        Assert.Null(TestDefaults.Outside.Installer);
        window.Close();
    }

    [AvaloniaFact]
    public async Task UpdateNowDownloadsAndChecksTheFileAndThenWaitsToBeTold()
    {
        var window = Open();
        await window.CheckForUpdatesAsync(byHand: true);
        await window.StartUpdateAsync();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(UpdateStage.ReadyToInstall, window.UpdateNow.Stage);
        Assert.Equal(1, window.UpdateNow.Share);
        Assert.True(Has(window, "Install and restart"));

        // The file arrived and was checked, and still nothing has been started.
        Assert.Contains(TestDefaults.Outside.Asked, a => a == ("download", Address));
        Assert.Null(TestDefaults.Outside.Installer);
        window.Close();
    }

    [AvaloniaFact]
    public async Task AFileThatDoesNotMatchIsRefusedInWordsAndNothingIsStarted()
    {
        TestDefaults.Outside.Download[Address] = Encoding.UTF8.GetBytes("not the installer at all");
        var window = Open();
        await window.CheckForUpdatesAsync(byHand: true);
        await window.StartUpdateAsync();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(UpdateStage.Refused, window.UpdateNow.Stage);
        Assert.Equal(UpdatePolicy.DownloadDoesNotMatch, window.UpdateNow.Says);
        Assert.False(Has(window, "Install and restart"));
        Assert.Null(TestDefaults.Outside.Installer);
        window.Close();
    }

    [AvaloniaFact]
    public async Task StoppingADownloadLeavesTheApplicationExactlyAsItWas()
    {
        var window = Open();
        await window.CheckForUpdatesAsync(byHand: true);

        // Stop is pressed while the download is part way through, which is the only moment it exists.
        TestDefaults.Outside.WhileDownloading = (_, _) =>
        {
            Assert.Equal(UpdateStage.Downloading, window.UpdateNow.Stage);
            Assert.True(Has(window, "Stop"));
            window.StopUpdate();
            return Task.CompletedTask;
        };

        await window.StartUpdateAsync();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(UpdateStage.Idle, window.UpdateNow.Stage);
        Assert.Contains("stopped", window.UpdateNow.Says, StringComparison.Ordinal);
        Assert.Null(TestDefaults.Outside.Installer);
        window.Close();
    }

    [AvaloniaFact]
    public async Task SkipSilencesOneVersionAndNotTheNextOne()
    {
        var window = Open();
        await window.CheckForUpdatesAsync(byHand: true);
        Click(window, "Skip this version");

        Assert.Equal("0.2.0-nightly.13", window.UpdatePreferencesNow.SkippedVersion);
        Assert.False(window.ShowingUpdateBar);

        await window.CheckForUpdatesAsync(byHand: true);
        Assert.NotEqual(UpdateStage.Offered, window.UpdateNow.Stage);

        // Something newer than the skipped build is offered again, because Skip was never "stop asking".
        TestDefaults.Outside.Text[UpdateTrain.Nightly.ManifestAddress()!] = Signed("0.2.0-nightly.14").ToJson();
        await window.CheckForUpdatesAsync(byHand: true);
        Assert.Equal(UpdateStage.Offered, window.UpdateNow.Stage);
        Assert.Equal("0.2.0-nightly.14", window.UpdateNow.Version!.Number);
        window.Close();
    }

    /// <summary>
    /// Entry 123 sections 2.3 and 2.4, the sequence itself: the session is saved, one line says what is about to happen, the installer is
    /// started silently, the window closes, and the version that comes back says what it updated from, on the screen the person was on.
    /// </summary>
    [AvaloniaFact]
    public async Task SaveCloseInstallAndReopenOnTheScreenTheyWereOn()
    {
        var window = Open();
        window.ShowLibrary();
        Dispatcher.UIThread.RunJobs();

        bool closed = false;
        window.CloseForUpdate = () => closed = true;

        await window.CheckForUpdatesAsync(byHand: true);
        await window.StartUpdateAsync();
        Click(window, "Install and restart");

        // One line, and it says what is about to happen rather than asking anything.
        Assert.Equal(UpdateStage.Installing, window.UpdateNow.Stage);
        Assert.Contains("close and reopen", window.UpdateNow.Says, StringComparison.Ordinal);
        Assert.Contains("0.2.0-nightly.13", window.UpdateNow.Says, StringComparison.Ordinal);

        // Started silently, through the one way out, with the switches that show no window and ask for no restart.
        var installer = Assert.NotNull(TestDefaults.Outside.Installer);
        Assert.EndsWith(ThePlatformsFile, installer.Path, StringComparison.Ordinal);
        Assert.Contains("/VERYSILENT", installer.Arguments, StringComparison.Ordinal);
        Assert.Contains("/SUPPRESSMSGBOXES", installer.Arguments, StringComparison.Ordinal);
        Assert.Contains("/relaunch=yes", installer.Arguments, StringComparison.Ordinal);
        Assert.True(closed);
        window.Close();

        // What the installer relaunches: the same settings, a newer build. It says so once, and goes back where the person was.
        MainWindow.ThisBuild = BuildIdentity.Read("0.2.0-nightly.13+def5678", "nightly");
        var after = Open();
        Assert.True(after.ShowingUpdateBar);
        Assert.Equal(MainWindow.UpdatedLine("0.2.0-nightly.12", "0.2.0-nightly.13"), after.UpdateNow.Says);
        Assert.True(after.ShowingLibrary);
        after.Close();

        // Said once. The launch after that has nothing to say and shows no bar.
        var later = Open();
        Assert.False(later.ShowingUpdateBar);
        later.Close();
    }
}
