using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Updates;

namespace GroupLab.App;

/// <summary>
/// The updater as a person meets it, NOTES-FROM-PLANNING.md entry 123 section 2: a bar under the header that says what was found, downloads
/// in the background with a way to stop it, checks what arrived, and installs it silently after saving the person's work.
/// <para>
/// <b>The mechanism is the Inno Setup installer run silently</b> (entry 119 section 4.6), started through <see cref="IOutsideWorld"/> so a
/// test drives the whole sequence against the recorder and no test ever starts an installer. The reasons are in <c>docs/UPDATES.md</c>.
/// </para>
/// <para>
/// Nothing here surprises anybody. The bar is a bar, not a dialog; it never takes the keyboard; Later leaves it until the next check and Skip
/// silences one version rather than all of them; and no installation begins until a person presses Update now.
/// </para>
/// </summary>
public partial class MainWindow
{
    /// <summary>Where a downloaded installer goes: a folder GroupLab owns, beside its logs, never the machine's temporary directory.</summary>
    internal static string UpdateFolder { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GroupLab", "updates");

    /// <summary>
    /// What this build is and whose key it trusts, in one place. They are what the build stamped in and what is compiled into it; a test
    /// sets them, because a whole update cannot otherwise be driven from a working copy, which is a development build that trusts Alan's
    /// key and so would refuse every manifest a test could sign. Nothing in the application ever writes to them.
    /// </summary>
    internal static BuildIdentity ThisBuild { get; set; } = AppInfo.Build;

    internal static string TrustedKey { get; set; } = UpdateKeys.PublicKey;

    /// <summary>
    /// Whether opening the window looks for an update, entry 119 section 4.2, where on every launch is the default. The tests turn it off,
    /// for the same reason entry 76 turned detection off: a window opened by a test should do what the test asked and nothing else.
    /// </summary>
    internal static bool CheckOnLaunchByDefault { get; set; } = true;

    private readonly Border updateBar = new() { IsVisible = false, Classes = { AppStyles.Bar } };
    private readonly TextBlock updateSays = new() { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(Tokens.Space16, Tokens.Space4) };
    private readonly ProgressBar updateProgress = new() { Width = 160, Height = 6, Minimum = 0, Maximum = 1, IsVisible = false, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(Tokens.Space8, 0) };
    private readonly StackPanel updateButtons = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(Tokens.Space8, 2) };

    private UpdateRun? updateRun;
    private CancellationTokenSource? updateStopper;
    private UpdateState updateNow = new(UpdateStage.Idle, "");

    /// <summary>What the bar is saying and doing, for the headless tests.</summary>
    internal UpdateState UpdateNow => updateNow;

    /// <summary>Whether the bar is showing, for the headless tests.</summary>
    internal bool ShowingUpdateBar => updateBar.IsVisible;

    private Control BuildUpdateBar()
    {
        var row = new DockPanel();
        DockPanel.SetDock(updateButtons, Dock.Right);
        DockPanel.SetDock(updateProgress, Dock.Right);
        row.Children.Add(updateButtons);
        row.Children.Add(updateProgress);
        row.Children.Add(updateSays);
        updateBar.Child = row;
        return updateBar;
    }

    /// <summary>
    /// Looks for a newer build, entry 119 sections 4.1 and 4.2. It is the only thing on this screen that reaches the network, it goes through
    /// the one way out of the process so no test can, and it says what it found either way.
    /// </summary>
    internal void CheckForUpdates() => _ = CheckForUpdatesAsync(byHand: true);

    /// <summary>
    /// The check itself. By hand it reports everything, including "you are up to date"; on a launch it says nothing when there is nothing to
    /// say, because an update bar that appears to tell you there is no update is a bar nobody wants.
    /// </summary>
    internal async Task CheckForUpdatesAsync(bool byHand, CancellationToken token = default)
    {
        updates = updates with { LastCheckUtc = DateTimeOffset.UtcNow };
        settingsStore.SaveUpdatePreferences(updates);

        if (ThisBuild.IsDevelopment)
        {
            updateState.Text = "This is a development build, so there is nothing to update it to.";
            return;
        }

        if (TrustedKey.Length == 0)
        {
            updateState.Text = UpdateSignature.Refusal.NoKey.Words();
            DiagnosticLog.Info("update.check", ("result", "no key"));
            return;
        }

        updateState.Text = "Looking for a newer build…";
        DiagnosticLog.Info("update.check", ("train", updates.Train.Words()));

        var run = new UpdateRun(TheOutsideWorld.Current, ThisBuild, UpdateFolder);
        var (state, decision) = await run.CheckAsync(updates, TrustedKey, token).ConfigureAwait(true);
        updateRun = state.Stage == UpdateStage.Offered ? run : null;
        updateState.Text = state.Says;
        DiagnosticLog.Info("update.check", ("result", state.Stage.ToString()), ("refusal", decision?.Refusal.ToString() ?? "none"));

        // Entry 119 section 4.1: a check that finds nothing is silent unless a person asked for it.
        Show(state, byHand || state.Stage is UpdateStage.Offered or UpdateStage.Refused);
    }

