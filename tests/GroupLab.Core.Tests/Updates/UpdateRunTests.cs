using System.Text;
using GroupLab.Core.Updates;

namespace GroupLab.Core.Tests.Updates;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 123 section 2.6: a whole update, driven without a network and without an installer. The recorder answers the
/// check and the download, so every one of these runs offline and none of them can start anything on the machine it runs on.
/// <para>
/// The four the entry names are all here: a hash that does not match, a download that arrives short, a stop part way through, and the happy
/// path as far as a checked file and the switches to run it with. What happens after that is the window's, and the App tests drive it.
/// </para>
/// </summary>
public class UpdateRunTests : IDisposable
{
    private static readonly (string PrivateKeyBase64, string PublicKeyBase64) Key = UpdateSignature.NewKeyPair();

    private const string Address = "https://example.invalid/grouplab-setup-win-x64.exe";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "grouplab-updates-" + Guid.NewGuid().ToString("N"));

    private static readonly byte[] TheInstaller = Encoding.UTF8.GetBytes("this stands in for an installer");

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try
        {
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }

    private static SignedManifest Manifest(byte[] contents, long? sayBytes = null, string? saySha = null) =>
        UpdateSignature.Sign(
            new UpdateManifest(UpdateManifest.Current, "0.2.0-nightly.13", "nightly", "abc1234", "2026-09-21T00:00:00Z", "what changed",
                [new UpdateAsset("windows", "installer", "grouplab-setup-win-x64.exe", sayBytes ?? contents.LongLength, saySha ?? UpdateSignature.Sha256(contents), Address)]),
            Convert.FromBase64String(Key.PrivateKeyBase64));

    private static BuildIdentity Installed() => BuildIdentity.Read("0.2.0-nightly.12+abc1234", "nightly");

    private static RecordedOutsideWorld Offering(SignedManifest signed, byte[]? downloads)
    {
        var outside = new RecordedOutsideWorld();
        outside.Text[UpdateTrain.Nightly.ManifestAddress()!] = signed.ToJson();
        outside.Download[Address] = downloads;
        return outside;
    }

    private async Task<(UpdateRun Run, UpdateState State)> Offered(RecordedOutsideWorld outside)
    {
        var run = new UpdateRun(outside, Installed(), _folder);
        var (state, _) = await run.CheckAsync(UpdatePreferences.Default(UpdateTrain.Nightly), Key.PublicKeyBase64, CancellationToken.None);
        return (run, state);
    }

    [Fact]
    public async Task AWholeUpdateEndsInACheckedFileAndSilentSwitches()
    {
        var outside = Offering(Manifest(TheInstaller), TheInstaller);
        var (run, offered) = await Offered(outside);
        Assert.Equal(UpdateStage.Offered, offered.Stage);
        Assert.Equal("0.2.0-nightly.13", offered.Version!.Number);
        Assert.Equal("what changed", offered.Notes);

        var seen = new List<double>();
        var ready = await run.DownloadAsync("windows", "installer", new Progress<double>(seen.Add), CancellationToken.None);

        Assert.Equal(UpdateStage.ReadyToInstall, ready.Stage);
        Assert.True(run.Ready);
        Assert.Equal(TheInstaller, await File.ReadAllBytesAsync(run.Downloaded!, CancellationToken.None));

        // It never starts anything itself: the switches are handed back, and only the window decides to use them.
        Assert.Null(outside.Installer);
        Assert.Contains("/VERYSILENT", run.InstallerArguments(), StringComparison.Ordinal);
        Assert.Contains("/relaunch=yes", run.InstallerArguments(), StringComparison.Ordinal);

        // Nothing here touches the network beyond the manifest and the file the manifest names.
        Assert.Equal([("get", UpdateTrain.Nightly.ManifestAddress()!), ("download", Address)], outside.Asked);
    }

    [Fact]
    public async Task AFileThatDoesNotMatchTheManifestIsThrownAwayAndSaidSo()
    {
        // Signed properly, so the only thing wrong is the file that arrives: this is the swapped-installer case.
        var outside = Offering(Manifest(TheInstaller), Encoding.UTF8.GetBytes("something else entirely"));
        var (run, _) = await Offered(outside);

        var state = await run.DownloadAsync("windows", "installer", null, CancellationToken.None);

        Assert.Equal(UpdateStage.Refused, state.Stage);
        Assert.Equal(UpdatePolicy.DownloadDoesNotMatch, state.Says);
        Assert.False(run.Ready);
        Assert.Empty(Directory.GetFiles(_folder));
    }

    [Fact]
    public async Task ADownloadThatArrivesShortIsRefusedRatherThanRun()
    {
        // The hash is of the whole file and the length is the whole file's, but only part of it arrives. Length is checked as well as the
        // hash, so a truncation is named by the same refusal rather than depending on a hash collision never happening.
        var outside = Offering(Manifest(TheInstaller), TheInstaller[..10]);
        var (run, _) = await Offered(outside);

        var state = await run.DownloadAsync("windows", "installer", null, CancellationToken.None);

        Assert.Equal(UpdateStage.Refused, state.Stage);
        Assert.Equal(UpdatePolicy.DownloadDoesNotMatch, state.Says);
        Assert.Empty(Directory.GetFiles(_folder));
    }

    [Fact]
    public async Task StoppingPartWayThroughKeepsNothingAndSaysNothingWasInstalled()
    {
        var outside = Offering(Manifest(TheInstaller), TheInstaller);
        using var stopper = new CancellationTokenSource();
        outside.WhileDownloading = (_, _) =>
        {
            stopper.Cancel();
            return Task.CompletedTask;
        };

        var (run, _) = await Offered(outside);
        var state = await run.DownloadAsync("windows", "installer", null, stopper.Token);

        Assert.Equal(UpdateStage.Idle, state.Stage);
        Assert.Contains("stopped", state.Says, StringComparison.Ordinal);
        Assert.Contains("nothing was kept", state.Says, StringComparison.Ordinal);
        Assert.False(run.Ready);
        Assert.Empty(Directory.GetFiles(_folder));
    }

    [Fact]
    public async Task ADroppedConnectionLeavesNothingBehindAndTheNextTryStartsAgain()
    {
        // The recorder refuses the first download, as a dropped connection does. Nothing part-finished is left for the next try to resume
        // into, because a half file with the right name is exactly what would pass a length check and fail a hash one.
        var outside = Offering(Manifest(TheInstaller), null);
        var (run, _) = await Offered(outside);
        var refused = await run.DownloadAsync("windows", "installer", null, CancellationToken.None);

        Assert.Equal(UpdateStage.Refused, refused.Stage);
        Assert.Contains("GroupLab is unchanged", refused.Says, StringComparison.Ordinal);
        Assert.Empty(Directory.GetFiles(_folder));

        outside.Download[Address] = TheInstaller;
        var ready = await run.DownloadAsync("windows", "installer", null, CancellationToken.None);

        Assert.Equal(UpdateStage.ReadyToInstall, ready.Stage);
        Assert.Equal(TheInstaller, await File.ReadAllBytesAsync(run.Downloaded!, CancellationToken.None));
    }

    [Fact]
    public async Task APlatformTheBuildHasNothingForIsToldSoRatherThanOfferedTheWrongFile()
    {
        var outside = Offering(Manifest(TheInstaller), TheInstaller);
        var (run, _) = await Offered(outside);

        var state = await run.DownloadAsync("linux", "tarball", null, CancellationToken.None);

        Assert.Equal(UpdateStage.Refused, state.Stage);
        Assert.Contains("linux", state.Says, StringComparison.Ordinal);
        Assert.DoesNotContain(outside.Asked, a => a.What == "download");
    }

    [Fact]
    public async Task ACheckThatCannotReachTheTrainChangesNothingAndDownloadsNothing()
    {
        var outside = new RecordedOutsideWorld();
        var run = new UpdateRun(outside, Installed(), _folder);
        var (state, decision) = await run.CheckAsync(UpdatePreferences.Default(UpdateTrain.Nightly), Key.PublicKeyBase64, CancellationToken.None);

        Assert.Equal(UpdateStage.Idle, state.Stage);
        Assert.Contains("Nothing has changed", state.Says, StringComparison.Ordinal);
        Assert.Null(decision);
        Assert.Null(run.Manifest);
        Assert.False(Directory.Exists(_folder));
    }

    [Fact]
    public async Task AManifestSignedByAStrangerIsRefusedBeforeAnythingIsDownloaded()
    {
        var stranger = UpdateSignature.NewKeyPair();
        var signed = UpdateSignature.Sign(Manifest(TheInstaller).Payload, Convert.FromBase64String(stranger.PrivateKeyBase64));
        var outside = Offering(signed, TheInstaller);
        var (run, state) = await Offered(outside);

        Assert.Equal(UpdateStage.Refused, state.Stage);
        Assert.Null(run.Manifest);
        Assert.DoesNotContain(("download", Address), outside.Asked);

        // And with no manifest accepted there is nothing to download, whatever asks.
        var nothing = await run.DownloadAsync("windows", "installer", null, CancellationToken.None);
        Assert.Equal(UpdateStage.Refused, nothing.Stage);
    }

    [Fact]
    public async Task ADevelopmentBuildAsksTheTrainNothingAtAll()
    {
        var outside = Offering(Manifest(TheInstaller), TheInstaller);
        var run = new UpdateRun(outside, BuildIdentity.Read("0.2.0+abc1234", null), _folder);
        var (state, decision) = await run.CheckAsync(UpdatePreferences.Default(UpdateTrain.Nightly), Key.PublicKeyBase64, CancellationToken.None);

        Assert.Equal(UpdateStage.Idle, state.Stage);
        Assert.Contains("development build", state.Says, StringComparison.Ordinal);
        Assert.Null(decision);
        Assert.Empty(outside.Asked);
    }
}
