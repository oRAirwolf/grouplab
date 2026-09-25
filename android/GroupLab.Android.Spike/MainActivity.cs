using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace GroupLab.Android.Spike;

/// <summary>
/// The one screen. Entry 199 section 1.2: folding, unfolding and turning the device are ordinary events, so the activity keeps running
/// through every one of them and the layout follows the size it is given, rather than the activity being torn down and rebuilt.
/// </summary>
[Activity(
    Label = "GroupLab spike",
    Theme = "@style/GroupLabTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize
        | ConfigChanges.UiMode | ConfigChanges.Density)]
public class MainActivity : AvaloniaMainActivity
{
}

[global::Android.App.Application]
public class SpikeApplication : AvaloniaAndroidApplication<App>
{
    protected SpikeApplication(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }
}
