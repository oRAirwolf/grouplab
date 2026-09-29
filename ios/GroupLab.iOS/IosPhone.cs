using System.Runtime.InteropServices;
using AVFoundation;
using Foundation;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Gltd.Model;
using GroupLab.Mobile;
using ImageIO;
using OpenCvSharp;
using UIKit;

namespace GroupLab.iOS;

/// <summary>
/// What the shared phone screens need from iOS, NOTES-FROM-PLANNING.md entry 290 section 2 item 3: the application's folders, the sheets
/// in its bundle, a reduced decode through OpenCV, the memory budget, the camera's permission, and UIKit's share sheet, print sheet and
/// pasteboard. The camera itself comes in a later build; until then its place offers the files picker.
/// </summary>
internal sealed class IosPhone : IPhonePlatform
{
    /// <summary>The application's Documents folder, where the sessions, the database, the settings and the log live.</summary>
    internal static string Documents { get; } = Folder(NSSearchPathDirectory.DocumentDirectory);

    /// <summary>Library/Caches, which iOS may empty when the device is short of space.</summary>
    internal static string Caches { get; } = Folder(NSSearchPathDirectory.CachesDirectory);

    private static string Folder(NSSearchPathDirectory which)
    {
        string? path = NSFileManager.DefaultManager.GetUrls(which, NSSearchPathDomain.User).FirstOrDefault()?.Path;
        path ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), which == NSSearchPathDirectory.CachesDirectory ? "Caches" : "Documents");
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>The device's own region, entry 232, since invariant globalization hides it from .NET.</summary>
    internal static string? Region()
    {
        // "en_US" or "en_US@calendar=gregorian": the part after the underscore is the region.
        string identifier = NSLocale.CurrentLocale.LocaleIdentifier ?? "";
        int under = identifier.IndexOf('_', StringComparison.Ordinal);
        string region = under < 0 ? "" : new string(identifier[(under + 1)..].TakeWhile(char.IsLetter).ToArray());
        return region.Length == 2 ? region.ToUpperInvariant() : null;
    }

    public string FilesFolder => Documents;

    public string CacheFolder => Caches;

    public string DeviceWords => "GroupLab " + AppInfo.Version + " on " + Model();

    /// <summary>The device as the survey describes it: iOS's or iPadOS's own version and the model identifier, as docs/SURVEY.md lists them.</summary>
    public GroupLab.Core.Survey.MachineFacts Machine() =>
        GroupLab.Core.Survey.SurveyReport.ThisMachine(device: Model())
            with { OperatingSystem = UIDevice.CurrentDevice.SystemName + " " + UIDevice.CurrentDevice.SystemVersion };

    /// <summary>"iPhone17,1" on a device; on the simulator, the device it stands in for.</summary>
    private static string Model()
    {
        if (Environment.GetEnvironmentVariable("SIMULATOR_MODEL_IDENTIFIER") is { Length: > 0 } simulated)
        {
            return "Simulator " + simulated;
        }

        return MachineName() ?? UIDevice.CurrentDevice.Model;
    }

    private static string? MachineName()
    {
        nuint size = 0;
        if (SystemControl("hw.machine", IntPtr.Zero, ref size, IntPtr.Zero, 0) != 0 || size == 0)
        {
            return null;
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            return SystemControl("hw.machine", buffer, ref size, IntPtr.Zero, 0) == 0 ? Marshal.PtrToStringAnsi(buffer) : null;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [DllImport("/usr/lib/libSystem.dylib", EntryPoint = "sysctlbyname")]
    private static extern int SystemControl([MarshalAs(UnmanagedType.LPStr)] string name, IntPtr value, ref nuint size, IntPtr newValue, nuint newSize);

    /// <summary>The sheets the application carries, from its bundle's targets and targets/frozen folders.</summary>
    public void CopyBundledTargets(string into)
    {
        string bundle = NSBundle.MainBundle.ResourcePath ?? AppContext.BaseDirectory;
        foreach (string folder in (string[])["targets", Path.Combine("targets", "frozen")])
        {
            string to = Path.Combine(into, folder);
            Directory.CreateDirectory(to);
            string from = Path.Combine(bundle, folder);
            if (!Directory.Exists(from))
            {
                DiagnosticLog.Info("ios.targets", ("missing", folder));
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(from, "*.gltd.json"))
            {
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), overwrite: true);
            }
        }
    }

    /// <remarks>
    /// Entry 239, as on Android: the picture's size is read first from its header by ImageIO, which decodes nothing, then OpenCV decodes it
    /// at the power of two fraction <see cref="GroupLab.Core.Imaging.WorkingSize.SampleFor"/> chooses (IMREAD_REDUCED_COLOR_2, 4 or 8), so a
    /// 600 dpi scan never exists in memory at full size. Like the desktop's decode, it leaves the orientation flag to the marking, which reads it.
    /// </remarks>
    public (Mat Colour, int Width, int Height, int Sample)? DecodeReduced(string photo, double mostMegapixels)
    {
        var (width, height) = HeaderSize(photo);
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        int sample = Math.Min(8, GroupLab.Core.Imaging.WorkingSize.SampleFor(width, height, mostMegapixels));
        var mode = sample switch
        {
            8 => ImreadModes.ReducedColor8,
            4 => ImreadModes.ReducedColor4,
            2 => ImreadModes.ReducedColor2,
            _ => ImreadModes.Color,
        };
        var colour = Cv2.ImRead(photo, mode | ImreadModes.IgnoreOrientation);
        if (colour.Empty())
        {
            colour.Dispose();
            return null;
        }

        return (colour, width, height, sample);
    }

    /// <summary>The picture's width and height from its header, through ImageIO; zero where it is not an image.</summary>
    internal static (int Width, int Height) HeaderSize(string photo)
    {
        using var url = NSUrl.FromFilename(photo);
        using var source = CGImageSource.FromUrl(url);
        if (source is null || source.ImageCount < 1)
        {
            return (0, 0);
        }

        var properties = source.GetProperties(0, null);
        return (properties?.PixelWidth ?? 0, properties?.PixelHeight ?? 0);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 240 on iOS: the device's memory from NSProcessInfo, and what this application may still use before iOS
    /// would end it, from os_proc_available_memory. iOS gives no low memory threshold. The simulator says nothing about the second, so half the
    /// Mac's memory stands for it there; the budget is capped at a tenth of the whole either way.
    /// </summary>
    public double MemoryBudgetMegabytes()
    {
        const double Mb = 1024.0 * 1024;
        double total = NSProcessInfo.ProcessInfo.PhysicalMemory / Mb;
        double available = 0;
        try
        {
            available = AvailableMemory() / Mb;
        }
        catch (EntryPointNotFoundException)
        {
        }

        bool said = available > 0;
        if (!said)
        {
            available = total / 2;
        }

        double budget = GroupLab.Core.Imaging.MemoryBudget.Phone(total, available, 0, false);
        DiagnosticLog.Info("memory.budget", ("totalMb", (long)total), ("availableMb", (long)available), ("availableSaid", said), ("budgetMb", (long)budget));
        return budget;
    }

    [DllImport("/usr/lib/libSystem.dylib", EntryPoint = "os_proc_available_memory")]
    private static extern nuint AvailableMemory();

    /// <summary>Whether the camera may be used; the first time, iOS asks, and the person presses again once they have answered.</summary>
    public bool CameraAllowed()
    {
        var status = AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Video);
        if (status == AVAuthorizationStatus.Authorized)
        {
            return true;
        }

        if (status == AVAuthorizationStatus.NotDetermined)
        {
            _ = AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video);
        }

        return false;
    }

    public Avalonia.Controls.Control Camera(Action<string, bool> taken, Action back, Action choose, Action? result = null) => new LaterCamera(back, choose);

    public bool IsCamera(object? content) => content is LaterCamera;

    public bool CameraOpen => false;

    public string? ShareFile(string path, string mimeType, string title)
    {
        if (Top() is not { } top)
        {
            return "The share sheet is not available.";
        }

        var share = new UIActivityViewController([NSUrl.FromFilename(path)], null);
        Anchor(share, top);
        top.PresentViewController(share, true, () => { });
        return null;
    }

    public string? SharePdf(byte[] pdf, string name)
    {
        string file = Path.Combine(Caches, Safe(name) + ".pdf");
        File.WriteAllBytes(file, pdf);
        return ShareFile(file, "application/pdf", name);
    }

    /// <summary>The iOS print sheet, with the PDF as it is; the paper is chosen there, and GroupLab's own scale check measures what came out.</summary>
    public string? PrintPdf(byte[] pdf, string name, PageSize paper)
    {
        if (!UIPrintInteractionController.PrintingAvailable)
        {
            return "Printing is not available on this device.";
        }

        if (Top() is not { } top || top.View is not { } view)
        {
            return "The print sheet is not available.";
        }

        var printer = UIPrintInteractionController.SharedPrintController;
        var info = UIPrintInfo.PrintInfo;
        info.OutputType = UIPrintInfoOutputType.General;
        info.JobName = name;
        printer.PrintInfo = info;
        printer.PrintingItem = NSData.FromArray(pdf);
        DiagnosticLog.Info("ios.print", ("paper", paper.ToString()));
        if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad)
        {
            printer.PresentFromRect(Middle(view), view, true, (_, _, _) => { });
        }
        else
        {
            printer.Present(true, (_, _, _) => { });
        }

        return null;
    }

    /// <summary>
    /// Entry 258 on iOS: a picture copied in another application, from the pasteboard as the bytes it was copied as where it says what they
    /// are, and as a JPEG made from the image where it does not. iOS asks the person the first time an application pastes.
    /// </summary>
    public Task<string?> PastePicture(string folder)
    {
        var board = UIPasteboard.General;
        foreach (var (type, extension) in (ReadOnlySpan<(string, string)>)[("public.jpeg", ".jpg"), ("public.png", ".png"), ("public.heic", ".heic")])
        {
            if (board.DataForPasteboardType(type) is { Length: > 0 } data)
            {
                string copy = Path.Combine(folder, "pasted" + extension);
                File.WriteAllBytes(copy, data.ToArray());
                return Task.FromResult<string?>(copy);
            }
        }

        if (board.HasImages && board.Image?.AsJPEG(0.95f) is { } jpeg)
        {
            string copy = Path.Combine(folder, "pasted.jpg");
            File.WriteAllBytes(copy, jpeg.ToArray());
            return Task.FromResult<string?>(copy);
        }

        return Task.FromResult<string?>(null);
    }

    /// <summary>iOS updates GroupLab through TestFlight and the App Store, so there is no updater card.</summary>
    public Avalonia.Controls.Control? UpdateCard() => null;

    public bool IsDevBuild => false;

    /// <summary>The view controller in front, which a sheet is presented from.</summary>
    private static UIViewController? Top()
    {
        var window = UIApplication.SharedApplication.ConnectedScenes.ToArray().OfType<UIWindowScene>()
            .SelectMany(s => s.Windows).FirstOrDefault(w => w.IsKeyWindow)
            ?? UIApplication.SharedApplication.ConnectedScenes.ToArray().OfType<UIWindowScene>().SelectMany(s => s.Windows).FirstOrDefault();
        var top = window?.RootViewController;
        while (top?.PresentedViewController is { } presented)
        {
            top = presented;
        }

        return top;
    }

    /// <summary>On an iPad a share sheet is a popover and has to point at something: the middle of the screen, with no arrow.</summary>
    private static void Anchor(UIViewController sheet, UIViewController from)
    {
        if (sheet.PopoverPresentationController is { } popover && from.View is { } view)
        {
            popover.SourceView = view;
            popover.SourceRect = Middle(view);
            popover.PermittedArrowDirections = 0;
        }
    }

    private static CoreGraphics.CGRect Middle(UIView view) => new(view.Bounds.GetMidX(), view.Bounds.GetMidY(), 1, 1);

    private static string Safe(string name) => string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) || c == ' ' ? '-' : c));
}
