using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace GroupLab.App.Theme;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 411 section 2, Alan: "For android, follow the system". Every text size the styles set, and the few set in
/// code on screens the phone shows, passes through here. On the desktop and the iPhone nothing is followed and a size is itself; the Android
/// head follows the phone's own text size setting, through Android's own conversion, so the larger sizes grow less than the smaller ones as
/// they do in Android's own apps (Android 14 and later).
/// </summary>
public static class TextSize
{
    private static Func<double, double> follow = size => size;

    /// <summary>Raised after <see cref="Follow"/> changes the sizes, so the styles are built again at the new ones.</summary>
    public static event Action? Changed;

    /// <summary><paramref name="size"/>, a size from <see cref="Tokens"/>, at the system's text size where one is followed.</summary>
    public static double Of(double size) => Math.Round(follow(size), 2);

    /// <summary>Follows <paramref name="system"/>, which gives a size at the system's text size, and builds the styles again.</summary>
    public static void Follow(Func<double, double> system)
    {
        ArgumentNullException.ThrowIfNull(system);
        follow = system;
        Changed?.Invoke();
    }

    /// <summary>A style's setter value: a text size at the followed size, anything else as it is.</summary>
    public static object Scaled(AvaloniaProperty property, object value) =>
        value is double size && (property == TextBlock.FontSizeProperty || property == TemplatedControl.FontSizeProperty) ? Of(size) : value;
}
