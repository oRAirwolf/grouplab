using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A4: the first place along the bottom. Take a picture with the capture screen, or choose one
/// already on the phone; either is analyzed at the working size and the result shown, and saved in Sessions.
/// </summary>
public sealed class CapturePage : UserControl
{
    private readonly TextBlock status = Screens.Line("");
    private readonly Control start;

    public CapturePage()
    {
        start = Screens.Page(new StackPanel
        {
            Spacing = 12,
            Children =
            {
                Screens.Heading("Capture"),
                Screens.Line("Photograph a GroupLab target and GroupLab finds the holes and measures the group. Hold the phone square over the sheet; the words at the top of the camera say what to change, and it takes the picture itself when everything is right."),
                Screens.Choice("Take a picture", Camera),
                Screens.Choice("Choose a photograph", () => _ = Choose()),
                status,
            },
        });
        Content = start;
    }

    private void Camera()
    {
        if (!MainActivity.CameraAllowed())
        {
            status.Text = "GroupLab needs the camera to take the picture. Allow it, then press Take a picture again.";
            return;
        }

        Content = new CameraView(path => _ = Analyze(path, deleteAfter: true), () => Content = start);
    }

    private async Task Choose()
    {
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { } storage)
        {
            return;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a photograph of a target",
            AllowMultiple = false,
            FileTypeFilter = [FilePickerFileTypes.ImageAll],
        });
        if (files.Count == 0)
        {
            return;
        }

        // The picker hands a content address, not a path; the picture is copied into the cache, analyzed, and the copy deleted.
        string copy = Path.Combine(global::Android.App.Application.Context.CacheDir!.AbsolutePath, "chosen" + Path.GetExtension(files[0].Name));
        await using (var from = await files[0].OpenReadAsync())
        await using (var to = File.Create(copy))
        {
            await from.CopyToAsync(to);
        }

        await Analyze(copy, deleteAfter: true);
    }

    private async Task Analyze(string photo, bool deleteAfter)
    {
        Content = Screens.Words("Reading the sheet", "Finding the sheet's markers and the holes. This takes a few seconds.");
        var units = App.Settings.LoadUnits();
        PhoneResult result;
        try
        {
            result = await Task.Run(() => PhoneAnalysis.Run(photo, null, units, App.Survey, CancellationToken.None));
        }
        catch (Exception e) when (e is IOException or InvalidOperationException or OpenCvSharp.OpenCVException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "phone.detect", e);
            result = new PhoneResult(MarkingState.Empty, null, "The picture could not be analyzed: " + e.Message, null);
        }
        finally
        {
            if (deleteAfter)
            {
                File.Delete(photo);
            }
        }

        Dispatcher.UIThread.Post(() => Content = new ResultView(result, units, () => Content = start));
    }
}