    /// <summary>Draws one state: the words, the share done, and the buttons that make sense at that moment and no others.</summary>
    private void Show(UpdateState state, bool visible = true)
    {
        updateNow = state;
        updateSays.Text = state.Says;
        updateProgress.Value = state.Share;
        updateProgress.IsVisible = state.Stage == UpdateStage.Downloading;
        updateSays.Classes.Set(AppStyles.Warn, state.Stage == UpdateStage.Refused);
        updateButtons.Children.Clear();

        switch (state.Stage)
        {
            case UpdateStage.Offered:
                // Only the Windows installer can replace GroupLab where it stands. The zip and the tarball are unpacked wherever their owner
                // put them, and writing over that is not GroupLab's to do, so those say where the build is and leave it to the person.
                updateButtons.Children.Add(UpdateAssets.CanInstallItself
                    ? Primary("Update now", () => StartUpdateAsync())
                    : Primary("Get the download", ShowUpdateNotes));
                if (state.Notes is { Length: > 0 })
                {
                    updateButtons.Children.Add(Button("What changed", ShowUpdateNotes));
                }

                updateButtons.Children.Add(Button("Later", () => Show(updateNow with { Stage = UpdateStage.Idle }, visible: false)));
                updateButtons.Children.Add(Button("Skip this version", SkipThisVersion));
                break;

            case UpdateStage.Downloading:
                updateButtons.Children.Add(Button("Stop", StopUpdate));
                break;

            case UpdateStage.ReadyToInstall:
                updateButtons.Children.Add(Primary("Install and restart", InstallUpdate));
                updateButtons.Children.Add(Button("Later", () => Show(updateNow with { Stage = UpdateStage.Idle }, visible: false)));
                break;

            case UpdateStage.Installing:
                break;

            default:
                updateButtons.Children.Add(Button("Hide", () => updateBar.IsVisible = false));
                break;
        }

        updateBar.IsVisible = visible && state.Says.Length > 0;
        updateState.Text = state.Says;
    }

    private static Button Primary(string label, Action action)
    {
        var button = Button(label, action);
        button.Classes.Add(AppStyles.Primary);
        return button;
    }

    private static Button Primary(string label, Func<Task> action)
    {
        var button = Button(label, action);
        button.Classes.Add(AppStyles.Primary);
        return button;
    }

    /// <summary>The notes for the build being offered, on the page they were published to. Nothing is fetched to show them.</summary>
    private void ShowUpdateNotes()
    {
        if (updateRun?.Manifest is { } signed)
        {
            OpenInTheBrowser("https://github.com/oRAirwolf/grouplab/releases/tag/v" + signed.Payload.Version);
        }
    }

    /// <summary>Skip means silence about this one version until something newer appears, entry 119 section 4.4. It is not "never again".</summary>
    private void SkipThisVersion()
    {
        if (updateNow.Version is { } version)
        {
            updates = updates with { SkippedVersion = version.Number };
            settingsStore.SaveUpdatePreferences(updates);
            DiagnosticLog.Info("update.skip", ("version", version.Number));
        }

        updateRun = null;
        Show(new UpdateState(UpdateStage.Idle, ""), visible: false);
    }

    /// <summary>
    /// Downloads the offered build in the background, with the share done in the bar and a Stop beside it. Nothing is installed here: a file
    /// that arrives whole and matches its hash leaves the bar offering to install it, and a person decides.
    /// </summary>
    internal async Task StartUpdateAsync()
    {
        if (updateRun is not { } run)
        {
            return;
        }

        updateStopper?.Dispose();
        updateStopper = new CancellationTokenSource();
        var token = updateStopper.Token;

        Show(new UpdateState(UpdateStage.Downloading, "Downloading GroupLab " + (updateNow.Version?.Number ?? "the newer build") + "…", updateNow.Version, 0, updateNow.Notes));
        var progress = new Progress<double>(share => Show(updateNow with { Share = share }));

        var state = await run.DownloadAsync(UpdateAssets.Platform, UpdateAssets.Kind, progress, token).ConfigureAwait(true);
        DiagnosticLog.Info("update.download", ("result", state.Stage.ToString()));
        Show(state);
    }

