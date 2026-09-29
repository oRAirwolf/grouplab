using Avalonia.Controls;
using GroupLab.Mobile;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 3: the camera's place on iPhone and iPad until the AVFoundation camera arrives
/// (docs/IOS-PLAN.md section 2). It says so, and offers the files picker, which reads a picture exactly as a chosen one is read.
/// </summary>
internal sealed class LaterCamera : UserControl
{
    public LaterCamera(Action back, Action choose)
    {
        Content = Screens.Page(new StackPanel
        {
            Spacing = 12,
            Children =
            {
                Screens.Title("Take a picture"),
                Screens.Line("The camera on iPhone and iPad arrives in a later build. Until then, photograph the target with the Camera app and choose the picture here; GroupLab reads it exactly as it would read its own."),
                Screens.Primary("Choose a picture", choose),
                Screens.Choice("Back", back),
            },
        });
    }
}
