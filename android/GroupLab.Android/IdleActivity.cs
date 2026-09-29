#if GROUPLAB_DEV
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;

using GroupLab.Mobile;

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 268, GroupLab Dev only: a pure black screen for a device left on overnight for testing. Alan's phone and
/// tablet have OLED screens, whose black pixels are switched off, so nothing can burn in, while the device stays awake and unlocked for adb.
/// Ordinary immersive mode only: no pinning, kiosk or lock task, and Home, Back and Recents all leave it as they leave any application. A tap
/// shows, for a few seconds, one dim line saying what it is and a Close button; Close ends it. Opened over adb by the test extra
/// <c>org.grouplab.test.idle</c> on the main activity.
/// </summary>
[Activity(Theme = "@style/GroupLabTheme", Exported = false, ExcludeFromRecents = true, ScreenOrientation = ScreenOrientation.FullUser,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
public sealed class IdleActivity : Activity
{
    private LinearLayout? words;
    private readonly Handler handler = new(Looper.MainLooper!);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        float density = Resources!.DisplayMetrics!.Density;
        var root = new FrameLayout(this);
        root.SetBackgroundColor(Color.Black);

        var line = new TextView(this) { Text = "GroupLab Dev idle screen, used for overnight testing", TextSize = 14 };
        line.SetTextColor(Color.Rgb(90, 90, 90));
        var close = new Button(this) { Text = "Close" };
        close.SetMinimumHeight((int)(48 * density));
        close.SetMinimumWidth((int)(96 * density));
        close.Click += (_, _) => Finish();
        words = new LinearLayout(this) { Orientation = global::Android.Widget.Orientation.Vertical, Visibility = ViewStates.Gone };
        words.SetGravity(GravityFlags.Center);
        words.AddView(line, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));
        words.AddView(close, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) { TopMargin = (int)(16 * density) });
        root.AddView(words, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent, GravityFlags.Center));
        root.Click += (_, _) => Reveal();
        SetContentView(root);
        Window!.AddFlags(WindowManagerFlags.KeepScreenOn);
        Hide();
    }

    protected override void OnResume()
    {
        base.OnResume();
        Hide();
    }

    /// <summary>The status and navigation bars hidden, as any full-screen application hides them; a swipe from an edge brings them back.</summary>
    private void Hide()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(30) && Window?.InsetsController is { } insets)
        {
            insets.Hide(WindowInsets.Type.SystemBars());
            insets.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
        }
        else if (Window?.DecorView is { } decor)
        {
#pragma warning disable CA1422 // The flags are how Android 10 hides the bars; Android 11 and later take the branch above.
            decor.SystemUiFlags = SystemUiFlags.ImmersiveSticky | SystemUiFlags.Fullscreen | SystemUiFlags.HideNavigation | SystemUiFlags.LayoutStable;
#pragma warning restore CA1422
        }
    }

    /// <summary>A tap shows the line and Close for four seconds, then the screen is black again.</summary>
    private void Reveal()
    {
        if (words is null)
        {
            return;
        }

        words.Visibility = ViewStates.Visible;
        handler.RemoveCallbacksAndMessages(null);
        handler.PostDelayed(() => words.Visibility = ViewStates.Gone, 4000);
    }
}
#endif
