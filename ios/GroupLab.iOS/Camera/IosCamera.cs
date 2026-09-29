using AVFoundation;
using Avalonia.Controls;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 5: what <see cref="IosPhone"/> answers for the camera. The back wide camera where there
/// is one; where there is none, which is the iOS Simulator, the files picker in its place (<see cref="NoCamera"/>).
/// </summary>
internal static class IosCamera
{
    /// <summary>The back wide camera, or any camera where there is no back one; null on the simulator.</summary>
    internal static AVCaptureDevice? Device() =>
        AVCaptureDevice.GetDefaultDevice(AVCaptureDeviceType.BuiltInWideAngleCamera, AVMediaTypes.Video, AVCaptureDevicePosition.Back)
        ?? AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);

    /// <summary>
    /// Whether the camera may be used; the first time, iOS asks, and the person presses again once they have answered. With no camera there
    /// is nothing to ask for, and the picker takes its place.
    /// </summary>
    internal static bool Allowed()
    {
        if (Device() is null)
        {
            return true;
        }

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

    internal static Control Screen(Action<string, bool> taken, Action back, Action choose, Action? result) =>
        Device() is { } device ? new CameraView(device, taken, back, choose, result) : new NoCamera(back, choose);

    internal static bool IsScreen(object? content) => content is CameraView or NoCamera;

    /// <summary>Whether the camera is running now, or let go for the background and to be taken again.</summary>
    internal static bool Open => CameraSession.Active is not null;
}
