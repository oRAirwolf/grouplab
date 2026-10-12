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
    private static readonly double[] Sp = [0, 8, 10, 12, 14, 18, 20, 24, 30, 100];
    private static readonly double[] Dp = [0, 16, 20, 24, 26, 30, 34, 36, 38, 100];

    /// <summary><paramref name="sp"/> at Android's largest text size, in dp.</summary>
    public static double Largest(double sp)
    {
        if (sp >= Sp[^1])
        {
            return sp;
        }

        int i = 1;
        while (Sp[i] < sp)
        {
            i++;
        }

        return Dp[i - 1] + ((sp - Sp[i - 1]) / (Sp[i] - Sp[i - 1]) * (Dp[i] - Dp[i - 1]));
    }

    /// <summary>Follows Android's largest text size until <paramref name="window"/> closes, then the sizes as they are again.</summary>
    public static void LargestUntilClosed(Window window)
    {
        TextSize.Follow(Largest);
        window.Closed += (_, _) => TextSize.Follow(size => size);
    }
}
