using Avalonia;

namespace GroupLab.App;

/// <summary>What a scroll does to the sheet.</summary>
internal enum WheelAction
{
    Zoom,
    Pan,
}

/// <summary>
/// What scrolling and pinching do to the sheet, NOTES-FROM-PLANNING.md entry 166 section 3, which resolved the contradiction in entry 163
/// section 1: on a Mac trackpad a two finger drag is a scroll, so "a drag always pans" and "a scroll always zooms" could not both hold.
/// <list type="bullet">
/// <item>Command, or Control on Windows and Linux, with a scroll zooms everywhere. A Windows precision touchpad's pinch arrives this way, as
/// the Control key with a wheel, because Windows turns a pinch into that for any application that does not ask for the gesture itself.</item>
/// <item>On a Mac a plain scroll pans, from a trackpad or a mouse, which is what every Mac image viewer does; the trackpad's pinch
/// arrives as its own magnify gesture and zooms.</item>
/// <item>On Windows and Linux a plain scroll zooms when it comes in whole wheel notches, as it always has, and pans when it does not,
/// which is how a touchpad's two finger drag arrives. Avalonia gives no pointer type for a scroll, so the size of the step is the only
/// thing that tells a wheel from a touchpad; a free-spinning or high resolution wheel will pan, and Control with it still zooms.</item>
/// <item>A touch screen's two finger pinch zooms about the point between the fingers.</item>
/// </list>
/// Every scroll and pinch is written to the verbose log with what it arrived as and what it did, so a tester can show what their own
/// hardware delivers.
/// </summary>
internal static class SheetGestures
{
    /// <summary>Screen pixels a unit of scroll moves the sheet.</summary>
    public const double PixelsPerUnit = 50;

    public static WheelAction ForWheel(Vector delta, bool command, bool mac) =>
        command ? WheelAction.Zoom : mac ? WheelAction.Pan : IsWholeNotch(delta) ? WheelAction.Zoom : WheelAction.Pan;

    /// <summary>A wheel's notch arrives as a whole unit on one axis; a touchpad's movement arrives in fractions.</summary>
    public static bool IsWholeNotch(Vector delta) => delta.X == 0 && delta.Y != 0 && Math.Abs(delta.Y - Math.Round(delta.Y)) < 1e-9;

    /// <summary>A notch zooms by 1.2, as it always has, and a fraction of one by that fraction, so a touchpad zooms smoothly.</summary>
    public static double ZoomFactor(double deltaY) => Math.Pow(1.2, Math.Clamp(deltaY, -3, 3));

    /// <summary>A Mac trackpad's magnify step is a fraction of the current size, positive as the fingers spread.</summary>
    public static double MagnifyFactor(double magnification) => Math.Exp(Math.Clamp(magnification, -0.5, 0.5));
}
