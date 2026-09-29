using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using OpenCvSharp;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 292 section 1.6: every way a photograph reaches GroupLab from another app, tested where it runs off the
/// phone. The system photo picker and the apps chooser (and the fall back to the apps where a phone has no photo picker), a picture shared
/// or opened from any app, several at once as a set, a photograph kept only in the cloud fetched with progress and Cancel, a clear sentence
/// when it cannot be fetched, and a reduced copy said to be one. The pickers themselves open only on a phone; the Fold 7 sitting tries them.
/// </summary>
public class PhotoIntakeTests
{
    private static readonly string Photo = Repo.PathTo("scans", "phase0", "20260913_130543.jpg");

    private static string Folder() => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "grouplab-intake-" + Guid.NewGuid().ToString("N"))).FullName;

    private static PhotoHandle Handle(byte[] bytes, string? app = null, int? width = null, int? height = null) =>
        new(app, width, height, bytes.Length, ".jpg", () => Task.FromResult<Stream?>(new MemoryStream(bytes)));

    [Fact]
    public void ChooseAPhotographOpensThePhotoPickerOrTheAppsWhereThereIsNone()
    {
        Assert.Equal(PhotoSource.Photos, PhotoIntake.Opens(PhotoSource.Photos, photoPickerAvailable: true));

        // Section 4 item 3: a phone without Google Play services goes straight to the apps, never to an error.
        Assert.Equal(PhotoSource.OtherApp, PhotoIntake.Opens(PhotoSource.Photos, photoPickerAvailable: false));
        Assert.Equal(PhotoSource.OtherApp, PhotoIntake.Opens(PhotoSource.OtherApp, photoPickerAvailable: true));
        Assert.Equal(PhotoSource.OtherApp, PhotoIntake.Opens(PhotoSource.OtherApp, photoPickerAvailable: false));
    }

    [Fact]
    public void APictureFromAnyAppIsTakenBySharingOpeningOrEditing()
    {
        foreach (string action in (string[])["android.intent.action.SEND", "android.intent.action.SEND_MULTIPLE", "android.intent.action.VIEW", "android.intent.action.EDIT"])
        {
            Assert.True(PhotoIntake.IsIncoming(action, "image/jpeg"), action);
            Assert.True(PhotoIntake.IsIncoming(action, "image/*"), action);
            Assert.False(PhotoIntake.IsIncoming(action, "text/plain"), action);
        }

        Assert.False(PhotoIntake.IsIncoming("android.intent.action.MAIN", "image/jpeg"));
        Assert.False(PhotoIntake.IsIncoming(null, "image/jpeg"));
        Assert.Equal(".heic", PhotoIntake.Extension("image/heic"));
        Assert.Equal(".png", PhotoIntake.Extension("image/png"));
        Assert.Equal(".jpg", PhotoIntake.Extension(null));
    }

    /// <summary>The system's own media store is no app anyone chose; Google's and Samsung's galleries by their makers' names; any other by its label.</summary>
    [Fact]
    public void TheAppAPhotoComesFromIsNamedAsAPersonKnowsIt()
    {
        Assert.Null(PhotoIntake.AppName("media", "com.google.android.providers.media.module", "Media Storage"));
        Assert.Null(PhotoIntake.AppName("com.android.providers.media.photopicker", "com.android.providers.media.module", "Media Storage"));
        Assert.Equal("Google Photos", PhotoIntake.AppName("com.google.android.apps.photos.contentprovider", "com.google.android.apps.photos", "Photos"));
        Assert.Equal("Samsung Gallery", PhotoIntake.AppName("com.sec.android.gallery3d.provider", "com.sec.android.gallery3d", "Gallery"));
        Assert.Equal("Gallery", PhotoIntake.AppName("com.miui.gallery.open", "com.miui.gallery", " Gallery "));
        Assert.Null(PhotoIntake.AppName("com.example.pictures", null, null));
    }

    [Fact]
    public void TheProgressLineSaysWhichAppAndHowFar()
    {
        Assert.Equal("Getting the photo from Google Photos", PhotoIntake.Getting("Google Photos", 0, 1, 0, null));
        Assert.Equal("Getting the photo", PhotoIntake.Getting(null, 0, 1, 0, null));
        Assert.Contains("Getting photo 2 of 3 from Samsung Gallery", PhotoIntake.Getting("Samsung Gallery", 1, 3, 0, null), StringComparison.Ordinal);
        Assert.Matches(@"^Getting the photo from Google Photos, 1[.,]0 of 4[.,]0 MB$", PhotoIntake.Getting("Google Photos", 0, 1, 1048576, 4 * 1048576L));
        Assert.Matches(@"MB so far$", PhotoIntake.Getting("Google Photos", 0, 1, 1048576, null));
    }

    [Fact]
    public void AReducedCopyIsToldApartFromTheWholePhotographEitherWayUp()
    {
        Assert.True(PhotoIntake.Reduced(1024, 768, 4000, 3000));
        Assert.True(PhotoIntake.Reduced(2000, 1500, 3000, 4000));
        Assert.False(PhotoIntake.Reduced(4000, 3000, 4000, 3000));
        Assert.False(PhotoIntake.Reduced(3000, 4000, 4000, 3000));
        Assert.False(PhotoIntake.Reduced(3998, 2999, 4000, 3000));
        Assert.False(PhotoIntake.Reduced(1024, 768, null, null));
        Assert.False(PhotoIntake.Reduced(1024, 768, 0, 0));
    }

    [Fact]
    public async Task AWholePhotographIsReadWithNothingToSay()
    {
        string folder = Folder();
        byte[] bytes = await File.ReadAllBytesAsync(Photo, TestContext.Current.CancellationToken);
        var heard = new List<string>();

        var fetched = await PhotoIntake.Fetch(Handle(bytes, "Google Photos", 4000, 3000), folder, "chosen", 0, 1, heard.Add, CancellationToken.None);

        Assert.False(fetched.Canceled);
        Assert.Null(fetched.Said);
        Assert.NotNull(fetched.Photo);
        Assert.Null(fetched.Photo.Reduced);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(fetched.Photo.Path, TestContext.Current.CancellationToken));
        Assert.Equal("Getting the photo from Google Photos, 0.0 of " + (bytes.Length / 1048576.0).ToString("0.0", System.Globalization.CultureInfo.CurrentCulture) + " MB", heard[0]);
        Assert.True(heard.Count > 1, "a photograph of several megabytes says how far it has got");
    }

    /// <summary>Section 1.4: an app that hands over less than the photograph it states is said to have, by its stated size or by the camera's.</summary>
    [Fact]
    public async Task AReducedCopyIsSaidToBeOne()
    {
        string folder = Folder();
        byte[] small = SmallJpegRecordedAs(4000, 3000);

        var stated = await PhotoIntake.Fetch(Handle(small, "Samsung Gallery", 4000, 3000), folder, "chosen", 0, 1, null, CancellationToken.None);
        Assert.Contains("Samsung Gallery handed over a smaller copy of this photo, 400 by 300 pixels of its 4000 by 3000", stated.Photo!.Reduced, StringComparison.Ordinal);

        // No size stated by the app: the size the camera recorded in the picture is the one compared.
        var recorded = await PhotoIntake.Fetch(Handle(small), folder, "chosen", 0, 1, null, CancellationToken.None);
        Assert.Contains("The app handed over a smaller copy", recorded.Photo!.Reduced, StringComparison.Ordinal);
        Assert.Contains("download it to the phone first", recorded.Photo.Reduced, StringComparison.Ordinal);
    }

    [Fact]
    public async Task APhotographThatCannotBeFetchedSaysWhatToDoAndLeavesNothing()
    {
        string folder = Folder();
        var offline = new PhotoHandle("Google Photos", null, null, null, ".jpg", () => Task.FromResult<Stream?>(new FailingStream()));

        var fetched = await PhotoIntake.Fetch(offline, folder, "chosen", 0, 1, null, CancellationToken.None);

        Assert.Null(fetched.Photo);
        Assert.False(fetched.Canceled);
        Assert.StartsWith("GroupLab could not get the photo from Google Photos. If it is kept only in the cloud, the phone has to be online", fetched.Said, StringComparison.Ordinal);
        Assert.Empty(Directory.EnumerateFiles(folder));

        Assert.StartsWith("The phone is offline, so GroupLab could not get the photo from Google Photos: a photo kept only in the cloud needs the internet.",
            PhotoIntake.CouldNotGet("Google Photos", online: false), StringComparison.Ordinal);
        Assert.Equal(PhotoIntake.CouldNotGet("Google Photos"), PhotoIntake.CouldNotGet("Google Photos", online: true));

        var none = new PhotoHandle(null, null, null, null, ".jpg", () => Task.FromResult<Stream?>(null));
        Assert.StartsWith("GroupLab could not get the photo. ", (await PhotoIntake.Fetch(none, folder, "chosen", 0, 1, null, CancellationToken.None)).Said, StringComparison.Ordinal);
    }

    /// <summary>A download from the cloud that stalls does not keep the person waiting: Cancel returns at once, and what was written goes.</summary>
    [Fact]
    public async Task CancelReturnsAtOnceEvenFromAStalledDownload()
    {
        string folder = Folder();
        var stalled = new StalledStream();
        using var cancel = new CancellationTokenSource();
        var fetching = PhotoIntake.Fetch(new PhotoHandle("Google Photos", null, null, 5_000_000, ".jpg", () => Task.FromResult<Stream?>(stalled)), folder, "chosen", 0, 1, null, cancel.Token);
        await stalled.Reading.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        await cancel.CancelAsync();
        var fetched = await fetching.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.True(fetched.Canceled);
        Assert.Null(fetched.Photo);
        stalled.Release();
        for (int i = 0; i < 100 && Directory.EnumerateFiles(folder).Any(); i++)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        Assert.Empty(Directory.EnumerateFiles(folder));
    }

    /// <summary>
    /// Section 2.1: iOS's photo picker downloads a photograph kept only in iCloud whole before it can be read. The line says how far the
    /// download has got, and Cancel stops the download itself, not only the waiting, so nothing more is fetched.
    /// </summary>
    [Fact]
    public async Task APhotographDownloadedWholeSaysHowFarAndCancelStopsIt()
    {
        Assert.Equal("Getting the photo from Photos", PhotoIntake.Downloading("Photos", 0, 1, 0));
        Assert.Equal("Getting the photo from Photos, 45 percent", PhotoIntake.Downloading("Photos", 0, 1, 0.45));
        Assert.Equal("Getting photo 2 of 2, 100 percent", PhotoIntake.Downloading(null, 1, 2, 1.2));

        string folder = Folder();
        byte[] bytes = await File.ReadAllBytesAsync(Photo, TestContext.Current.CancellationToken);
        var heard = new List<string>();
        var whole = Handle(bytes, "Photos") with
        {
            Download = (done, _) =>
            {
                done(0.5);
                done(1);
                return Task.FromResult<Stream?>(new MemoryStream(bytes));
            },
        };

        var fetched = await PhotoIntake.Fetch(whole, folder, "chosen", 0, 1, heard.Add, CancellationToken.None);

        Assert.NotNull(fetched.Photo);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(fetched.Photo.Path, TestContext.Current.CancellationToken));
        Assert.Contains("Getting the photo from Photos, 50 percent", heard);

        // Cancel during the download: the person is back at once and the download is told to stop.
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancel = new CancellationTokenSource();
        var slow = Handle(bytes, "Photos") with
        {
            Download = async (_, token) =>
            {
                token.Register(() => stopped.TrySetResult());
                started.TrySetResult();
                await Task.Delay(Timeout.Infinite, token);
                return null;
            },
        };
        var fetching = PhotoIntake.Fetch(slow, folder, "chosen2", 0, 1, null, cancel.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await cancel.CancelAsync();

        Assert.True((await fetching.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Canceled);
        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Section 1.3: several pictures shared at once are read one after another as a set, each its own file, and the page the person was on
    /// comes back; a head without pickers of its own (the iOS head until entry 292 section 2) opens nothing without a window.
    /// </summary>
    [AvaloniaFact]
    public async Task SeveralPicturesAreReadAsASet()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        byte[] bytes = await File.ReadAllBytesAsync(Photo, TestContext.Current.CancellationToken);
        var host = new ContentControl { Content = new TextBlock { Text = "before" } };
        var said = new List<string>();

        var read = await PhotoPages.Read(host, [Handle(bytes, "Samsung Gallery"), Handle(bytes, "Samsung Gallery"), Handle(bytes, "Samsung Gallery")], "shared", said.Add);

        Assert.Equal(3, read.Count);
        Assert.Equal(["shared-1.jpg", "shared-2.jpg", "shared-3.jpg"], read.Select(p => Path.GetFileName(p.Path)));
        Assert.Empty(said);
        Assert.Equal("before", (host.Content as TextBlock)?.Text);
        PhotoPages.Forget(read);
        Assert.All(read, p => Assert.False(File.Exists(p.Path)));

        Assert.Empty(await Phone.Platform!.PickPhotos(PhotoSource.Photos, null));
    }

    /// <summary>A 400 by 300 JPEG whose EXIF block says the camera recorded it at <paramref name="width"/> by <paramref name="height"/>.</summary>
    private static byte[] SmallJpegRecordedAs(int width, int height)
    {
        using var image = new Mat(300, 400, MatType.CV_8UC3, Scalar.All(200));
        Cv2.ImEncode(".jpg", image, out byte[] jpeg);
        var tiff = new List<byte>();
        void U16(int v) => tiff.AddRange(BitConverter.GetBytes((ushort)v));
        void U32(uint v) => tiff.AddRange(BitConverter.GetBytes(v));
        void Entry(int tag, int type, uint count, uint value)
        {
            U16(tag);
            U16(type);
            U32(count);
            U32(value);
        }

        tiff.AddRange("II"u8.ToArray());
        U16(42);
        U32(8);
        U16(1);
        Entry(0x8769, 4, 1, 26);
        U32(0);
        U16(2);
        Entry(0xA002, 4, 1, (uint)width);
        Entry(0xA003, 4, 1, (uint)height);
        U32(0);
        byte[] body = [.. "Exif\0\0"u8.ToArray(), .. tiff];
        int length = body.Length + 2;
        return [0xFF, 0xD8, 0xFF, 0xE1, (byte)(length >> 8), (byte)length, .. body, .. jpeg[2..]];
    }

    /// <summary>A stream from a cloud app when the phone is offline.</summary>
    private sealed class FailingStream : MemoryStream
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromException<int>(new IOException("Unable to resolve host"));
    }

    /// <summary>A stream whose read sits until released, as a download from the cloud can, and ignores the cancel it is given.</summary>
    private sealed class StalledStream : MemoryStream
    {
        private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Reading { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Release() => released.TrySetResult();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Reading.TrySetResult();
            await released.Task.ConfigureAwait(false);
            return 0;
        }
    }
}
