using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using AndroidX.Camera.View;

namespace GroupLab.Android.Spike;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A2: the capture screen, spike grade. CameraX's preview fills it, hosted as a native Android view
/// inside the Avalonia screen; over it, the one instruction of docs/MOBILE-CAPTURE.md item C3 and the frame's timing; beneath it, the lens
/// by zoom, a manual shutter as the override item C1 allows, and the way back.
/// </summary>
public sealed class CaptureView : UserControl
{
    private readonly TextBlock say = new() { FontSize = 26, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock timing = new() { FontSize = 13, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock result = new() { FontSize = 13, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap };
    private CameraSession? session;

    public CaptureView(string targets, Action back)
    {
        var host = new CameraHost(targets, s =>
        {
            session = s;
            s.Judged += (verdict, time) => Dispatcher.UIThread.Post(() => { say.Text = verdict.Words; timing.Text = time; });
            s.Taken += line => Dispatcher.UIThread.Post(() => { result.Text = line; SpikeView.Note(line); });
        });

        Avalonia.Controls.Button Button(string words, Action act)
        {
            var b = new Avalonia.Controls.Button { Content = words, MinHeight = 48, MinWidth = 64, Margin = new Thickness(4) };
            b.Click += (_, _) => act();
            return b;
        }

        var controls = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                Button("0.6x", () => session?.Zoom(0.6f)),
                Button("1x", () => session?.Zoom(1f)),
                Button("3x", () => session?.Zoom(3f)),
                Button("Take", () => session?.Take("manual")),
                Button("Back", back),
            },
        };
        var top = new StackPanel { Margin = new Thickness(16), Spacing = 4, Background = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)), Children = { say, timing, result } };
        var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };
        Grid.SetRow(host, 0);
        Grid.SetRowSpan(host, 3);
        Grid.SetRow(top, 0);
        Grid.SetRow(controls, 2);
        grid.Children.Add(host);
        grid.Children.Add(top);
        grid.Children.Add(controls);
        Content = grid;
    }

    /// <summary>CameraX's PreviewView inside the Avalonia screen; a tap on it focuses and meters there.</summary>
    private sealed class CameraHost(string targets, Action<CameraSession> started) : NativeControlHost
    {
        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            var activity = MainActivity.Current!;
            var preview = new PreviewView(activity);
            var session = new CameraSession(activity, activity, preview, targets);
            preview.Touch += (_, e) =>
            {
                if (e.Event?.Action == global::Android.Views.MotionEventActions.Up)
                {
                    session.FocusAt(e.Event.GetX(), e.Event.GetY());
                }
            };
            started(session);
            session.Start();
            return new global::Avalonia.Android.AndroidViewControlHandle(preview);
        }
    }
}
