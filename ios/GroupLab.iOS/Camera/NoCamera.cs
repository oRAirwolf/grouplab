using Avalonia.Controls;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;
using Screens = GroupLab.Mobile.Screens;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 5: the camera's place where there is no camera, which is the iOS Simulator. The photos
/// picker opens at once, and reads the chosen picture exactly as a picture from the camera is read; if the picker is closed, this says
/// why and offers it again.
/// </summary>
internal sealed class NoCamera : UserControl
{
    private bool opened;

    public NoCamera(Action back, Action choose)
    {
        Content = Screens.Page(new StackPanel
        {
            Spacing = 12,
            Children =
            {
                Screens.Title("Take a picture"),
                Screens.Line("This device has no camera GroupLab can use, so your photos open instead. Photograph the target with another camera and choose the picture; GroupLab reads it exactly as it would read its own."),
                Screens.Primary("Choose a picture", choose),
                Screens.Choice("Back", back),
            },
        });

        // Posted, not called: the camera's place has to be showing before the picker is asked for, since choosing closes it first.
        AttachedToVisualTree += (_, _) =>
        {
            if (opened)
            {
                return;
            }

            opened = true;
            DiagnosticLog.Info("camera.none", ("fallback", "picker"));
            Dispatcher.UIThread.Post(choose);
        };
    }
}
