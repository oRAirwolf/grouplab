using Foundation;
using GroupLab.App.Diagnostics;
using GroupLab.Mobile;
using PhotosUI;
using UIKit;
using UniformTypeIdentifiers;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 292 sections 2.1 and 2.2: the two pickers behind Choose a photo and From another app on iPhone and
/// iPad. Choose a photo opens the Photos picker, which shows every photograph in the person's library, those kept only in iCloud
/// Photos too, and asks for no permission, because GroupLab sees only what is chosen. From another app opens Files, which reaches iCloud
/// Drive, Google Drive, OneDrive, Dropbox and every other place a Files provider is installed for. Each hands back the chosen photograph
/// unread; the shared <see cref="PhotoIntake.Fetch"/> reads it with the progress line and Cancel.
/// </summary>
internal static class PhotoPickers
{
    /// <summary>What the Photos picker is called in the progress line and in a sentence saying it could not be fetched.</summary>
    internal const string PhotosApp = "Photos";

    /// <summary>The picker open now, kept so it lives until it answers.</summary>
    private static NSObject? answering;

    internal static Task<IReadOnlyList<PhotoHandle>> Pick(PhotoSource source)
    {
        if (IosPhone.Top() is not { } top)
        {
            throw new InvalidOperationException("there is no screen to open the picker from");
        }

        var chosen = new TaskCompletionSource<IReadOnlyList<PhotoHandle>>(TaskCreationOptions.RunContinuationsAsynchronously);
        UIViewController picker = source == PhotoSource.Photos ? PhotosPicker(chosen) : FilesPicker(chosen);
        DiagnosticLog.Info("ios.pick", ("source", source.ToString()), ("picker", picker.GetType().Name));
        top.PresentViewController(picker, true, null);
        return chosen.Task;
    }

    /// <summary>
    /// Section 2.1: the Photos picker, images only, one at a time as on Android. The current representation is asked for, which is the
    /// photograph as it was taken, never a copy made for the screen or converted for compatibility.
    /// </summary>
    private static PHPickerViewController PhotosPicker(TaskCompletionSource<IReadOnlyList<PhotoHandle>> chosen)
    {
        var configuration = new PHPickerConfiguration
        {
            Filter = PHPickerFilter.ImagesFilter,
            SelectionLimit = 1,
            PreferredAssetRepresentationMode = PHPickerConfigurationAssetRepresentationMode.Current,
        };
        var picker = new PHPickerViewController(configuration);
        var listener = new PhotosAnswer(chosen);
        answering = listener;
        picker.Delegate = listener;
        return picker;
    }

    /// <summary>Section 2.2: Files, for any picture, copied to GroupLab by Files itself, which downloads one kept only in the cloud first.</summary>
    private static UIDocumentPickerViewController FilesPicker(TaskCompletionSource<IReadOnlyList<PhotoHandle>> chosen)
    {
        var picker = new UIDocumentPickerViewController([UTTypes.Image], true) { AllowsMultipleSelection = false };
        var listener = new FilesAnswer(chosen);
        answering = listener;
        picker.Delegate = listener;
        return picker;
    }

    /// <summary>
    /// Closes whichever picker is open as its own Cancel does, so whatever waits on it hears that nothing was chosen; the self-test uses
    /// it. False where no picker of GroupLab's is open.
    /// </summary>
    internal static bool CancelOpen()
    {
        var shown = IosPhone.Top();
        switch (shown)
        {
            case PHPickerViewController photos when photos.Delegate is PhotosAnswer answer:
                answer.DidFinishPicking(photos, []);
                return true;
            case UIDocumentPickerViewController files when files.Delegate is FilesAnswer answer:
                answer.WasCancelled(files);
                return true;
            default:
                return false;
        }
    }

    /// <summary>A photograph chosen in Photos, fetched when the screens ask for it: downloaded from iCloud where it is kept only there.</summary>
    internal static PhotoHandle FromProvider(NSItemProvider provider, string? app)
    {
        string type = ImageType(provider);
        Task<Stream?> Download(Action<double> done, CancellationToken cancel) => Load(provider, type, done, cancel);
        return new PhotoHandle(app, null, null, null, PhotoFiles.Ending(type), () => Download(_ => { }, CancellationToken.None))
        {
            Download = Download,
        };
    }