    /// <summary>Stop: the part file goes, nothing is kept, and the offer stands so a person can try again.</summary>
    internal void StopUpdate()
    {
        updateStopper?.Cancel();
        DiagnosticLog.Info("update.download", ("result", "stopped"));
    }

    /// <summary>
    /// Entry 123 section 2.3. In order: save everything, say in one line what is about to happen, write down what this version was and where
    /// the person was, start the installer silently, and close. The installer relaunches GroupLab, which reads that note and says so.
    /// </summary>
    internal void InstallUpdate()
    {
        if (updateRun is not { Ready: true } run || run.Downloaded is not { } installer)
        {
            return;
        }

        SaveSession();
        settingsStore.SaveUpdatePreferences(updates with { SkippedVersion = null });
        settingsStore.SaveHandover(ThisBuild.Version.Number, destination.ToString());

        Show(new UpdateState(UpdateStage.Installing,
            "GroupLab will close and reopen in a moment to finish updating to " + (updateNow.Version?.Number ?? "the newer build") + ".",
            updateNow.Version));

        string arguments = run.InstallerArguments();
        DiagnosticLog.Info("update.install", ("version", updateNow.Version?.Number ?? ""), ("silent", "yes"));
        TheOutsideWorld.Current.StartInstaller(installer, arguments);
        CloseForUpdate();
    }

    /// <summary>Closes the window so the installer can replace the files. A test replaces it, so the sequence runs without ending the run.</summary>
    internal Action CloseForUpdate { get; set; } = () => { };

    /// <summary>
    /// The first launch after an update, entry 123 section 2.4: one line saying what it updated from and to, with a way to read what changed,
    /// said once. It is the same bar, so nothing new appears on the screen and nothing has to be dismissed before working.
    /// </summary>
    private void SayIfUpdated()
    {
        if (settingsStore.LoadHandover() is not { } handover)
        {
            return;
        }

        settingsStore.ClearHandover();
        if (Enum.TryParse(handover.Screen, out Destination screen) && Enum.IsDefined(screen))
        {
            Go(screen);
        }

        if (string.Equals(handover.From, ThisBuild.Version.Number, StringComparison.Ordinal))
        {
            // The installer ran but this is the same version: say nothing rather than claim an update that did not happen.
            return;
        }

        DiagnosticLog.Info("update.arrived", ("from", handover.From), ("to", ThisBuild.Version.Number));
        Show(new UpdateState(UpdateStage.Idle, UpdatedLine(handover.From, ThisBuild.Version.Number), ThisBuild.Version, 0,
            "https://github.com/oRAirwolf/grouplab/releases/tag/v" + ThisBuild.Version.Number));
        updateButtons.Children.Clear();
        updateButtons.Children.Add(Button("What changed", () => OpenInTheBrowser("https://github.com/oRAirwolf/grouplab/releases/tag/v" + ThisBuild.Version.Number)));
        updateButtons.Children.Add(Button("Hide", () => updateBar.IsVisible = false));
    }

    /// <summary>The line the new version says about itself, kept here so a test reads the same words the window shows.</summary>
    internal static string UpdatedLine(string from, string to) =>
        string.Create(CultureInfo.InvariantCulture, $"GroupLab updated from {from} to {to}.");
}

/// <summary>Which asset in a manifest this machine installs. One place, so the window does not carry an operating system test around.</summary>
public static class UpdateAssets
{
    /// <summary>The platform word used in a manifest.</summary>
    public static string Platform =>
        OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "macos" : "linux";

    /// <summary>
    /// What is installed on this platform. Windows has the installer, which is the whole mechanism; elsewhere there is an archive, and
    /// entry 119 section 4.6's silent install is a Windows story until somebody asks for another one.
    /// </summary>
    public static string Kind => OperatingSystem.IsWindows() ? "installer" : OperatingSystem.IsMacOS() ? "zip" : "tarball";

    /// <summary>
    /// Whether a build on this platform can replace itself. Only the Windows installer knows where GroupLab was put and how to put the new
    /// one in the same place; an archive was unpacked wherever its owner chose, and GroupLab does not write over a folder it did not make.
    /// </summary>
    public static bool CanInstallItself => OperatingSystem.IsWindows();
}
