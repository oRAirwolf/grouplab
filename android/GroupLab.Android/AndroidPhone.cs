using Android.Content;
using Avalonia.Controls;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Gltd.Model;
using GroupLab.Mobile;
using OpenCvSharp;

namespace GroupLab.Android;

/// <summary>
/// What the shared phone screens need from Android, NOTES-FROM-PLANNING.md entry 290 section 2 item 1. Each member is what the screens did
/// themselves before they moved into the shared mobile project, unchanged, so Android behaves exactly as it did.
/// </summary>
internal sealed class AndroidPhone : IPhonePlatform
{
    private static Context Context => global::Android.App.Application.Context;

    /// <summary>The file provider's authority, from the manifest's <c>${applicationId}</c>, so nothing names the package.</summary>
    internal static string Authority => Context.PackageName + ".files";

    public string FilesFolder => Context.FilesDir!.AbsolutePath;

    public string CacheFolder => Context.CacheDir!.AbsolutePath;

    public string DeviceWords => "GroupLab " + AppInfo.Version + " on " + global::Android.OS.Build.Manufacturer + " " + global::Android.OS.Build.Model;

    /// <summary>The phone as the survey describes it: Android's own version and the model, as docs/SURVEY.md section 2 lists them.</summary>
    public GroupLab.Core.Survey.MachineFacts Machine() =>
        GroupLab.Core.Survey.SurveyReport.ThisMachine(device: global::Android.OS.Build.Manufacturer + " " + global::Android.OS.Build.Model)
            with { OperatingSystem = "Android " + global::Android.OS.Build.VERSION.Release };

    public void CopyBundledTargets(string into)
    {
        foreach (string assets in (string[])["targets", "targets/frozen"])
        {
            string folder = Path.Combine(into, assets);
            Directory.CreateDirectory(folder);
            foreach (string name in (Context.Assets!.List(assets) ?? []).Where(n => n.EndsWith(".gltd.json", StringComparison.Ordinal)))
            {
                using var from = Context.Assets.Open($"{assets}/{name}");
                using var file = File.Create(Path.Combine(folder, name));
                from.CopyTo(file);
            }
        }
    }

    /// <remarks>
    /// Entry 239: the picture is decoded by Android at a power of two fraction of its size (<see cref="GroupLab.Core.Imaging.WorkingSize.SampleFor"/>),
    /// so a 600 dpi scan of 32 megapixels is 8 from the start and never exists in memory at full size. Like the OpenCV decode it replaces, it
    /// leaves the orientation flag to the marking, which reads it.
    /// </remarks>
    public (Mat Colour, int Width, int Height, int Sample)? DecodeReduced(string photo, double mostMegapixels)
    {
        var bounds = new global::Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
        global::Android.Graphics.BitmapFactory.DecodeFile(photo, bounds);
        if (bounds.OutWidth <= 0 || bounds.OutHeight <= 0)
        {
            return null;
        }

        int sample = GroupLab.Core.Imaging.WorkingSize.SampleFor(bounds.OutWidth, bounds.OutHeight, mostMegapixels);
        var colour = new Mat();
        using (var bitmap = global::Android.Graphics.BitmapFactory.DecodeFile(photo, new global::Android.Graphics.BitmapFactory.Options { InSampleSize = sample, InPreferredConfig = global::Android.Graphics.Bitmap.Config.Argb8888, InScaled = false }))
        {
            if (bitmap is null)
            {
                colour.Dispose();
                return null;
            }

            IntPtr pixels = bitmap.LockPixels();
            try
            {
                using var rgba = Mat.FromPixelData(bitmap.Height, bitmap.Width, MatType.CV_8UC4, pixels, bitmap.RowBytes);
                Cv2.CvtColor(rgba, colour, ColorConversionCodes.RGBA2BGR);
            }
            finally
            {
                bitmap.UnlockPixels();
                bitmap.Recycle();
            }
        }

        return (colour, bounds.OutWidth, bounds.OutHeight, sample);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 240: the memory this analysis may use, from what the device says now: its total and available memory and
    /// its low memory threshold (<c>ActivityManager.getMemoryInfo</c>), the floor when it says memory is low. Logged with the device's memory
    /// classes, so a report says what the phone allowed.
    /// </summary>
    public double MemoryBudgetMegabytes()
    {
        var activity = (global::Android.App.ActivityManager?)Context.GetSystemService(Context.ActivityService);
        if (activity is null)
        {
            return GroupLab.Core.Imaging.MemoryBudget.FloorMegabytes;
        }

        var info = new global::Android.App.ActivityManager.MemoryInfo();
        activity.GetMemoryInfo(info);
        const double Mb = 1024.0 * 1024;
        double budget = GroupLab.Core.Imaging.MemoryBudget.Phone(info.TotalMem / Mb, info.AvailMem / Mb, info.Threshold / Mb, info.LowMemory);
        DiagnosticLog.Info("memory.budget", ("totalMb", (long)(info.TotalMem / Mb)), ("availableMb", (long)(info.AvailMem / Mb)), ("low", info.LowMemory),
            ("class", activity.MemoryClass), ("largeClass", activity.LargeMemoryClass), ("budgetMb", (long)budget));
        return budget;
    }

    public bool CameraAllowed() => MainActivity.CameraAllowed();

    public Control Camera(Action<string, bool> taken, Action back, Action choose, Action? result = null) => new CameraView(taken, back, choose, result);

    public bool IsCamera(object? content) => content is CameraView;

    public bool CameraOpen => CameraSession.Active is not null;

    public string? ShareFile(string path, string mimeType, string title)
    {
        if (MainActivity.Current is not { } activity)
        {
            return "The share sheet is not available.";
        }

        var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(activity, Authority, new Java.IO.File(path));
        var send = new Intent(Intent.ActionSend);
        send.SetType(mimeType);
        send.PutExtra(Intent.ExtraStream, uri);
        send.AddFlags(ActivityFlags.GrantReadUriPermission);
        activity.StartActivity(Intent.CreateChooser(send, title));
        return null;
    }

    public string? PrintPdf(byte[] pdf, string name, PageSize paper) => PdfOut.Print(pdf, name, paper);

    public string? SharePdf(byte[] pdf, string name) => PdfOut.Share(pdf, name);

    /// <summary>
    /// Entry 258: Paste a picture. Android lets an application read the clipboard only while it is in front, which it is when the button is
    /// pressed; the clipboard holds its content address; the picture is copied into <paramref name="folder"/>.
    /// </summary>
    public async Task<string?> PastePicture(string folder)
    {
        var clipboard = Context.GetSystemService(Context.ClipboardService) as ClipboardManager;
        var uri = clipboard?.PrimaryClip is { ItemCount: > 0 } clip ? clip.GetItemAt(0)?.Uri : null;
        string? type = uri is null ? null : Context.ContentResolver?.GetType(uri);
        if (uri is null || type?.StartsWith("image/", StringComparison.Ordinal) != true || Context.ContentResolver?.OpenInputStream(uri) is not { } from)
        {
            return null;
        }

        string copy = Path.Combine(folder, "pasted" + (type == "image/png" ? ".png" : ".jpg"));
        await using (from)
        await using (var to = File.Create(copy))
        {
            await from.CopyToAsync(to);
        }

        return copy;
    }

    public Control? UpdateCard() =>
#if GROUPLAB_UPDATER
        Updates.UpdateCard.Build();
#else
        null;
#endif

    public bool IsDevBuild =>
#if GROUPLAB_DEV
        true;
#else
        false;
#endif
}
