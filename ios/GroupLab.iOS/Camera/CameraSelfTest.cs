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
/// picture on the Capture screen must open the files picker in the camera's place without a crash; and a picture the camera gives as HEIC,
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
                var take = Shell.Current!.GetVisualDescendants().OfType<Button>()
                    .FirstOrDefault(b => b.Content is "Take a picture" or TextBlock { Text: "Take a picture" });
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
                await SelfTest.OnUi(() => Presented()?.DismissViewController(false, null));
                await SelfTest.WaitFor(() => Presented() is null, TimeSpan.FromSeconds(10));
            }

            bool alive = await SelfTest.OnUi(() => Shell.Current is not null && Shell.Current.Showing == Shell.Place.Capture);
            check.Passed = pressed && picker && alive;
            check.Detail = !pressed
                ? "the Take a picture button was not found"
                : $"no camera on this simulator; Take a picture {(picker ? "opened the files picker (" + kind + ")" : "did not open the files picker")}, {(taken ? "photographed" : "not photographed")}, and the Capture screen {(alive ? "is still there" : "is gone")}";
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            check.Detail = e.GetType().Name + ": " + e.Message;
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
            if (NativeOpenCv.Linked && written)
            {
                using var read = OpenCvSharp.Cv2.ImRead(path, OpenCvSharp.ImreadModes.Grayscale);
                opencv = !read.Empty() && read.Width == Width && read.Height == Height;
                check.Numbers["opencvWidth"] = read.Width;
            }
            check.Numbers["width"] = width;
            check.Numbers["height"] = height;
            File.Delete(path);
            check.Passed = written && jpeg && width == Width && height == Height && opencv;
            check.Detail = $"a {kind} picture was written {(jpeg ? "as a JPEG" : "not as a JPEG")} of {width} by {height}{(opencv ? "" : ", which OpenCV did not read at its size")}";
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

    /// <summary>The view controller presented over GroupLab's own, which the files picker is.</summary>
    private static UIViewController? Presented()
    {
        var windows = UIApplication.SharedApplication.ConnectedScenes.ToArray().OfType<UIWindowScene>().SelectMany(s => s.Windows).ToArray();
        var root = (windows.FirstOrDefault(w => w.IsKeyWindow) ?? windows.FirstOrDefault())?.RootViewController;
        return root?.PresentedViewController;
    }
}
