using GroupLab.Core.StoreTargets;
using GroupLab.Core.Updates;
using GroupLab.Tests.Support;

namespace GroupLab.Core.Tests.StoreTargets;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 347: a newer store-bought fingerprint library reaches the application through the signed update manifest,
/// checked by the manifest's SHA-256 and by its own signature, with grouplab.org's copy trusted only by its signature and the built-in
/// library as the floor. Every library here holds the shipped products, so installing one changes nothing another test recognizes.
/// </summary>
public class StoreLibraryUpdateTests
{
    private static readonly (string PrivateKeyBase64, string PublicKeyBase64) Key = UpdateSignature.NewKeyPair();

    private const string LibraryAddress = "https://example.invalid/nightly/" + StoreLibraryUpdate.FileName;

    private static string Library(int version, string? privateKey = null) =>
        StoreLibraryFile.Sign(StoreLibraryFile.Payload(version, StoreTargetLibrary.Shipped.Select(t => new TargetReference(t, t.Fingerprint, ScaleSource.Scan, 0, ""))),
            Convert.FromBase64String(privateKey ?? Key.PrivateKeyBase64));

    /// <summary>The nightly's manifest listing <paramref name="library"/> beside an installer, signed with <paramref name="privateKey"/>.</summary>
    private static RecordedOutsideWorld Publishing(string library, string? sayShaOf = null, string? privateKey = null)
    {
        var manifest = new UpdateManifest(UpdateManifest.Current, "0.2.0-nightly.160", "nightly", "abc1234", "2026-10-02T00:00:00Z", "what changed",
        [
            new UpdateAsset("windows", "installer", "grouplab-setup-win-x64.exe", 10, new string('0', 64), "https://example.invalid/setup.exe"),
            new UpdateAsset(StoreLibraryUpdate.Platform, StoreLibraryUpdate.Kind, StoreLibraryUpdate.FileName, library.Length,
                UpdateSignature.Sha256(System.Text.Encoding.UTF8.GetBytes(sayShaOf ?? library)), LibraryAddress),
        ]);
        var outside = new RecordedOutsideWorld();
        outside.Text[UpdateTrain.Nightly.PublishedAddress()!] = UpdateSignature.Publish(manifest, Convert.FromBase64String(privateKey ?? Key.PrivateKeyBase64)).ToJson();
        outside.Text[LibraryAddress] = library;
        return outside;
    }

    private static async Task<(string Said, bool Kept)> Check(RecordedOutsideWorld outside, string folder) =>
        (await StoreLibraryUpdate.CheckAsync(outside, folder, UpdateTrain.Nightly, Key.PublicKeyBase64), File.Exists(StoreLibraryUpdate.SavedPath(folder)));

    [Fact]
    public async Task ANewerLibraryIsKeptAndInstalledOnceAndNotFetchedAgain()
    {
        string folder = Temp.Folder("library-update");
        try
        {
            int newer = StoreTargetLibrary.BuiltInVersion + 1;
            var outside = Publishing(Library(newer));
            var (said, kept) = await Check(outside, folder);
            Assert.Equal($"installed library version {newer}, {StoreTargetLibrary.Shipped.Count} products", said);
            Assert.True(kept);

            // The next check sees the kept file's SHA-256 in the manifest and reads no library at all.
            outside.Text.Remove(LibraryAddress);
            Assert.Equal("the kept library is the newest", (await Check(outside, folder)).Said);

            // And the next start installs the kept one without a network.
            Assert.StartsWith($"the kept library, version {newer}", StoreLibraryUpdate.LoadSaved(folder, Key.PublicKeyBase64), StringComparison.Ordinal);
        }
        finally
        {
            StoreTargetLibrary.Uninstall();
            Temp.Delete(folder);
        }
    }

    [Fact]
    public async Task NothingIsUsedThatDoesNotCheck()
    {
        string folder = Temp.Folder("library-update");
        try
        {
            int newer = StoreTargetLibrary.BuiltInVersion + 1;

            // A library that is not the file the manifest names.
            Assert.Equal("the library did not match the manifest's SHA-256, so it was not used",
                (await Check(Publishing(Library(newer), sayShaOf: "something else"), folder)).Said);

            // A manifest signed by another key.
            var (otherPrivate, _) = UpdateSignature.NewKeyPair();
            Assert.Equal("the update manifest's signature did not check, so no library was read",
                (await Check(Publishing(Library(newer), privateKey: otherPrivate), folder)).Said);

            // A library listed honestly but signed by another key.
            string forged = Library(newer, otherPrivate);
            Assert.Equal("the library was refused: BadSignature", (await Check(Publishing(forged), folder)).Said);

            // A library no newer than the built-in one is the floor's equal and is left alone.
            Assert.StartsWith("the newest library, version", (await Check(Publishing(Library(StoreTargetLibrary.BuiltInVersion)), folder)).Said, StringComparison.Ordinal);
            Assert.False(File.Exists(StoreLibraryUpdate.SavedPath(folder)));

            // A kept file changed on the disk is refused at the next start.
            Directory.CreateDirectory(folder);
            File.WriteAllText(StoreLibraryUpdate.SavedPath(folder), forged);
            Assert.Equal("the kept library was refused: BadSignature", StoreLibraryUpdate.LoadSaved(folder, Key.PublicKeyBase64));
        }
        finally
        {
            StoreTargetLibrary.Uninstall();
            Temp.Delete(folder);
        }
    }

    [Fact]
    public async Task GroupLabOrgsCopyIsReadWhenGitHubIsNotAndOnlyByItsSignature()
    {
        string folder = Temp.Folder("library-update");
        try
        {
            int newer = StoreTargetLibrary.BuiltInVersion + 1;
            var outside = new RecordedOutsideWorld();
            Assert.Equal("no library could be reached", (await Check(outside, folder)).Said);

            outside.Text[StoreLibraryUpdate.Mirror] = Library(newer, UpdateSignature.NewKeyPair().PrivateKeyBase64);
            Assert.Equal("the library was refused: BadSignature", (await Check(outside, folder)).Said);

            outside.Text[StoreLibraryUpdate.Mirror] = Library(newer);
            Assert.StartsWith($"installed library version {newer}", (await Check(outside, folder)).Said, StringComparison.Ordinal);
        }
        finally
        {
            StoreTargetLibrary.Uninstall();
            Temp.Delete(folder);
        }
    }
}
