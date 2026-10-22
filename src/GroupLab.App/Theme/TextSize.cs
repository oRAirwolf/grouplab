using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace GroupLab.App.Theme;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 411 section 2, Alan: "For android, follow the system". Every text size the styles set, and the few set in
/// code on screens the phone shows, passes through here. On the desktop and the iPhone nothing is followed and a size is itself; the Android
/// head follows the phone's own text size setting, through Android's own conversion, so the larger sizes grow less than the smaller ones as
/// they do in Android's own apps (Android 14 and later). Entry 415 section 1, Alan: the iPhone follows its own text size setting too
/// (Dynamic Type), through <see cref="ByFactor"/>, never larger at any size than Android's largest (<see cref="AndroidLargest"/>).
/// </summary>
public static class TextSize
{
    private static Func<double, double> follow = size => size;

    // Each application's styles, built again when the size followed changes. Held by a weak table, not an event: the headless tests start
    // an application for every test, and a static event held all of them, so the desktop suite ran the Linux runner out of memory.
    private static readonly ConditionalWeakTable<Application, List<Action<Application>>> restyles = new();

    /// <summary><paramref name="size"/>, a size from <see cref="Tokens"/>, at the system's text size where one is followed.</summary>
    public static double Of(double size) => Math.Round(follow(size), 2);

    /// <summary>Builds <paramref name="app"/>'s styles again with <paramref name="restyle"/> whenever <see cref="Follow"/> changes the sizes.</summary>
    public static void RestyleOnChange(Application app, Action<Application> restyle)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(restyle);
        restyles.GetOrCreateValue(app).Add(restyle);
    }

    /// <summary>Entry 468 section 2.6: how much larger the computer's text is at each choice; Normal is the type scale itself.</summary>
    public static double Factor(TextSizeChoice choice) => choice switch
    {
        TextSizeChoice.Larger => 1.15,
        TextSizeChoice.Largest => 1.3,
        _ => 1.0,
    };

    /// <summary>Entry 468 section 2.6: the computer's text at <paramref name="choice"/>, every size from the tokens scaled alike.</summary>
    public static void Choose(TextSizeChoice choice)
    {
        double factor = Factor(choice);
        Follow(size => size * factor);
    }

    /// <summary>Follows <paramref name="system"/>, which gives a size at the system's text size, and builds the styles again.</summary>
    public static void Follow(Func<double, double> system)
    {
        ArgumentNullException.ThrowIfNull(system);
        follow = system;
        foreach (var (app, actions) in restyles.ToList())
        {
            foreach (var restyle in actions.ToList())
            {
                restyle(app);
            }
        }
    }

    // Android's largest text size, `font_scale 2.0`, as Android 14 and later convert it: the points of AOSP's FontScaleConverterFactory
    // table for 2.0, sp to dp, joined by straight lines as Android joins them. Above the last point a size is itself.
    private static readonly double[] LargestSp = [0, 8, 10, 12, 14, 18, 20, 24, 30, 100];
    private static readonly double[] LargestDp = [0, 16, 20, 24, 26, 30, 34, 36, 38, 100];

    /// <summary><paramref name="size"/> at Android's largest text size: the largest any screen is held to by the sweeps.</summary>
    public static double AndroidLargest(double size)
    {
        if (size >= LargestSp[^1])
        {
            return size;
        }

        int i = 1;
        while (LargestSp[i] < size)
        {
            i++;
        }

        return LargestDp[i - 1] + ((size - LargestSp[i - 1]) / (LargestSp[i] - LargestSp[i - 1]) * (LargestDp[i] - LargestDp[i - 1]));
    }

    /// <summary>
    /// Entry 415 section 1: a size grown by <paramref name="factor"/>, as iOS grows text by one factor for every size, held at Android's
    /// largest for that size, so a screen the sweeps hold at Android's largest holds at the iPhone's too. iOS's own largest, AX5, is about
    /// 3.1 times; Android's largest is 2 times for small text and less for large, and that is the cap. Below it nothing is held.
    /// </summary>
    public static Func<double, double> ByFactor(double factor) =>
        size => Math.Min(size * (factor > 0 && double.IsFinite(factor) ? factor : 1), AndroidLargest(size));

    /// <summary>A style's setter value: a text size at the followed size, anything else as it is.</summary>
    public static object Scaled(AvaloniaProperty property, object value) =>
        value is double size && (property == TextBlock.FontSizeProperty || property == TemplatedControl.FontSizeProperty) ? Of(size) : value;
}
