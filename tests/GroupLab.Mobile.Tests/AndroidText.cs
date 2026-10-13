using Avalonia.Controls;
using GroupLab.App.Theme;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 411 section 2: the phone's text at Android's largest text size, as the emulator sweep sets it (`font_scale
/// 2.0` on API 34). From Android 14 a size in sp is converted by a table rather than doubled, so the larger sizes grow less; these are the
/// table's points for 2.0 in AOSP's FontScaleConverterFactory, sp to dp, joined by straight lines as Android joins them.
/// </summary>
internal static class AndroidText
{
    /// <summary><paramref name="sp"/> at Android's largest text size, in dp (<see cref="TextSize.AndroidLargest"/>, entry 415).</summary>
    public static double Largest(double sp) => TextSize.AndroidLargest(sp);

    /// <summary>Follows Android's largest text size until <paramref name="window"/> closes, then the sizes as they are again.</summary>
    public static void LargestUntilClosed(Window window)
    {
        TextSize.Follow(Largest);
        window.Closed += (_, _) => TextSize.Follow(size => size);
    }
}
