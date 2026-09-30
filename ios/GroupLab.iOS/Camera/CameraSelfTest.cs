using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CoreGraphics;
using Foundation;
using GroupLab.Mobile;
using ImageIO;
using UIKit;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 5: what the simulator can prove of the camera, which has no camera. Pressing Take a
/// picture on the Capture screen must open the Photos picker in the camera's place without a crash; and a picture the camera gives as HEIC,
/// or as anything but a JPEG, must come out as a JPEG that ImageIO and OpenCV both read at its own size. The rest is for a device
/// (docs/IOS-PLAN.md, "The first TestFlight sitting").
/// </summary>
internal static class CameraSelfTest
{
    /// <summary>The Capture screen's Take a picture pressed: on the simulator the picker opens, is photographed, and is closed again.</summary>
    internal static async Task<SelfTestCheck> Fallback(int n)
    {
        var check = new SelfTestCheck("camera fallback");
        try
        {
            bool camera = IosCamera.Device() is not null;
            check.Numbers["cameraDevice"] = camera ? 1 : 0;
            await SelfTest.OnUi(() => Shell.Current!.Show(Shell.Place.Capture));
            await Task.Delay(TimeSpan.FromSeconds(2));
            bool pressed = await SelfTest.OnUi(() =>
            {
                // Take a picture on the tab's start, or the Camera button above a result, which opens the same camera.
                var buttons = Shell.Current!.GetVisualDescendants().OfType<Button>().ToList();
                var take = buttons.FirstOrDefault(b => b.Content is TextBlock { Text: "Take a picture" })
                    ?? buttons.FirstOrDefault(b => b.Content is TextBlock { Text: "Camera" });
                take?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                return take is not null;
            });
            check.Numbers["pressed"] = pressed ? 1 : 0;
            if (camera)
            {
                // A simulator with a camera (none has one yet): the camera screen itself must show.
                bool shown = await SelfTest.WaitFor(() => Shell.Current!.GetVisualDescendants().OfType<CameraView>().Any(), TimeSpan.FromSeconds(20));
                check.Passed = pressed && shown;
                check.Detail = $"this simulator has a camera; the camera screen {(shown ? "opened" : "did not open")}";
                return check;
            }

            bool picker = await SelfTest.WaitFor(() => Presented() is not null, TimeSpan.FromSeconds(20));
            string? kind = await SelfTest.OnUi(() => Presented()?.GetType().Name);
            check.Numbers["pickerShown"] = picker ? 1 : 0;
            bool taken = picker && await SelfTest.Photographed($"{n:00}-camera-fallback");
            if (picker)
            {
                // Closed as its own Cancel closes it, so the Capture screen hears that nothing was chosen (entry 292).
                await SelfTest.OnUi(() =>
                {
                    if (!PhotoPickers.CancelOpen())
                    {
                        Presented()?.DismissViewController(false, null);
                    }
                });
                await SelfTest.WaitFor(() => Presented() is null, TimeSpan.FromSeconds(10));
            }

            bool alive = await SelfTest.OnUi(() => Shell.Current is not null && Shell.Current.Showing == Shell.Place.Capture);
            check.Passed = pressed && picker && alive;
            check.Detail = !pressed
                ? "the Take a picture button was not found"
                : $"no camera on this simulator; Take a picture {(picker ? "opened the picker (" + kind + ")" : "did not open the picker")}, {(taken ? "photographed" : "not photographed")}, and the Capture screen {(alive ? "is still there" : "is gone")}";
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            check.Detail = e.GetType().Name + ": " + e.Message;
        }

        return check;
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 313 section 2: the camera screen laid out over the whole window, with a preview that has no camera
    /// behind it, since the simulator has none. The panel must sit under the status bar and above the camera, the camera between the panel
    /// and the shutter, and nothing beside the quality bar before there is a score. Photographed, then taken away.
    /// </summary>
    internal static async Task<SelfTestCheck> Layout(int n)
    {
        var check = new SelfTestCheck("camera layout");
        CaptureScreen? screen = null;
        try
        {
            var geometry = await SelfTest.OnUi(() =>
            {
                var host = IosPhone.Top()?.View ?? throw new InvalidOperationException("no view to lay the camera screen over");
                screen = new CaptureScreen(new AVFoundation.AVCaptureVideoPreviewLayer(new AVFoundation.AVCaptureSession())) { Frame = host.Bounds };
                host.AddSubview(screen);
                screen.SetNeedsLayout();
                screen.LayoutIfNeeded();

                // Entry 311 section 2: the level as the iPad lying flat on a table gives it, Core Motion's gravity straight down, which
                // must show green, crosshair and all, in the photograph.
                var (x, y, z) = GroupLab.Core.Capture.PhoneCamera.LevelFromGravity(0, 0, -1, GroupLab.Core.Capture.ScreenTurn.Upright);
                screen.ShowLevel(new GroupLab.Core.Capture.CameraLevel().Felt(x, y, z, 0));
                return screen.Geometry();
            });
            bool levelGreen = await SelfTest.OnUi(() => screen?.LevelReady == true);
            check.Numbers["levelGreenFlat"] = levelGreen ? 1 : 0;
            await Task.Delay(TimeSpan.FromSeconds(1));
            bool taken = await SelfTest.Photographed($"{n:00}-camera-layout");
            var (panel, camera, shutter, safeTop, score) = geometry;
            check.Numbers["panelTop"] = Math.Round((double)panel.Top, 1);
            check.Numbers["panelBottom"] = Math.Round((double)panel.Bottom, 1);
            check.Numbers["cameraTop"] = Math.Round((double)camera.Top, 1);
            check.Numbers["cameraBottom"] = Math.Round((double)camera.Bottom, 1);
            check.Numbers["shutterTop"] = Math.Round((double)shutter.Top, 1);
            check.Numbers["safeTop"] = Math.Round(safeTop, 1);
            bool underStatus = panel.Top >= safeTop;
            bool panelAbove = panel.Bottom <= camera.Top;
            bool shutterBelow = camera.Bottom <= shutter.Top && camera.Height > 0;
            bool noDash = score.Length == 0;
            check.Passed = underStatus && panelAbove && shutterBelow && noDash && levelGreen;
            check.Detail = $"panel {(int)panel.Top} to {(int)panel.Bottom} under a safe area of {(int)safeTop}, camera {(int)camera.Top} to {(int)camera.Bottom}, shutter from {(int)shutter.Top}; "
                + (panelAbove ? "the panel is above the camera" : "the panel overlaps the camera")
                + (noDash ? ", nothing beside the bar" : $", \"{score}\" beside the bar")
                + (levelGreen ? ", the level green lying flat" : ", the level not green lying flat")
                + (taken ? "; photographed" : "; not photographed");
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            check.Detail = e.GetType().Name + ": " + e.Message;
        }
        finally
        {
            await SelfTest.OnUi(() => screen?.RemoveFromSuperview());
        }

        return check;
    }

    /// <summary>
    /// A picture that is not a JPEG, HEIC where the simulator can write HEIC and PNG where not, written through the camera's own path: it
    /// must come out a JPEG at its own size, which OpenCV reads where it is linked.
    /// </summary>
    internal static async Task<SelfTestCheck> Jpeg() => await SelfTest.OnUi(() =>
    {
        var check = new SelfTestCheck("camera picture as JPEG");
        try
        {
            const int Width = 400, Height = 300;
            using var renderer = new UIGraphicsImageRenderer(new CGSize(Width, Height), new UIGraphicsImageRendererFormat { Scale = 1 });
            using var image = renderer.CreateImage(_ =>
            {
                UIColor.White.SetFill();
                UIBezierPath.FromRect(new CGRect(0, 0, Width, Height)).Fill();
                UIColor.Black.SetFill();
                UIBezierPath.FromOval(new CGRect(150, 100, 100, 100)).Fill();
            });
            var (data, kind) = Heic(image.CGImage!) is { } heic ? (heic, "HEIC") : (image.AsPNG()!, "PNG");
            check.Numbers["heic"] = kind == "HEIC" ? 1 : 0;
            string path = Path.Combine(IosPhone.Caches, "selftest-still.jpg");
            bool written = StillFile.WriteJpeg(data, path);
            byte[] start = written ? File.ReadAllBytes(path).Take(3).ToArray() : [];
            var (width, height) = written ? IosPhone.HeaderSize(path) : (0, 0);
            bool jpeg = StillFile.IsJpeg(start);
            bool opencv = true;
            string openCvWords = "";
            if (NativeOpenCv.Linked && written)
            {
                try
                {
                    using var read = OpenCvSharp.Cv2.ImRead(path, OpenCvSharp.ImreadModes.Grayscale);
                    opencv = !read.Empty() && read.Width == Width && read.Height == Height;
                    check.Numbers["opencvWidth"] = read.Width;
                    openCvWords = opencv ? ", which OpenCV read at its size" : ", which OpenCV did not read at its size";
                }
                catch (TypeInitializationException)
                {
                    // OpenCV itself would not start, which the imaging checks report; the JPEG is judged by ImageIO alone here.
                    openCvWords = ", not read by OpenCV, which would not start";
                }
            }
            check.Numbers["width"] = width;
            check.Numbers["height"] = height;
            File.Delete(path);
            check.Passed = written && jpeg && width == Width && height == Height && opencv;
            check.Detail = $"a {kind} picture was written {(jpeg ? "as a JPEG" : "not as a JPEG")} of {width} by {height}{openCvWords}";
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            check.Detail = e.GetType().Name + ": " + e.Message;
        }

        return check;
    });

    /// <summary>The image as HEIC, where this device or simulator can write it; null where it cannot.</summary>
    private static NSData? Heic(CGImage image)
    {
        var data = new NSMutableData();
        using var destination = CGImageDestination.Create(data, "public.heic", 1, null);
        if (destination is null)
        {
            return null;
        }

        destination.AddImage(image, (CGImageDestinationOptions?)null);
        return destination.Close() && data.Length > 0 ? data : null;
    }

    /// <summary>The view controller presented over GroupLab's own, which a picker is.</summary>
    private static UIViewController? Presented()
    {
        var windows = UIApplication.SharedApplication.ConnectedScenes.ToArray().OfType<UIWindowScene>().SelectMany(s => s.Windows).ToArray();
        var root = (windows.FirstOrDefault(w => w.IsKeyWindow) ?? windows.FirstOrDefault())?.RootViewController;
        return root?.PresentedViewController;
    }
}