    /// <summary>The first picture kind the provider offers, which is the photograph's own; "public.image" where none is named.</summary>
    private static string ImageType(NSItemProvider provider) =>
        provider.RegisteredTypeIdentifiers.FirstOrDefault(id => UTType.CreateFromIdentifier(id)?.ConformsTo(UTTypes.Image) == true) ?? "public.image";

    /// <summary>
    /// The photograph's own file, from the item provider: iOS downloads it from iCloud first where it has to, and says how far it has got
    /// through the provider's progress, which Cancel stops. The file iOS hands over lasts only while it is being handed over, so it is
    /// copied into the cache there and then, and made readable as a JPEG where it is HEIC.
    /// </summary>
    private static Task<Stream?> Load(NSItemProvider provider, string type, Action<double> done, CancellationToken cancel)
    {
        var loaded = new TaskCompletionSource<Stream?>(TaskCreationOptions.RunContinuationsAsynchronously);
        NSProgress? progress = null;
        progress = provider.LoadFileRepresentation(type, (url, error) =>
        {
            if (error is not null || url?.Path is not { } from)
            {
                loaded.TrySetException(Failed(error, cancel));
                return;
            }

            try
            {
                string copy = Path.Combine(IosPhone.Caches, "picked-" + Guid.NewGuid().ToString("N") + Path.GetExtension(from));
                File.Copy(from, copy, overwrite: true);
                loaded.TrySetResult(PhotoFiles.OpenOnce(copy));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                loaded.TrySetException(e);
            }
        });

        if (progress is not null)
        {
            var watching = progress.AddObserver("fractionCompleted", NSKeyValueObservingOptions.New, _ => done(progress.FractionCompleted));
            var stopping = cancel.Register(() => progress.Cancel());
            _ = loaded.Task.ContinueWith(_ =>
            {
                watching.Dispose();
                stopping.Dispose();
            }, TaskScheduler.Default);
        }

        return loaded.Task;
    }

    /// <summary>
    /// Why the provider gave nothing, as the shared reading expects it: canceled where the person canceled, else an input error, which
    /// the reading turns into the sentence about being offline or kept only in the cloud.
    /// </summary>
    private static Exception Failed(NSError? error, CancellationToken cancel)
    {
        if (cancel.IsCancellationRequested || (error?.Domain == "NSCocoaErrorDomain" && error.Code == (int)NSCocoaError.UserCancelled))
        {
            return new OperationCanceledException(cancel);
        }

        DiagnosticLog.Info("ios.pick.load", ("domain", error?.Domain ?? "none"), ("code", error?.Code ?? 0));
        return new IOException("the photograph could not be loaded");
    }

    /// <summary>The Photos picker's answer: the one photograph chosen, or nothing where the person canceled.</summary>
    private sealed class PhotosAnswer(TaskCompletionSource<IReadOnlyList<PhotoHandle>> chosen) : PHPickerViewControllerDelegate
    {
        public override void DidFinishPicking(PHPickerViewController picker, PHPickerResult[] results)
        {
            picker.DismissViewController(true, null);
            answering = null;
            chosen.TrySetResult([.. results.Select(r => FromProvider(r.ItemProvider, PhotosApp))]);
        }
    }

    /// <summary>Files' answer: the picture Files copied for GroupLab, or nothing where the person canceled.</summary>
    private sealed class FilesAnswer(TaskCompletionSource<IReadOnlyList<PhotoHandle>> chosen) : UIDocumentPickerDelegate
    {
        public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl[] urls)
        {
            answering = null;
            chosen.TrySetResult([.. urls.Select(u => u.Path).OfType<string>().Where(File.Exists).Select(FromFile)]);
        }

        public override void WasCancelled(UIDocumentPickerViewController controller)
        {
            // Files closes itself when the person cancels; the self-test's Cancel is the one that has to close it.
            if (controller.PresentingViewController is not null && !controller.IsBeingDismissed)
            {
                controller.DismissViewController(true, null);
            }

            answering = null;
            chosen.TrySetResult([]);
        }
    }

    /// <summary>A picture already in GroupLab's own files, as Files and "Open in GroupLab" leave one; read once and then deleted.</summary>
    internal static PhotoHandle FromFile(string path) =>
        new(null, null, null, new FileInfo(path).Length, PhotoFiles.Ending(Path.GetExtension(path)), () => Task.FromResult<Stream?>(PhotoFiles.OpenOnce(path)));
}
