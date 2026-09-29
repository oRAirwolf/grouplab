using AVFoundation;
using Avalonia.Controls;
using Avalonia.iOS;
using Avalonia.Platform;
using Avalonia.Threading;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 5: the capture screen on iPhone and iPad, Capture B as on Android. Everything on it is
/// <see cref="CaptureScreen"/>, UIKit's own views around AVFoundation's preview, hosted in the Avalonia screen as one native view, the way
/// the Android head hosts its CaptureScreen. In Guided mode the shutter fires by itself after <see cref="GroupLab.Core.Capture.PhoneCamera.ReadyFrames"/>
/// ready frames in a row and can be pressed sooner; in Manual it fires only when pressed. The mode and the torch are remembered in the
/// same settings as on Android. The still goes to the application's cache and is handed on to be analyzed and checked.
/// </summary>
internal sealed class CameraView : UserControl
{
    /// <param name="device">The back camera.</param>
    /// <param name="taken">The still's path and whether the torch was on when it was taken.</param>
    /// <param name="back">Leave the camera.</param>
    /// <param name="pick">Leave the camera for a photograph already on the device.</param>
    /// <param name="result">Back to the last result, where there is one (entry 281 section 1.3).</param>
    public CameraView(AVCaptureDevice device, Action<string, bool> taken, Action back, Action pick, Action? result = null)
    {
        Content = new CameraHost(device, taken, back, pick, result);
    }

    /// <summary>The native screen inside the Avalonia one, filling it.</summary>
    private sealed class CameraHost(AVCaptureDevice device, Action<string, bool> taken, Action back, Action pick, Action? result) : NativeControlHost
    {
        private CameraSession? session;

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            var capture = new AVCaptureSession();
            var screen = new CaptureScreen(new AVCaptureVideoPreviewLayer(capture));
            session = new CameraSession(device, capture, screen);
            session.Taken += (path, torch) => Dispatcher.UIThread.Post(() => taken(path, torch));
            screen.BackPressed += back;
            screen.PickerPressed += pick;
            screen.ShowResultButton(result is not null);
            screen.ResultPressed += () => result?.Invoke();
            session.Start();
            return new UIViewControlHandle(screen);
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            session?.Close();
            session = null;
            base.DestroyNativeControlCore(control);
        }
    }
}
